using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Pots;
using LowRollers.Api.Domain.StateMachine;
using LowRollers.Api.Features.GameEngine.Showdown;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LowRollers.Api.Tests.Features.GameEngine.Showdown;

public class ShowdownHandlerTests
{
    private readonly IHandEvaluationService _evaluationService = new HandEvaluationService();
    private readonly IPotManager _potManager = new PotManager();
    private readonly IHandEventStore _eventStore;
    private readonly ILogger<ShowdownHandler> _logger;
    private readonly ShowdownHandler _handler;

    public ShowdownHandlerTests()
    {
        _eventStore = Substitute.For<IHandEventStore>();
        _eventStore.GetLastSequenceNumberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _eventStore.AppendAsync(Arg.Any<IHandEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _logger = Substitute.For<ILogger<ShowdownHandler>>();

        _handler = new ShowdownHandler(
            _evaluationService,
            _potManager,
            _eventStore,
            _logger);
    }

    #region Helper Methods

    private static Card C(Rank rank, Suit suit) => new(suit, rank);

    private static Table CreateTableWithPlayers(int playerCount, decimal potAmount = 100m)
    {
        var table = new Table
        {
            Id = Guid.NewGuid(),
            Name = "Test Table",
            SmallBlind = 1m,
            BigBlind = 2m,
            ButtonPosition = 1
        };

        for (int i = 0; i < playerCount; i++)
        {
            var player = Player.Create(
                Guid.NewGuid(),
                $"Player{i + 1}",
                i + 1,
                1000m);
            player.Status = PlayerStatus.Active;
            table.Players[player.Id] = player;
        }

        return table;
    }

    private static Hand CreateShowdownHand(Table table, decimal potAmount = 100m)
    {
        var playerIds = table.Players.Keys.ToList();
        var hand = Hand.Create(
            table.Id,
            1,
            1,
            2,
            3,
            1m,
            2m,
            playerIds);

        hand.Phase = HandPhase.Showdown;
        hand.Pots[0].Amount = potAmount;
        foreach (var playerId in playerIds)
        {
            hand.Pots[0].AddEligiblePlayer(playerId);
        }

        // Add community cards
        hand.CommunityCards.AddRange([
            C(Rank.Two, Suit.Clubs),
            C(Rank.Five, Suit.Diamonds),
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Jack, Suit.Spades),
            C(Rank.King, Suit.Clubs)
        ]);

        table.CurrentHand = hand;
        return hand;
    }

    #endregion

    #region ExecuteShowdownAsync Tests

    [Fact]
    public async Task ExecuteShowdownAsync_NoActiveHand_ReturnsFailure()
    {
        var table = CreateTableWithPlayers(2);
        table.CurrentHand = null;

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.False(result.IsSuccess);
        Assert.Contains("No active hand", result.Error);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_IncompleteCommunityCards_ReturnsFailure()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);
        hand.CommunityCards.RemoveAt(4); // Remove one card

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.False(result.IsSuccess);
        Assert.Contains("Community cards not fully dealt", result.Error);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_SinglePlayerRemaining_AwardsWithoutShowdown()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        // Set hole cards for players
        var players = table.Players.Values.ToList();
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[0].Status = PlayerStatus.Active;
        players[1].Status = PlayerStatus.Folded; // One player folded

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);
        Assert.Single(result.PlayerResults);
        Assert.Contains(players[0].Id, result.TotalWinnings.Keys);
        Assert.Equal(100m, result.TotalWinnings[players[0].Id]);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_TwoPlayers_CorrectlyDeterminesWinner()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        // Player 1: Pair of Aces (better hand)
        var playerWithAces = players[0];
        playerWithAces.HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        // Player 2: Pair of Sixes (worse hand - no conflicts with board)
        var playerWithSixes = players[1];
        playerWithSixes.HoleCards = [C(Rank.Six, Suit.Hearts), C(Rank.Six, Suit.Spades)];

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.PlayerResults.Count);

        // Player with Aces should win
        var winner = result.PlayerResults.First(r => r.AmountWon > 0);
        Assert.Equal(playerWithAces.Id, winner.PlayerId);
        Assert.Equal(100m, winner.AmountWon);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_SplitPot_DividesEvenly()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        // Both players have same hand (pair of Kings from board)
        players[0].HoleCards = [C(Rank.Three, Suit.Hearts), C(Rank.Four, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.Three, Suit.Spades), C(Rank.Four, Suit.Clubs)];

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);

        // Both players should win (tie)
        Assert.True(result.TotalWinnings.ContainsKey(players[0].Id));
        Assert.True(result.TotalWinnings.ContainsKey(players[1].Id));
        Assert.Equal(50m, result.TotalWinnings[players[0].Id]);
        Assert.Equal(50m, result.TotalWinnings[players[1].Id]);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_WithOddChips_AwardsRemainderToFirstFromButton()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table, 101m); // Odd pot

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        // Both players have same hand
        players[0].HoleCards = [C(Rank.Three, Suit.Hearts), C(Rank.Four, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.Three, Suit.Spades), C(Rank.Four, Suit.Clubs)];

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);

        // First player from button gets the extra cent
        var winnings = result.TotalWinnings;
        Assert.True(winnings.Values.Sum() == 101m);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_MultiplePots_AwardsEachCorrectly()
    {
        var table = CreateTableWithPlayers(3);
        var hand = CreateShowdownHand(table, 60m);

        // Add a side pot
        var sidePot = Pot.CreateSidePot([table.Players.Keys.First()], 1);
        sidePot.Amount = 40m;
        sidePot.AddEligiblePlayer(table.Players.Keys.Skip(1).First());
        hand.Pots.Add(sidePot);

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        // Player 1: Royal Flush (best)
        players[0].HoleCards = [C(Rank.Ace, Suit.Clubs), C(Rank.Queen, Suit.Clubs)];
        // Player 2: Pair
        players[1].HoleCards = [C(Rank.Five, Suit.Hearts), C(Rank.Five, Suit.Clubs)];
        // Player 3: High card
        players[2].HoleCards = [C(Rank.Nine, Suit.Hearts), C(Rank.Ten, Suit.Spades)];

        // Change community cards to include clubs for flush
        hand.CommunityCards.Clear();
        hand.CommunityCards.AddRange([
            C(Rank.Ten, Suit.Clubs),
            C(Rank.Jack, Suit.Clubs),
            C(Rank.King, Suit.Clubs),
            C(Rank.Two, Suit.Hearts),
            C(Rank.Three, Suit.Diamonds)
        ]);

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.PotAwards.Count);
    }

    #endregion

    #region GetShowOrder Tests

    [Fact]
    public void GetShowOrder_WithLastAggressor_AggressorShowsFirst()
    {
        var table = CreateTableWithPlayers(3);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        hand.LastAggressorId = players[1].Id; // Middle player was last aggressor

        // Set hole cards for all players
        foreach (var player in players)
        {
            player.HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts)];
        }

        var showOrder = _handler.GetShowOrder(table);

        Assert.Equal(3, showOrder.Count);
        Assert.Equal(players[1].Id, showOrder[0]); // Last aggressor first
    }

    [Fact]
    public void GetShowOrder_NoAggressor_FirstToActShowsFirst()
    {
        var table = CreateTableWithPlayers(3);
        var hand = CreateShowdownHand(table);
        hand.LastAggressorId = null; // No aggressor (all checked)

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        foreach (var player in players)
        {
            player.HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts)];
        }

        var showOrder = _handler.GetShowOrder(table);

        Assert.Equal(3, showOrder.Count);
        // First to act is left of button (seat 2)
        Assert.Equal(players[1].Id, showOrder[0]);
    }

    [Fact]
    public void GetShowOrder_NoHand_ReturnsEmptyList()
    {
        var table = CreateTableWithPlayers(2);
        table.CurrentHand = null;

        var showOrder = _handler.GetShowOrder(table);

        Assert.Empty(showOrder);
    }

    #endregion

    #region RequestMuckAsync Tests

    [Fact]
    public async Task RequestMuckAsync_ValidPlayer_ReturnsTrue()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.Two, Suit.Hearts), C(Rank.Three, Suit.Spades)];

        var result = await _handler.RequestMuckAsync(table, players[1].Id);

        Assert.True(result);
    }

    [Fact]
    public async Task RequestMuckAsync_NoHand_ReturnsFalse()
    {
        var table = CreateTableWithPlayers(2);
        table.CurrentHand = null;

        var result = await _handler.RequestMuckAsync(table, Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RequestMuckAsync_PlayerNotInHand_ReturnsFalse()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        players[0].Status = PlayerStatus.Folded; // Not in hand

        var result = await _handler.RequestMuckAsync(table, players[0].Id);

        Assert.False(result);
    }

    [Fact]
    public async Task RequestMuckAsync_NonexistentPlayer_ReturnsFalse()
    {
        var table = CreateTableWithPlayers(2);
        CreateShowdownHand(table);

        var result = await _handler.RequestMuckAsync(table, Guid.NewGuid());

        Assert.False(result);
    }

    #endregion

    #region Auto-Muck Tests

    [Fact]
    public async Task ExecuteShowdownAsync_InferiorHand_AutoMucks()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);
        hand.LastAggressorId = table.Players.Keys.First(); // First player shows first

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        // Player 1: Royal Flush (shows first as aggressor)
        players[0].HoleCards = [C(Rank.Ace, Suit.Clubs), C(Rank.Queen, Suit.Clubs)];
        // Player 2: Low pair (should auto-muck)
        players[1].HoleCards = [C(Rank.Two, Suit.Hearts), C(Rank.Two, Suit.Spades)];

        // Modify community for flush
        hand.CommunityCards.Clear();
        hand.CommunityCards.AddRange([
            C(Rank.Ten, Suit.Clubs),
            C(Rank.Jack, Suit.Clubs),
            C(Rank.King, Suit.Clubs),
            C(Rank.Three, Suit.Hearts),
            C(Rank.Four, Suit.Diamonds)
        ]);

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);

        var player2Result = result.PlayerResults.First(r => r.PlayerId == players[1].Id);
        Assert.False(player2Result.Showed);
        Assert.True(player2Result.AutoMucked);
    }

    [Fact]
    public async Task ExecuteShowdownAsync_RequestedMuck_HonorsRequest()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);
        hand.LastAggressorId = table.Players.Keys.First();

        var players = table.Players.Values.OrderBy(p => p.SeatPosition).ToList();
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.Two, Suit.Hearts), C(Rank.Three, Suit.Spades)];

        // Request muck before showdown
        await _handler.RequestMuckAsync(table, players[1].Id);

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);

        var player2Result = result.PlayerResults.First(r => r.PlayerId == players[1].Id);
        Assert.False(player2Result.Showed);
    }

    #endregion

    #region Event Recording Tests

    [Fact]
    public async Task ExecuteShowdownAsync_RecordsShowEvents()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.King, Suit.Hearts), C(Rank.King, Suit.Diamonds)];

        await _handler.ExecuteShowdownAsync(table);

        // Verify events were recorded
        await _eventStore.Received().AppendAsync(
            Arg.Any<PlayerShowedCardsEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteShowdownAsync_RecordsPotAwardedEvents()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.King, Suit.Hearts), C(Rank.King, Suit.Diamonds)];

        await _handler.ExecuteShowdownAsync(table);

        await _eventStore.Received().AppendAsync(
            Arg.Any<PotAwardedEvent>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Hand Description Tests

    [Fact]
    public async Task ExecuteShowdownAsync_PotAward_IncludesHandDescription()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        // Player 1: Pair of Aces
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.Three, Suit.Hearts), C(Rank.Four, Suit.Spades)];

        var result = await _handler.ExecuteShowdownAsync(table);

        Assert.True(result.IsSuccess);
        Assert.Single(result.PotAwards);

        var award = result.PotAwards[0];
        Assert.False(string.IsNullOrEmpty(award.WinningHandDescription));
    }

    [Fact]
    public async Task ExecuteShowdownAsync_WinnerResult_IncludesEvaluatedHand()
    {
        var table = CreateTableWithPlayers(2);
        var hand = CreateShowdownHand(table);

        var players = table.Players.Values.ToList();
        players[0].HoleCards = [C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds)];
        players[1].HoleCards = [C(Rank.Three, Suit.Hearts), C(Rank.Four, Suit.Spades)];

        var result = await _handler.ExecuteShowdownAsync(table);

        var winnerResult = result.PlayerResults.First(r => r.AmountWon > 0);
        Assert.NotNull(winnerResult.EvaluatedHand);
        Assert.Equal(HandCategory.Pair, winnerResult.EvaluatedHand.Value.Category);
    }

    #endregion
}

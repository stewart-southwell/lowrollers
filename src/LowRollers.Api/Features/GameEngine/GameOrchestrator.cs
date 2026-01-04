using System.Collections.Concurrent;
using LowRollers.Api.Domain.Betting;
using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Pots;
using LowRollers.Api.Domain.Services;
using LowRollers.Api.Domain.StateMachine;
using LowRollers.Api.Features.GameEngine.Showdown;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Features.GameEngine;

/// <summary>
/// Thin orchestrator that coordinates poker hand flow by delegating to domain services.
/// Maintains only the minimal state needed: deck and betting round per active hand.
/// </summary>
public sealed class GameOrchestrator : IGameOrchestrator
{
    private readonly IShuffleService _shuffleService;
    private readonly IPotManager _potManager;
    private readonly IHandEventStore _eventStore;
    private readonly HandStateMachine _stateMachine;
    private readonly IShowdownHandler _showdownHandler;
    private readonly ActionValidator _actionValidator;
    private readonly ILogger<GameOrchestrator> _logger;

    // Minimal state: deck and betting round per active hand
    private readonly ConcurrentDictionary<Guid, Deck> _decks = new();
    private readonly ConcurrentDictionary<Guid, BettingRound> _bettingRounds = new();

    public GameOrchestrator(
        IShuffleService shuffleService,
        IPotManager potManager,
        IHandEventStore eventStore,
        HandStateMachine stateMachine,
        IShowdownHandler showdownHandler,
        ILogger<GameOrchestrator> logger)
    {
        _shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
        _potManager = potManager ?? throw new ArgumentNullException(nameof(potManager));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _showdownHandler = showdownHandler ?? throw new ArgumentNullException(nameof(showdownHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionValidator = new ActionValidator();
    }

    /// <inheritdoc/>
    public async Task<HandStartResult> StartNewHandAsync(Table table, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(table);

        var activePlayers = GetActivePlayers(table);
        if (activePlayers.Count < 2)
        {
            return HandStartResult.Failure("Need at least 2 active players to start a hand.");
        }

        // Rotate button and create hand
        table.ButtonPosition = table.GetNextButtonPosition();
        table.HandCount++;

        var (sbPos, bbPos) = CalculateBlindPositions(activePlayers, table.ButtonPosition);
        var hand = Hand.Create(
            table.Id, table.HandCount, table.ButtonPosition,
            sbPos, bbPos, table.SmallBlind, table.BigBlind,
            activePlayers.Select(p => p.Id));

        table.CurrentHand = hand;

        // Prepare deck
        var deck = new Deck(_shuffleService);
        deck.Shuffle();
        _decks[hand.Id] = deck;

        // Reset players for new hand
        foreach (var player in activePlayers)
        {
            player.ResetForNewHand();
            player.Status = PlayerStatus.Active;
        }

        // Post blinds
        var sbPlayer = activePlayers.First(p => p.SeatPosition == sbPos);
        var bbPlayer = activePlayers.First(p => p.SeatPosition == bbPos);
        PostBlinds(hand, table, sbPlayer, bbPlayer);

        // Create betting round with blinds posted
        _bettingRounds[hand.Id] = BettingRound.CreatePreflop(
            table.SmallBlind, table.BigBlind, sbPlayer.Id, bbPlayer.Id);

        // Deal hole cards
        var holeCards = DealHoleCards(deck, activePlayers);

        // Transition to preflop
        await _stateMachine.TransitionAsync(hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        // Set first to act (UTG = left of BB)
        hand.CurrentPlayerId = GetFirstToAct(activePlayers, bbPos, isPreflop: true);

        // Record events
        await RecordHandStartAsync(hand, holeCards, sbPlayer, bbPlayer, ct);

        _logger.LogInformation(
            "Hand {HandNumber} started. Button: {Button}, SB: {SB}, BB: {BB}",
            hand.HandNumber, table.ButtonPosition, sbPos, bbPos);

        return HandStartResult.Success(hand, holeCards);
    }

    /// <inheritdoc/>
    public async Task<HandStartResult> StartBombPotAsync(
        Table table, decimal anteAmount, bool isDoubleBoard = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(table);

        var activePlayers = table.SeatedPlayers
            .Where(p => p.Status != PlayerStatus.Away && p.ChipStack >= anteAmount)
            .OrderBy(p => p.SeatPosition)
            .ToList();

        if (activePlayers.Count < 2)
        {
            return HandStartResult.Failure("Need at least 2 players with enough chips for ante.");
        }

        // Bomb pots don't rotate the button - it stays with current dealer
        // Button will rotate on the next regular hand
        table.HandCount++;

        var (sbPos, bbPos) = CalculateBlindPositions(activePlayers, table.ButtonPosition);
        var hand = Hand.CreateBombPot(
            table.Id, table.HandCount, table.ButtonPosition,
            sbPos, bbPos, table.SmallBlind, table.BigBlind,
            anteAmount, activePlayers.Select(p => p.Id), isDoubleBoard);

        table.CurrentHand = hand;

        var deck = new Deck(_shuffleService);
        deck.Shuffle();
        _decks[hand.Id] = deck;

        // Collect antes and reset players
        foreach (var player in activePlayers)
        {
            player.ResetForNewHand();
            player.Status = PlayerStatus.Active;
            player.ChipStack -= anteAmount;
            player.TotalBetThisHand = anteAmount;
        }
        hand.Pots[0].Amount = anteAmount * activePlayers.Count;

        // Deal hole cards
        var holeCards = DealHoleCards(deck, activePlayers);

        // Skip preflop - deal flop immediately
        await _stateMachine.TransitionAsync(hand, HandPhase.Preflop, TransitionTrigger.StartHand);

        deck.Burn();
        hand.CommunityCards.AddRange(deck.Deal(3));

        await _stateMachine.TransitionAsync(hand, HandPhase.Flop, TransitionTrigger.BettingComplete);

        // Create betting round for flop
        _bettingRounds[hand.Id] = BettingRound.Create(table.BigBlind);
        hand.CurrentPlayerId = GetFirstToAct(activePlayers, table.ButtonPosition, isPreflop: false);

        await RecordBombPotStartAsync(hand, holeCards, anteAmount, table, ct);

        return HandStartResult.Success(hand, holeCards);
    }

    /// <inheritdoc/>
    public async Task<ActionResult> ExecutePlayerActionAsync(
        Table table, Guid playerId, PlayerActionType actionType, decimal amount = 0, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(table);

        var hand = table.CurrentHand;
        if (hand == null)
            return ActionResult.Failure("No active hand.");

        if (hand.CurrentPlayerId != playerId)
            return ActionResult.Failure("Not your turn.");

        if (!table.Players.TryGetValue(playerId, out var player))
            return ActionResult.Failure("Player not found.");

        if (!_bettingRounds.TryGetValue(hand.Id, out var round))
            return ActionResult.Failure("No active betting round.");

        // Validate via ActionValidator
        var validation = _actionValidator.Validate(player, round, actionType, amount, isPlayersTurn: true);
        if (!validation.IsValid)
            return ActionResult.Failure(validation.ErrorMessage ?? "Invalid action.");

        var action = validation.Action!;

        // Execute action - update player and betting round
        ApplyAction(player, round, action, hand);

        await RecordPlayerActionAsync(hand, action, table, ct);

        // Check betting round completion
        var playersInHand = GetPlayersInHand(table, hand);

        if (playersInHand.Count <= 1)
            return await CompleteHandAllFoldedAsync(table, hand, playersInHand.FirstOrDefault(), ct);

        if (IsBettingRoundComplete(table, hand, round))
            return await AdvancePhaseAsync(table, hand, ct);

        // Find next player
        hand.CurrentPlayerId = GetNextToAct(table, hand, round);

        return ActionResult.Success(hand, hand.CurrentPlayerId);
    }

    /// <inheritdoc/>
    public AvailableActions? GetAvailableActions(Table table)
    {
        var hand = table.CurrentHand;
        if (hand?.CurrentPlayerId == null) return null;
        if (!table.Players.TryGetValue(hand.CurrentPlayerId.Value, out var player)) return null;
        if (!_bettingRounds.TryGetValue(hand.Id, out var round)) return null;

        return _actionValidator.GetAvailableActions(player, round, isPlayersTurn: true);
    }

    /// <inheritdoc/>
    public async Task<ActionResult> ForceTimeoutFoldAsync(Table table, CancellationToken ct = default)
    {
        var hand = table.CurrentHand;
        if (hand?.CurrentPlayerId == null)
            return ActionResult.Failure("No player to fold.");

        _logger.LogInformation("Forcing timeout fold for player {PlayerId}", hand.CurrentPlayerId);
        return await ExecutePlayerActionAsync(table, hand.CurrentPlayerId.Value, PlayerActionType.Fold, ct: ct);
    }

    /// <inheritdoc/>
    public BettingRound? GetBettingRound(Guid handId) => _bettingRounds.GetValueOrDefault(handId);

    /// <inheritdoc/>
    public async Task<ShowdownResult> ExecuteShowdownAsync(Table table, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(table);

        var hand = table.CurrentHand;
        if (hand == null)
        {
            return ShowdownResult.Failure("No active hand.");
        }

        if (hand.Phase != HandPhase.Showdown)
        {
            return ShowdownResult.Failure($"Hand is in {hand.Phase} phase, not showdown.");
        }

        // Execute showdown
        var result = await _showdownHandler.ExecuteShowdownAsync(table, ct);

        if (result.IsSuccess)
        {
            // Transition to complete
            await _stateMachine.AdvanceAsync(hand, TransitionTrigger.ShowdownComplete);

            // Record hand completed event
            await RecordHandCompletedAsync(hand, result.TotalWinnings.Keys.ToList(), table, ct);

            // Cleanup hand state
            CleanupHand(hand.Id);
            hand.CompletedAt = DateTimeOffset.UtcNow;
            table.CurrentHand = null;

            _logger.LogInformation(
                "Hand {HandNumber} showdown complete. Total pot: {TotalPot}",
                hand.HandNumber,
                result.TotalWinnings.Values.Sum());
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> RequestShowdownMuckAsync(Table table, Guid playerId, CancellationToken ct = default)
    {
        return await _showdownHandler.RequestMuckAsync(table, playerId, ct);
    }

    #region Private Helpers

    private static List<Player> GetActivePlayers(Table table) =>
        table.SeatedPlayers
            .Where(p => p.Status != PlayerStatus.Away && p.ChipStack > 0)
            .OrderBy(p => p.SeatPosition)
            .ToList();

    private static List<Player> GetPlayersInHand(Table table, Hand hand) =>
        table.Players.Values
            .Where(p => hand.PlayerIds.Contains(p.Id) && p.IsInHand)
            .ToList();

    private static (int sb, int bb) CalculateBlindPositions(List<Player> players, int button)
    {
        var positions = players.Select(p => p.SeatPosition).OrderBy(p => p).ToList();
        var btnIdx = positions.IndexOf(button);

        if (players.Count == 2) // Heads-up: button is SB
            return (button, positions[(btnIdx + 1) % positions.Count]);

        return (positions[(btnIdx + 1) % positions.Count], positions[(btnIdx + 2) % positions.Count]);
    }

    private static void PostBlinds(Hand hand, Table table, Player sb, Player bb)
    {
        var sbAmt = Math.Min(table.SmallBlind, sb.ChipStack);
        sb.ChipStack -= sbAmt;
        sb.CurrentBet = sbAmt;
        sb.TotalBetThisHand = sbAmt;
        if (sb.ChipStack == 0) sb.Status = PlayerStatus.AllIn;

        var bbAmt = Math.Min(table.BigBlind, bb.ChipStack);
        bb.ChipStack -= bbAmt;
        bb.CurrentBet = bbAmt;
        bb.TotalBetThisHand = bbAmt;
        if (bb.ChipStack == 0) bb.Status = PlayerStatus.AllIn;

        hand.CurrentBet = table.BigBlind;
        hand.MinRaise = table.BigBlind;
        hand.Pots[0].Amount = sbAmt + bbAmt;
    }

    private static Dictionary<Guid, Card[]> DealHoleCards(Deck deck, List<Player> players)
    {
        var holeCards = new Dictionary<Guid, Card[]>();

        // Deal one card to each player, then second card (standard dealing)
        foreach (var p in players) p.HoleCards = [deck.Deal(), default];
        foreach (var p in players)
        {
            p.HoleCards![1] = deck.Deal();
            holeCards[p.Id] = p.HoleCards;
        }

        return holeCards;
    }

    private static Guid GetFirstToAct(List<Player> players, int referencePosition, bool isPreflop)
    {
        var positions = players.Select(p => p.SeatPosition).OrderBy(p => p).ToList();
        var refIdx = positions.IndexOf(referencePosition);
        var startIdx = (refIdx + 1) % positions.Count; // Left of reference

        for (int i = 0; i < positions.Count; i++)
        {
            var idx = (startIdx + i) % positions.Count;
            var player = players.First(p => p.SeatPosition == positions[idx]);
            if (player.Status == PlayerStatus.Active)
                return player.Id;
        }

        return players[0].Id;
    }

    private void ApplyAction(Player player, BettingRound round, ValidatedAction action, Hand hand)
    {
        switch (action.Type)
        {
            case PlayerActionType.Fold:
                player.Status = PlayerStatus.Folded;
                round.RecordFold(player.Id);
                _potManager.RemovePlayerFromPots(hand.Pots, player.Id);
                break;

            case PlayerActionType.Check:
                round.RecordCheck(player.Id);
                break;

            case PlayerActionType.Call:
                player.ChipStack -= action.Amount;
                player.CurrentBet = action.NewTotalBet;
                player.TotalBetThisHand += action.Amount;
                round.RecordCall(player.Id, action.Amount);
                break;

            case PlayerActionType.Raise:
                player.ChipStack -= action.Amount;
                player.CurrentBet = action.NewTotalBet;
                player.TotalBetThisHand += action.Amount;
                hand.CurrentBet = action.NewTotalBet;
                hand.RaisesThisRound++;
                hand.LastAggressorId = player.Id;
                round.RecordRaise(player.Id, action.NewTotalBet);
                break;

            case PlayerActionType.AllIn:
                player.ChipStack = 0;
                player.Status = PlayerStatus.AllIn;
                player.CurrentBet = action.NewTotalBet;
                player.TotalBetThisHand += action.Amount;
                round.RecordAllIn(player.Id, action.Amount, action.IsRaise);
                if (action.IsRaise)
                {
                    hand.CurrentBet = action.NewTotalBet;
                    hand.LastAggressorId = player.Id;
                    hand.RaisesThisRound++;
                }
                break;
        }
    }

    private bool IsBettingRoundComplete(Table table, Hand hand, BettingRound round)
    {
        var activePlayers = table.Players.Values
            .Where(p => hand.PlayerIds.Contains(p.Id) && p.Status == PlayerStatus.Active)
            .ToList();

        if (activePlayers.Count == 0) return true;

        // All active players must have acted and matched the bet
        var allMatched = activePlayers.All(p => round.GetPlayerBet(p.Id) >= round.CurrentBet);
        var allActed = activePlayers.All(p => round.Actions.Any(a => a.PlayerId == p.Id));

        return allMatched && allActed;
    }

    private Guid? GetNextToAct(Table table, Hand hand, BettingRound round)
    {
        var currentPos = table.Players[hand.CurrentPlayerId!.Value].SeatPosition;
        var activePlayers = table.Players.Values
            .Where(p => hand.PlayerIds.Contains(p.Id) && p.Status == PlayerStatus.Active)
            .OrderBy(p => p.SeatPosition)
            .ToList();

        if (activePlayers.Count == 0) return null;

        var positions = activePlayers.Select(p => p.SeatPosition).ToList();
        var currentIdx = positions.IndexOf(currentPos);
        if (currentIdx < 0) currentIdx = 0;

        for (int i = 1; i <= positions.Count; i++)
        {
            var nextIdx = (currentIdx + i) % positions.Count;
            var player = activePlayers.First(p => p.SeatPosition == positions[nextIdx]);

            // Player needs to act if:
            // 1. Their bet is less than current bet (need to call/fold/raise), OR
            // 2. They haven't acted yet this round (e.g., BB option to check/raise)
            var hasActed = round.Actions.Any(a => a.PlayerId == player.Id);
            var needsToMatch = round.GetPlayerBet(player.Id) < round.CurrentBet;

            if (needsToMatch || !hasActed)
                return player.Id;
        }

        return null;
    }

    private async Task<ActionResult> CompleteHandAllFoldedAsync(
        Table table, Hand hand, Player? winner, CancellationToken ct)
    {
        CollectBetsIntoPots(table, hand);

        var winnings = new Dictionary<Guid, decimal>();
        if (winner != null)
        {
            var total = hand.TotalPot;
            winner.ChipStack += total;
            winnings[winner.Id] = total;

            await RecordPotAwardedAsync(hand, winner.Id, total, ct);
        }

        await _stateMachine.AdvanceAsync(hand, TransitionTrigger.AllFolded);
        await RecordHandCompletedAsync(hand, winnings.Keys.ToList(), table, ct);

        CleanupHand(hand.Id);
        table.CurrentHand = null;

        _logger.LogInformation("Hand {Num} complete - all folded. Winner: {Winner}",
            hand.HandNumber, winner?.DisplayName ?? "none");

        return new ActionResult
        {
            IsSuccess = true, Hand = hand, HandComplete = true,
            BettingRoundComplete = true, Winnings = winnings
        };
    }

    private async Task<ActionResult> AdvancePhaseAsync(Table table, Hand hand, CancellationToken ct)
    {
        CollectBetsIntoPots(table, hand);
        ResetForNextRound(table, hand);

        await RecordBettingRoundCompletedAsync(hand, table, ct);

        // Check if only all-in players remain
        var canAct = table.Players.Values
            .Count(p => hand.PlayerIds.Contains(p.Id) && p.Status == PlayerStatus.Active);

        Card[]? newCards = null;

        if (canAct <= 1)
        {
            // Run out remaining cards
            newCards = await RunOutCardsAsync(hand, ct);
            return new ActionResult
            {
                IsSuccess = true, Hand = hand, BettingRoundComplete = true,
                NewCommunityCards = newCards, NextPlayerId = null
            };
        }

        // Deal next street
        var nextPhase = GetNextStreet(hand.Phase);
        if (nextPhase is HandPhase.Flop or HandPhase.Turn or HandPhase.River)
        {
            newCards = DealStreet(hand, nextPhase);
            await RecordCommunityCardsAsync(hand, newCards, nextPhase, ct);
        }

        await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);

        if (hand.Phase == HandPhase.Showdown)
        {
            return new ActionResult
            {
                IsSuccess = true, Hand = hand, BettingRoundComplete = true,
                NewCommunityCards = newCards, NextPlayerId = null
            };
        }

        var activePlayers = table.Players.Values
            .Where(p => hand.PlayerIds.Contains(p.Id) && p.Status == PlayerStatus.Active)
            .OrderBy(p => p.SeatPosition)
            .ToList();

        hand.CurrentPlayerId = GetFirstToAct(activePlayers, hand.ButtonPosition, isPreflop: false);

        _logger.LogInformation("Hand {Num} advanced to {Phase}", hand.HandNumber, hand.Phase);

        return new ActionResult
        {
            IsSuccess = true, Hand = hand, BettingRoundComplete = true,
            NewCommunityCards = newCards, NextPlayerId = hand.CurrentPlayerId
        };
    }

    private async Task<Card[]> RunOutCardsAsync(Hand hand, CancellationToken ct)
    {
        var allCards = new List<Card>();
        var deck = _decks[hand.Id];

        while (hand.CommunityCards.Count < 5)
        {
            var phase = hand.CommunityCards.Count switch
            {
                0 => HandPhase.Flop,
                3 => HandPhase.Turn,
                4 => HandPhase.River,
                _ => throw new InvalidOperationException()
            };

            var cards = DealStreet(hand, phase);
            allCards.AddRange(cards);
            await RecordCommunityCardsAsync(hand, cards, phase, ct);
            await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);
        }

        // After dealing all cards, transition to showdown
        if (hand.Phase == HandPhase.River)
        {
            await _stateMachine.AdvanceAsync(hand, TransitionTrigger.BettingComplete);
        }

        return allCards.ToArray();
    }

    private Card[] DealStreet(Hand hand, HandPhase phase)
    {
        var deck = _decks[hand.Id];
        deck.Burn();

        var cards = phase == HandPhase.Flop ? deck.Deal(3) : [deck.Deal()];
        hand.CommunityCards.AddRange(cards);
        return cards;
    }

    private static HandPhase GetNextStreet(HandPhase current) => current switch
    {
        HandPhase.Preflop => HandPhase.Flop,
        HandPhase.Flop => HandPhase.Turn,
        HandPhase.Turn => HandPhase.River,
        HandPhase.River => HandPhase.Showdown,
        _ => HandPhase.Complete
    };

    private void CollectBetsIntoPots(Table table, Hand hand)
    {
        var contributions = hand.PlayerIds
            .Where(id => table.Players.ContainsKey(id))
            .ToDictionary(id => id, id => table.Players[id].TotalBetThisHand);

        var allIn = hand.PlayerIds
            .Where(id => table.Players.TryGetValue(id, out var p) && p.Status == PlayerStatus.AllIn)
            .ToHashSet();

        var folded = hand.PlayerIds
            .Where(id => table.Players.TryGetValue(id, out var p) && p.Status == PlayerStatus.Folded)
            .ToHashSet();

        var newPots = _potManager.CalculatePots(contributions, allIn, folded);
        hand.Pots.Clear();
        hand.Pots.AddRange(newPots);
    }

    private void ResetForNextRound(Table table, Hand hand)
    {
        _bettingRounds[hand.Id] = BettingRound.Create(table.BigBlind);

        foreach (var playerId in hand.PlayerIds)
        {
            if (table.Players.TryGetValue(playerId, out var player))
                player.CurrentBet = 0;
        }

        hand.CurrentBet = 0;
        hand.RaisesThisRound = 0;
    }

    private void CleanupHand(Guid handId)
    {
        _decks.TryRemove(handId, out _);
        _bettingRounds.TryRemove(handId, out _);
    }

    #endregion

    #region Event Recording

    private async Task RecordHandStartAsync(Hand hand, Dictionary<Guid, Card[]> holeCards,
        Player sb, Player bb, CancellationToken ct)
    {
        await _eventStore.AppendAsync(new HandStartedEvent
        {
            HandId = hand.Id, TableId = hand.TableId, HandNumber = hand.HandNumber,
            ButtonPosition = hand.ButtonPosition, SmallBlindPosition = hand.SmallBlindPosition,
            BigBlindPosition = hand.BigBlindPosition, SmallBlindAmount = hand.SmallBlindAmount,
            BigBlindAmount = hand.BigBlindAmount, PlayerIds = hand.PlayerIds
        }, ct);

        await _eventStore.AppendAsync(new BlindsPostedEvent
        {
            HandId = hand.Id, SequenceNumber = 2,
            SmallBlindPlayerId = sb.Id, SmallBlindAmount = sb.CurrentBet,
            BigBlindPlayerId = bb.Id, BigBlindAmount = bb.CurrentBet,
            PotTotal = hand.TotalPot
        }, ct);

        await _eventStore.AppendAsync(new HoleCardsDealtEvent
        {
            HandId = hand.Id, SequenceNumber = 3,
            PlayerCards = holeCards
        }, ct);
    }

    private async Task RecordBombPotStartAsync(Hand hand, Dictionary<Guid, Card[]> holeCards,
        decimal ante, Table table, CancellationToken ct)
    {
        await _eventStore.AppendAsync(new HandStartedEvent
        {
            HandId = hand.Id, TableId = hand.TableId, HandNumber = hand.HandNumber,
            ButtonPosition = hand.ButtonPosition, SmallBlindPosition = hand.SmallBlindPosition,
            BigBlindPosition = hand.BigBlindPosition, SmallBlindAmount = hand.SmallBlindAmount,
            BigBlindAmount = hand.BigBlindAmount, PlayerIds = hand.PlayerIds,
            IsBombPot = true, IsDoubleBoard = hand.IsDoubleBoard, AnteAmount = ante
        }, ct);

        // Record ante for each player individually
        var seq = 2;
        foreach (var playerId in hand.PlayerIds)
        {
            var player = table.Players[playerId];
            await _eventStore.AppendAsync(new AntePostedEvent
            {
                HandId = hand.Id, SequenceNumber = seq++,
                PlayerId = playerId, Amount = ante,
                RemainingStack = player.ChipStack, PotTotal = hand.TotalPot
            }, ct);
        }

        await _eventStore.AppendAsync(new HoleCardsDealtEvent
        {
            HandId = hand.Id, SequenceNumber = seq++,
            PlayerCards = holeCards
        }, ct);

        await _eventStore.AppendAsync(new CommunityCardsDealtEvent
        {
            HandId = hand.Id, SequenceNumber = seq,
            Cards = hand.CommunityCards.ToList(), Phase = HandPhase.Flop,
            BoardState = hand.CommunityCards.ToList()
        }, ct);
    }

    private async Task RecordPlayerActionAsync(Hand hand, ValidatedAction action, Table table, CancellationToken ct)
    {
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct) + 1;
        await _eventStore.AppendAsync(new PlayerActedEvent
        {
            HandId = hand.Id, SequenceNumber = seq,
            PlayerId = action.PlayerId, ActionType = action.Type,
            Amount = action.Amount, Phase = hand.Phase,
            RemainingStack = action.RemainingStack,
            PotTotal = hand.TotalPot, CurrentBetLevel = hand.CurrentBet
        }, ct);
    }

    private async Task RecordBettingRoundCompletedAsync(Hand hand, Table table, CancellationToken ct)
    {
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct) + 1;
        var playersInHand = table.Players.Values.Count(p => hand.PlayerIds.Contains(p.Id) && p.IsInHand);
        var activePlayers = table.Players.Values.Count(p => hand.PlayerIds.Contains(p.Id) && p.Status == PlayerStatus.Active);

        await _eventStore.AppendAsync(new BettingRoundCompletedEvent
        {
            HandId = hand.Id, SequenceNumber = seq,
            CompletedPhase = hand.Phase, PotTotal = hand.TotalPot,
            ActivePlayerCount = activePlayers, PlayersInHand = playersInHand
        }, ct);
    }

    private async Task RecordCommunityCardsAsync(Hand hand, Card[] cards, HandPhase phase, CancellationToken ct)
    {
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct) + 1;
        await _eventStore.AppendAsync(new CommunityCardsDealtEvent
        {
            HandId = hand.Id, SequenceNumber = seq,
            Cards = cards.ToList(), Phase = phase,
            BoardState = hand.CommunityCards.ToList()
        }, ct);
    }

    private async Task RecordPotAwardedAsync(Hand hand, Guid winnerId, decimal amount, CancellationToken ct)
    {
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct) + 1;
        await _eventStore.AppendAsync(new PotAwardedEvent
        {
            HandId = hand.Id, SequenceNumber = seq,
            PotId = hand.MainPot.Id, PotType = PotType.Main,
            Amount = amount, WinnerIds = [winnerId],
            WinnerAmounts = new Dictionary<Guid, decimal> { [winnerId] = amount },
            WonByFold = true
        }, ct);
    }

    private async Task RecordHandCompletedAsync(Hand hand, List<Guid> winners, Table table, CancellationToken ct)
    {
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct) + 1;
        var duration = (DateTimeOffset.UtcNow - hand.StartedAt).TotalMilliseconds;

        // Calculate player results (winnings - contributions)
        var playerResults = new Dictionary<Guid, decimal>();
        foreach (var playerId in hand.PlayerIds)
        {
            if (table.Players.TryGetValue(playerId, out var player))
            {
                // For now, just track if they won or lost
                playerResults[playerId] = winners.Contains(playerId) ? hand.TotalPot : -player.TotalBetThisHand;
            }
        }

        await _eventStore.AppendAsync(new HandCompletedEvent
        {
            HandId = hand.Id, SequenceNumber = seq,
            TotalPotAmount = hand.TotalPot, DurationMs = (long)duration,
            PlayerCount = hand.PlayerIds.Count, WentToShowdown = hand.Phase == HandPhase.Showdown,
            FinalPhase = hand.Phase, PlayerResults = playerResults,
            WinnerIds = winners
        }, ct);
    }

    #endregion
}

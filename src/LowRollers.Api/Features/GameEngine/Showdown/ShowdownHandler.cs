using LowRollers.Api.Domain.Evaluation;
using LowRollers.Api.Domain.Events;
using LowRollers.Api.Domain.Models;
using LowRollers.Api.Domain.Pots;
using Microsoft.Extensions.Logging;

namespace LowRollers.Api.Features.GameEngine.Showdown;

/// <summary>
/// Handles showdown logic including show order, hand evaluation,
/// auto-mucking, and pot distribution.
/// </summary>
public sealed partial class ShowdownHandler : IShowdownHandler
{
    private readonly IHandEvaluationService _evaluationService;
    private readonly IPotManager _potManager;
    private readonly IHandEventStore _eventStore;
    private readonly ILogger<ShowdownHandler> _logger;

    // Track pending muck requests per hand
    private readonly Dictionary<Guid, HashSet<Guid>> _muckRequests = new();

    public ShowdownHandler(
        IHandEvaluationService evaluationService,
        IPotManager potManager,
        IHandEventStore eventStore,
        ILogger<ShowdownHandler> logger)
    {
        _evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));
        _potManager = potManager ?? throw new ArgumentNullException(nameof(potManager));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ShowdownResult> ExecuteShowdownAsync(Table table, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(table);

        var hand = table.CurrentHand;
        if (hand == null)
        {
            return ShowdownResult.Failure("No active hand.");
        }

        if (hand.CommunityCards.Count != 5)
        {
            return ShowdownResult.Failure("Community cards not fully dealt.");
        }

        // Get players still in the hand (not folded)
        var playersInHand = GetPlayersInHand(table, hand);
        if (playersInHand.Count == 0)
        {
            return ShowdownResult.Failure("No players in hand.");
        }

        // If only one player remains, they win by default (no showdown needed)
        if (playersInHand.Count == 1)
        {
            return await AwardToSinglePlayerAsync(table, hand, playersInHand[0], ct);
        }

        // Determine show order
        var showOrder = GetShowOrderInternal(table, hand, playersInHand);

        // Evaluate all hands
        var evaluatedPlayers = EvaluatePlayerHands(playersInHand, hand.CommunityCards);

        // Determine winners per pot and auto-muck inferior hands
        var (playerResults, potAwards) = await ProcessShowdownAsync(
            table, hand, showOrder, evaluatedPlayers, ct);

        // Award pots
        var totalWinnings = await AwardPotsAsync(table, hand, potAwards, ct);

        // Apply winnings to player stacks
        foreach (var (playerId, amount) in totalWinnings)
        {
            if (table.Players.TryGetValue(playerId, out var player))
            {
                player.ChipStack += amount;
            }
        }

        // Clean up muck requests for this hand
        _muckRequests.Remove(hand.Id);

        Log.ShowdownComplete(
            _logger,
            hand.HandNumber,
            string.Join(", ", totalWinnings.Keys.Select(id =>
                table.Players.TryGetValue(id, out var p) ? p.DisplayName : id.ToString())));

        return ShowdownResult.Success(hand.Id, playerResults, potAwards, totalWinnings);
    }

    /// <inheritdoc/>
    public async Task<bool> RequestMuckAsync(Table table, Guid playerId, CancellationToken ct = default)
    {
        var hand = table.CurrentHand;
        if (hand == null)
        {
            return false;
        }

        if (!table.Players.TryGetValue(playerId, out var player))
        {
            return false;
        }

        // Player must be in the hand
        if (!player.IsInHand)
        {
            return false;
        }

        // Track the muck request
        if (!_muckRequests.TryGetValue(hand.Id, out var muckSet))
        {
            muckSet = new HashSet<Guid>();
            _muckRequests[hand.Id] = muckSet;
        }

        muckSet.Add(playerId);

        Log.MuckRequested(_logger, playerId, hand.Id);

        return await Task.FromResult(true);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> GetShowOrder(Table table)
    {
        var hand = table.CurrentHand;
        if (hand == null)
        {
            return [];
        }

        var playersInHand = GetPlayersInHand(table, hand);
        return GetShowOrderInternal(table, hand, playersInHand);
    }

    #region Private Helpers

    private static List<Player> GetPlayersInHand(Table table, Hand hand)
    {
        return table.Players.Values
            .Where(p => hand.PlayerIds.Contains(p.Id) && p.IsInHand)
            .OrderBy(p => p.SeatPosition)
            .ToList();
    }

    private static List<Guid> GetShowOrderInternal(Table table, Hand hand, List<Player> playersInHand)
    {
        // Last aggressor shows first
        // If no aggressor (all checked to showdown), first to act shows first
        var firstToShow = hand.LastAggressorId ?? GetFirstToActPlayerId(playersInHand, hand.ButtonPosition);

        var result = new List<Guid>();

        // Find the first player in seat order starting from firstToShow
        var orderedByPosition = playersInHand
            .OrderBy(p => p.SeatPosition)
            .ToList();

        var startIndex = orderedByPosition.FindIndex(p => p.Id == firstToShow);
        if (startIndex < 0)
        {
            startIndex = 0; // Fallback to first player
        }

        // Add players in clockwise order starting from first to show
        for (int i = 0; i < orderedByPosition.Count; i++)
        {
            var index = (startIndex + i) % orderedByPosition.Count;
            result.Add(orderedByPosition[index].Id);
        }

        return result;
    }

    private static Guid GetFirstToActPlayerId(List<Player> players, int buttonPosition)
    {
        // First to act post-flop is first player to the left of the button
        var orderedPositions = players
            .Select(p => p.SeatPosition)
            .OrderBy(p => p)
            .ToList();

        var buttonIdx = -1;
        for (int i = 0; i < orderedPositions.Count; i++)
        {
            if (orderedPositions[i] >= buttonPosition)
            {
                buttonIdx = i;
                break;
            }
        }

        if (buttonIdx < 0)
        {
            buttonIdx = orderedPositions.Count - 1;
        }

        // First to act is left of button
        var firstToActIdx = (buttonIdx + 1) % orderedPositions.Count;
        var firstToActPosition = orderedPositions[firstToActIdx];

        return players.First(p => p.SeatPosition == firstToActPosition).Id;
    }

    private Dictionary<Guid, (Player Player, EvaluatedHand Hand)> EvaluatePlayerHands(
        List<Player> players, List<Card> communityCards)
    {
        var evaluated = new Dictionary<Guid, (Player, EvaluatedHand)>();

        foreach (var player in players)
        {
            if (player.HoleCards == null || player.HoleCards.Length != 2)
            {
                Log.InvalidHoleCardsAtShowdown(_logger, player.Id);
                continue;
            }

            var evaluatedHand = _evaluationService.Evaluate(player.HoleCards, communityCards);
            evaluated[player.Id] = (player, evaluatedHand);
        }

        return evaluated;
    }

    private async Task<(List<PlayerShowdownResult> Results, List<PotAward> Awards)> ProcessShowdownAsync(
        Table table,
        Hand hand,
        List<Guid> showOrder,
        Dictionary<Guid, (Player Player, EvaluatedHand Hand)> evaluatedPlayers,
        CancellationToken ct)
    {
        var results = new List<PlayerShowdownResult>();
        var potAwards = new List<PotAward>();
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct);

        // Track the best hand shown so far for auto-muck logic
        EvaluatedHand? bestHandShown = null;
        var shownPlayers = new HashSet<Guid>();
        var muckedPlayers = new HashSet<Guid>();

        // Check for muck requests
        _muckRequests.TryGetValue(hand.Id, out var muckRequests);
        muckRequests ??= new HashSet<Guid>();

        int showOrderNum = 0;
        foreach (var playerId in showOrder)
        {
            showOrderNum++;

            if (!evaluatedPlayers.TryGetValue(playerId, out var evaluated))
            {
                continue;
            }

            var (player, playerHand) = evaluated;

            // Determine if player should show or muck
            bool shouldMuck = false;
            bool isAutoMuck = false;

            // If player requested muck and their hand is inferior, allow it
            if (muckRequests.Contains(playerId) && bestHandShown.HasValue)
            {
                if (playerHand.Ranking > bestHandShown.Value.Ranking) // Higher rank = worse
                {
                    shouldMuck = true;
                    isAutoMuck = false; // Player chose to muck
                }
            }

            // Auto-muck if player's hand cannot beat the best shown hand
            // (But first player to show must always show)
            if (!shouldMuck && bestHandShown.HasValue && showOrderNum > 1)
            {
                if (playerHand.Ranking > bestHandShown.Value.Ranking)
                {
                    // Check if player can win any pot they're eligible for
                    var canWinAnyPot = CanPlayerWinAnyPot(hand, playerId, evaluatedPlayers, shownPlayers);
                    if (!canWinAnyPot)
                    {
                        shouldMuck = true;
                        isAutoMuck = true;
                    }
                }
            }

            if (shouldMuck)
            {
                muckedPlayers.Add(playerId);

                var muckResult = new PlayerShowdownResult
                {
                    PlayerId = playerId,
                    DisplayName = player.DisplayName,
                    HoleCards = player.HoleCards!,
                    Showed = false,
                    AutoMucked = isAutoMuck,
                    ShowOrder = showOrderNum,
                    EvaluatedHand = null
                };
                results.Add(muckResult);

                // Record muck event
                await _eventStore.AppendAsync(new PlayerMuckedCardsEvent
                {
                    HandId = hand.Id,
                    SequenceNumber = ++seq,
                    PlayerId = playerId,
                    IsAutoMuck = isAutoMuck,
                    ShowdownOrder = showOrderNum
                }, ct);

                Log.PlayerMuckedAtShowdown(_logger, player.DisplayName, isAutoMuck ? "auto-mucked" : "mucked");
            }
            else
            {
                // Player shows
                shownPlayers.Add(playerId);

                if (!bestHandShown.HasValue || playerHand.Ranking < bestHandShown.Value.Ranking)
                {
                    bestHandShown = playerHand;
                }

                var showResult = new PlayerShowdownResult
                {
                    PlayerId = playerId,
                    DisplayName = player.DisplayName,
                    HoleCards = player.HoleCards!,
                    Showed = true,
                    AutoMucked = false,
                    ShowOrder = showOrderNum,
                    EvaluatedHand = playerHand
                };
                results.Add(showResult);

                // Record show event
                await _eventStore.AppendAsync(new PlayerShowedCardsEvent
                {
                    HandId = hand.Id,
                    SequenceNumber = ++seq,
                    PlayerId = playerId,
                    HoleCards = player.HoleCards!,
                    HandCategory = playerHand.Category,
                    HandDescription = playerHand.Description,
                    HandRanking = playerHand.Ranking,
                    BestFiveCards = playerHand.Cards,
                    ShowOrder = showOrderNum
                }, ct);

                Log.PlayerShowedHand(_logger, player.DisplayName, playerHand.Description);
            }
        }

        // Calculate winners per pot
        potAwards = CalculatePotWinners(table, hand, evaluatedPlayers, shownPlayers);

        // Update player results with winnings
        var winningsByPlayer = new Dictionary<Guid, (decimal Amount, List<Guid> PotIds)>();
        foreach (var award in potAwards)
        {
            foreach (var (winnerId, amount) in award.WinnerAmounts)
            {
                if (!winningsByPlayer.TryGetValue(winnerId, out var current))
                {
                    current = (0, new List<Guid>());
                }
                current.Amount += amount;
                current.PotIds.Add(award.PotId);
                winningsByPlayer[winnerId] = current;
            }
        }

        // Update results with winnings
        for (int i = 0; i < results.Count; i++)
        {
            if (winningsByPlayer.TryGetValue(results[i].PlayerId, out var winData))
            {
                results[i] = results[i] with
                {
                    WonPotIds = winData.PotIds,
                    AmountWon = winData.Amount
                };
            }
        }

        return (results, potAwards);
    }

    private static bool CanPlayerWinAnyPot(
        Hand hand,
        Guid playerId,
        Dictionary<Guid, (Player Player, EvaluatedHand Hand)> evaluatedPlayers,
        HashSet<Guid> alreadyShown)
    {
        foreach (var pot in hand.Pots)
        {
            if (!pot.IsPlayerEligible(playerId))
            {
                continue;
            }

            // Get eligible opponents who have shown or will show
            var eligibleOpponents = pot.EligiblePlayerIds
                .Where(id => id != playerId && evaluatedPlayers.ContainsKey(id))
                .ToList();

            if (eligibleOpponents.Count == 0)
            {
                // No opponents for this pot - player can win
                return true;
            }

            // Check if player can beat or tie all eligible opponents
            var playerHand = evaluatedPlayers[playerId].Hand;
            var canWinOrTie = true;

            foreach (var opponentId in eligibleOpponents)
            {
                var opponentHand = evaluatedPlayers[opponentId].Hand;
                if (opponentHand.Ranking < playerHand.Ranking)
                {
                    // Opponent has strictly better hand
                    canWinOrTie = false;
                    break;
                }
            }

            if (canWinOrTie)
            {
                return true; // Can win or tie this pot
            }
        }

        return false;
    }

    private List<PotAward> CalculatePotWinners(
        Table table,
        Hand hand,
        Dictionary<Guid, (Player Player, EvaluatedHand Hand)> evaluatedPlayers,
        HashSet<Guid> shownPlayers)
    {
        var awards = new List<PotAward>();

        // Process pots in order (main pot first, then side pots)
        foreach (var pot in hand.Pots.OrderBy(p => p.CreationOrder))
        {
            if (pot.Amount <= 0)
            {
                continue;
            }

            // Get eligible players who showed (or all-in players must show)
            var eligibleShowing = pot.EligiblePlayerIds
                .Where(id => evaluatedPlayers.ContainsKey(id))
                .ToList();

            if (eligibleShowing.Count == 0)
            {
                Log.PotHasNoEligiblePlayers(_logger, pot.Id);
                continue;
            }

            // Find best hand among eligible players
            var bestRanking = eligibleShowing
                .Select(id => evaluatedPlayers[id].Hand.Ranking)
                .Min();

            var winners = eligibleShowing
                .Where(id => evaluatedPlayers[id].Hand.Ranking == bestRanking)
                .ToList();

            var winningHand = evaluatedPlayers[winners[0]].Hand;

            // Calculate split amounts
            var winnerAmounts = CalculateSplitAmounts(table, hand, pot.Amount, winners);

            awards.Add(new PotAward
            {
                PotId = pot.Id,
                PotType = pot.Type,
                Amount = pot.Amount,
                WinnerIds = winners,
                WinnerAmounts = winnerAmounts,
                WinningHandDescription = winningHand.Description,
                WinningHandCategory = winningHand.Category
            });
        }

        return awards;
    }

    private static Dictionary<Guid, decimal> CalculateSplitAmounts(
        Table table,
        Hand hand,
        decimal potAmount,
        List<Guid> winners)
    {
        var amounts = new Dictionary<Guid, decimal>();

        if (winners.Count == 0)
        {
            return amounts;
        }

        if (winners.Count == 1)
        {
            amounts[winners[0]] = potAmount;
            return amounts;
        }

        // Split evenly, with remainder going to first player from button
        var share = Math.Floor(potAmount / winners.Count * 100) / 100;
        var remainder = potAmount - (share * winners.Count);

        // Order winners by position from button
        var buttonPos = hand.ButtonPosition;
        var orderedWinners = winners
            .Select(id => (Id: id, Pos: table.Players.TryGetValue(id, out var p) ? p.SeatPosition : 0))
            .OrderBy(w => GetPositionFromButton(w.Pos, buttonPos))
            .Select(w => w.Id)
            .ToList();

        for (int i = 0; i < orderedWinners.Count; i++)
        {
            var winnerId = orderedWinners[i];
            var amount = share;

            // First winner from button gets remainder
            if (i == 0 && remainder > 0)
            {
                amount += remainder;
            }

            amounts[winnerId] = amount;
        }

        return amounts;
    }

    private static int GetPositionFromButton(int seatPosition, int buttonPosition)
    {
        // Calculate clockwise distance from button
        // Seats are 1-10, so we need to handle wraparound
        if (seatPosition > buttonPosition)
        {
            return seatPosition - buttonPosition;
        }
        return seatPosition + 10 - buttonPosition;
    }

    private async Task<ShowdownResult> AwardToSinglePlayerAsync(
        Table table, Hand hand, Player winner, CancellationToken ct)
    {
        var totalWinnings = new Dictionary<Guid, decimal>();
        var potAwards = new List<PotAward>();

        foreach (var pot in hand.Pots.OrderBy(p => p.CreationOrder))
        {
            if (pot.Amount <= 0)
            {
                continue;
            }

            if (!totalWinnings.ContainsKey(winner.Id))
            {
                totalWinnings[winner.Id] = 0;
            }
            totalWinnings[winner.Id] += pot.Amount;

            potAwards.Add(new PotAward
            {
                PotId = pot.Id,
                PotType = pot.Type,
                Amount = pot.Amount,
                WinnerIds = [winner.Id],
                WinnerAmounts = new Dictionary<Guid, decimal> { [winner.Id] = pot.Amount },
                WinningHandDescription = "Won uncontested",
                WinningHandCategory = HandCategory.HighCard
            });

            // Record pot awarded event
            var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct) + 1;
            await _eventStore.AppendAsync(new PotAwardedEvent
            {
                HandId = hand.Id,
                SequenceNumber = seq,
                PotId = pot.Id,
                PotType = pot.Type,
                Amount = pot.Amount,
                WinnerIds = [winner.Id],
                WinnerAmounts = new Dictionary<Guid, decimal> { [winner.Id] = pot.Amount },
                WonByFold = true
            }, ct);
        }

        // Apply winnings
        winner.ChipStack += totalWinnings.GetValueOrDefault(winner.Id);

        var playerResult = new PlayerShowdownResult
        {
            PlayerId = winner.Id,
            DisplayName = winner.DisplayName,
            HoleCards = winner.HoleCards ?? [],
            Showed = false,
            AutoMucked = false,
            ShowOrder = 1,
            AmountWon = totalWinnings.GetValueOrDefault(winner.Id)
        };

        return ShowdownResult.Success(hand.Id, [playerResult], potAwards, totalWinnings);
    }

    private async Task<Dictionary<Guid, decimal>> AwardPotsAsync(
        Table table,
        Hand hand,
        List<PotAward> potAwards,
        CancellationToken ct)
    {
        var totalWinnings = new Dictionary<Guid, decimal>();
        var seq = await _eventStore.GetLastSequenceNumberAsync(hand.Id, ct);

        foreach (var award in potAwards)
        {
            foreach (var (winnerId, amount) in award.WinnerAmounts)
            {
                if (!totalWinnings.ContainsKey(winnerId))
                {
                    totalWinnings[winnerId] = 0;
                }
                totalWinnings[winnerId] += amount;
            }

            // Record pot awarded event
            await _eventStore.AppendAsync(new PotAwardedEvent
            {
                HandId = hand.Id,
                SequenceNumber = ++seq,
                PotId = award.PotId,
                PotType = award.PotType,
                Amount = award.Amount,
                WinnerIds = award.WinnerIds.ToList(),
                WinnerAmounts = award.WinnerAmounts.ToDictionary(x => x.Key, x => x.Value),
                WinningHandDescription = award.WinningHandDescription,
                WonByFold = false
            }, ct);
        }

        return totalWinnings;
    }

    #endregion

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Showdown complete for hand {HandNumber}. Winners: {Winners}")]
        public static partial void ShowdownComplete(ILogger logger, int handNumber, string winners);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Player {PlayerId} requested muck for hand {HandId}")]
        public static partial void MuckRequested(ILogger logger, Guid playerId, Guid handId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Player {PlayerId} has invalid hole cards at showdown")]
        public static partial void InvalidHoleCardsAtShowdown(ILogger logger, Guid playerId);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Player {DisplayName} {MuckType} at showdown")]
        public static partial void PlayerMuckedAtShowdown(ILogger logger, string displayName, string muckType);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Player {DisplayName} showed {HandDescription}")]
        public static partial void PlayerShowedHand(ILogger logger, string displayName, string handDescription);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Pot {PotId} has no eligible players with shown hands")]
        public static partial void PotHasNoEligiblePlayers(ILogger logger, Guid potId);
    }
}

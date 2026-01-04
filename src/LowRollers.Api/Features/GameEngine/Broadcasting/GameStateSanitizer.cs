using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine.Broadcasting;

/// <summary>
/// Creates sanitized game state views for specific viewers.
/// Implements per-player information hiding for poker game integrity.
/// </summary>
public sealed class GameStateSanitizer : IGameStateSanitizer
{
    /// <inheritdoc/>
    public TableGameState Sanitize(
        Table table,
        Guid? viewerPlayerId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null)
    {
        var players = table.Players.Values
            .OrderBy(p => p.SeatPosition)
            .Select(p => SanitizePlayer(p, viewerPlayerId, shownCards))
            .ToList();

        HandState? handState = null;
        if (table.CurrentHand != null)
        {
            handState = BuildHandState(table.CurrentHand);
        }

        return new TableGameState
        {
            TableId = table.Id,
            TableName = table.Name,
            Status = table.Status,
            Players = players,
            CurrentHand = handState,
            ButtonPosition = table.ButtonPosition,
            SmallBlind = table.SmallBlind,
            BigBlind = table.BigBlind,
            HandCount = table.HandCount,
            ActionTimerSeconds = table.ActionTimerSeconds,
            TimeBankEnabled = table.TimeBankEnabled,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Sanitizes a player's state based on who is viewing.
    /// </summary>
    private static PlayerState SanitizePlayer(
        Player player,
        Guid? viewerPlayerId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards)
    {
        var isViewer = viewerPlayerId.HasValue && player.Id == viewerPlayerId.Value;
        var hasCards = player.HoleCards != null && player.HoleCards.Length > 0;

        // Determine which cards to show
        CardDto[]? visibleCards = null;

        if (hasCards)
        {
            // Show cards if:
            // 1. This is the viewer's own cards, OR
            // 2. Cards are in the shownCards dictionary (showdown)
            if (isViewer)
            {
                visibleCards = player.HoleCards!.Select(CardDto.FromCard).ToArray();
            }
            else if (shownCards?.TryGetValue(player.Id, out var shown) == true)
            {
                visibleCards = shown.Select(CardDto.FromCard).ToArray();
            }
        }

        return new PlayerState
        {
            PlayerId = player.Id,
            DisplayName = player.DisplayName,
            SeatPosition = player.SeatPosition,
            ChipStack = player.ChipStack,
            Status = player.Status,
            CurrentBet = player.CurrentBet,
            TotalBetThisHand = player.TotalBetThisHand,
            HoleCards = visibleCards,
            HasHiddenCards = hasCards && visibleCards == null,
            IsHost = player.IsHost,
            TimeBankSeconds = player.TimeBankSeconds,
            IsViewer = isViewer
        };
    }

    /// <summary>
    /// Builds the hand state (all information is public).
    /// </summary>
    private static HandState BuildHandState(Hand hand)
    {
        var pots = hand.Pots.Select((p, i) => new PotState
        {
            Amount = p.Amount,
            Type = p.Type,
            EligiblePlayerIds = p.EligiblePlayerIds.ToList(),
            Name = p.Type == PotType.Side ? $"Side Pot {p.CreationOrder}" : null
        }).ToList();

        return new HandState
        {
            HandId = hand.Id,
            HandNumber = hand.HandNumber,
            Phase = hand.Phase,
            CommunityCards = hand.CommunityCards.Select(CardDto.FromCard).ToList(),
            SecondBoard = hand.SecondBoard?.Select(CardDto.FromCard).ToList(),
            Pots = pots,
            TotalPot = hand.TotalPot,
            CurrentBet = hand.CurrentBet,
            MinRaise = hand.MinRaise,
            CurrentPlayerId = hand.CurrentPlayerId,
            SmallBlindPosition = hand.SmallBlindPosition,
            BigBlindPosition = hand.BigBlindPosition,
            IsBombPot = hand.IsBombPot,
            IsDoubleBoard = hand.IsDoubleBoard,
            StartedAt = hand.StartedAt
        };
    }
}

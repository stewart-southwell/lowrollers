namespace LowRollers.Api.Features.GameEngine.ActionTimer;

/// <summary>
/// Represents the current state of an action timer for a table.
/// </summary>
public sealed record ActionTimerState
{
    /// <summary>
    /// The table this timer is for.
    /// </summary>
    public required Guid TableId { get; init; }

    /// <summary>
    /// The hand this timer is for.
    /// </summary>
    public required Guid HandId { get; init; }

    /// <summary>
    /// The player whose turn it is.
    /// </summary>
    public required Guid ActivePlayerId { get; init; }

    /// <summary>
    /// Remaining time in seconds on the main action timer.
    /// </summary>
    public int RemainingSeconds { get; init; }

    /// <summary>
    /// Whether the time bank is available for this player.
    /// </summary>
    public bool HasTimeBank { get; init; }

    /// <summary>
    /// Whether the time bank is currently being used (main timer expired).
    /// </summary>
    public bool IsTimeBankActive { get; init; }

    /// <summary>
    /// Remaining time bank seconds.
    /// </summary>
    public int TimeBankRemainingSeconds { get; init; }

    /// <summary>
    /// Original time bank seconds at the start of this turn.
    /// Used to calculate how much time bank was consumed.
    /// </summary>
    public int OriginalTimeBankSeconds { get; init; }

    /// <summary>
    /// The timestamp when the timer started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// The total action time allowed in seconds.
    /// </summary>
    public int TotalActionSeconds { get; init; }

    /// <summary>
    /// Whether a warning has been sent for this timer.
    /// </summary>
    public bool WarningSent { get; init; }

    /// <summary>
    /// Whether time bank activation has been broadcast.
    /// </summary>
    public bool TimeBankActivationBroadcast { get; init; }

    /// <summary>
    /// Creates a new timer state for a player's turn.
    /// </summary>
    public static ActionTimerState Create(
        Guid tableId,
        Guid handId,
        Guid playerId,
        int actionSeconds,
        bool hasTimeBank,
        int timeBankSeconds)
    {
        return new ActionTimerState
        {
            TableId = tableId,
            HandId = handId,
            ActivePlayerId = playerId,
            RemainingSeconds = actionSeconds,
            TotalActionSeconds = actionSeconds,
            HasTimeBank = hasTimeBank,
            IsTimeBankActive = false,
            TimeBankRemainingSeconds = timeBankSeconds,
            OriginalTimeBankSeconds = timeBankSeconds,
            StartedAt = DateTimeOffset.UtcNow,
            WarningSent = false,
            TimeBankActivationBroadcast = false
        };
    }

    /// <summary>
    /// Creates a copy with decremented time.
    /// If main timer expires and time bank is available, switches to time bank.
    /// </summary>
    public ActionTimerState Tick()
    {
        if (IsTimeBankActive)
        {
            // Time bank is active, decrement it (prevent going negative)
            return this with { TimeBankRemainingSeconds = Math.Max(0, TimeBankRemainingSeconds - 1) };
        }

        if (RemainingSeconds > 1)
        {
            // Main timer still has time
            return this with { RemainingSeconds = RemainingSeconds - 1 };
        }

        // Main timer expired
        if (HasTimeBank && TimeBankRemainingSeconds > 0)
        {
            // Switch to time bank
            return this with
            {
                RemainingSeconds = 0,
                IsTimeBankActive = true
            };
        }

        // No time bank, timer expired
        return this with { RemainingSeconds = 0 };
    }

    /// <summary>
    /// Marks the warning as sent.
    /// </summary>
    public ActionTimerState WithWarningSent() => this with { WarningSent = true };

    /// <summary>
    /// Marks the time bank activation as broadcast.
    /// </summary>
    public ActionTimerState WithTimeBankActivationBroadcast() => this with { TimeBankActivationBroadcast = true };

    /// <summary>
    /// Whether the timer has completely expired (including time bank if applicable).
    /// </summary>
    public bool IsExpired =>
        RemainingSeconds <= 0 && (!HasTimeBank || !IsTimeBankActive || TimeBankRemainingSeconds <= 0);

    /// <summary>
    /// Gets the effective remaining seconds (combines main timer and time bank).
    /// </summary>
    public int EffectiveRemainingSeconds =>
        IsTimeBankActive ? TimeBankRemainingSeconds : RemainingSeconds;

    /// <summary>
    /// Whether time bank just became active and needs to be broadcast.
    /// </summary>
    public bool NeedsTimeBankActivationBroadcast =>
        IsTimeBankActive && !TimeBankActivationBroadcast;

    /// <summary>
    /// Gets the amount of time bank seconds used (only valid if time bank was active).
    /// </summary>
    public int TimeBankSecondsUsed =>
        IsTimeBankActive ? OriginalTimeBankSeconds - TimeBankRemainingSeconds : 0;
}

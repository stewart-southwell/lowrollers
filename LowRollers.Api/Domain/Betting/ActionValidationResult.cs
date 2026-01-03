namespace LowRollers.Api.Domain.Betting;

/// <summary>
/// Result of validating a player action.
/// </summary>
public sealed record ActionValidationResult
{
    /// <summary>
    /// Whether the action is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if the action is invalid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The validated action details (if valid).
    /// </summary>
    public ValidatedAction? Action { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ActionValidationResult Valid(ValidatedAction action) => new()
    {
        IsValid = true,
        Action = action
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ActionValidationResult Invalid(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// A validated action ready for execution.
/// </summary>
public sealed record ValidatedAction
{
    /// <summary>
    /// The type of action.
    /// </summary>
    public required PlayerActionType Type { get; init; }

    /// <summary>
    /// The player performing the action.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// The amount to add to the pot (0 for fold/check).
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// The player's new total bet for this round.
    /// </summary>
    public decimal NewTotalBet { get; init; }

    /// <summary>
    /// Whether this action constitutes a raise (for all-in that may be less than min raise).
    /// </summary>
    public bool IsRaise { get; init; }

    /// <summary>
    /// The player's remaining stack after this action.
    /// </summary>
    public decimal RemainingStack { get; init; }
}

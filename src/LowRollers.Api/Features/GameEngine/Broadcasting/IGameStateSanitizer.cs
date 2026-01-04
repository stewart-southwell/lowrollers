using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine.Broadcasting;

/// <summary>
/// Creates sanitized game state views for specific viewers.
/// Handles the logic of which information each viewer is allowed to see.
/// </summary>
public interface IGameStateSanitizer
{
    /// <summary>
    /// Creates a sanitized game state for a specific viewer.
    /// </summary>
    /// <param name="table">The authoritative table state.</param>
    /// <param name="viewerPlayerId">
    /// The player viewing (null for spectator).
    /// Players see their own hole cards; spectators see none.
    /// </param>
    /// <param name="shownCards">
    /// Cards explicitly shown at showdown (visible to all viewers).
    /// </param>
    /// <returns>A TableGameState with appropriate information hidden/visible.</returns>
    TableGameState Sanitize(
        Table table,
        Guid? viewerPlayerId,
        IReadOnlyDictionary<Guid, Card[]>? shownCards = null);
}

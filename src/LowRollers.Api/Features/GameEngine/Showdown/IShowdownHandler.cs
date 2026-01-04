using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine.Showdown;

/// <summary>
/// Handles showdown logic including show order, hand evaluation, and pot distribution.
/// </summary>
public interface IShowdownHandler
{
    /// <summary>
    /// Executes the showdown for a hand that has reached the showdown phase.
    /// Determines show order, evaluates hands, distributes pots, and records events.
    /// </summary>
    /// <param name="table">The table with the current hand.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The showdown result with winners and pot distributions.</returns>
    Task<ShowdownResult> ExecuteShowdownAsync(Table table, CancellationToken ct = default);

    /// <summary>
    /// Allows a player to voluntarily muck their cards at showdown.
    /// Only valid for players who are not required to show (losers).
    /// </summary>
    /// <param name="table">The table with the current hand.</param>
    /// <param name="playerId">The player requesting to muck.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if muck was successful, false if player must show.</returns>
    Task<bool> RequestMuckAsync(Table table, Guid playerId, CancellationToken ct = default);

    /// <summary>
    /// Gets the order in which players should show their cards.
    /// Last aggressor shows first, then clockwise.
    /// If all players checked, first-to-act shows first.
    /// </summary>
    /// <param name="table">The table with the current hand.</param>
    /// <returns>Ordered list of player IDs for showing.</returns>
    IReadOnlyList<Guid> GetShowOrder(Table table);
}

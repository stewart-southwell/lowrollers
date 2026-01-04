namespace LowRollers.Api.Features.GameEngine;

/// <summary>
/// Shared constants and utilities for SignalR game hub communication.
/// </summary>
public static class GameHubConstants
{
    /// <summary>
    /// Gets the SignalR group name for a table.
    /// All players at the same table are added to this group for broadcasting.
    /// </summary>
    public static string GetTableGroupName(Guid tableId) => $"table-{tableId}";
}

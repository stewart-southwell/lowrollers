namespace LowRollers.Api.Features.GameEngine.Connections;

/// <summary>
/// Represents a connection's association with a table and optionally a player.
/// </summary>
/// <param name="TableId">The table this connection is watching.</param>
/// <param name="PlayerId">The player ID if seated, null for spectators.</param>
public sealed record ConnectionInfo(Guid TableId, Guid? PlayerId)
{
    /// <summary>
    /// Whether this connection is a spectator (not a seated player).
    /// </summary>
    public bool IsSpectator => !PlayerId.HasValue;
}

/// <summary>
/// Manages SignalR connection mappings for game tables.
/// Maps connection IDs to table and player associations.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Registers a player connection to a table.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID.</param>
    /// <param name="tableId">The table the player is joining.</param>
    /// <param name="playerId">The player's ID.</param>
    void AddPlayerConnection(string connectionId, Guid tableId, Guid playerId);

    /// <summary>
    /// Registers a spectator connection to a table.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID.</param>
    /// <param name="tableId">The table the spectator is watching.</param>
    void AddSpectatorConnection(string connectionId, Guid tableId);

    /// <summary>
    /// Removes a connection.
    /// </summary>
    /// <param name="connectionId">The connection ID to remove.</param>
    /// <returns>The connection info if found, null otherwise.</returns>
    ConnectionInfo? RemoveConnection(string connectionId);

    /// <summary>
    /// Gets connection info for a specific connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <returns>The connection info if found, null otherwise.</returns>
    ConnectionInfo? GetConnection(string connectionId);

    /// <summary>
    /// Gets all connection IDs for players at a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <returns>Dictionary of connection IDs to player IDs.</returns>
    IReadOnlyDictionary<string, Guid> GetPlayerConnections(Guid tableId);

    /// <summary>
    /// Gets all spectator connection IDs for a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <returns>List of spectator connection IDs.</returns>
    IReadOnlyList<string> GetSpectatorConnections(Guid tableId);

    /// <summary>
    /// Gets the connection ID for a specific player at a table.
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <param name="playerId">The player ID.</param>
    /// <returns>The connection ID if found, null otherwise.</returns>
    string? GetPlayerConnectionId(Guid tableId, Guid playerId);

    /// <summary>
    /// Gets all connection IDs at a table (players and spectators).
    /// </summary>
    /// <param name="tableId">The table ID.</param>
    /// <returns>List of all connection IDs.</returns>
    IReadOnlyList<string> GetAllConnections(Guid tableId);
}

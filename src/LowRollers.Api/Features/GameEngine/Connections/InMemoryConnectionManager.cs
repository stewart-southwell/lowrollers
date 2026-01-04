using System.Collections.Concurrent;

namespace LowRollers.Api.Features.GameEngine.Connections;

/// <summary>
/// In-memory implementation of connection management.
/// Thread-safe for concurrent access from multiple SignalR connections.
/// </summary>
/// <remarks>
/// Design decision: Single connection per player.
/// If a player opens a second tab, the new connection replaces the old one.
/// This is intentional for poker where playing on multiple tabs would be problematic.
/// The old connection ID is returned so callers can handle cleanup (e.g., close old tab).
///
/// TODO: Replace with Redis-backed implementation for multi-instance deployment.
/// </remarks>
public sealed class InMemoryConnectionManager : IConnectionManager
{
    // Connection ID -> ConnectionInfo
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();

    // Table ID -> Set of connection IDs for that table
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _tableConnections = new();

    // Player ID -> Current connection ID (for O(1) player lookups and detecting reconnects)
    private readonly ConcurrentDictionary<Guid, string> _playerConnections = new();

    /// <inheritdoc/>
    public void AddPlayerConnection(string connectionId, Guid tableId, Guid playerId)
    {
        // Check for existing connection from this player (reconnect scenario)
        if (_playerConnections.TryGetValue(playerId, out var existingConnId) &&
            existingConnId != connectionId)
        {
            // Remove the old connection - new tab wins
            RemoveConnection(existingConnId);
        }

        var info = new ConnectionInfo(tableId, playerId);
        _connections[connectionId] = info;
        _playerConnections[playerId] = connectionId;

        var tableConns = _tableConnections.GetOrAdd(tableId, _ => new ConcurrentDictionary<string, byte>());
        tableConns[connectionId] = 0;
    }

    /// <inheritdoc/>
    public void AddSpectatorConnection(string connectionId, Guid tableId)
    {
        // Spectators can have multiple connections (e.g., streaming to multiple displays)
        var info = new ConnectionInfo(tableId, null);
        _connections[connectionId] = info;

        var tableConns = _tableConnections.GetOrAdd(tableId, _ => new ConcurrentDictionary<string, byte>());
        tableConns[connectionId] = 0;
    }

    /// <inheritdoc/>
    public ConnectionInfo? RemoveConnection(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var info))
        {
            return null;
        }

        // Clean up player connection tracking
        if (info.PlayerId.HasValue)
        {
            // Only remove if this connection is still the active one for the player
            _playerConnections.TryRemove(
                new KeyValuePair<Guid, string>(info.PlayerId.Value, connectionId));
        }

        if (_tableConnections.TryGetValue(info.TableId, out var tableConns))
        {
            tableConns.TryRemove(connectionId, out _);

            // Clean up empty table entries
            if (tableConns.IsEmpty)
            {
                _tableConnections.TryRemove(info.TableId, out _);
            }
        }

        return info;
    }

    /// <inheritdoc/>
    public ConnectionInfo? GetConnection(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var info);
        return info;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, Guid> GetPlayerConnections(Guid tableId)
    {
        if (!_tableConnections.TryGetValue(tableId, out var tableConns))
        {
            return new Dictionary<string, Guid>();
        }

        var result = new Dictionary<string, Guid>();
        foreach (var connId in tableConns.Keys)
        {
            if (_connections.TryGetValue(connId, out var info) && info.PlayerId.HasValue)
            {
                result[connId] = info.PlayerId.Value;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetSpectatorConnections(Guid tableId)
    {
        if (!_tableConnections.TryGetValue(tableId, out var tableConns))
        {
            return [];
        }

        var result = new List<string>();
        foreach (var connId in tableConns.Keys)
        {
            if (_connections.TryGetValue(connId, out var info) && info.IsSpectator)
            {
                result.Add(connId);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public string? GetPlayerConnectionId(Guid tableId, Guid playerId)
    {
        // O(1) lookup using player index
        if (!_playerConnections.TryGetValue(playerId, out var connId))
        {
            return null;
        }

        // Verify the connection is still valid and for this table
        if (_connections.TryGetValue(connId, out var info) && info.TableId == tableId)
        {
            return connId;
        }

        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAllConnections(Guid tableId)
    {
        if (!_tableConnections.TryGetValue(tableId, out var tableConns))
        {
            return [];
        }

        return tableConns.Keys.ToList();
    }
}

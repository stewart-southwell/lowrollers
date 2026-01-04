using System.Collections.Concurrent;
using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Features.GameEngine;

/// <summary>
/// In-memory table storage for development and testing.
/// TODO: Replace with persistent storage (Redis/database) for production.
/// </summary>
public interface ITableManager
{
    /// <summary>
    /// Gets a table by ID.
    /// </summary>
    Table? GetTable(Guid tableId);

    /// <summary>
    /// Adds or updates a table.
    /// </summary>
    void SetTable(Table table);

    /// <summary>
    /// Removes a table.
    /// </summary>
    bool RemoveTable(Guid tableId);

    /// <summary>
    /// Gets all active tables.
    /// </summary>
    IEnumerable<Table> GetAllTables();
}

/// <summary>
/// In-memory implementation of ITableManager.
/// Thread-safe for concurrent access from multiple SignalR connections.
/// </summary>
public sealed class InMemoryTableManager : ITableManager
{
    private readonly ConcurrentDictionary<Guid, Table> _tables = new();

    /// <inheritdoc/>
    public Table? GetTable(Guid tableId)
    {
        _tables.TryGetValue(tableId, out var table);
        return table;
    }

    /// <inheritdoc/>
    public void SetTable(Table table)
    {
        _tables[table.Id] = table;
    }

    /// <inheritdoc/>
    public bool RemoveTable(Guid tableId)
    {
        return _tables.TryRemove(tableId, out _);
    }

    /// <inheritdoc/>
    public IEnumerable<Table> GetAllTables()
    {
        return _tables.Values;
    }
}

namespace LowRollers.Api.Domain.Models;

/// <summary>
/// Represents the current state of a poker table.
/// </summary>
public enum TableStatus
{
    /// <summary>Table is open but game hasn't started.</summary>
    Lobby = 0,

    /// <summary>Game is in progress.</summary>
    Playing = 1,

    /// <summary>Game is paused between hands.</summary>
    Paused = 2,

    /// <summary>Table is closed.</summary>
    Closed = 3
}

/// <summary>
/// Represents a poker table.
/// </summary>
public sealed class Table
{
    /// <summary>
    /// Unique identifier for the table.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the table.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Current status of the table.
    /// </summary>
    public TableStatus Status { get; set; } = TableStatus.Lobby;

    /// <summary>
    /// Hashed invite code for joining the table.
    /// </summary>
    public string InviteCodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Players currently at the table.
    /// </summary>
    public Dictionary<Guid, Player> Players { get; init; } = [];

    /// <summary>
    /// ID of the current host.
    /// </summary>
    public Guid HostId { get; set; }

    /// <summary>
    /// Current hand being played (null if between hands).
    /// </summary>
    public Hand? CurrentHand { get; set; }

    /// <summary>
    /// Current dealer button position (1-10).
    /// </summary>
    public int ButtonPosition { get; set; } = 1;

    /// <summary>
    /// Number of hands played this session.
    /// </summary>
    public int HandCount { get; set; }

    /// <summary>
    /// Small blind amount.
    /// </summary>
    public decimal SmallBlind { get; set; }

    /// <summary>
    /// Big blind amount.
    /// </summary>
    public decimal BigBlind { get; set; }

    /// <summary>
    /// Minimum buy-in amount.
    /// </summary>
    public decimal MinBuyIn { get; set; }

    /// <summary>
    /// Maximum buy-in amount (0 for no max).
    /// </summary>
    public decimal MaxBuyIn { get; set; }

    /// <summary>
    /// Action timer in seconds (0 for unlimited).
    /// </summary>
    public int ActionTimerSeconds { get; set; } = 30;

    /// <summary>
    /// Whether time bank is enabled.
    /// </summary>
    public bool TimeBankEnabled { get; set; } = true;

    /// <summary>
    /// Initial time bank seconds per player.
    /// </summary>
    public int InitialTimeBankSeconds { get; set; } = 60;

    /// <summary>
    /// Banned display names for this table.
    /// </summary>
    public HashSet<string> BannedNames { get; init; } = [];

    /// <summary>
    /// Button money kitty amount.
    /// </summary>
    public decimal ButtonMoneyKitty { get; set; }

    /// <summary>
    /// Button money contribution per hand (0 if disabled).
    /// </summary>
    public decimal ButtonMoneyAmount { get; set; }

    /// <summary>
    /// Whether bomb pots are enabled.
    /// </summary>
    public bool BombPotsEnabled { get; set; }

    /// <summary>
    /// Timestamp when the table was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets players sorted by seat position.
    /// </summary>
    public IEnumerable<Player> SeatedPlayers => Players.Values
        .Where(p => p.SeatPosition > 0)
        .OrderBy(p => p.SeatPosition);

    /// <summary>
    /// Gets the count of seated players.
    /// </summary>
    public int SeatedPlayerCount => Players.Values.Count(p => p.SeatPosition > 0);

    /// <summary>
    /// Checks if a seat is available.
    /// </summary>
    public bool IsSeatAvailable(int position) =>
        position >= 1 && position <= 10 &&
        !Players.Values.Any(p => p.SeatPosition == position);

    /// <summary>
    /// Gets available seat positions.
    /// </summary>
    public IEnumerable<int> AvailableSeats => Enumerable.Range(1, 10)
        .Where(IsSeatAvailable);

    /// <summary>
    /// Checks if a display name is banned.
    /// </summary>
    public bool IsNameBanned(string displayName) =>
        BannedNames.Contains(displayName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if a display name is already taken at this table.
    /// </summary>
    public bool IsNameTaken(string displayName) =>
        Players.Values.Any(p =>
            p.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the next button position (skipping empty seats).
    /// </summary>
    public int GetNextButtonPosition()
    {
        var seatedPositions = SeatedPlayers.Select(p => p.SeatPosition).OrderBy(p => p).ToList();
        if (seatedPositions.Count == 0) return 1;

        var currentIndex = seatedPositions.IndexOf(ButtonPosition);
        var nextIndex = (currentIndex + 1) % seatedPositions.Count;
        return seatedPositions[nextIndex];
    }

    /// <summary>
    /// Gets the player at a specific seat position.
    /// </summary>
    public Player? GetPlayerAtSeat(int position) =>
        Players.Values.FirstOrDefault(p => p.SeatPosition == position);

    /// <summary>
    /// Creates a new table with default settings.
    /// </summary>
    public static Table Create(string name, Guid hostId, string hostDisplayName, decimal smallBlind, decimal bigBlind)
    {
        var table = new Table
        {
            Name = name,
            HostId = hostId,
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            MinBuyIn = bigBlind * 20,  // Default min: 20 BB
            MaxBuyIn = bigBlind * 200  // Default max: 200 BB
        };

        // Add host as first player
        var host = Player.Create(hostId, hostDisplayName, 0, 0, isHost: true);
        table.Players[hostId] = host;

        return table;
    }
}

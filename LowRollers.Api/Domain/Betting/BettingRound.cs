using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Betting;

/// <summary>
/// Tracks the state of a single betting round within a hand.
/// </summary>
public sealed class BettingRound
{
    private readonly Dictionary<Guid, decimal> _playerBets = new();
    private readonly List<PlayerAction> _actions = [];

    /// <summary>
    /// The current bet amount that players must match to stay in the hand.
    /// </summary>
    public decimal CurrentBet { get; private set; }

    /// <summary>
    /// The size of the last raise (used to calculate minimum raise).
    /// </summary>
    public decimal LastRaiseAmount { get; private set; }

    /// <summary>
    /// The minimum allowed raise amount (equals LastRaiseAmount or big blind).
    /// </summary>
    public decimal MinimumRaise { get; private set; }

    /// <summary>
    /// Number of raises in this betting round.
    /// </summary>
    public int RaiseCount { get; private set; }

    /// <summary>
    /// The player who last raised (for tracking aggressor).
    /// </summary>
    public Guid? LastAggressorId { get; private set; }

    /// <summary>
    /// All actions taken in this round.
    /// </summary>
    public IReadOnlyList<PlayerAction> Actions => _actions.AsReadOnly();

    /// <summary>
    /// Gets the amount a specific player has bet in this round.
    /// </summary>
    public decimal GetPlayerBet(Guid playerId)
        => _playerBets.GetValueOrDefault(playerId, 0);

    /// <summary>
    /// Gets all player bets in this round.
    /// </summary>
    public IReadOnlyDictionary<Guid, decimal> PlayerBets => _playerBets;

    /// <summary>
    /// Creates a new betting round with the given minimum raise (typically big blind).
    /// </summary>
    public static BettingRound Create(decimal minimumRaise)
    {
        return new BettingRound
        {
            MinimumRaise = minimumRaise,
            LastRaiseAmount = minimumRaise
        };
    }

    /// <summary>
    /// Creates a preflop betting round with blinds already posted.
    /// </summary>
    public static BettingRound CreatePreflop(
        decimal smallBlind,
        decimal bigBlind,
        Guid smallBlindPlayerId,
        Guid bigBlindPlayerId)
    {
        var round = new BettingRound
        {
            CurrentBet = bigBlind,
            MinimumRaise = bigBlind,
            LastRaiseAmount = bigBlind
        };

        // Record blind posts
        round._playerBets[smallBlindPlayerId] = smallBlind;
        round._playerBets[bigBlindPlayerId] = bigBlind;

        return round;
    }

    /// <summary>
    /// Records a fold action.
    /// </summary>
    public void RecordFold(Guid playerId)
    {
        var action = PlayerAction.Fold(playerId);
        _actions.Add(action);
    }

    /// <summary>
    /// Records a check action.
    /// </summary>
    public void RecordCheck(Guid playerId)
    {
        var action = PlayerAction.Check(playerId);
        _actions.Add(action);
    }

    /// <summary>
    /// Records a call action and updates player's bet.
    /// </summary>
    /// <param name="playerId">The player calling.</param>
    /// <param name="amountToCall">The amount being added to match the current bet.</param>
    public void RecordCall(Guid playerId, decimal amountToCall)
    {
        var currentPlayerBet = GetPlayerBet(playerId);
        var newTotal = currentPlayerBet + amountToCall;

        _playerBets[playerId] = newTotal;
        _actions.Add(PlayerAction.Call(playerId, amountToCall));
    }

    /// <summary>
    /// Records a raise action and updates betting state.
    /// </summary>
    /// <param name="playerId">The player raising.</param>
    /// <param name="totalBetAmount">The total amount the player is betting (not just the raise increment).</param>
    public void RecordRaise(Guid playerId, decimal totalBetAmount)
    {
        var currentPlayerBet = GetPlayerBet(playerId);
        var raiseAmount = totalBetAmount - CurrentBet;

        _playerBets[playerId] = totalBetAmount;
        CurrentBet = totalBetAmount;
        LastRaiseAmount = raiseAmount;
        MinimumRaise = raiseAmount;
        RaiseCount++;
        LastAggressorId = playerId;

        _actions.Add(PlayerAction.Raise(playerId, totalBetAmount));
    }

    /// <summary>
    /// Records an all-in action.
    /// </summary>
    /// <param name="playerId">The player going all-in.</param>
    /// <param name="allInAmount">The total amount the player is betting.</param>
    /// <param name="isRaise">Whether this all-in constitutes a raise.</param>
    public void RecordAllIn(Guid playerId, decimal allInAmount, bool isRaise)
    {
        var currentPlayerBet = GetPlayerBet(playerId);
        var totalBet = currentPlayerBet + allInAmount;

        _playerBets[playerId] = totalBet;

        if (isRaise && totalBet > CurrentBet)
        {
            var raiseAmount = totalBet - CurrentBet;

            // Only update minimum raise if it's a full raise
            if (raiseAmount >= MinimumRaise)
            {
                LastRaiseAmount = raiseAmount;
                MinimumRaise = raiseAmount;
            }

            CurrentBet = totalBet;
            RaiseCount++;
            LastAggressorId = playerId;
        }

        _actions.Add(PlayerAction.AllIn(playerId, allInAmount));
    }

    /// <summary>
    /// Gets the amount a player needs to call to match the current bet.
    /// </summary>
    public decimal GetAmountToCall(Guid playerId)
    {
        var playerBet = GetPlayerBet(playerId);
        return Math.Max(0, CurrentBet - playerBet);
    }

    /// <summary>
    /// Gets the minimum total bet for a valid raise.
    /// </summary>
    public decimal GetMinimumRaiseTotal()
    {
        return CurrentBet + MinimumRaise;
    }

    /// <summary>
    /// Resets the round for a new betting street (flop, turn, river).
    /// </summary>
    public void Reset(decimal minimumRaise)
    {
        _playerBets.Clear();
        _actions.Clear();
        CurrentBet = 0;
        LastRaiseAmount = minimumRaise;
        MinimumRaise = minimumRaise;
        RaiseCount = 0;
        // Note: LastAggressorId is preserved across streets for showdown order
    }
}

using LowRollers.Api.Domain.Models;

namespace LowRollers.Api.Domain.Betting;

/// <summary>
/// Validates player actions according to poker betting rules.
/// </summary>
public sealed class ActionValidator
{
    /// <summary>
    /// Validates a fold action.
    /// Fold is always valid when it's the player's turn.
    /// </summary>
    public ActionValidationResult ValidateFold(
        Player player,
        BettingRound round,
        bool isPlayersTurn)
    {
        if (!isPlayersTurn)
        {
            return ActionValidationResult.Invalid("It's not your turn to act.");
        }

        if (!player.CanAct)
        {
            return ActionValidationResult.Invalid("You cannot act in your current state.");
        }

        return ActionValidationResult.Valid(new ValidatedAction
        {
            Type = PlayerActionType.Fold,
            PlayerId = player.Id,
            Amount = 0,
            NewTotalBet = round.GetPlayerBet(player.Id),
            IsRaise = false,
            RemainingStack = player.ChipStack
        });
    }

    /// <summary>
    /// Validates a check action.
    /// Check is only valid when there's no bet facing the player.
    /// </summary>
    public ActionValidationResult ValidateCheck(
        Player player,
        BettingRound round,
        bool isPlayersTurn)
    {
        if (!isPlayersTurn)
        {
            return ActionValidationResult.Invalid("It's not your turn to act.");
        }

        if (!player.CanAct)
        {
            return ActionValidationResult.Invalid("You cannot act in your current state.");
        }

        var amountToCall = round.GetAmountToCall(player.Id);
        if (amountToCall > 0)
        {
            return ActionValidationResult.Invalid(
                $"You cannot check. You must call {amountToCall:C} or fold.");
        }

        return ActionValidationResult.Valid(new ValidatedAction
        {
            Type = PlayerActionType.Check,
            PlayerId = player.Id,
            Amount = 0,
            NewTotalBet = round.GetPlayerBet(player.Id),
            IsRaise = false,
            RemainingStack = player.ChipStack
        });
    }

    /// <summary>
    /// Validates a call action.
    /// Call is only valid when there's a bet to call.
    /// </summary>
    public ActionValidationResult ValidateCall(
        Player player,
        BettingRound round,
        bool isPlayersTurn)
    {
        if (!isPlayersTurn)
        {
            return ActionValidationResult.Invalid("It's not your turn to act.");
        }

        if (!player.CanAct)
        {
            return ActionValidationResult.Invalid("You cannot act in your current state.");
        }

        var amountToCall = round.GetAmountToCall(player.Id);
        if (amountToCall <= 0)
        {
            return ActionValidationResult.Invalid("There is nothing to call. You can check instead.");
        }

        // If player doesn't have enough to call, they go all-in
        var actualCallAmount = Math.Min(amountToCall, player.ChipStack);
        var isAllIn = actualCallAmount >= player.ChipStack;

        if (isAllIn)
        {
            // Redirect to all-in validation for proper handling
            return ValidateAllIn(player, round, isPlayersTurn);
        }

        var currentBet = round.GetPlayerBet(player.Id);
        return ActionValidationResult.Valid(new ValidatedAction
        {
            Type = PlayerActionType.Call,
            PlayerId = player.Id,
            Amount = actualCallAmount,
            NewTotalBet = currentBet + actualCallAmount,
            IsRaise = false,
            RemainingStack = player.ChipStack - actualCallAmount
        });
    }

    /// <summary>
    /// Validates a raise action.
    /// Raise must be at least the minimum raise amount.
    /// </summary>
    public ActionValidationResult ValidateRaise(
        Player player,
        BettingRound round,
        decimal raiseToAmount,
        bool isPlayersTurn)
    {
        if (!isPlayersTurn)
        {
            return ActionValidationResult.Invalid("It's not your turn to act.");
        }

        if (!player.CanAct)
        {
            return ActionValidationResult.Invalid("You cannot act in your current state.");
        }

        var currentPlayerBet = round.GetPlayerBet(player.Id);
        var additionalAmount = raiseToAmount - currentPlayerBet;

        // Check if player has enough chips
        if (additionalAmount > player.ChipStack)
        {
            return ActionValidationResult.Invalid(
                $"You don't have enough chips. You have {player.ChipStack:C} but need {additionalAmount:C}.");
        }

        // Check minimum raise
        var minimumRaiseTotal = round.GetMinimumRaiseTotal();
        if (raiseToAmount < minimumRaiseTotal)
        {
            // If player is going all-in, it's allowed even if less than min raise
            if (additionalAmount == player.ChipStack)
            {
                return ValidateAllIn(player, round, isPlayersTurn);
            }

            return ActionValidationResult.Invalid(
                $"Minimum raise is to {minimumRaiseTotal:C}. You attempted to raise to {raiseToAmount:C}.");
        }

        // Check if this would put player all-in
        if (additionalAmount == player.ChipStack)
        {
            return ActionValidationResult.Valid(new ValidatedAction
            {
                Type = PlayerActionType.AllIn,
                PlayerId = player.Id,
                Amount = additionalAmount,
                NewTotalBet = raiseToAmount,
                IsRaise = true,
                RemainingStack = 0
            });
        }

        return ActionValidationResult.Valid(new ValidatedAction
        {
            Type = PlayerActionType.Raise,
            PlayerId = player.Id,
            Amount = additionalAmount,
            NewTotalBet = raiseToAmount,
            IsRaise = true,
            RemainingStack = player.ChipStack - additionalAmount
        });
    }

    /// <summary>
    /// Validates an all-in action.
    /// All-in is always valid when it's the player's turn.
    /// </summary>
    public ActionValidationResult ValidateAllIn(
        Player player,
        BettingRound round,
        bool isPlayersTurn)
    {
        if (!isPlayersTurn)
        {
            return ActionValidationResult.Invalid("It's not your turn to act.");
        }

        if (!player.CanAct)
        {
            return ActionValidationResult.Invalid("You cannot act in your current state.");
        }

        if (player.ChipStack <= 0)
        {
            return ActionValidationResult.Invalid("You have no chips to go all-in with.");
        }

        var currentPlayerBet = round.GetPlayerBet(player.Id);
        var newTotalBet = currentPlayerBet + player.ChipStack;

        // Determine if this constitutes a raise
        var isRaise = newTotalBet > round.CurrentBet;

        return ActionValidationResult.Valid(new ValidatedAction
        {
            Type = PlayerActionType.AllIn,
            PlayerId = player.Id,
            Amount = player.ChipStack,
            NewTotalBet = newTotalBet,
            IsRaise = isRaise,
            RemainingStack = 0
        });
    }

    /// <summary>
    /// Validates any action type.
    /// </summary>
    public ActionValidationResult Validate(
        Player player,
        BettingRound round,
        PlayerActionType actionType,
        decimal amount,
        bool isPlayersTurn)
    {
        return actionType switch
        {
            PlayerActionType.Fold => ValidateFold(player, round, isPlayersTurn),
            PlayerActionType.Check => ValidateCheck(player, round, isPlayersTurn),
            PlayerActionType.Call => ValidateCall(player, round, isPlayersTurn),
            PlayerActionType.Raise => ValidateRaise(player, round, amount, isPlayersTurn),
            PlayerActionType.AllIn => ValidateAllIn(player, round, isPlayersTurn),
            _ => ActionValidationResult.Invalid($"Unknown action type: {actionType}")
        };
    }

    /// <summary>
    /// Gets the available actions for a player in the current state.
    /// </summary>
    public AvailableActions GetAvailableActions(
        Player player,
        BettingRound round,
        bool isPlayersTurn)
    {
        if (!isPlayersTurn || !player.CanAct)
        {
            return AvailableActions.None;
        }

        var amountToCall = round.GetAmountToCall(player.Id);
        var canCheck = amountToCall <= 0;
        var canCall = amountToCall > 0 && player.ChipStack > 0;
        var minRaiseTotal = round.GetMinimumRaiseTotal();
        var currentPlayerBet = round.GetPlayerBet(player.Id);
        var canRaise = player.ChipStack > (minRaiseTotal - currentPlayerBet);

        return new AvailableActions
        {
            CanFold = true,
            CanCheck = canCheck,
            CanCall = canCall,
            CallAmount = canCall ? Math.Min(amountToCall, player.ChipStack) : 0,
            CanRaise = canRaise,
            MinRaiseTotal = minRaiseTotal,
            MaxRaiseTotal = currentPlayerBet + player.ChipStack,
            CanAllIn = player.ChipStack > 0,
            AllInAmount = player.ChipStack
        };
    }
}

/// <summary>
/// Represents the actions available to a player.
/// </summary>
public sealed record AvailableActions
{
    public bool CanFold { get; init; }
    public bool CanCheck { get; init; }
    public bool CanCall { get; init; }
    public decimal CallAmount { get; init; }
    public bool CanRaise { get; init; }
    public decimal MinRaiseTotal { get; init; }
    public decimal MaxRaiseTotal { get; init; }
    public bool CanAllIn { get; init; }
    public decimal AllInAmount { get; init; }

    public static AvailableActions None => new()
    {
        CanFold = false,
        CanCheck = false,
        CanCall = false,
        CanRaise = false,
        CanAllIn = false
    };
}

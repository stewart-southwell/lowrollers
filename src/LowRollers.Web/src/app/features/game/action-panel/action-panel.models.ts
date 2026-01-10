/**
 * Action panel models and interfaces.
 */

// Import and re-export shared formatting utilities
import {
  formatCurrency as _formatCurrency,
  formatTime as _formatTime,
} from '../../../shared/utils/format.utils';
export const formatCurrency = _formatCurrency;
export const formatTime = _formatTime;

/**
 * Player action types available in Texas Hold'em.
 */
export type PlayerActionType = 'fold' | 'check' | 'call' | 'raise' | 'all-in';

/**
 * Represents the current player's state for the action panel.
 */
export interface CurrentPlayerState {
  /** Player ID */
  id: string;
  /** Display name */
  name: string;
  /** Emoji avatar */
  avatar: string;
  /** Current chip stack */
  chips: number;
  /** Whether it's this player's turn to act */
  isCurrentTurn: boolean;
  /** Remaining time in seconds (for action timer) */
  remainingTime?: number;
  /** Total action time in seconds */
  totalTime?: number;
  /** Whether time bank is available */
  hasTimeBank?: boolean;
  /** Time bank seconds remaining */
  timeBankRemaining?: number;
}

/**
 * Betting context for determining available actions.
 */
export interface BettingContext {
  /** Current bet amount to call (0 if no bet) */
  currentBet: number;
  /** Player's current contribution to the pot this round */
  playerContribution: number;
  /** Minimum raise amount */
  minRaise: number;
  /** Maximum raise amount (player's remaining chips) */
  maxRaise: number;
  /** Big blind amount (for quick bet calculations) */
  bigBlind: number;
  /** Current pot size (for pot-sized bets) */
  potSize: number;
}

/**
 * Quick bet preset button configuration.
 */
export interface QuickBetPreset {
  /** Display label */
  label: string;
  /** Type of calculation */
  type: 'bb' | 'pot' | 'max';
  /** Multiplier (for BB or pot fraction) */
  multiplier?: number;
}

/**
 * Default quick bet presets.
 */
export const DEFAULT_QUICK_BET_PRESETS: QuickBetPreset[] = [
  { label: '2BB', type: 'bb', multiplier: 2 },
  { label: '3BB', type: 'bb', multiplier: 3 },
  { label: '1/2 Pot', type: 'pot', multiplier: 0.5 },
  { label: 'POT', type: 'pot', multiplier: 1 },
  { label: '2x Pot', type: 'pot', multiplier: 2 },
];

/**
 * Action button configuration.
 */
export interface ActionButtonConfig {
  /** Action type */
  action: PlayerActionType;
  /** Display label */
  label: string;
  /** Amount (for call/raise) */
  amount?: number;
  /** Keyboard hotkey */
  hotkey: string;
  /** Whether button is disabled */
  disabled: boolean;
  /** CSS class for styling */
  cssClass: string;
}

/**
 * Event emitted when a player action is taken.
 */
export interface PlayerActionEvent {
  /** Action type */
  action: PlayerActionType;
  /** Amount (for raise) */
  amount?: number;
}

/**
 * Calculate the amount to call.
 * Returns 0 if player has already matched or exceeded the current bet.
 */
export function calculateCallAmount(context: BettingContext): number {
  const toCall = context.currentBet - context.playerContribution;
  // Defensive: prevent negative values if state is inconsistent
  return Math.max(0, Math.min(toCall, context.maxRaise));
}

/**
 * Calculate whether player can check (no bet to call).
 */
export function canCheck(context: BettingContext): boolean {
  return context.currentBet === 0 || context.playerContribution >= context.currentBet;
}

/**
 * Calculate whether player can raise.
 * Player can raise if they have chips beyond what's needed to call
 * and can meet the minimum raise requirement.
 */
export function canRaise(context: BettingContext): boolean {
  const callAmount = calculateCallAmount(context);
  return context.maxRaise > callAmount && context.maxRaise >= context.minRaise;
}

/**
 * Check if an amount represents an all-in bet.
 * Useful for showing "ALL-IN" label and triggering confirmation dialog.
 */
export function isAllInAmount(amount: number, context: BettingContext): boolean {
  return amount >= context.maxRaise;
}

/**
 * Calculate quick bet amount based on preset type.
 *
 * For BB presets: "Raise to X big blinds" - most intuitive for preflop raises.
 * The result is clamped between minRaise and maxRaise.
 *
 * For pot presets: Standard pot-fraction calculation.
 */
export function calculateQuickBetAmount(preset: QuickBetPreset, context: BettingContext): number {
  switch (preset.type) {
    case 'bb': {
      // "Raise to X BB" - the target total bet amount
      const targetAmount = context.bigBlind * (preset.multiplier ?? 1);
      // Ensure we meet minimum raise but don't exceed max
      return Math.min(Math.max(targetAmount, context.minRaise), context.maxRaise);
    }
    case 'pot':
      return Math.min(context.potSize * (preset.multiplier ?? 1), context.maxRaise);
    case 'max':
      return context.maxRaise;
    default:
      return context.minRaise;
  }
}

/**
 * Validate a raise amount.
 */
export function validateRaiseAmount(
  amount: number,
  context: BettingContext
): { valid: boolean; error?: string } {
  if (amount < context.minRaise) {
    return { valid: false, error: `Minimum raise is ${formatCurrency(context.minRaise)}` };
  }
  if (amount > context.maxRaise) {
    return { valid: false, error: `Maximum raise is ${formatCurrency(context.maxRaise)}` };
  }
  return { valid: true };
}

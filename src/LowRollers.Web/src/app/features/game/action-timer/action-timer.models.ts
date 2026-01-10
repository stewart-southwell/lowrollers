/**
 * Action Timer Models
 *
 * TypeScript interfaces matching the backend SignalR timer messages
 * and local timer state management.
 */

// Re-export shared formatting utilities
export { formatTime } from '../../../shared/utils/format.utils';

/**
 * Timer color states matching the design spec:
 * - 'safe': Green (>50% time remaining)
 * - 'warning': Yellow (≤50% time remaining)
 * - 'danger': Orange (≤25% time remaining)
 * - 'critical': Red + pulse (≤10% time remaining)
 */
export type TimerColorState = 'safe' | 'warning' | 'danger' | 'critical';

/**
 * Color constants for timer states.
 * Components can import this for consistent styling.
 */
export const TIMER_COLORS: Record<TimerColorState, string> = {
  safe: '#22c55e', // Green
  warning: '#eab308', // Yellow
  danger: '#f97316', // Orange
  critical: '#dc2626', // Red
};

/**
 * Timer state for a player's action turn.
 * Managed by ActionTimerService and consumed by components.
 */
export interface ActionTimerState {
  /** The player whose turn it is */
  playerId: string;
  /** Remaining time in seconds on the main action timer */
  remainingSeconds: number;
  /** Total action time allowed in seconds */
  totalSeconds: number;
  /** Whether the time bank is currently active */
  isTimeBankActive: boolean;
  /** Remaining time bank seconds */
  timeBankRemaining: number;
  /** Whether time bank is available for this player */
  hasTimeBank: boolean;
  /** Current color state based on time remaining */
  colorState: TimerColorState;
  /** Whether the timer has expired */
  isExpired: boolean;
}

/**
 * SignalR message: Timer started for a player.
 */
export interface TimerStartedMessage {
  playerId: string;
  totalSeconds: number;
  timeBankAvailable: number;
}

/**
 * SignalR message: Timer tick (sent every second).
 */
export interface TimerTickMessage {
  playerId: string;
  remainingSeconds: number;
  isTimeBankActive: boolean;
  timeBankRemaining: number;
}

/**
 * SignalR message: Timer warning (≤10 seconds remaining).
 */
export interface TimerWarningMessage {
  playerId: string;
  remainingSeconds: number;
}

/**
 * SignalR message: Timer cancelled (player acted).
 */
export interface TimerCancelledMessage {
  playerId: string;
}

/**
 * SignalR message: Time bank activated.
 */
export interface TimeBankActivatedMessage {
  playerId: string;
  timeBankSecondsAdded: number;
  timeBankRemaining: number;
}

/**
 * SignalR message: Timer expired (auto-fold triggered).
 */
export interface TimerExpiredMessage {
  playerId: string;
}

/**
 * Configuration for the action timer display.
 */
export interface ActionTimerConfig {
  /** Threshold percentage for warning state (default: 50) - ≤50% → yellow */
  warningThreshold: number;
  /** Threshold percentage for danger state (default: 25) - ≤25% → orange */
  dangerThreshold: number;
  /** Threshold percentage for critical state (default: 10) - ≤10% → red + pulse */
  criticalThreshold: number;
  /** Default total action time in seconds (default: 30) */
  defaultTotalSeconds: number;
}

/** Default timer configuration */
export const DEFAULT_TIMER_CONFIG: ActionTimerConfig = {
  warningThreshold: 50,
  dangerThreshold: 25,
  criticalThreshold: 10,
  defaultTotalSeconds: 30,
};

/**
 * Calculate timer percentage (0-100).
 * @param remaining - Remaining seconds
 * @param total - Total seconds
 * @returns Percentage value (0-100)
 */
export function calculateTimerPercentage(remaining: number, total: number): number {
  if (total <= 0) return 0;
  return Math.max(0, Math.min(100, (remaining / total) * 100));
}

/**
 * Get timer color state based on remaining percentage.
 * Color transitions: green → yellow → orange → red
 * @param percentage - Remaining time percentage (0-100)
 * @param config - Timer configuration
 * @returns Color state: 'safe', 'warning', 'danger', or 'critical'
 */
export function getTimerColorState(
  percentage: number,
  config: ActionTimerConfig = DEFAULT_TIMER_CONFIG
): TimerColorState {
  if (percentage <= config.criticalThreshold) return 'critical';
  if (percentage <= config.dangerThreshold) return 'danger';
  if (percentage <= config.warningThreshold) return 'warning';
  return 'safe';
}

/**
 * Get timer color state from remaining seconds and total.
 * @param remaining - Remaining seconds
 * @param total - Total seconds
 * @param config - Timer configuration
 * @returns Color state: 'safe', 'warning', 'danger', or 'critical'
 */
export function getTimerColorStateFromSeconds(
  remaining: number,
  total: number,
  config: ActionTimerConfig = DEFAULT_TIMER_CONFIG
): TimerColorState {
  const percentage = calculateTimerPercentage(remaining, total);
  return getTimerColorState(percentage, config);
}

/**
 * Create timer state from TimerStarted message.
 */
export function createTimerStateFromStart(
  message: TimerStartedMessage,
  config: ActionTimerConfig = DEFAULT_TIMER_CONFIG
): ActionTimerState {
  return {
    playerId: message.playerId,
    remainingSeconds: message.totalSeconds,
    totalSeconds: message.totalSeconds,
    isTimeBankActive: false,
    timeBankRemaining: message.timeBankAvailable,
    hasTimeBank: message.timeBankAvailable > 0,
    colorState: getTimerColorStateFromSeconds(message.totalSeconds, message.totalSeconds, config),
    isExpired: false,
  };
}

/**
 * Update timer state from TimerTick message.
 */
export function updateTimerStateFromTick(
  state: ActionTimerState,
  message: TimerTickMessage,
  config: ActionTimerConfig = DEFAULT_TIMER_CONFIG
): ActionTimerState {
  return {
    ...state,
    remainingSeconds: message.remainingSeconds,
    isTimeBankActive: message.isTimeBankActive,
    timeBankRemaining: message.timeBankRemaining,
    colorState: getTimerColorStateFromSeconds(message.remainingSeconds, state.totalSeconds, config),
    isExpired: false,
  };
}

/**
 * Create expired timer state from TimerExpired message.
 */
export function createExpiredTimerState(state: ActionTimerState): ActionTimerState {
  return {
    ...state,
    remainingSeconds: 0,
    isExpired: true,
    colorState: 'critical',
  };
}

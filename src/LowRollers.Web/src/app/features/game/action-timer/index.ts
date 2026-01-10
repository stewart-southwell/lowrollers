/**
 * Action Timer Module
 *
 * Provides timer display components and services for poker action timing.
 */

// Models and utilities
export {
  type TimerColorState,
  type ActionTimerState,
  type ActionTimerConfig,
  type TimerStartedMessage,
  type TimerTickMessage,
  type TimerWarningMessage,
  type TimerCancelledMessage,
  type TimeBankActivatedMessage,
  type TimerExpiredMessage,
  TIMER_COLORS,
  DEFAULT_TIMER_CONFIG,
  calculateTimerPercentage,
  getTimerColorState,
  getTimerColorStateFromSeconds,
  formatTime,
  createTimerStateFromStart,
  updateTimerStateFromTick,
  createExpiredTimerState,
} from './action-timer.models';

// Service
export { ActionTimerService } from './action-timer.service';

// Component
export {
  ActionTimerComponent,
  type ActionTimerVariant,
  type ActionTimerSize,
} from './action-timer.component';

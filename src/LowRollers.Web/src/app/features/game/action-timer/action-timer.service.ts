import { Injectable, signal, computed, DestroyRef, inject } from '@angular/core';
import {
  type ActionTimerState,
  type TimerStartedMessage,
  type TimerTickMessage,
  type TimerExpiredMessage,
  type TimeBankActivatedMessage,
  type ActionTimerConfig,
  DEFAULT_TIMER_CONFIG,
  createTimerStateFromStart,
  updateTimerStateFromTick,
  createExpiredTimerState,
  getTimerColorStateFromSeconds,
  calculateTimerPercentage,
  formatTime,
} from './action-timer.models';

/**
 * Service for managing action timer state.
 *
 * This service:
 * - Maintains reactive timer state using Angular Signals
 * - Provides methods to handle SignalR timer events
 * - Exposes computed values for timer display (percentage, color, formatted time)
 *
 * Usage:
 * - Inject into components that need timer display
 * - Call event handlers when SignalR messages are received
 * - Future: Connect to SignalR hub in a central game service
 */
@Injectable({
  providedIn: 'root',
})
export class ActionTimerService {
  private destroyRef = inject(DestroyRef);

  /** Timer configuration */
  private config: ActionTimerConfig = DEFAULT_TIMER_CONFIG;

  /** Current timer state (null when no timer active) */
  private _timerState = signal<ActionTimerState | null>(null);

  /** Client-side countdown interval */
  private countdownInterval: ReturnType<typeof setInterval> | null = null;

  // ============ Public Signals ============

  /** Current timer state (readonly) */
  readonly timerState = this._timerState.asReadonly();

  /** Whether a timer is currently active */
  readonly isActive = computed(() => this._timerState() !== null && !this._timerState()?.isExpired);

  /**
   * Effective remaining seconds for display.
   * Uses time bank seconds when time bank is active, otherwise main timer.
   */
  readonly effectiveRemainingSeconds = computed(() => {
    const state = this._timerState();
    if (!state) return 0;
    return state.isTimeBankActive ? state.timeBankRemaining : state.remainingSeconds;
  });

  /** Formatted time string for display (e.g., "0:18") */
  readonly formattedTime = computed(() => formatTime(this.effectiveRemainingSeconds()));

  /** Current timer percentage (0-100) based on effective remaining time */
  readonly percentage = computed(() => {
    const state = this._timerState();
    if (!state) return 100;
    return calculateTimerPercentage(this.effectiveRemainingSeconds(), state.totalSeconds);
  });

  /** Current color state */
  readonly colorState = computed(() => this._timerState()?.colorState ?? 'safe');

  /** Whether timer is in critical state (for pulse animation) */
  readonly isCritical = computed(() => this.colorState() === 'critical');

  /** Active player ID */
  readonly activePlayerId = computed(() => this._timerState()?.playerId ?? null);

  /** Whether time bank is active */
  readonly isTimeBankActive = computed(() => this._timerState()?.isTimeBankActive ?? false);

  /** Time bank remaining seconds */
  readonly timeBankRemaining = computed(() => this._timerState()?.timeBankRemaining ?? 0);

  constructor() {
    // Clean up interval on destroy
    this.destroyRef.onDestroy(() => this.stopCountdown());
  }

  // ============ Configuration ============

  /**
   * Update timer configuration.
   */
  setConfig(config: Partial<ActionTimerConfig>): void {
    this.config = { ...this.config, ...config };
  }

  // ============ SignalR Event Handlers ============

  /**
   * Handle TimerStarted SignalR event.
   * Called when a player's action timer begins.
   */
  onTimerStarted(message: TimerStartedMessage): void {
    const state = createTimerStateFromStart(message, this.config);
    this._timerState.set(state);
    this.startCountdown();
  }

  /**
   * Handle TimerTick SignalR event.
   * Called every second by the server to sync timer state.
   */
  onTimerTick(message: TimerTickMessage): void {
    const currentState = this._timerState();
    if (!currentState || currentState.playerId !== message.playerId) return;

    const newState = updateTimerStateFromTick(currentState, message, this.config);
    this._timerState.set(newState);
  }

  /**
   * Handle TimerExpired SignalR event.
   * Called when the timer expires and player will be auto-folded.
   */
  onTimerExpired(message: TimerExpiredMessage): void {
    const currentState = this._timerState();
    if (!currentState || currentState.playerId !== message.playerId) return;

    this._timerState.set(createExpiredTimerState(currentState));
    this.stopCountdown();
  }

  /**
   * Handle TimerCancelled SignalR event.
   * Called when player acts before timer expires.
   */
  onTimerCancelled(playerId: string): void {
    const currentState = this._timerState();
    if (!currentState || currentState.playerId !== playerId) return;

    this._timerState.set(null);
    this.stopCountdown();
  }

  /**
   * Handle TimeBankActivated SignalR event.
   * Called when main timer expires and time bank kicks in.
   */
  onTimeBankActivated(message: TimeBankActivatedMessage): void {
    const currentState = this._timerState();
    if (!currentState || currentState.playerId !== message.playerId) return;

    this._timerState.set({
      ...currentState,
      isTimeBankActive: true,
      timeBankRemaining: message.timeBankRemaining,
      remainingSeconds: 0,
      colorState: getTimerColorStateFromSeconds(
        message.timeBankRemaining,
        currentState.totalSeconds,
        this.config
      ),
    });
  }

  // ============ Client-Side Countdown ============

  /**
   * Start client-side countdown between server ticks.
   * Provides smooth visual updates while server sends authoritative ticks.
   */
  private startCountdown(): void {
    this.stopCountdown();

    this.countdownInterval = setInterval(() => {
      const state = this._timerState();
      if (!state || state.isExpired) {
        this.stopCountdown();
        return;
      }

      // Calculate new values based on which timer is active
      let newRemainingSeconds = state.remainingSeconds;
      let newTimeBankRemaining = state.timeBankRemaining;

      if (state.isTimeBankActive) {
        // Time bank is active - decrement time bank
        newTimeBankRemaining = Math.max(0, state.timeBankRemaining - 1);
      } else {
        // Main timer is active - decrement remaining seconds
        newRemainingSeconds = Math.max(0, state.remainingSeconds - 1);
      }

      // Determine effective remaining for color calculation
      const effectiveRemaining = state.isTimeBankActive
        ? newTimeBankRemaining
        : newRemainingSeconds;

      // Check for expiration (don't set expired locally - wait for server)
      if (effectiveRemaining <= 0 && (!state.hasTimeBank || state.isTimeBankActive)) {
        return;
      }

      this._timerState.set({
        ...state,
        remainingSeconds: newRemainingSeconds,
        timeBankRemaining: newTimeBankRemaining,
        colorState: getTimerColorStateFromSeconds(
          effectiveRemaining,
          state.totalSeconds,
          this.config
        ),
      });
    }, 1000);
  }

  /**
   * Stop client-side countdown.
   */
  private stopCountdown(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
  }

  // ============ Demo/Testing Methods ============

  /**
   * Start a demo timer for testing.
   * @param playerId - Player ID
   * @param totalSeconds - Total action time
   * @param timeBankAvailable - Time bank seconds available
   */
  startDemoTimer(playerId: string, totalSeconds = 30, timeBankAvailable = 0): void {
    this.onTimerStarted({
      playerId,
      totalSeconds,
      timeBankAvailable,
    });
  }

  /**
   * Stop any active timer.
   */
  stopTimer(): void {
    const state = this._timerState();
    if (state) {
      this.onTimerCancelled(state.playerId);
    }
  }
}

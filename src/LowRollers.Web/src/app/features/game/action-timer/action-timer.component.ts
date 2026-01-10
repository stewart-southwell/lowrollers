import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import {
  TIMER_COLORS,
  calculateTimerPercentage,
  getTimerColorStateFromSeconds,
  formatTime,
} from './action-timer.models';

/**
 * Display variant for the action timer.
 * - 'bar': Compact progress bar with time text (used on player avatar)
 * - 'panel': Full timer display with icon and label (used in action panel)
 */
export type ActionTimerVariant = 'bar' | 'panel';

/**
 * Size variant for the timer.
 * - 'sm': Small (for player avatar)
 * - 'md': Medium (default)
 * - 'lg': Large (for action panel)
 */
export type ActionTimerSize = 'sm' | 'md' | 'lg';

/**
 * Action timer component for displaying a player's remaining action time.
 *
 * Features:
 * - Custom CSS progress bar with smooth color transitions
 * - Color states: safe (green) → warning (yellow) → danger (orange) → critical (red)
 * - Pulse animation in critical state
 * - Time bank indicator when time bank is active
 * - Two variants: inline bar for player-info card, full panel for action panel
 * - Configurable label and digital display options
 * - Accessible with ARIA attributes
 * - CSS custom properties for theming flexibility
 *
 * CSS Custom Properties:
 * - --timer-track-height: Height of the progress bar track (default: 4px)
 * - --timer-track-bg: Background color of the bar track (default: rgba(255,255,255,0.2))
 * - --timer-track-radius: Border radius (default: 2px)
 * - --timer-panel-bg: Background color of the panel (default: #2a2d36)
 *
 * Usage:
 * ```html
 * <!-- Inside player-info card -->
 * <app-action-timer
 *   [remainingSeconds]="18"
 *   [totalSeconds]="30"
 *   variant="bar"
 * />
 *
 * <!-- In action panel -->
 * <app-action-timer
 *   [remainingSeconds]="18"
 *   [totalSeconds]="30"
 *   [isTimeBankActive]="false"
 *   [timeBankRemaining]="30"
 *   variant="panel"
 *   label="Your Turn"
 * />
 * ```
 */
@Component({
  selector: 'app-action-timer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (variant() === 'bar') {
      <!-- Inline bar variant (for player-info card) -->
      <div
        class="timer-bar-container"
        [class.critical]="isCritical()"
        role="timer"
        [attr.aria-valuenow]="displaySeconds()"
        [attr.aria-valuemax]="totalSeconds()"
        aria-label="Action timer"
      >
        <div class="timer-bar-track">
          <div
            class="timer-bar-fill"
            [style.width.%]="percentage()"
            [style.backgroundColor]="currentColor()"
          ></div>
        </div>
        @if (showDigital()) {
          <div class="timer-text-inline" [style.color]="currentColor()" aria-hidden="true">
            {{ formattedTime() }}
          </div>
        }
      </div>
    } @else {
      <!-- Panel variant (for action panel) -->
      <div
        class="timer-panel"
        [class.warning]="colorState() === 'warning'"
        [class.danger]="colorState() === 'danger'"
        [class.critical]="isCritical()"
        [class.size-sm]="size() === 'sm'"
        [class.size-lg]="size() === 'lg'"
        role="timer"
        [attr.aria-valuenow]="displaySeconds()"
        [attr.aria-valuemax]="totalSeconds()"
        [attr.aria-label]="label()"
      >
        <span class="turn-label">{{ label() }}</span>
        <span class="time" [style.color]="currentColor()">
          <svg class="clock-icon" viewBox="0 0 24 24" aria-hidden="true">
            <path
              d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z"
            />
          </svg>
          {{ formattedTime() }}
        </span>
        @if (isTimeBankActive()) {
          <span class="time-bank-indicator">Time Bank</span>
        } @else if (showTimeBank() && timeBankRemaining() > 0) {
          <span class="time-bank-available">+{{ timeBankRemaining() }}s bank</span>
        }
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;

        /* CSS Custom Properties for theming */
        --timer-track-height: 4px;
        --timer-track-bg: rgba(255, 255, 255, 0.2);
        --timer-track-radius: 2px;
        --timer-panel-bg: #2a2d36;
      }

      /* ============ BAR VARIANT (inline in player-info card) ============ */
      .timer-bar-container {
        width: 100%;
        margin-top: 6px;
      }

      .timer-bar-track {
        width: 100%;
        height: var(--timer-track-height);
        background: var(--timer-track-bg);
        border-radius: var(--timer-track-radius);
        overflow: hidden;
      }

      .timer-bar-fill {
        height: 100%;
        border-radius: var(--timer-track-radius);
        transition:
          width 1s linear,
          background-color 0.3s ease;
      }

      .timer-bar-container.critical .timer-bar-fill {
        animation: pulse-bar 0.5s ease-in-out infinite alternate;
      }

      @keyframes pulse-bar {
        from {
          opacity: 1;
        }
        to {
          opacity: 0.6;
        }
      }

      .timer-text-inline {
        font-size: 11px;
        font-weight: 700;
        text-align: center;
        margin-top: 3px;
        font-family: 'SF Mono', 'Monaco', 'Consolas', monospace;
        transition: color 0.3s ease;
      }

      /* ============ PANEL VARIANT ============ */
      .timer-panel {
        display: flex;
        flex-direction: column;
        align-items: center;
        background: var(--timer-panel-bg);
        padding: 8px 16px;
        border-radius: 8px;
        border-left: 3px solid #22c55e;
        transition: border-color 0.3s ease;
      }

      .timer-panel.size-sm {
        padding: 6px 12px;
      }

      .timer-panel.size-lg {
        padding: 10px 20px;
      }

      .timer-panel.warning {
        border-left-color: #eab308;
      }

      .timer-panel.danger {
        border-left-color: #f97316;
      }

      .timer-panel.critical {
        border-left-color: #dc2626;
        animation: pulse-panel 0.5s ease-in-out infinite alternate;
      }

      @keyframes pulse-panel {
        from {
          background: var(--timer-panel-bg);
        }
        to {
          background: rgba(220, 38, 38, 0.2);
        }
      }

      .turn-label {
        font-size: 10px;
        color: #888;
        text-transform: uppercase;
        letter-spacing: 1px;
      }

      .size-sm .turn-label {
        font-size: 9px;
      }

      .size-lg .turn-label {
        font-size: 11px;
      }

      .time {
        font-size: 20px;
        font-weight: 700;
        display: flex;
        align-items: center;
        gap: 5px;
        transition: color 0.3s ease;
      }

      .size-sm .time {
        font-size: 16px;
      }

      .size-lg .time {
        font-size: 24px;
      }

      .clock-icon {
        width: 16px;
        height: 16px;
        fill: currentColor;
      }

      .size-sm .clock-icon {
        width: 14px;
        height: 14px;
      }

      .size-lg .clock-icon {
        width: 20px;
        height: 20px;
      }

      .time-bank-indicator {
        font-size: 10px;
        color: #f97316;
        margin-top: 2px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }

      .time-bank-available {
        font-size: 10px;
        color: #22c55e;
        margin-top: 2px;
      }
    `,
  ],
})
export class ActionTimerComponent {
  /** Remaining time in seconds on the main action timer */
  remainingSeconds = input.required<number>();

  /** Total action time in seconds */
  totalSeconds = input.required<number>();

  /** Display variant */
  variant = input<ActionTimerVariant>('bar');

  /** Size variant */
  size = input<ActionTimerSize>('md');

  /** Whether to show digital time display (e.g., "0:18") */
  showDigital = input<boolean>(true);

  /** Label text for panel variant */
  label = input<string>('Your Turn');

  /** Whether time bank is currently active */
  isTimeBankActive = input<boolean>(false);

  /** Time bank seconds remaining */
  timeBankRemaining = input<number>(0);

  /** Whether to show time bank info (panel variant only) */
  showTimeBank = input<boolean>(true);

  /** Effective seconds to display (main timer or time bank) */
  displaySeconds = computed(() =>
    this.isTimeBankActive() ? this.timeBankRemaining() : this.remainingSeconds()
  );

  /** Calculate timer percentage (0-100) */
  percentage = computed(() => calculateTimerPercentage(this.displaySeconds(), this.totalSeconds()));

  /** Get current color state */
  colorState = computed(() =>
    getTimerColorStateFromSeconds(this.displaySeconds(), this.totalSeconds())
  );

  /** Get current color hex value */
  currentColor = computed(() => TIMER_COLORS[this.colorState()]);

  /** Get glow/shadow for the progress bar - uses white outline for visibility */
  progressGlow = computed(() => {
    return `0 0 0 2px rgba(255, 255, 255, 0.8), 0 0 8px rgba(0, 0, 0, 0.5)`;
  });

  /** Whether timer is in critical state */
  isCritical = computed(() => this.colorState() === 'critical');

  /** Format time for display */
  formattedTime = computed(() => formatTime(this.displaySeconds()));
}

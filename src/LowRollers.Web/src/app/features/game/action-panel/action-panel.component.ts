import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  computed,
  linkedSignal,
  HostListener,
  inject,
} from '@angular/core';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

import {
  type CurrentPlayerState,
  type BettingContext,
  type PlayerActionEvent,
  type QuickBetPreset,
  DEFAULT_QUICK_BET_PRESETS,
  formatCurrency,
  formatTime,
  calculateCallAmount,
  canCheck,
  canRaise,
  isAllInAmount,
  validateRaiseAmount,
} from './action-panel.models';
import { RaiseSliderComponent } from './raise-slider';

/**
 * Action panel component for poker player actions.
 *
 * Layout (v4 design):
 * - Left: player avatar, chip count, turn timer
 * - Center container:
 *   - Left buttons: Fold, Call/Check
 *   - Raise group: Raise button, slider, input, quick bet buttons
 *   - Right button: All-In
 * - Keyboard hotkeys (F/C/R/A)
 * - All-in confirmation dialog
 */
@Component({
  selector: 'app-action-panel',
  standalone: true,
  imports: [ConfirmDialogModule, RaiseSliderComponent],
  providers: [ConfirmationService],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Main Action Panel -->
    <div class="action-panel" [class.disabled]="!isActive()">
      <!-- Left Section: Player Info & Timer -->
      <div class="action-left">
        <div class="your-avatar">{{ player()?.avatar ?? '?' }}</div>
        <div class="your-info">
          <span class="label">{{ player()?.name ?? 'You' }}</span>
          <span class="chips">{{ formatCurrency(player()?.chips ?? 0) }}</span>
        </div>
        @if (isActive() && player()?.remainingTime !== undefined) {
          <div
            class="turn-timer"
            [class.warning]="timerPercentage() < 50"
            [class.danger]="timerPercentage() < 20"
          >
            <span class="turn-label">Your Turn</span>
            <span class="time">
              <svg viewBox="0 0 24 24">
                <path
                  d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z"
                />
              </svg>
              {{ formatTime(player()?.remainingTime ?? 0) }}
            </span>
            @if (player()?.hasTimeBank && player()?.timeBankRemaining) {
              <span class="time-bank">+{{ player()?.timeBankRemaining }}s bank</span>
            }
          </div>
        }
      </div>

      <!-- Rounded Action Container -->
      <div class="action-container">
        <!-- Fold & Call/Check -->
        <div class="left-buttons">
          <button
            type="button"
            class="action-btn btn-fold"
            [disabled]="!isActive()"
            (click)="onFold()"
          >
            Fold
            <span class="hotkey">[F]</span>
          </button>

          @if (canCheckComputed()) {
            <button
              type="button"
              class="action-btn btn-call"
              [disabled]="!isActive()"
              (click)="onCheck()"
            >
              Check
              <span class="hotkey">[C]</span>
            </button>
          } @else {
            <button
              type="button"
              class="action-btn btn-call"
              [disabled]="!isActive()"
              (click)="onCall()"
            >
              @if (isCallAllIn()) {
                Call All-In
              } @else {
                Call {{ formatCurrency(callAmount()) }}
              }
              <span class="hotkey">[C]</span>
            </button>
          }
        </div>

        <!-- Raise Group with Slider and Quick Bets -->
        <div class="raise-group">
          <button
            type="button"
            class="action-btn btn-raise"
            [disabled]="!isActive() || !canRaiseComputed()"
            (click)="onRaise()"
          >
            @if (isRaiseAllIn()) {
              All-In
            } @else {
              Raise
            }
            <span class="hotkey">[R]</span>
          </button>

          @if (canRaiseComputed()) {
            <app-raise-slider
              [(amount)]="raiseAmount"
              [bettingContext]="bettingContext()"
              [quickBetPresets]="quickBetPresets()"
              [showQuickBets]="showQuickBets()"
              [disabled]="!isActive()"
            />
          }
        </div>

        <!-- All-In -->
        <div class="right-buttons">
          <button
            type="button"
            class="action-btn btn-allin"
            [disabled]="!isActive()"
            (click)="onAllIn()"
          >
            All-In
            <span class="hotkey">[A]</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Confirmation Dialog -->
    <p-confirmDialog
      key="allInConfirm"
      header="Confirm All-In"
      acceptLabel="Go All-In"
      rejectLabel="Cancel"
      acceptButtonStyleClass="p-button-danger"
      [style]="{ width: '400px' }"
    />
  `,
  styles: [
    `
      :host {
        display: contents;
      }

      /* ============ ACTION PANEL ============ */
      .action-panel {
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        background: var(--bg-secondary);
        border-top: 1px solid #2a2d36;
        padding: 15px 30px;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 20px;
        z-index: var(--z-actions);
      }

      .action-panel.disabled {
        opacity: 0.6;
        pointer-events: none;
      }

      /* ============ LEFT SECTION ============ */
      .action-left {
        display: flex;
        align-items: center;
        gap: 15px;
        flex-shrink: 0;
      }

      .your-avatar {
        width: 50px;
        height: 50px;
        border-radius: var(--radius-full);
        background: linear-gradient(135deg, var(--accent-blue), #1d4ed8);
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 24px;
      }

      .your-info {
        display: flex;
        flex-direction: column;
      }

      .your-info .label {
        font-size: 13px;
        font-weight: 600;
        color: var(--text-primary);
      }

      .your-info .chips {
        font-size: 18px;
        font-weight: 700;
        color: var(--accent-yellow);
      }

      .turn-timer {
        display: flex;
        flex-direction: column;
        align-items: center;
        background: #2a2d36;
        padding: 8px 16px;
        border-radius: var(--radius-md);
        border-left: 3px solid #f4a31a;
      }

      .turn-timer.warning {
        border-left-color: #eab308;
      }

      .turn-timer.danger {
        border-left-color: var(--accent-red);
      }

      .turn-timer .turn-label {
        font-size: 10px;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 1px;
      }

      .turn-timer .time {
        font-size: 20px;
        font-weight: 700;
        color: #f4a31a;
        display: flex;
        align-items: center;
        gap: 5px;
      }

      .turn-timer.warning .time {
        color: #eab308;
      }

      .turn-timer.danger .time {
        color: var(--accent-red);
      }

      .turn-timer .time svg {
        width: 16px;
        height: 16px;
        fill: currentColor;
      }

      .turn-timer .time-bank {
        font-size: 10px;
        color: var(--accent-green);
        margin-top: 2px;
      }

      /* ============ ACTION CONTAINER ============ */
      .action-container {
        display: flex;
        align-items: center;
        background: #1e2128;
        border: 1px solid #2a2d36;
        border-radius: 12px;
        padding: 8px;
        gap: 8px;
        flex-shrink: 0;
      }

      .left-buttons {
        display: flex;
        gap: 8px;
        flex-shrink: 0;
      }

      .right-buttons {
        display: flex;
        gap: 8px;
        flex-shrink: 0;
      }

      /* ============ RAISE GROUP ============ */
      .raise-group {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      /* ============ ACTION BUTTONS ============ */
      .action-btn {
        padding: 12px 20px;
        border: none;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 700;
        cursor: pointer;
        transition: all 0.2s;
        text-transform: uppercase;
        display: flex;
        flex-direction: column;
        align-items: center;
        min-width: 90px;
      }

      .action-btn:hover:not(:disabled) {
        filter: brightness(1.1);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
      }

      .action-btn:active:not(:disabled) {
        filter: brightness(0.95);
        box-shadow: none;
      }

      .action-btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .action-btn .hotkey {
        font-size: 10px;
        opacity: 0.7;
        font-weight: 500;
        margin-top: 2px;
      }

      /* Fold - Gray */
      .btn-fold {
        background: linear-gradient(135deg, #4b5563, #374151);
        color: var(--text-primary);
        min-width: 100px;
      }

      /* Call/Check - Green */
      .btn-call {
        background: linear-gradient(135deg, #22c55e, #16a34a);
        color: var(--text-primary);
        min-width: 100px;
      }

      /* Raise - Yellow/Gold */
      .btn-raise {
        background: linear-gradient(135deg, #eab308, #ca8a04);
        color: var(--text-primary);
        min-width: 100px;
      }

      /* All-In - Red */
      .btn-allin {
        background: linear-gradient(135deg, #dc2626, #b91c1c);
        color: var(--text-primary);
        min-width: 100px;
      }

      /* ============ RESPONSIVE ============ */
      @media (max-width: 900px) {
        .action-panel {
          padding: 10px 15px;
        }

        .action-btn {
          padding: 10px 14px;
          min-width: 70px;
          font-size: 13px;
        }

        .action-container {
          padding: 6px;
          gap: 6px;
        }

        .raise-group {
          gap: 6px;
        }
      }
    `,
  ],
})
export class ActionPanelComponent {
  private confirmationService = inject(ConfirmationService);

  /** Current player state */
  player = input<CurrentPlayerState | null>(null);

  /** Betting context for available actions */
  bettingContext = input<BettingContext | null>(null);

  /** Quick bet presets to display */
  quickBetPresets = input<QuickBetPreset[]>(DEFAULT_QUICK_BET_PRESETS);

  /** Whether to show quick bet buttons */
  showQuickBets = input<boolean>(true);

  /** Emits when a player action is taken */
  actionTaken = output<PlayerActionEvent>();

  /**
   * Current raise amount.
   * Automatically resets to minRaise when bettingContext changes,
   * but can be manually overridden via .set() or quick bet buttons.
   */
  raiseAmount = linkedSignal(() => this.bettingContext()?.minRaise ?? 0);

  /** Whether player is active (their turn) */
  isActive = computed(() => this.player()?.isCurrentTurn ?? false);

  /** Timer percentage for warning colors */
  timerPercentage = computed(() => {
    const p = this.player();
    if (!p?.remainingTime || !p?.totalTime) return 100;
    return (p.remainingTime / p.totalTime) * 100;
  });

  /** Calculate call amount */
  callAmount = computed(() => {
    const ctx = this.bettingContext();
    return ctx ? calculateCallAmount(ctx) : 0;
  });

  /** Whether player can check */
  canCheckComputed = computed(() => {
    const ctx = this.bettingContext();
    return ctx ? canCheck(ctx) : false;
  });

  /** Whether player can raise */
  canRaiseComputed = computed(() => {
    const ctx = this.bettingContext();
    return ctx ? canRaise(ctx) : false;
  });

  /** Whether call would be all-in */
  isCallAllIn = computed(() => {
    const ctx = this.bettingContext();
    if (!ctx) return false;
    return this.callAmount() >= ctx.maxRaise;
  });

  /** Whether current raise amount is all-in */
  isRaiseAllIn = computed(() => {
    const ctx = this.bettingContext();
    if (!ctx) return false;
    return isAllInAmount(this.raiseAmount(), ctx);
  });

  /** Slider step based on big blind */
  sliderStep = computed(() => {
    const ctx = this.bettingContext();
    return ctx?.bigBlind ?? 1;
  });

  /** Format currency utility */
  formatCurrency = formatCurrency;

  /** Format time utility */
  formatTime = formatTime;

  /** Handle keyboard hotkeys */
  @HostListener('window:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (!this.isActive()) return;

    // Don't handle if user is typing in an input
    if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
      return;
    }

    switch (event.key.toUpperCase()) {
      case 'F':
        event.preventDefault();
        this.onFold();
        break;
      case 'C':
        event.preventDefault();
        if (this.canCheckComputed()) {
          this.onCheck();
        } else {
          this.onCall();
        }
        break;
      case 'R':
        event.preventDefault();
        if (this.canRaiseComputed()) {
          this.onRaise();
        }
        break;
      case 'A':
        event.preventDefault();
        this.onAllIn();
        break;
    }
  }

  /** Fold action */
  onFold(): void {
    this.actionTaken.emit({ action: 'fold' });
  }

  /** Check action */
  onCheck(): void {
    this.actionTaken.emit({ action: 'check' });
  }

  /** Call action */
  onCall(): void {
    const ctx = this.bettingContext();
    if (this.isCallAllIn() && ctx) {
      // All-in call requires confirmation
      this.confirmAllIn(() => {
        this.actionTaken.emit({ action: 'call', amount: this.callAmount() });
      });
    } else {
      this.actionTaken.emit({ action: 'call', amount: this.callAmount() });
    }
  }

  /** Raise action */
  onRaise(): void {
    const ctx = this.bettingContext();
    if (!ctx) return;

    const validation = validateRaiseAmount(this.raiseAmount(), ctx);
    if (!validation.valid) {
      console.warn(validation.error);
      return;
    }

    if (this.isRaiseAllIn()) {
      // All-in raise requires confirmation
      this.confirmAllIn(() => {
        this.actionTaken.emit({ action: 'raise', amount: this.raiseAmount() });
      });
    } else {
      this.actionTaken.emit({ action: 'raise', amount: this.raiseAmount() });
    }
  }

  /** All-in action */
  onAllIn(): void {
    const ctx = this.bettingContext();
    if (!ctx) return;

    this.confirmAllIn(() => {
      this.actionTaken.emit({ action: 'all-in', amount: ctx.maxRaise });
    });
  }

  /** Show all-in confirmation dialog */
  private confirmAllIn(onAccept: () => void): void {
    const ctx = this.bettingContext();
    if (!ctx) return;

    this.confirmationService.confirm({
      key: 'allInConfirm',
      message: `Are you sure you want to go all-in for ${formatCurrency(ctx.maxRaise)}?`,
      accept: onAccept,
    });
  }
}

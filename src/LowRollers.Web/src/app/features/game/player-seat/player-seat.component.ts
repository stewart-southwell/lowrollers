import { Component, ChangeDetectionStrategy, input, computed, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardComponent } from '../../../shared/card/card.component';
import {
  PlayerData,
  SeatPosition,
  ChipStack,
  getBetPositionStyle,
  amountToChipStacks,
  getChipSymbolId,
  createChipArray,
} from './player-seat.models';

/** Angles for each seat position around the ellipse (0° = right, 90° = bottom, 180° = left, 270° = top) */
const SEAT_ANGLES: Record<SeatPosition, number> = {
  right: 0,
  'bottom-right': 45,
  bottom: 90,
  'bottom-left': 135,
  left: 180,
  'top-left': 225,
  top: 270,
  'top-right': 315,
};

/**
 * Calculate seat position as percentages based on ellipse geometry.
 * @param position - Seat position identifier
 * @param tableWidthPercent - Table width as % of container (default 80)
 * @param tableAspectRatio - Table height/width ratio (default 0.5625 for 45/80)
 * @param offsetPercent - How far outside the table edge to place seats (default 5)
 */
function calculateSeatPosition(
  position: SeatPosition,
  tableWidthPercent = 80,
  tableAspectRatio = 0.5625,
  offsetPercent = 5
): Record<string, string> {
  const angle = SEAT_ANGLES[position];
  const radians = (angle * Math.PI) / 180;

  // Semi-axes: table dimensions plus offset for player position
  // Note: We use the table's visual proportions, not container percentages
  const a = tableWidthPercent / 2 + offsetPercent; // horizontal radius
  const b = (tableWidthPercent * tableAspectRatio) / 2 + offsetPercent; // vertical radius

  // Calculate position on ellipse (50% is center)
  const x = 50 + a * Math.cos(radians);
  const y = 50 + b * Math.sin(radians);

  return {
    left: `${x.toFixed(1)}%`,
    top: `${y.toFixed(1)}%`,
    transform: 'translate(-50%, -50%)',
  };
}

/** Pre-computed seat positions using ellipse calculation */
const SEAT_POSITIONS: Record<SeatPosition, Record<string, string>> = {
  top: calculateSeatPosition('top'),
  'top-right': calculateSeatPosition('top-right'),
  right: calculateSeatPosition('right'),
  'bottom-right': calculateSeatPosition('bottom-right'),
  bottom: calculateSeatPosition('bottom'),
  'bottom-left': calculateSeatPosition('bottom-left'),
  left: calculateSeatPosition('left'),
  'top-left': calculateSeatPosition('top-left'),
};

/** Cached currency formatter for performance */
const CURRENCY_FORMATTER = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});

/**
 * Player seat component displaying a player's avatar, info, cards, and bet.
 * Handles all player states: playing, folded, all-in, sitting-out, and empty seat.
 */
@Component({
  selector: 'app-player-seat',
  standalone: true,
  imports: [CommonModule, CardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="player-seat" [ngStyle]="seatPositionStyle()" [class.empty-seat]="!player()">
      @if (player(); as p) {
        <!-- Occupied seat -->
        <div
          class="player-container"
          [class.seat-right-side]="isRightSide()"
          [class.seat-left-side]="!isRightSide()"
        >
          <!-- Avatar wrapper -->
          <div class="avatar-wrapper" [class.active]="p.isCurrentTurn">
            <div
              class="player-avatar"
              [class.active]="p.isCurrentTurn"
              [class.folded]="p.status === 'folded'"
              [class.sitting-out]="p.status === 'sitting-out'"
            >
              <span class="avatar-emoji">{{ p.avatar }}</span>

              <!-- Status badges (mic/video) -->
              @if (p.hasMic || p.hasVideo) {
                <div class="status-badges">
                  @if (p.hasMic) {
                    <svg viewBox="0 0 24 24" stroke-width="2" aria-label="Microphone on">
                      <path
                        d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z"
                      />
                    </svg>
                  }
                  @if (p.hasVideo) {
                    <svg viewBox="0 0 24 24" stroke-width="2" aria-label="Video on">
                      <path
                        d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"
                      />
                    </svg>
                  }
                </div>
              }

              <!-- Action timer bar (only for current turn) -->
              @if (p.isCurrentTurn && p.remainingTime !== undefined) {
                <div class="action-timer-bar">
                  <div
                    class="timer-bar-progress"
                    [style.width.%]="timerPercentage()"
                    [class.warning]="timerPercentage() < 50"
                    [class.danger]="timerPercentage() < 20"
                  ></div>
                </div>
                <span
                  class="timer-text"
                  [class.warning]="timerPercentage() < 50"
                  [class.danger]="timerPercentage() < 20"
                >
                  {{ formatTime(p.remainingTime) }}
                </span>
              }
            </div>

            <!-- Hole cards (below avatar) -->
            @if (p.holeCards && p.holeCards.length > 0) {
              <div class="hole-cards" [class.folded-cards]="p.status === 'folded'">
                @for (card of p.holeCards; track $index) {
                  <app-card
                    [rank]="card.rank"
                    [suit]="card.suit"
                    [faceDown]="!p.holeCardsVisible"
                    [folded]="p.status === 'folded'"
                    size="mini"
                  />
                }
              </div>
            }
          </div>

          <!-- Player info card -->
          <div
            class="player-info"
            [class.active]="p.isCurrentTurn"
            [class.folded]="p.status === 'folded'"
            [class.sitting-out]="p.status === 'sitting-out'"
          >
            <div class="player-name">{{ p.name }}</div>
            <div class="player-chips">{{ formatCurrency(p.chips) }}</div>
            @if (p.status === 'folded') {
              <div class="folded-label">Folded</div>
            }
            @if (p.status === 'all-in') {
              <div class="all-in-label">All-In</div>
            }
            @if (p.status === 'sitting-out') {
              <div class="sitting-out-label">Sitting Out</div>
            }
          </div>
        </div>

        <!-- Player bet display -->
        @if (p.currentBet && p.currentBet > 0) {
          <div class="player-bet" [ngStyle]="betPositionStyle()">
            <div class="chip-stack">
              @for (stack of betChipStacks(); track stack.color) {
                <div class="chip-column">
                  @for (i of createChipArray(stack.count); track i) {
                    <div class="chip">
                      <svg viewBox="0 0 100 100">
                        <use [attr.href]="getChipSymbolId(stack.color)" />
                      </svg>
                    </div>
                  }
                </div>
              }
            </div>
            <div class="bet-amount">{{ formatCurrency(p.currentBet) }}</div>
          </div>
        }
      } @else {
        <!-- Empty seat -->
        <div class="player-container seat-right-side">
          <div class="avatar-wrapper">
            <button
              class="player-avatar empty"
              (click)="seatClick.emit(position())"
              aria-label="Take this seat"
            >
              <svg
                viewBox="0 0 24 24"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <path
                  d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"
                />
              </svg>
            </button>
          </div>
          <div class="player-info empty">
            <div class="player-name">Open Seat</div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [
    `
      /* ============ PLAYER SEAT CONTAINER ============ */
      .player-seat {
        position: absolute;
        z-index: var(--z-player);
      }

      .player-seat:has(.active) {
        z-index: var(--z-player-active);
      }

      /* Main container - horizontal flex */
      .player-container {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      /* Right side seats: info on RIGHT of avatar */
      .player-container.seat-right-side {
        flex-direction: row;
      }

      /* Left side seats: info on LEFT of avatar */
      .player-container.seat-left-side {
        flex-direction: row-reverse;
      }

      /* ============ AVATAR WRAPPER ============ */
      .avatar-wrapper {
        position: relative;
        display: flex;
        flex-direction: column;
        align-items: center;
      }

      /* Pulsating turn indicator */
      .avatar-wrapper.active::after {
        content: '';
        position: absolute;
        top: -8px;
        right: -8px;
        width: 24px;
        height: 24px;
        background: var(--accent-orange);
        border-radius: var(--radius-full);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
        animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
        z-index: 15;
      }

      /* ============ PLAYER AVATAR ============ */
      .player-avatar {
        width: 70px;
        height: 70px;
        border-radius: var(--radius-lg);
        background: linear-gradient(135deg, #6366f1, #7c3aed);
        display: flex;
        align-items: center;
        justify-content: center;
        position: relative;
        border: 3px solid var(--accent-green);
        box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4);
        overflow: visible;
      }

      .player-avatar .avatar-emoji {
        font-size: 32px;
      }

      .player-avatar.active {
        border-color: var(--accent-orange);
        box-shadow:
          0 0 0 3px var(--border-active),
          0 6px 20px rgba(0, 0, 0, 0.4);
      }

      .player-avatar.folded,
      .player-avatar.sitting-out {
        opacity: 0.4;
        border-color: #4b5563;
      }

      /* ============ STATUS BADGES ============ */
      .status-badges {
        position: absolute;
        bottom: -4px;
        left: 50%;
        transform: translateX(-50%);
        display: flex;
        gap: 2px;
        background: var(--bg-glass);
        padding: 3px 6px;
        border-radius: 10px;
      }

      .status-badges svg {
        width: 12px;
        height: 12px;
        stroke: var(--accent-green);
        fill: none;
      }

      /* ============ ACTION TIMER ============ */
      .action-timer-bar {
        position: absolute;
        bottom: -8px;
        left: 50%;
        transform: translateX(-50%);
        width: 80px;
        height: 6px;
        background: rgba(0, 0, 0, 0.5);
        border-radius: 3px;
        overflow: hidden;
        z-index: 20;
      }

      .timer-bar-progress {
        height: 100%;
        background: #22c55e;
        border-radius: 3px;
        transition:
          width var(--animation-very-slow) linear,
          background var(--animation-normal);
      }

      .timer-bar-progress.warning {
        background: #eab308;
      }

      .timer-bar-progress.danger {
        background: #ef4444;
      }

      .timer-text {
        position: absolute;
        bottom: -26px;
        left: 50%;
        transform: translateX(-50%);
        background: var(--bg-glass);
        color: #22c55e;
        font-size: 11px;
        font-weight: 700;
        padding: 2px 8px;
        border-radius: var(--radius-sm);
        font-family: 'SF Mono', 'Monaco', monospace;
        z-index: 20;
      }

      .timer-text.warning {
        color: #eab308;
      }

      .timer-text.danger {
        color: #ef4444;
      }

      /* ============ HOLE CARDS ============ */
      .hole-cards {
        display: flex;
        gap: 3px;
        margin-top: 6px;
      }

      /* ============ PLAYER INFO CARD ============ */
      .player-info {
        background: var(--bg-glass);
        border-radius: var(--radius-md);
        padding: 6px 10px;
        text-align: center;
        border: 2px solid rgba(16, 185, 129, 0.4);
        min-width: 70px;
      }

      .player-info.active {
        border-color: var(--accent-orange);
        background: rgba(249, 115, 22, 0.2);
      }

      .player-info.folded,
      .player-info.sitting-out {
        opacity: 0.5;
        border-color: #4b5563;
      }

      .player-name {
        font-size: 12px;
        font-weight: 700;
        color: var(--text-primary);
        line-height: 1.3;
      }

      .player-chips {
        font-size: 12px;
        font-weight: 700;
        color: var(--accent-green-light);
        line-height: 1.3;
      }

      /* Status labels */
      .folded-label {
        background: rgba(239, 68, 68, 0.2);
        color: var(--accent-red);
        font-size: 10px;
        font-weight: 700;
        padding: 3px 10px;
        border-radius: var(--radius-sm);
        text-transform: uppercase;
        letter-spacing: 1px;
        margin-top: 4px;
        border: 1px solid rgba(239, 68, 68, 0.3);
      }

      .all-in-label {
        background: rgba(249, 115, 22, 0.2);
        color: var(--accent-orange);
        font-size: 10px;
        font-weight: 700;
        padding: 3px 10px;
        border-radius: var(--radius-sm);
        text-transform: uppercase;
        letter-spacing: 1px;
        margin-top: 4px;
        border: 1px solid rgba(249, 115, 22, 0.3);
      }

      .sitting-out-label {
        background: rgba(107, 114, 128, 0.2);
        color: #9ca3af;
        font-size: 10px;
        font-weight: 700;
        padding: 3px 10px;
        border-radius: var(--radius-sm);
        text-transform: uppercase;
        letter-spacing: 1px;
        margin-top: 4px;
        border: 1px solid rgba(107, 114, 128, 0.3);
      }

      /* ============ PLAYER BET ============ */
      .player-bet {
        position: absolute;
        display: flex;
        align-items: center;
        gap: 6px;
      }

      .chip-stack {
        display: flex;
        align-items: flex-end;
        gap: 2px;
      }

      .chip-column {
        display: flex;
        flex-direction: column-reverse;
      }

      .chip {
        width: 22px;
        height: 22px;
        position: relative;
        margin-bottom: -14px;
      }

      .chip:first-child {
        margin-bottom: 0;
      }

      .chip svg {
        width: 100%;
        height: 100%;
        filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.4));
      }

      .bet-amount {
        background: rgba(234, 179, 8, 0.9);
        color: #000;
        padding: 3px 8px;
        border-radius: var(--radius-lg);
        font-size: 11px;
        font-weight: 700;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
        white-space: nowrap;
      }

      /* ============ EMPTY SEAT ============ */
      .empty-seat .player-avatar.empty {
        background: rgba(255, 255, 255, 0.05);
        border: 3px dashed rgba(255, 255, 255, 0.3);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
        cursor: pointer;
        transition: all var(--animation-fast);
      }

      .empty-seat .player-avatar.empty:hover {
        background: rgba(255, 255, 255, 0.1);
        border-color: rgba(255, 255, 255, 0.5);
      }

      .empty-seat .player-avatar.empty svg {
        width: 28px;
        height: 28px;
        stroke: rgba(255, 255, 255, 0.4);
        fill: none;
      }

      .empty-seat .player-info.empty {
        border-color: transparent;
        background: rgba(0, 0, 0, 0.5);
      }

      .empty-seat .player-info.empty .player-name {
        color: var(--text-muted);
        font-weight: 500;
      }
    `,
  ],
})
export class PlayerSeatComponent {
  /** Player data (null for empty seat) */
  player = input<PlayerData | null>(null);

  /** Seat position around the table */
  position = input.required<SeatPosition>();

  /** Emits when an empty seat is clicked */
  seatClick = output<SeatPosition>();

  /** Check if this is a right-side seat (info displayed on right of avatar) */
  isRightSide = computed(() => {
    const rightSideSeats: SeatPosition[] = ['right', 'top-right', 'bottom-right'];
    return rightSideSeats.includes(this.position());
  });

  /** Get seat position styles */
  seatPositionStyle = computed(() => SEAT_POSITIONS[this.position()]);

  /** Get bet position styles based on seat position */
  betPositionStyle = computed(() => getBetPositionStyle(this.position()));

  /** Convert current bet to chip stacks for display */
  betChipStacks = computed((): ChipStack[] => {
    const p = this.player();
    if (!p?.currentBet) return [];
    return amountToChipStacks(p.currentBet);
  });

  /** Calculate timer percentage */
  timerPercentage = computed(() => {
    const p = this.player();
    if (!p?.remainingTime || !p?.totalTime) return 100;
    return (p.remainingTime / p.totalTime) * 100;
  });

  /** Format currency for display */
  formatCurrency(amount: number): string {
    return CURRENCY_FORMATTER.format(amount);
  }

  /** Format time in seconds to display string */
  formatTime(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return mins > 0
      ? `${mins}:${secs.toString().padStart(2, '0')}`
      : `0:${secs.toString().padStart(2, '0')}`;
  }

  /** Shared utility - create array for ngFor iteration */
  createChipArray = createChipArray;

  /** Shared utility - get chip symbol ID for SVG href */
  getChipSymbolId = getChipSymbolId;
}

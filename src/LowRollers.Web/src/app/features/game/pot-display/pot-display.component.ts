import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  OnDestroy,
  signal,
} from '@angular/core';
import {
  getChipSymbolId,
  createChipArray,
  type ChipStack,
} from '../../../shared/models/chip.models';
import { type Pot, potAmountToChipStacks } from './pot-display.models';

/** Cached currency formatter */
const CURRENCY_FORMATTER = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});

/**
 * Pot display component showing the main pot and any side pots with chip graphics.
 *
 * Features:
 * - Chip stack visualization using SVG symbols
 * - Main pot display with label and amount
 * - Side pots displayed horizontally with eligible player count
 * - Chip addition animation when pot increases
 */
@Component({
  selector: 'app-pot-display',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (mainPot(); as pot) {
      <div class="pot-display" role="region" aria-label="Pot display">
        <!-- All pots in a row -->
        <div class="pots-row">
          <!-- Main Pot -->
          <div class="pot-container main-pot" [class.animating]="isAnimating()">
            <div class="pot-chips" aria-hidden="true">
              @for (stack of mainPotChips(); track stack.color; let colIdx = $index) {
                <div
                  class="chip-column"
                  [style.animation-delay.ms]="colIdx * 50"
                  [class.animate-in]="isAnimating()"
                >
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
            <div class="pot-info">
              <div class="pot-label">Main Pot</div>
              <div class="pot-amount">{{ formatCurrency(pot.amount) }}</div>
            </div>
          </div>

          <!-- Side Pots -->
          @for (sidePot of sidePots(); track sidePot.id; let idx = $index) {
            <div class="pot-container side-pot" [class.animating]="isAnimating()">
              <div class="pot-chips small" aria-hidden="true">
                @for (
                  stack of sidePotChipsMap().get(sidePot.id) ?? [];
                  track stack.color;
                  let colIdx = $index
                ) {
                  <div
                    class="chip-column"
                    [style.animation-delay.ms]="colIdx * 50"
                    [class.animate-in]="isAnimating()"
                  >
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
              <div class="pot-info small">
                <div class="pot-label">
                  Side {{ idx + 1 }}
                  @if (sidePot.eligiblePlayerCount) {
                    <span class="eligible-count">({{ sidePot.eligiblePlayerCount }})</span>
                  }
                </div>
                <div class="pot-amount">{{ formatCurrency(sidePot.amount) }}</div>
              </div>
            </div>
          }
        </div>

        <!-- Total (shown when side pots exist) -->
        @if (sidePots().length > 0) {
          <div class="total-display">
            <span class="total-label">Total:</span>
            <span class="total-amount">{{ formatCurrency(totalAmount()) }}</span>
          </div>
        }
      </div>
    }
  `,
  styles: [
    `
      .pot-display {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 6px;
      }

      .pots-row {
        display: flex;
        align-items: flex-end;
        gap: 16px;
      }

      .pot-container {
        display: flex;
        align-items: center;
        gap: 10px;
      }

      .pot-container.side-pot {
        transform: scale(0.8);
        opacity: 0.9;
      }

      /* ============ CHIP STACKS ============ */
      .pot-chips {
        display: flex;
        align-items: flex-end;
        gap: 3px;
      }

      .pot-chips.small {
        gap: 2px;
      }

      .chip-column {
        display: flex;
        flex-direction: column-reverse;
      }

      .chip-column.animate-in {
        animation: chipColumnAppear 0.4s ease-out forwards;
      }

      @keyframes chipColumnAppear {
        from {
          opacity: 0;
          transform: translateY(-10px) scale(0.8);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }

      .chip {
        width: 26px;
        height: 26px;
        position: relative;
        margin-bottom: -16px;
      }

      .chip:first-child {
        margin-bottom: 0;
      }

      .pot-chips.small .chip {
        width: 20px;
        height: 20px;
        margin-bottom: -12px;
      }

      .pot-chips.small .chip:first-child {
        margin-bottom: 0;
      }

      .chip svg {
        width: 100%;
        height: 100%;
        filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.4));
      }

      /* ============ POT INFO ============ */
      .pot-info {
        background: var(--bg-card, rgba(0, 0, 0, 0.6));
        backdrop-filter: blur(4px);
        -webkit-backdrop-filter: blur(4px);
        padding: 8px 20px;
        border-radius: var(--radius-md, 8px);
        text-align: center;
      }

      .pot-info.small {
        padding: 6px 12px;
      }

      .pot-label {
        font-size: 10px;
        font-weight: 600;
        color: var(--text-secondary, #9ca3af);
        text-transform: uppercase;
        letter-spacing: 1px;
      }

      .pot-info.small .pot-label {
        font-size: 9px;
        letter-spacing: 0.5px;
      }

      .eligible-count {
        font-size: 9px;
        text-transform: none;
        letter-spacing: 0;
        opacity: 0.8;
      }

      .pot-amount {
        font-size: 28px;
        font-weight: 900;
        color: var(--accent-yellow, #facc15);
        text-shadow: 0 2px 10px rgba(234, 179, 8, 0.5);
      }

      .pot-info.small .pot-amount {
        font-size: 16px;
      }

      /* ============ TOTAL DISPLAY ============ */
      .total-display {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: 4px 12px;
        background: rgba(0, 0, 0, 0.4);
        border-radius: var(--radius-sm, 4px);
      }

      .total-label {
        font-size: 10px;
        font-weight: 600;
        color: var(--text-secondary, #9ca3af);
        text-transform: uppercase;
      }

      .total-amount {
        font-size: 14px;
        font-weight: 700;
        color: var(--accent-yellow, #facc15);
      }

      /* ============ ANIMATION ============ */
      .pot-container.animating .pot-amount {
        animation: amountPulse 0.4s ease-out;
      }

      @keyframes amountPulse {
        0% {
          transform: scale(1);
        }
        50% {
          transform: scale(1.1);
          color: #fef08a;
        }
        100% {
          transform: scale(1);
        }
      }
    `,
  ],
})
export class PotDisplayComponent implements OnDestroy {
  /** Array of pots to display (main + side pots) */
  pots = input<Pot[]>([]);

  /** Whether to animate chip additions */
  animateAdditions = input<boolean>(true);

  /** Track animation state */
  isAnimating = signal(false);

  /** Animation timeout reference */
  private animationTimeout: ReturnType<typeof setTimeout> | null = null;

  /** Previous total for detecting changes */
  private previousTotal = 0;

  /** Skip animation on first load */
  private isFirstLoad = true;

  /** Main pot (first pot with type 'main') */
  mainPot = computed(() => this.pots().find((p) => p.type === 'main') ?? null);

  /** Side pots (all pots with type 'side') */
  sidePots = computed(() => this.pots().filter((p) => p.type === 'side'));

  /** Total amount across all pots */
  totalAmount = computed(() => this.pots().reduce((sum, p) => sum + p.amount, 0));

  /** Chip stacks for main pot */
  mainPotChips = computed((): ChipStack[] => {
    const pot = this.mainPot();
    return pot ? potAmountToChipStacks(pot.amount) : [];
  });

  /** Memoized chip stacks for side pots (keyed by pot id) */
  sidePotChipsMap = computed(() => {
    const map = new Map<string, ChipStack[]>();
    for (const pot of this.sidePots()) {
      map.set(pot.id, potAmountToChipStacks(pot.amount));
    }
    return map;
  });

  constructor() {
    // Effect to trigger animation when total changes (skip first load)
    effect(() => {
      const total = this.totalAmount();
      if (!this.isFirstLoad && total > this.previousTotal && this.animateAdditions()) {
        this.triggerAnimation();
      }
      this.isFirstLoad = false;
      this.previousTotal = total;
    });
  }

  ngOnDestroy(): void {
    if (this.animationTimeout) {
      clearTimeout(this.animationTimeout);
    }
  }

  /** Format currency for display */
  formatCurrency(amount: number): string {
    return CURRENCY_FORMATTER.format(amount);
  }

  /** Shared utility - create array for ngFor iteration */
  createChipArray = createChipArray;

  /** Shared utility - get chip symbol ID for SVG href */
  getChipSymbolId = getChipSymbolId;

  /** Trigger chip addition animation */
  private triggerAnimation(): void {
    this.isAnimating.set(true);

    if (this.animationTimeout) {
      clearTimeout(this.animationTimeout);
    }

    this.animationTimeout = setTimeout(() => {
      this.isAnimating.set(false);
    }, 500);
  }
}

import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  computed,
  model,
  effect,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SliderModule } from 'primeng/slider';
import { InputNumberModule } from 'primeng/inputnumber';

import {
  type BettingContext,
  type QuickBetPreset,
  DEFAULT_QUICK_BET_PRESETS,
  formatCurrency,
  calculateQuickBetAmount,
} from '../action-panel.models';

/**
 * Raise slider component for controlling bet/raise amounts.
 *
 * Design reference: docs/designs/poker-table-mockup-v4.html
 * - .raise-control, .raise-slider, .raise-input
 *
 * Features:
 * - Slider from min raise to max (all-in)
 * - Number input for exact amount
 * - Quick bet buttons: 2BB, 3BB, 1/2 Pot, POT, 2x Pot
 * - Yellow/gold themed slider matching v4 design
 */
@Component({
  selector: 'app-raise-slider',
  standalone: true,
  imports: [FormsModule, SliderModule, InputNumberModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="raise-control">
      <!-- Optional Label -->
      @if (showLabel()) {
        <span class="raise-label">Raise to:</span>
      }

      <!-- PrimeNG Slider -->
      <p-slider
        [ngModel]="amount()"
        (ngModelChange)="onAmountChange($event)"
        [min]="minValue()"
        [max]="maxValue()"
        [step]="step()"
        [disabled]="disabled()"
        styleClass="raise-slider"
        ariaLabel="Raise amount slider"
      />

      <!-- Number Input -->
      <p-inputNumber
        [ngModel]="amount()"
        (ngModelChange)="onAmountChange($event)"
        [min]="minValue()"
        [max]="maxValue()"
        [disabled]="disabled()"
        mode="currency"
        currency="USD"
        locale="en-US"
        [maxFractionDigits]="0"
        inputStyleClass="raise-input"
        ariaLabel="Raise amount input"
      />

      <!-- Quick Bet Buttons -->
      @if (showQuickBets()) {
        <div class="quick-bet-inline" role="group" aria-label="Quick bet presets">
          @for (preset of quickBetPresets(); track preset.label) {
            <button
              type="button"
              class="quick-bet-sm"
              [class.active]="isPresetActive(preset)"
              [disabled]="disabled()"
              [attr.aria-label]="getPresetAriaLabel(preset)"
              [attr.aria-pressed]="isPresetActive(preset)"
              (click)="onQuickBet(preset)"
            >
              {{ preset.label }}
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: contents;
      }

      /* ============ RAISE CONTROL CONTAINER ============ */
      .raise-control {
        display: flex;
        align-items: center;
        gap: 12px;
        background: #2a2d36;
        padding: 8px 16px;
        border-radius: var(--radius-md);
      }

      .raise-label {
        font-size: 12px;
        font-weight: 600;
        color: var(--text-secondary);
        white-space: nowrap;
      }

      /* ============ QUICK BET BUTTONS ============ */
      .quick-bet-inline {
        display: flex;
        gap: 6px;
      }

      .quick-bet-sm {
        background: #4a4d56;
        border: 1px solid #5a5d66;
        color: #e5e7eb;
        padding: 8px 12px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
        transition: all var(--animation-fast);
      }

      .quick-bet-sm:hover:not(:disabled) {
        background: #5a5d66;
        color: #fff;
      }

      .quick-bet-sm.active {
        background: #d97706;
        border-color: #b45309;
        color: #fff;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
      }

      .quick-bet-sm:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      /* ============ SLIDER CUSTOMIZATION ============ */
      :host ::ng-deep .raise-slider {
        width: 180px;
      }

      :host ::ng-deep .raise-slider .p-slider {
        background: #444;
        height: 6px;
        border-radius: 3px;
      }

      :host ::ng-deep .raise-slider .p-slider-range {
        background: linear-gradient(135deg, #eab308, #ca8a04);
        border-radius: 3px;
      }

      :host ::ng-deep .raise-slider .p-slider-handle {
        width: 18px;
        height: 18px;
        background: linear-gradient(135deg, #eab308, #ca8a04);
        border: 2px solid #fff;
        border-radius: 50%;
        box-shadow: 0 2px 6px rgba(234, 179, 8, 0.4);
        margin-top: -7px;
        margin-left: -9px;
        transition: transform var(--animation-fast);
      }

      :host ::ng-deep .raise-slider .p-slider-handle:hover {
        background: #ca8a04;
        transform: scale(1.1);
      }

      :host ::ng-deep .raise-slider .p-slider-handle:focus {
        box-shadow:
          0 0 0 3px rgba(234, 179, 8, 0.3),
          0 2px 6px rgba(234, 179, 8, 0.4);
      }

      /* ============ INPUT CUSTOMIZATION ============ */
      :host ::ng-deep .raise-input {
        width: 90px;
        padding: 8px 10px;
        background: var(--bg-primary);
        border: 1px solid #444;
        border-radius: 6px;
        color: var(--text-primary);
        font-size: 14px;
        font-weight: 600;
        text-align: center;
      }

      :host ::ng-deep .raise-input:focus {
        border-color: #eab308;
        box-shadow: 0 0 0 2px rgba(234, 179, 8, 0.2);
        outline: none;
      }

      :host ::ng-deep .p-inputnumber-input:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      /* ============ RESPONSIVE ============ */
      @media (max-width: 1024px) {
        .raise-control {
          gap: 8px;
          padding: 6px 12px;
        }

        :host ::ng-deep .raise-slider {
          width: 140px;
        }

        :host ::ng-deep .raise-input {
          width: 80px;
        }
      }

      @media (max-width: 768px) {
        .quick-bet-inline {
          display: none;
        }

        .raise-label {
          display: none;
        }

        :host ::ng-deep .raise-slider {
          width: 120px;
        }

        :host ::ng-deep .raise-input {
          width: 70px;
        }
      }
    `,
  ],
})
export class RaiseSliderComponent {
  /** Betting context with min/max values and pot info */
  bettingContext = input<BettingContext | null>(null);

  /** Quick bet presets to display */
  quickBetPresets = input<QuickBetPreset[]>(DEFAULT_QUICK_BET_PRESETS);

  /** Whether to show quick bet buttons */
  showQuickBets = input<boolean>(true);

  /** Whether to show the "Raise to:" label (default: false, since Raise button is adjacent) */
  showLabel = input<boolean>(false);

  /** Whether the slider is disabled */
  disabled = input<boolean>(false);

  /** Two-way bound raise amount */
  amount = model<number>(0);

  /** Emits when a quick bet preset is selected */
  quickBetSelected = output<QuickBetPreset>();

  /** Minimum raise value */
  minValue = computed(() => this.bettingContext()?.minRaise ?? 0);

  /** Maximum raise value (all-in) */
  maxValue = computed(() => this.bettingContext()?.maxRaise ?? 100);

  /** Step size for slider (big blind) */
  step = computed(() => this.bettingContext()?.bigBlind ?? 1);

  /** Format currency utility */
  formatCurrency = formatCurrency;

  constructor() {
    // Clamp amount to valid range when betting context changes
    // Only adjusts if current amount is outside the valid range
    effect(() => {
      const ctx = this.bettingContext();
      if (!ctx) return;

      const currentAmount = this.amount();
      if (currentAmount < ctx.minRaise) {
        this.amount.set(ctx.minRaise);
      } else if (currentAmount > ctx.maxRaise) {
        this.amount.set(ctx.maxRaise);
      }
    });
  }

  /** Handle amount change from slider or input */
  onAmountChange(value: number): void {
    const min = this.minValue();
    const max = this.maxValue();

    // Clamp value to valid range
    const clampedValue = Math.max(min, Math.min(value ?? min, max));
    this.amount.set(clampedValue);
  }

  /** Handle quick bet button click */
  onQuickBet(preset: QuickBetPreset): void {
    const ctx = this.bettingContext();
    if (!ctx) return;

    const calculatedAmount = calculateQuickBetAmount(preset, ctx);
    this.amount.set(calculatedAmount);
    this.quickBetSelected.emit(preset);
  }

  /** Check if a preset is currently active (amount matches) */
  isPresetActive(preset: QuickBetPreset): boolean {
    const ctx = this.bettingContext();
    if (!ctx) return false;

    const presetAmount = calculateQuickBetAmount(preset, ctx);
    // Allow small tolerance for floating point comparison
    return Math.abs(this.amount() - presetAmount) < 0.01;
  }

  /** Generate accessible label for quick bet button */
  getPresetAriaLabel(preset: QuickBetPreset): string {
    const ctx = this.bettingContext();
    if (!ctx) return `Set raise to ${preset.label}`;

    const amount = calculateQuickBetAmount(preset, ctx);
    return `Set raise to ${formatCurrency(amount)} (${preset.label})`;
  }
}

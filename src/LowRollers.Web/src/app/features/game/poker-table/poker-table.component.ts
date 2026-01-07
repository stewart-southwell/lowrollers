import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommunityCardsComponent } from '../community-cards';
import { Card } from '../../../shared/models/card.models';

/** Position identifiers for seats around the table */
export type SeatPosition =
  | 'top'
  | 'top-right'
  | 'right'
  | 'bottom-right'
  | 'bottom'
  | 'bottom-left'
  | 'left'
  | 'top-left';

/** Pot information */
export interface PotInfo {
  label: string;
  amount: number;
}

/** Angles for each seat position around the ellipse */
const SEAT_ANGLES: Record<SeatPosition, number> = {
  'right': 0,
  'bottom-right': 45,
  'bottom': 90,
  'bottom-left': 135,
  'left': 180,
  'top-left': 225,
  'top': 270,
  'top-right': 315
};

/**
 * Calculate dealer button position on ellipse (inside the table on the felt).
 * Uses same geometry as seats but with negative offset to place inside table.
 */
function calculateDealerButtonPosition(
  position: SeatPosition,
  tableWidthPercent = 80,
  tableAspectRatio = 0.5625,
  offsetPercent = -8
): string {
  const angle = SEAT_ANGLES[position];
  const radians = (angle * Math.PI) / 180;

  const a = (tableWidthPercent / 2) + offsetPercent;
  const b = (tableWidthPercent * tableAspectRatio / 2) + offsetPercent;

  const x = 50 + (a * Math.cos(radians));
  const y = 50 + (b * Math.sin(radians));

  return `left: ${x.toFixed(1)}%; top: ${y.toFixed(1)}%; transform: translate(-50%, -50%);`;
}

/** Pre-computed dealer button positions using ellipse calculation */
const DEALER_BUTTON_POSITIONS: Record<SeatPosition, string> = {
  'top': calculateDealerButtonPosition('top'),
  'top-right': calculateDealerButtonPosition('top-right'),
  'right': calculateDealerButtonPosition('right'),
  'bottom-right': calculateDealerButtonPosition('bottom-right'),
  'bottom': calculateDealerButtonPosition('bottom'),
  'bottom-left': calculateDealerButtonPosition('bottom-left'),
  'left': calculateDealerButtonPosition('left'),
  'top-left': calculateDealerButtonPosition('top-left')
};

/**
 * Main poker table component displaying the oval table,
 * community cards, pot, and dealer button.
 * Player seats are projected via ng-content.
 */
@Component({
  selector: 'app-poker-table',
  standalone: true,
  imports: [CommonModule, CommunityCardsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="table-area">
      <div class="table-container">
        <!-- Poker Table Surface -->
        <div class="poker-table">
          <div class="table-rail"></div>
          <div class="table-felt"></div>
        </div>

        <!-- Pot Display -->
        @if (pot()) {
          <div class="pot-display">
            <div class="pot-chips" aria-hidden="true">
              <div class="chip-column">
                <div class="chip"><svg viewBox="0 0 100 100"><use href="#chipRed" /></svg></div>
                <div class="chip"><svg viewBox="0 0 100 100"><use href="#chipRed" /></svg></div>
                <div class="chip"><svg viewBox="0 0 100 100"><use href="#chipRed" /></svg></div>
              </div>
              <div class="chip-column">
                <div class="chip"><svg viewBox="0 0 100 100"><use href="#chipBlue" /></svg></div>
                <div class="chip"><svg viewBox="0 0 100 100"><use href="#chipBlue" /></svg></div>
              </div>
              <div class="chip-column">
                <div class="chip"><svg viewBox="0 0 100 100"><use href="#chipGreen" /></svg></div>
              </div>
            </div>
            <div class="pot-info">
              <div class="pot-label">{{ pot()!.label }}</div>
              <div class="pot-amount">{{ formatCurrency(pot()!.amount) }}</div>
            </div>
          </div>
        }

        <!-- Community Cards Area -->
        <app-community-cards
          [cards]="communityCards()"
          [winningCardIndices]="winningCardIndices()"
        />

        <!-- Dealer Button -->
        @if (dealerPosition()) {
          <div
            class="dealer-button"
            [style]="getDealerButtonPosition(dealerPosition()!)"
            role="img"
            aria-label="Dealer button"
          >
            D
          </div>
        }

        <!-- Player Seat Slots (content projected) -->
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .table-area {
      position: relative;
      width: 100%;
      height: calc(100vh - 200px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding-top: 60px;
    }

    .table-container {
      position: relative;
      width: 100%;
      max-width: 1200px;
      padding-bottom: 50%;
    }

    /* Poker Table */
    .poker-table {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 80%;
      padding-bottom: 45%;
      border-radius: 42%;
      box-shadow: 0 25px 50px rgba(0, 0, 0, 0.5);
    }

    .table-rail {
      position: absolute;
      inset: 0;
      border-radius: 42%;
      background: linear-gradient(
        180deg,
        var(--table-rail-light) 0%,
        var(--table-rail-mid) 50%,
        var(--table-rail-dark) 100%
      );
      box-shadow:
        inset 0 2px 4px rgba(255, 255, 255, 0.1),
        inset 0 -2px 4px rgba(0, 0, 0, 0.3);
    }

    .table-felt {
      position: absolute;
      inset: 20px;
      border-radius: 40%;
      background: radial-gradient(
        ellipse at center,
        var(--table-felt-light) 0%,
        var(--table-felt-mid) 40%,
        var(--table-felt-dark) 100%
      );
      box-shadow: inset 0 0 40px rgba(0, 0, 0, 0.5);
      border: 3px solid var(--table-rail-mid);
    }

    /* Pot Display */
    .pot-display {
      position: absolute;
      top: 35%;
      left: 50%;
      transform: translateX(-50%);
      display: flex;
      align-items: center;
      gap: 10px;
      z-index: var(--z-pot);
    }

    .pot-chips {
      display: flex;
      align-items: flex-end;
      gap: 3px;
    }

    .chip-column {
      display: flex;
      flex-direction: column-reverse;
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

    .chip svg {
      width: 100%;
      height: 100%;
      filter: drop-shadow(0 2px 2px rgba(0, 0, 0, 0.4));
    }

    .pot-info {
      background: var(--bg-card);
      backdrop-filter: blur(4px);
      -webkit-backdrop-filter: blur(4px);
      padding: 8px 20px;
      border-radius: var(--radius-md);
      text-align: center;
    }

    .pot-label {
      font-size: 10px;
      font-weight: 600;
      color: var(--text-secondary);
      text-transform: uppercase;
      letter-spacing: 1px;
    }

    .pot-amount {
      font-size: 28px;
      font-weight: 900;
      color: var(--accent-yellow);
      text-shadow: 0 2px 10px rgba(234, 179, 8, 0.5);
    }

    /* Community Cards - position the component on the table */
    app-community-cards {
      position: absolute;
      top: 55%;
      left: 50%;
      transform: translate(-50%, -50%);
      z-index: var(--z-cards);
    }

    /* Dealer Button */
    .dealer-button {
      position: absolute;
      width: 36px;
      height: 36px;
      background: #fff;
      border: 3px solid #eab308;
      border-radius: var(--radius-full);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 14px;
      font-weight: 900;
      color: #000;
      box-shadow: 0 3px 10px rgba(0, 0, 0, 0.4);
      z-index: var(--z-dealer-button);
    }

    /* Responsive scaling */
    @media (max-width: 1200px) {
      .table-container {
        transform: scale(0.85);
      }
    }

    @media (max-width: 900px) {
      .table-container {
        transform: scale(0.7);
      }
    }

    @media (max-width: 600px) {
      .table-container {
        transform: scale(0.5);
      }

      .pot-amount {
        font-size: 20px;
      }
    }
  `]
})
export class PokerTableComponent {
  /** Community cards to display (0-5 cards) */
  communityCards = input<Card[]>([]);

  /** Indices of winning cards to highlight (0-4) */
  winningCardIndices = input<number[]>([]);

  /** Current pot information */
  pot = input<PotInfo | null>(null);

  /** Position of the dealer button */
  dealerPosition = input<SeatPosition | null>(null);

  /** Format currency for display */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount);
  }

  /** Get CSS position for dealer button based on seat position */
  getDealerButtonPosition(position: SeatPosition): string {
    return DEALER_BUTTON_POSITIONS[position];
  }
}

import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommunityCardsComponent } from '../community-cards';
import { PotDisplayComponent, type Pot } from '../pot-display';
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

/** Angles for each seat position around the ellipse */
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

  const a = tableWidthPercent / 2 + offsetPercent;
  const b = (tableWidthPercent * tableAspectRatio) / 2 + offsetPercent;

  const x = 50 + a * Math.cos(radians);
  const y = 50 + b * Math.sin(radians);

  return `left: ${x.toFixed(1)}%; top: ${y.toFixed(1)}%; transform: translate(-50%, -50%);`;
}

/** Pre-computed dealer button positions using ellipse calculation */
const DEALER_BUTTON_POSITIONS: Record<SeatPosition, string> = {
  top: calculateDealerButtonPosition('top'),
  'top-right': calculateDealerButtonPosition('top-right'),
  right: calculateDealerButtonPosition('right'),
  'bottom-right': calculateDealerButtonPosition('bottom-right'),
  bottom: calculateDealerButtonPosition('bottom'),
  'bottom-left': calculateDealerButtonPosition('bottom-left'),
  left: calculateDealerButtonPosition('left'),
  'top-left': calculateDealerButtonPosition('top-left'),
};

/**
 * Main poker table component displaying the oval table,
 * community cards, pot, and dealer button.
 * Player seats are projected via ng-content.
 */
@Component({
  selector: 'app-poker-table',
  standalone: true,
  imports: [CommonModule, CommunityCardsComponent, PotDisplayComponent],
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
        @if (pots().length > 0) {
          <app-pot-display [pots]="pots()" />
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
  styles: [
    `
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

      /* Pot Display - position the component on the table */
      app-pot-display {
        position: absolute;
        top: 35%;
        left: 50%;
        transform: translateX(-50%);
        z-index: var(--z-pot);
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
      }
    `,
  ],
})
export class PokerTableComponent {
  /** Community cards to display (0-5 cards) */
  communityCards = input<Card[]>([]);

  /** Indices of winning cards to highlight (0-4) */
  winningCardIndices = input<number[]>([]);

  /** Pots to display (main + side pots) */
  pots = input<Pot[]>([]);

  /** Position of the dealer button */
  dealerPosition = input<SeatPosition | null>(null);

  /** Get CSS position for dealer button based on seat position */
  getDealerButtonPosition(position: SeatPosition): string {
    return DEALER_BUTTON_POSITIONS[position];
  }
}

import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CardSuit, CardRank, isRedSuit, getSuitSymbol } from '../models/card.models';

/**
 * Reusable playing card component.
 * Displays face-up cards with rank and suit, or face-down with card back design.
 */
@Component({
  selector: 'app-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (faceDown()) {
      <!-- Face down card -->
      <div
        class="card card-back"
        [class.card-mini]="size() === 'mini'"
        [class.card-large]="size() === 'large'"
        [class.folded]="folded()"
        role="img"
        [attr.aria-label]="'Face down card'"
      >
        <svg viewBox="0 0 208 303"><use href="#cardBack" /></svg>
      </div>
    } @else {
      <!-- Face up card -->
      <div
        class="card"
        [class.red]="isRed()"
        [class.black]="!isRed()"
        [class.card-mini]="size() === 'mini'"
        [class.card-large]="size() === 'large'"
        [class.highlighted]="highlighted()"
        [class.pulsing]="pulsing()"
        [class.folded]="folded()"
        role="img"
        [attr.aria-label]="rank() + ' of ' + suit()"
      >
        <div class="top-left">
          <div class="rank">{{ rank() }}</div>
          <div class="suit">{{ suitSymbol() }}</div>
        </div>
        <div class="center-suit">{{ suitSymbol() }}</div>
        <div class="bottom-right">
          <div class="rank">{{ rank() }}</div>
          <div class="suit">{{ suitSymbol() }}</div>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: inline-block;
    }

    .card {
      width: var(--card-width);
      height: var(--card-height);
      background: #fff;
      border-radius: var(--radius-md);
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      border: 1px solid #e5e7eb;
      transition: transform var(--animation-fast) var(--ease-out);
    }

    .card-mini {
      width: var(--card-mini-width);
      height: var(--card-mini-height);
      border-radius: var(--radius-sm);
      box-shadow: 0 2px 6px rgba(0, 0, 0, 0.4);
    }

    .card-large {
      width: var(--card-large-width);
      height: var(--card-large-height);
      border-radius: var(--radius-lg);
    }

    /* Highlighted state - for current player's cards or winning cards */
    .highlighted {
      border: 4px solid #3b82f6;
      box-shadow:
        0 0 0 4px rgba(59, 130, 246, 0.5),
        0 10px 30px rgba(0, 0, 0, 0.4);
    }

    /* Pulsing animation for winning cards at showdown */
    .highlighted.pulsing {
      animation: winningPulse 1.5s ease-in-out infinite alternate;
    }

    @keyframes winningPulse {
      from {
        box-shadow:
          0 0 0 4px rgba(59, 130, 246, 0.5),
          0 10px 30px rgba(0, 0, 0, 0.4);
      }
      to {
        box-shadow:
          0 0 0 6px rgba(59, 130, 246, 0.7),
          0 0 20px rgba(59, 130, 246, 0.5),
          0 10px 30px rgba(0, 0, 0, 0.4);
      }
    }

    /* Folded state - greyed out */
    .folded {
      filter: grayscale(100%) brightness(0.5);
      opacity: 0.5;
    }

    .card-back {
      padding: 0;
      overflow: hidden;
    }

    .card-back svg {
      width: 100%;
      height: 100%;
    }

    .top-left {
      position: absolute;
      top: 4px;
      left: 4px;
      display: flex;
      flex-direction: column;
      align-items: center;
      line-height: 1;
    }

    .card-mini .top-left {
      top: 2px;
      left: 2px;
    }

    .card-large .top-left {
      top: 6px;
      left: 6px;
    }

    .rank {
      font-size: 14px;
      font-weight: 700;
    }

    .card-mini .rank {
      font-size: 10px;
    }

    .card-large .rank {
      font-size: 20px;
    }

    .suit {
      font-size: 18px;
    }

    .card-mini .suit {
      font-size: 12px;
    }

    .card-large .suit {
      font-size: 16px;
    }

    .center-suit {
      font-size: 40px;
      font-weight: bold;
    }

    .card-mini .center-suit {
      font-size: 20px;
    }

    .card-large .center-suit {
      font-size: 34px;
    }

    .bottom-right {
      position: absolute;
      bottom: 4px;
      right: 4px;
      transform: rotate(180deg);
      display: flex;
      flex-direction: column;
      align-items: center;
      line-height: 1;
    }

    .card-mini .bottom-right {
      bottom: 2px;
      right: 2px;
    }

    .card-large .bottom-right {
      bottom: 6px;
      right: 6px;
    }

    .red {
      color: #dc2626;
    }

    .black {
      color: #1f2937;
    }
  `]
})
export class CardComponent {
  /** Card rank (A, 2-10, J, Q, K) */
  rank = input.required<CardRank>();

  /** Card suit */
  suit = input.required<CardSuit>();

  /** Whether the card is face down */
  faceDown = input<boolean>(false);

  /** Card size variant */
  size = input<'normal' | 'mini' | 'large'>('normal');

  /** Whether to show blue highlight (for current player's cards or winning cards) */
  highlighted = input<boolean>(false);

  /** Whether to show pulsing animation (for winning cards at showdown) */
  pulsing = input<boolean>(false);

  /** Whether to show folded/greyed out state */
  folded = input<boolean>(false);

  /** Whether this is a red suit (hearts or diamonds) */
  isRed = computed(() => isRedSuit(this.suit()));

  /** Get Unicode symbol for the suit */
  suitSymbol = computed(() => getSuitSymbol(this.suit()));
}

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
import { CardComponent } from '../../../shared/card/card.component';
import type { Card } from '../../../shared/models/card.models';

/** Represents a single card slot in the community cards display */
interface CardSlot {
  index: number;
  card: Card | null;
  isDealing: boolean;
  isRevealed: boolean;
  isWinning: boolean;
}

/**
 * Community cards component displaying the 5 shared cards in Texas Hold'em.
 *
 * Features:
 * - Displays 5 card positions in horizontal row
 * - Cards appear with deal + flip animation using CSS animation-delay
 * - Staggered animation for flop (3 cards with delay)
 * - Highlights winning cards at showdown with pulsing animation
 * - Empty slots shown as placeholders
 */
@Component({
  selector: 'app-community-cards',
  standalone: true,
  imports: [CommonModule, CardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="community-cards" role="region" aria-label="Community cards">
      @for (slot of cardSlots(); track slot.index) {
      <div class="card-slot" [class.dealing]="slot.isDealing">
        @if (slot.card) {
        <div class="card-flip-container" [class.flipped]="slot.isRevealed">
          <!-- Card back (visible before flip) -->
          <div class="card-face card-back">
            <app-card rank="A" suit="spades" [faceDown]="true" />
          </div>
          <!-- Card front (visible after flip) -->
          <div class="card-face card-front">
            <app-card
              [rank]="slot.card.rank"
              [suit]="slot.card.suit"
              [highlighted]="slot.isWinning"
              [pulsing]="slot.isWinning"
            />
          </div>
        </div>
        } @else {
        <div class="card-placeholder" aria-hidden="true"></div>
        }
      </div>
      }
    </div>
  `,
  styles: [
    `
      .community-cards {
        display: flex;
        gap: 8px;
        perspective: 1000px;
      }

      .card-slot {
        width: var(--card-width, 64px);
        height: var(--card-height, 96px);
      }

      .card-placeholder {
        width: 100%;
        height: 100%;
        background: rgba(255, 255, 255, 0.1);
        border: 2px dashed rgba(255, 255, 255, 0.2);
        border-radius: var(--radius-md, 8px);
      }

      /* Container for the flip effect */
      .card-flip-container {
        width: 100%;
        height: 100%;
        position: relative;
        transform-style: preserve-3d;
        transition: transform 0.6s ease-out;
      }

      /* When flipped, rotate to show front */
      .card-flip-container.flipped {
        transform: rotateY(180deg);
      }

      /* Both card faces need backface-visibility hidden */
      .card-face {
        position: absolute;
        width: 100%;
        height: 100%;
        backface-visibility: hidden;
        -webkit-backface-visibility: hidden;
      }

      /* Card back faces viewer initially */
      .card-back {
        transform: rotateY(0deg);
      }

      /* Card front starts facing away */
      .card-front {
        transform: rotateY(180deg);
      }

      /* Deal animation - card slides in from above */
      .card-slot.dealing {
        animation: dealCard 0.3s ease-out;
      }

      @keyframes dealCard {
        from {
          opacity: 0;
          transform: translateY(-20px) scale(0.8);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }
    `,
  ],
})
export class CommunityCardsComponent implements OnDestroy {
  /** Cards to display (0-5 cards) */
  cards = input<Card[]>([]);

  /** Indices of cards that are part of the winning hand (0-4) */
  winningCardIndices = input<number[]>([]);

  /** Whether to animate cards being dealt (set false to skip animation) */
  animateDealing = input<boolean>(true);

  /** Delay between dealing each card in ms (for staggered flop animation) */
  dealDelayMs = input<number>(150);

  /** Track which cards have been revealed - boolean array */
  private revealedState = signal<boolean[]>([false, false, false, false, false]);

  /** Track which cards are currently dealing - boolean array */
  private dealingState = signal<boolean[]>([false, false, false, false, false]);

  /** Previous card count for detecting new cards */
  private previousCardCount = 0;

  /** Timeout refs for cleanup */
  private dealTimeouts: ReturnType<typeof setTimeout>[] = [];

  /** Card slots for display (always 5 slots) */
  cardSlots = computed((): CardSlot[] => {
    const cards = this.cards();
    const revealed = this.revealedState();
    const dealing = this.dealingState();
    const winningIndices = this.winningCardIndices();

    return Array.from({ length: 5 }, (_, index) => ({
      index,
      card: cards[index] ?? null,
      isRevealed: revealed[index],
      isDealing: dealing[index],
      isWinning: winningIndices.includes(index),
    }));
  });

  constructor() {
    // Effect to handle card dealing animation when cards change
    effect(() => {
      const cards = this.cards();
      const currentCount = cards.length;
      const previousCount = this.previousCardCount;

      // Skip animation if cards are set initially (not incrementally dealt)
      const isInitialLoad = previousCount === 0 && currentCount > 1;
      if (isInitialLoad) {
        this.resetAnimationState(currentCount);
        this.previousCardCount = currentCount;
        return;
      }

      // Cards increased: animate new cards
      if (currentCount > previousCount && this.animateDealing()) {
        this.animateNewCards(previousCount, currentCount);
      }
      // Cards decreased or reset: reset state without animation
      else if (currentCount < previousCount) {
        this.resetAnimationState(currentCount);
      }

      this.previousCardCount = currentCount;
    });
  }

  ngOnDestroy(): void {
    this.clearTimeouts();
  }

  /** Animate new cards being dealt with staggered timing */
  private animateNewCards(fromIndex: number, toIndex: number): void {
    const delay = this.dealDelayMs();

    for (let i = fromIndex; i < toIndex; i++) {
      const cardIndex = i;
      const staggerDelay = (i - fromIndex) * delay;

      // Start dealing animation after stagger delay
      const dealTimeout = setTimeout(() => {
        this.updateDealingState(cardIndex, true);

        // After deal animation completes, start flip
        const flipTimeout = setTimeout(() => {
          this.updateRevealedState(cardIndex, true);

          // Clear dealing state after flip completes
          const clearTimeout = setTimeout(() => {
            this.updateDealingState(cardIndex, false);
          }, 600);

          this.dealTimeouts.push(clearTimeout);
        }, 300);

        this.dealTimeouts.push(flipTimeout);
      }, staggerDelay);

      this.dealTimeouts.push(dealTimeout);
    }
  }

  /** Update revealed state for a single index */
  private updateRevealedState(index: number, value: boolean): void {
    this.revealedState.update((arr) => {
      const newArr = [...arr];
      newArr[index] = value;
      return newArr;
    });
  }

  /** Update dealing state for a single index */
  private updateDealingState(index: number, value: boolean): void {
    this.dealingState.update((arr) => {
      const newArr = [...arr];
      newArr[index] = value;
      return newArr;
    });
  }

  /** Reset animation state for new hand */
  private resetAnimationState(cardCount: number): void {
    this.clearTimeouts();

    // Mark existing cards as already revealed (no animation)
    const revealed = [false, false, false, false, false];
    for (let i = 0; i < cardCount; i++) {
      revealed[i] = true;
    }
    this.revealedState.set(revealed);
    this.dealingState.set([false, false, false, false, false]);
  }

  /** Clear all pending animation timeouts */
  private clearTimeouts(): void {
    this.dealTimeouts.forEach((t) => clearTimeout(t));
    this.dealTimeouts = [];
  }
}

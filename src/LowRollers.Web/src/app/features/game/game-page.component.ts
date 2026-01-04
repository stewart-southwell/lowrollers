import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { PokerTableComponent, PotInfo, SeatPosition } from './poker-table/poker-table.component';
import { Card } from '../../shared/models/card.models';

/**
 * Main game page that hosts the poker table and all game UI.
 * This is the primary view when playing a poker game.
 */
@Component({
  selector: 'app-game-page',
  standalone: true,
  imports: [PokerTableComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Background pattern -->
    <div class="diagonal-pattern"></div>

    <!-- Poker Table -->
    <app-poker-table
      [communityCards]="communityCards()"
      [pot]="pot()"
      [dealerPosition]="dealerPosition()"
    >
      <!-- Player seats will be projected here in future -->
    </app-poker-table>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      height: 100vh;
      overflow: hidden;
    }
  `]
})
export class GamePageComponent {
  /** Demo community cards */
  communityCards = signal<Card[]>([
    { rank: 'A', suit: 'spades' },
    { rank: 'K', suit: 'diamonds' },
    { rank: 'Q', suit: 'spades' },
    { rank: '10', suit: 'clubs' },
    { rank: '7', suit: 'hearts' }
  ]);

  /** Demo pot */
  pot = signal<PotInfo>({
    label: 'Main Pot',
    amount: 327
  });

  /** Demo dealer position */
  dealerPosition = signal<SeatPosition>('top-right');
}

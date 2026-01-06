import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { PokerTableComponent, PotInfo, SeatPosition } from './poker-table/poker-table.component';
import { PlayerSeatComponent, PlayerData } from './player-seat';
import { Card } from '../../shared/models/card.models';

/**
 * Main game page that hosts the poker table and all game UI.
 * This is the primary view when playing a poker game.
 */
@Component({
  selector: 'app-game-page',
  standalone: true,
  imports: [PokerTableComponent, PlayerSeatComponent],
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
      <!-- Player seats -->
      @for (seat of seats(); track seat.position) {
        <app-player-seat
          [position]="seat.position"
          [player]="seat.player"
          (seatClick)="onSeatClick($event)"
        />
      }
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

  /** All seats around the table with demo player data */
  seats = signal<{ position: SeatPosition; player: PlayerData | null }[]>([
    {
      position: 'top',
      player: {
        id: '1',
        name: 'Stewart',
        avatar: '🧑‍💼',
        chips: 1250,
        status: 'playing',
        holeCards: [{ rank: 'A', suit: 'hearts' }, { rank: 'K', suit: 'hearts' }],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true,
        isCurrentTurn: true,
        remainingTime: 18,
        totalTime: 30
      }
    },
    {
      position: 'top-right',
      player: {
        id: '2',
        name: 'Blair',
        avatar: '👨‍⚖️',
        chips: 980,
        status: 'playing',
        holeCards: [{ rank: '7', suit: 'clubs' }, { rank: '8', suit: 'clubs' }],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true
      }
    },
    {
      position: 'right',
      player: null // Empty seat
    },
    {
      position: 'bottom-right',
      player: {
        id: '3',
        name: 'Alex',
        avatar: '👨‍💻',
        chips: 1500,
        status: 'folded',
        holeCards: [{ rank: '2', suit: 'spades' }, { rank: '7', suit: 'diamonds' }],
        holeCardsVisible: false
      }
    },
    {
      position: 'bottom-left',
      player: {
        id: '4',
        name: 'Jordan',
        avatar: '👩‍🎨',
        chips: 750,
        status: 'playing',
        holeCards: [{ rank: 'Q', suit: 'hearts' }, { rank: 'J', suit: 'hearts' }],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true
      }
    },
    {
      position: 'left',
      player: {
        id: '5',
        name: 'Morgan',
        avatar: '🧑‍🚀',
        chips: 0,
        status: 'all-in',
        holeCards: [{ rank: 'A', suit: 'clubs' }, { rank: 'A', suit: 'diamonds' }],
        holeCardsVisible: false,
        currentBet: 500
      }
    },
    {
      position: 'top-left',
      player: {
        id: '6',
        name: 'Casey',
        avatar: '👨‍🔬',
        chips: 1100,
        status: 'playing',
        holeCards: [{ rank: 'K', suit: 'spades' }, { rank: 'Q', suit: 'diamonds' }],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true
      }
    }
  ]);

  /** Handle empty seat click */
  onSeatClick(position: SeatPosition): void {
    console.log('Seat clicked:', position);
    // In a real app, this would open a dialog to take the seat
  }
}

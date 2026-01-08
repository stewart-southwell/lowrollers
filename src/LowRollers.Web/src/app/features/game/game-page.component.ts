import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { PokerTableComponent, SeatPosition } from './poker-table/poker-table.component';
import { PlayerSeatComponent, PlayerData } from './player-seat';
import { type Pot, createMainPot, createSidePot } from './pot-display';
import {
  ActionPanelComponent,
  type CurrentPlayerState,
  type BettingContext,
  type PlayerActionEvent,
} from './action-panel';
import { Card } from '../../shared/models/card.models';

/**
 * Main game page that hosts the poker table and all game UI.
 * This is the primary view when playing a poker game.
 */
@Component({
  selector: 'app-game-page',
  standalone: true,
  imports: [PokerTableComponent, PlayerSeatComponent, ActionPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Background pattern -->
    <div class="diagonal-pattern"></div>

    <!-- Poker Table -->
    <app-poker-table
      [communityCards]="communityCards()"
      [pots]="pots()"
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

    <!-- Action Panel -->
    <app-action-panel
      [player]="currentPlayer()"
      [bettingContext]="bettingContext()"
      (actionTaken)="onActionTaken($event)"
    />
  `,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        height: 100vh;
        overflow: hidden;
      }
    `,
  ],
})
export class GamePageComponent {
  /** Demo community cards */
  communityCards = signal<Card[]>([
    { rank: 'A', suit: 'spades' },
    { rank: 'K', suit: 'diamonds' },
    { rank: 'Q', suit: 'spades' },
    { rank: '10', suit: 'clubs' },
    { rank: '7', suit: 'hearts' },
  ]);

  /** Demo pots (main pot + side pots) */
  pots = signal<Pot[]>([
    createMainPot(327),
    createSidePot(1, 150, ['1', '2', '4']),
    createSidePot(2, 75, ['1', '2']),
  ]);

  /** Demo dealer position */
  dealerPosition = signal<SeatPosition>('top-right');

  /**
   * Demo betting context.
   * bigBlind=50 makes quick bet buttons useful: 2BB=$100 (equals minRaise), 3BB=$150
   */
  bettingContext = signal<BettingContext>({
    currentBet: 50,
    playerContribution: 0,
    minRaise: 100,
    maxRaise: 845,
    bigBlind: 50,
    potSize: 327,
  });

  /** Current player state (derived from the player with isCurrentTurn) */
  currentPlayer = computed<CurrentPlayerState | null>(() => {
    const activePlayer = this.seats().find((s) => s.player?.isCurrentTurn)?.player;
    if (!activePlayer) return null;
    return {
      id: activePlayer.id,
      name: activePlayer.name,
      avatar: activePlayer.avatar,
      chips: activePlayer.chips,
      isCurrentTurn: activePlayer.isCurrentTurn ?? false,
      remainingTime: activePlayer.remainingTime,
      totalTime: activePlayer.totalTime,
      // Time bank fields (optional - will be undefined if not on player model)
      hasTimeBank: (activePlayer as { hasTimeBank?: boolean }).hasTimeBank,
      timeBankRemaining: (activePlayer as { timeBankRemaining?: number }).timeBankRemaining,
    };
  });

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
        holeCards: [
          { rank: 'A', suit: 'hearts' },
          { rank: 'K', suit: 'hearts' },
        ],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true,
        isCurrentTurn: true,
        remainingTime: 18,
        totalTime: 30,
      },
    },
    {
      position: 'top-right',
      player: {
        id: '2',
        name: 'Blair',
        avatar: '👨‍⚖️',
        chips: 980,
        status: 'playing',
        holeCards: [
          { rank: '7', suit: 'clubs' },
          { rank: '8', suit: 'clubs' },
        ],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true,
      },
    },
    {
      position: 'right',
      player: null, // Empty seat
    },
    {
      position: 'bottom-right',
      player: {
        id: '3',
        name: 'Alex',
        avatar: '👨‍💻',
        chips: 1500,
        status: 'folded',
        holeCards: [
          { rank: '2', suit: 'spades' },
          { rank: '7', suit: 'diamonds' },
        ],
        holeCardsVisible: false,
      },
    },
    {
      position: 'bottom-left',
      player: {
        id: '4',
        name: 'Jordan',
        avatar: '👩‍🎨',
        chips: 750,
        status: 'playing',
        holeCards: [
          { rank: 'Q', suit: 'hearts' },
          { rank: 'J', suit: 'hearts' },
        ],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true,
      },
    },
    {
      position: 'left',
      player: {
        id: '5',
        name: 'Morgan',
        avatar: '🧑‍🚀',
        chips: 0,
        status: 'all-in',
        holeCards: [
          { rank: 'A', suit: 'clubs' },
          { rank: 'A', suit: 'diamonds' },
        ],
        holeCardsVisible: false,
        currentBet: 500,
      },
    },
    {
      position: 'top-left',
      player: {
        id: '6',
        name: 'Casey',
        avatar: '👨‍🔬',
        chips: 1100,
        status: 'playing',
        holeCards: [
          { rank: 'K', suit: 'spades' },
          { rank: 'Q', suit: 'diamonds' },
        ],
        holeCardsVisible: false,
        currentBet: 50,
        hasMic: true,
        hasVideo: true,
      },
    },
  ]);

  /** Handle empty seat click */
  onSeatClick(position: SeatPosition): void {
    console.log('Seat clicked:', position);
    // In a real app, this would open a dialog to take the seat
  }

  /** Handle player action from action panel */
  onActionTaken(event: PlayerActionEvent): void {
    console.log('Action taken:', event);
    // In a real app, this would send the action to the server via SignalR
  }
}

import type { Card } from '../../../shared/models/card.models';
import type { SeatPosition } from '../poker-table/poker-table.component';

// Re-export types for convenience
export type { SeatPosition } from '../poker-table/poker-table.component';
export type { ChipColor, ChipStack } from '../../../shared/models/chip.models';
export {
  amountToChipStacks,
  getChipSymbolId,
  createChipArray,
} from '../../../shared/models/chip.models';

/**
 * Player's status in the current hand.
 *
 * - 'playing': Player has cards and is still active in the hand
 * - 'folded': Player has folded their hand
 * - 'all-in': Player is all-in (still in hand but cannot act)
 * - 'sitting-out': Player is at the table but not participating in hands
 */
export type PlayerStatus = 'playing' | 'folded' | 'all-in' | 'sitting-out';

/**
 * Player data for the seat component.
 *
 * Note on status vs isCurrentTurn:
 * - `status`: Describes the player's state in the current hand (playing, folded, all-in, sitting-out)
 * - `isCurrentTurn`: Whether it's this player's turn to act (controls orange highlight/timer)
 *
 * A player can have status='playing' but isCurrentTurn=false (waiting for their turn).
 * A player with status='all-in' will never have isCurrentTurn=true (they can't act).
 */
export interface PlayerData {
  /** Unique player ID */
  id: string;
  /** Display name */
  name: string;
  /** Emoji avatar */
  avatar: string;
  /** Current chip count */
  chips: number;
  /** Player's status in the current hand */
  status: PlayerStatus;
  /** Hole cards. undefined = not dealt yet */
  holeCards?: Card[];
  /** Whether hole cards are visible (true = face up, false = face down/card backs) */
  holeCardsVisible?: boolean;
  /** Current bet in the round */
  currentBet?: number;
  /** Whether mic is enabled */
  hasMic?: boolean;
  /** Whether video is enabled */
  hasVideo?: boolean;
  /**
   * Whether it's this player's turn to act.
   * Controls the orange border glow, pulsating indicator, and action timer.
   * Independent of `status` - a playing player may or may not have current turn.
   */
  isCurrentTurn?: boolean;
  /** Remaining time in seconds (for action timer). Only relevant when isCurrentTurn=true */
  remainingTime?: number;
  /** Total action time in seconds. Only relevant when isCurrentTurn=true */
  totalTime?: number;
}

/** Style object for ngStyle binding */
export type StyleObject = Record<string, string>;

/** Get bet position style based on seat position */
export function getBetPositionStyle(position: SeatPosition): StyleObject {
  const positions: Record<SeatPosition, StyleObject> = {
    top: { top: '100%', left: '50%', transform: 'translateX(-50%)', marginTop: '8px' },
    'top-right': { top: '105%', left: '20px' },
    right: { top: '50%', left: '-80px', transform: 'translateY(-50%)' },
    'bottom-right': { bottom: '105%', left: '20px' },
    bottom: { bottom: '100%', left: '50%', transform: 'translateX(-50%)', marginBottom: '8px' },
    'bottom-left': { bottom: '105%', right: '20px' },
    left: { top: '50%', right: '-80px', transform: 'translateY(-50%)' },
    'top-left': { top: '105%', right: '20px' },
  };
  return positions[position];
}

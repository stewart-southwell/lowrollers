/**
 * Pot display models and utilities.
 */

import type { ChipStack } from '../../../shared/models/chip.models';
import { amountToChipStacks } from '../../../shared/models/chip.models';

/** Type of pot */
export type PotType = 'main' | 'side';

/**
 * Represents a single pot (main or side).
 *
 * - Main pot: All active players are eligible
 * - Side pot: Created when a player goes all-in; only players who contributed are eligible
 */
export interface Pot {
  /** Unique identifier for the pot */
  id: string;
  /** Type of pot */
  type: PotType;
  /** Total amount in the pot */
  amount: number;
  /** Number of players eligible to win this pot (for side pots) */
  eligiblePlayerCount?: number;
  /** Player IDs eligible to win this pot */
  eligiblePlayerIds?: string[];
}

/**
 * Convert pot amount to chip stacks for display.
 * Uses max 6 chips per color for cleaner pot display.
 */
export function potAmountToChipStacks(amount: number): ChipStack[] {
  return amountToChipStacks(amount, 6);
}

/**
 * Create a main pot with the given amount.
 */
export function createMainPot(amount: number): Pot {
  return {
    id: 'main',
    type: 'main',
    amount,
  };
}

/**
 * Create a side pot with the given parameters.
 */
export function createSidePot(index: number, amount: number, eligiblePlayerIds: string[]): Pot {
  return {
    id: `side-${index}`,
    type: 'side',
    amount,
    eligiblePlayerCount: eligiblePlayerIds.length,
    eligiblePlayerIds,
  };
}

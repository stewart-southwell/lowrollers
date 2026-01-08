/**
 * Shared chip models used across the application.
 * Centralized to avoid circular dependencies between components.
 */

import { capitalizeFirst } from '../utils/string.utils';

/** Chip color for stack display */
export type ChipColor = 'white' | 'red' | 'blue' | 'green' | 'black';

/** Chip representation for display */
export interface ChipStack {
  color: ChipColor;
  count: number;
}

/** Chip denominations in dollars (ordered high-to-low) */
export const CHIP_VALUES: Record<ChipColor, number> = {
  black: 100,
  green: 25,
  blue: 10,
  red: 5,
  white: 1,
};

/** Chip colors in denomination order (high-to-low) */
export const CHIP_COLORS_ORDERED: ChipColor[] = ['black', 'green', 'blue', 'red', 'white'];

/**
 * Get the SVG symbol ID for a chip color.
 * @param color - Chip color
 * @returns Symbol ID for use in SVG href (e.g., "#chipBlack")
 */
export function getChipSymbolId(color: ChipColor): string {
  return `#chip${capitalizeFirst(color)}`;
}

/**
 * Convert an amount to visual chip stacks.
 * @param amount - Dollar amount to convert
 * @param maxPerColor - Maximum chips per color (default 5)
 * @returns Array of chip stacks for display
 */
export function amountToChipStacks(amount: number, maxPerColor = 5): ChipStack[] {
  const stacks: ChipStack[] = [];
  let remaining = amount;

  for (const color of CHIP_COLORS_ORDERED) {
    const value = CHIP_VALUES[color];
    const count = Math.floor(remaining / value);
    const displayed = Math.min(count, maxPerColor);

    if (displayed > 0) {
      stacks.push({ color, count: displayed });
      remaining -= displayed * value;
    }
  }

  // Always show at least one chip for any positive amount
  if (stacks.length === 0 && amount > 0) {
    stacks.push({ color: 'white', count: 1 });
  }

  return stacks;
}

/**
 * Create an array of indices for ngFor iteration.
 * @param count - Number of elements
 * @returns Array of indices [0, 1, 2, ..., count-1]
 */
export function createChipArray(count: number): number[] {
  return Array.from({ length: count }, (_, i) => i);
}

/**
 * Shared string utility functions.
 */

/**
 * Capitalize the first letter of a string.
 * @param str - String to capitalize
 * @returns String with first letter capitalized
 */
export function capitalizeFirst(str: string): string {
  if (!str) return str;
  return str.charAt(0).toUpperCase() + str.slice(1);
}

/** Card suit type */
export type CardSuit = 'spades' | 'hearts' | 'diamonds' | 'clubs';

/** Card rank type */
export type CardRank = 'A' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' | '10' | 'J' | 'Q' | 'K';

/** Full card representation */
export interface Card {
  rank: CardRank;
  suit: CardSuit;
}

/** Check if a suit is red (hearts or diamonds) */
export function isRedSuit(suit: CardSuit): boolean {
  return suit === 'hearts' || suit === 'diamonds';
}

/** Get Unicode symbol for a suit */
export function getSuitSymbol(suit: CardSuit): string {
  const symbols: Record<CardSuit, string> = {
    spades: '\u2660',   // ♠
    hearts: '\u2665',   // ♥
    diamonds: '\u2666', // ♦
    clubs: '\u2663'     // ♣
  };
  return symbols[suit];
}

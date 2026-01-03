namespace LowRollers.Api.Domain.Evaluation;

/// <summary>
/// Standard poker hand categories from highest to lowest.
/// </summary>
public enum HandCategory
{
    HighCard = 0,
    Pair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8,
    RoyalFlush = 9
}

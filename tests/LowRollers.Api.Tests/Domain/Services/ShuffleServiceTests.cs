using LowRollers.Api.Domain.Services;

namespace LowRollers.Api.Tests.Domain.Services;

public class ShuffleServiceTests
{
    private readonly ShuffleService _shuffleService = new();

    [Fact]
    public void Shuffle_PreservesAllElements()
    {
        // Arrange
        var items = Enumerable.Range(0, 52).ToList();
        var originalItems = items.ToList();

        // Act
        _shuffleService.Shuffle(items);

        // Assert
        Assert.Equal(52, items.Count);
        Assert.True(items.All(originalItems.Contains));
        Assert.True(originalItems.All(items.Contains));
    }

    [Fact]
    public void Shuffle_ChangesOrder()
    {
        // Arrange
        var items = Enumerable.Range(0, 52).ToList();
        var originalOrder = items.ToList();

        // Act
        _shuffleService.Shuffle(items);

        // Assert - Very unlikely to remain in same order (1 in 52! chance)
        Assert.NotEqual(originalOrder, items);
    }

    [Fact]
    public void Shuffle_ProducesUniformDistribution_ChiSquareTest()
    {
        // Arrange
        const int iterations = 100_000;
        const int deckSize = 52;

        // Track how many times each card appears in each position
        var positionCounts = new int[deckSize, deckSize];

        // Act - Shuffle many times and track positions
        for (int i = 0; i < iterations; i++)
        {
            var cards = Enumerable.Range(0, deckSize).ToList();
            _shuffleService.Shuffle(cards);

            for (int pos = 0; pos < deckSize; pos++)
            {
                positionCounts[cards[pos], pos]++;
            }
        }

        // Assert - Chi-square test for uniform distribution
        // Expected count: each card should appear in each position ~iterations/deckSize times
        double expectedCount = (double)iterations / deckSize;
        double chiSquare = 0;

        for (int card = 0; card < deckSize; card++)
        {
            for (int pos = 0; pos < deckSize; pos++)
            {
                double observed = positionCounts[card, pos];
                double diff = observed - expectedCount;
                chiSquare += (diff * diff) / expectedCount;
            }
        }

        // Degrees of freedom: (deckSize - 1) * (deckSize - 1) = 51 * 51 = 2601
        // Critical value at 99.9% confidence for df=2601 is approximately 2829
        // Our chi-square should be less than this critical value
        const double criticalValue = 2900; // Slightly above 99.9% critical value for safety

        Assert.True(chiSquare < criticalValue,
            $"Chi-square value {chiSquare:F2} exceeds critical value {criticalValue}. " +
            $"The shuffle may not be uniformly distributed.");
    }

    [Fact]
    public void Shuffle_HandlesSingleElement()
    {
        // Arrange
        var items = new List<int> { 42 };

        // Act
        _shuffleService.Shuffle(items);

        // Assert
        Assert.Single(items);
        Assert.Equal(42, items[0]);
    }

    [Fact]
    public void Shuffle_HandlesEmptyList()
    {
        // Arrange
        var items = new List<int>();

        // Act
        _shuffleService.Shuffle(items);

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public void Shuffle_ThrowsOnNullInput()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _shuffleService.Shuffle<int>(null!));
    }

    [Fact]
    public void ShuffleCopy_ReturnsNewShuffledArray()
    {
        // Arrange
        var original = Enumerable.Range(0, 52).ToArray();

        // Act
        var shuffled = _shuffleService.ShuffleCopy(original);

        // Assert
        Assert.NotSame(original, shuffled);
        Assert.Equal(original.Length, shuffled.Length);
        Assert.True(shuffled.All(original.Contains));
        // Original should be unchanged
        Assert.Equal(Enumerable.Range(0, 52), original);
    }

    [Fact]
    public void ShuffleCopy_ThrowsOnNullInput()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _shuffleService.ShuffleCopy<int>(null!));
    }

    [Fact]
    public void VerifyShuffle_ReturnsTrueForValidShuffle()
    {
        // Arrange
        var original = Enumerable.Range(0, 52).ToList();
        var shuffled = original.ToList();
        _shuffleService.Shuffle(shuffled);

        // Act
        var result = _shuffleService.VerifyShuffle(original, shuffled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyShuffle_ReturnsFalseForDifferentCounts()
    {
        // Arrange
        var original = Enumerable.Range(0, 52).ToList();
        var shuffled = Enumerable.Range(0, 51).ToList();

        // Act
        var result = _shuffleService.VerifyShuffle(original, shuffled);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyShuffle_ReturnsFalseForDifferentElements()
    {
        // Arrange
        var original = Enumerable.Range(0, 52).ToList();
        var modified = Enumerable.Range(1, 52).ToList(); // Different elements

        // Act
        var result = _shuffleService.VerifyShuffle(original, modified);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyShuffle_ReturnsTrueForEmptyLists()
    {
        // Arrange
        var original = new List<int>();
        var shuffled = new List<int>();

        // Act
        var result = _shuffleService.VerifyShuffle(original, shuffled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyShuffle_ReturnsTrueForSingleElement()
    {
        // Arrange
        var original = new List<int> { 42 };
        var shuffled = new List<int> { 42 };

        // Act
        var result = _shuffleService.VerifyShuffle(original, shuffled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyShuffle_ThrowsOnNullOriginal()
    {
        // Arrange
        var shuffled = new List<int> { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _shuffleService.VerifyShuffle<int>(null!, shuffled));
    }

    [Fact]
    public void VerifyShuffle_ThrowsOnNullShuffled()
    {
        // Arrange
        var original = new List<int> { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _shuffleService.VerifyShuffle(original, null!));
    }

    [Fact]
    public void VerifyShuffle_HandlesListsWithDuplicates()
    {
        // Arrange
        var original = new List<int> { 1, 1, 2, 2, 3, 3 };
        var shuffled = new List<int> { 3, 1, 2, 1, 3, 2 };

        // Act
        var result = _shuffleService.VerifyShuffle(original, shuffled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyShuffle_ReturnsFalseForDifferentDuplicateCounts()
    {
        // Arrange
        var original = new List<int> { 1, 1, 2, 2, 3, 3 };
        var shuffled = new List<int> { 1, 1, 1, 2, 3, 3 }; // Wrong count of 1s and 2s

        // Act
        var result = _shuffleService.VerifyShuffle(original, shuffled);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Shuffle_ProducesReasonableVariance_PositionTest()
    {
        // Arrange - Track where card 0 ends up after many shuffles
        const int iterations = 10_000;
        var positions = new int[52];

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var cards = Enumerable.Range(0, 52).ToList();
            _shuffleService.Shuffle(cards);
            positions[cards.IndexOf(0)]++;
        }

        // Assert - Card 0 should appear roughly equally in all positions
        double expectedPerPosition = iterations / 52.0;
        double tolerance = expectedPerPosition * 0.20; // 20% tolerance

        foreach (var count in positions)
        {
            Assert.InRange(count, expectedPerPosition - tolerance, expectedPerPosition + tolerance);
        }
    }
}

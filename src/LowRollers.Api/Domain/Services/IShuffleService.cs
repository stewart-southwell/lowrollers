namespace LowRollers.Api.Domain.Services;

/// <summary>
/// Service for cryptographically secure shuffling of collections.
/// </summary>
public interface IShuffleService
{
    /// <summary>
    /// Shuffles a list in-place using Fisher-Yates algorithm with cryptographically secure RNG.
    /// </summary>
    /// <typeparam name="T">Type of elements in the list.</typeparam>
    /// <param name="items">The list to shuffle.</param>
    void Shuffle<T>(IList<T> items);

    /// <summary>
    /// Creates a shuffled copy of the source collection.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collection.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <returns>A new shuffled array.</returns>
    T[] ShuffleCopy<T>(IEnumerable<T> source);

    /// <summary>
    /// Verifies that a shuffle was performed correctly by checking basic statistical properties.
    /// </summary>
    /// <typeparam name="T">Type of elements in the collections.</typeparam>
    /// <param name="original">The original unshuffled collection.</param>
    /// <param name="shuffled">The shuffled collection.</param>
    /// <returns>True if the shuffle appears valid, false otherwise.</returns>
    bool VerifyShuffle<T>(IReadOnlyList<T> original, IReadOnlyList<T> shuffled) where T : notnull;
}

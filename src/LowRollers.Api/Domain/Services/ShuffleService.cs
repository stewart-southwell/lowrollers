using System.Security.Cryptography;

namespace LowRollers.Api.Domain.Services;

/// <summary>
/// Provides cryptographically secure shuffling using Fisher-Yates algorithm.
/// Uses System.Security.Cryptography.RandomNumberGenerator for entropy.
/// </summary>
public sealed class ShuffleService : IShuffleService
{
    /// <inheritdoc />
    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count <= 1)
        {
            return;
        }

        // Fisher-Yates shuffle (modern variant, iterating backwards)
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        for (int i = items.Count - 1; i > 0; i--)
        {
            // Generate cryptographically secure random index j where 0 <= j <= i
            int j = RandomNumberGenerator.GetInt32(i + 1);

            // Swap items[i] and items[j]
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <inheritdoc />
    public T[] ShuffleCopy<T>(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var array = source.ToArray();
        Shuffle(array);
        return array;
    }

    /// <inheritdoc />
    public bool VerifyShuffle<T>(IReadOnlyList<T> original, IReadOnlyList<T> shuffled) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(shuffled);

        // Must have same count
        if (original.Count != shuffled.Count)
        {
            return false;
        }

        // Empty or single element is trivially valid
        if (original.Count <= 1)
        {
            return true;
        }

        // Must contain the same elements (same multiset)
        var originalCounts = CountElements(original);
        var shuffledCounts = CountElements(shuffled);

        if (originalCounts.Count != shuffledCounts.Count)
        {
            return false;
        }

        foreach (var kvp in originalCounts)
        {
            if (!shuffledCounts.TryGetValue(kvp.Key, out int count) || count != kvp.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<T, int> CountElements<T>(IReadOnlyList<T> items) where T : notnull
    {
        var counts = new Dictionary<T, int>();
        foreach (var item in items)
        {
            counts.TryGetValue(item, out int count);
            counts[item] = count + 1;
        }
        return counts;
    }
}

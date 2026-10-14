namespace Talib;

// Rolling-window extremes in one pass, shared by the windowed indicators
// (stochastic, williams %R, donchian, midpoint). Two monotonic deques —
// indices whose values strictly decrease (max) or increase (min) — give
// the window extreme at the front in O(1) amortized per bar, instead of
// re-walking each window.

/// <summary>Batch rolling-window extremes shared by the windowed indicators.</summary>
internal static class Window
{
  /// <summary>
  /// One (max, min) pair per full window, oldest window first; window k
  /// covers values <c>[k - period + 1, k]</c>. Equal values keep the
  /// earliest index at the deque front, so ties resolve to the first
  /// occurrence.
  /// </summary>
  /// <param name="values">The input series, oldest first.</param>
  /// <param name="period">The window size; the caller guarantees <c>values.Length >= period</c>.</param>
  /// <returns>The per-window maxima and minima, each of length <c>values.Length - period + 1</c>.</returns>
  public static (double[] Maxes, double[] Mins) MaxesMins(double[] values, int period)
  {
    int n = values.Length;
    var maxes = new double[n - period + 1];
    var mins = new double[n - period + 1];

    var maxQ = new int[n];
    var minQ = new int[n];
    int maxHead = 0, maxTail = 0, minHead = 0, minTail = 0;

    for (int i = 0; i < n; i++)
    {
      double value = values[i];

      // Drop strictly-worse candidates from the rear so each deque
      // stays monotonic; equals stay, keeping the earlier index ahead.
      while (maxHead < maxTail && values[maxQ[maxTail - 1]] < value)
      {
        maxTail--;
      }

      while (minHead < minTail && values[minQ[minTail - 1]] > value)
      {
        minTail--;
      }

      maxQ[maxTail++] = i;
      minQ[minTail++] = i;

      // Drop the front index once it slides out of the window.
      if (maxQ[maxHead] <= i - period)
      {
        maxHead++;
      }

      if (minQ[minHead] <= i - period)
      {
        minHead++;
      }

      if (i >= period - 1)
      {
        int k = i - period + 1;
        maxes[k] = values[maxQ[maxHead]];
        mins[k] = values[minQ[minHead]];
      }
    }

    return (maxes, mins);
  }

  /// <summary>
  /// One (maxIndex, minIndex) pair — absolute indices, earliest tie wins —
  /// per full window, oldest window first; window k covers values
  /// <c>[k - period + 1, k]</c>. Equal values keep the earliest index at
  /// the deque front, so ties resolve to the first occurrence. For
  /// Aroon's days-since-extreme.
  /// </summary>
  /// <param name="values">The input series, oldest first.</param>
  /// <param name="period">The window size; the caller guarantees <c>values.Length >= period</c>.</param>
  /// <returns>The per-window maxima and minima indices, each of length <c>values.Length - period + 1</c>.</returns>
  public static (int[] MaxIndices, int[] MinIndices) Extremes(double[] values, int period)
  {
    int n = values.Length;
    var maxIndices = new int[n - period + 1];
    var minIndices = new int[n - period + 1];

    var maxQ = new int[n];
    var minQ = new int[n];
    int maxHead = 0, maxTail = 0, minHead = 0, minTail = 0;

    for (int i = 0; i < n; i++)
    {
      double value = values[i];

      // Drop strictly-worse candidates from the rear so each deque
      // stays monotonic; equals stay, keeping the earlier index ahead.
      while (maxHead < maxTail && values[maxQ[maxTail - 1]] < value)
      {
        maxTail--;
      }

      while (minHead < minTail && values[minQ[minTail - 1]] > value)
      {
        minTail--;
      }

      maxQ[maxTail++] = i;
      minQ[minTail++] = i;

      // Drop the front index once it slides out of the window.
      if (maxQ[maxHead] <= i - period)
      {
        maxHead++;
      }

      if (minQ[minHead] <= i - period)
      {
        minHead++;
      }

      if (i >= period - 1)
      {
        int k = i - period + 1;
        maxIndices[k] = maxQ[maxHead];
        minIndices[k] = minQ[minHead];
      }
    }

    return (maxIndices, minIndices);
  }
}

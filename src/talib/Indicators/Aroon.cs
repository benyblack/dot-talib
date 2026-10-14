namespace Talib.Indicators;

/// <summary>
/// Aroon indicator (<see href="https://en.wikipedia.org/wiki/Aroon">Wikipedia</see>).
///
/// <para>
///   <c>Aroon Up = 100 * index of the window's highest high / period</c>
///   and <c>Aroon Down = 100 * index of the window's lowest low / period</c>.
/// </para>
///
/// The window spans <c>period + 1</c> prices, so 100 means the extreme
/// is the latest value and 0 means it is <c>period</c> bars old. On ties
/// the earliest occurrence in the window wins. Aroon Oscillator =
/// <c>Up - Down</c>. Prices are always given in chronological order,
/// oldest first; series are aligned with the input, and <c>null</c> is
/// returned when there is not enough data.
/// </summary>
public static class Aroon
{
  /// <summary>The Aroon Up / Down values at a single point.</summary>
  public readonly record struct AroonResult(double? Up, double? Down);

  /// <summary>Calculates the latest Aroon Up / Down, over closing prices.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The Aroon Up / Down at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static AroonResult? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period + 1)
    {
      return null;
    }

    int start = prices.Length - (period + 1);
    int maxIndex = start;
    int minIndex = start;
    for (int i = start + 1; i < prices.Length; i++)
    {
      // Only a strictly better value replaces the current extreme, so
      // the earliest tie wins.
      if (prices[i] > prices[maxIndex])
      {
        maxIndex = i;
      }

      if (prices[i] < prices[minIndex])
      {
        minIndex = i;
      }
    }

    return new AroonResult(100.0 * (maxIndex - start) / period, 100.0 * (minIndex - start) / period);
  }

  /// <summary>
  /// Calculates the full Aroon Up / Down series over closing prices.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period</c> entries are <c>null</c>, and every later entry is the
  /// Aroon Up / Down of the window ending at that point. On ties the
  /// earliest occurrence in the window wins.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The null-padded Aroon Up / Down series.</returns>
  public static AroonResult?[] AroonSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new AroonResult?[prices.Length];
    if (prices.Length < period + 1)
    {
      return result;
    }

    var (maxIndices, minIndices) = Window.Extremes(prices, period + 1);
    for (int k = 0; k < maxIndices.Length; k++)
    {
      int windowStart = k;
      result[k + period] = new AroonResult(
          100.0 * (maxIndices[k] - windowStart) / period,
          100.0 * (minIndices[k] - windowStart) / period);
    }

    return result;
  }

  /// <summary>The Aroon Oscillator: <c>Up - Down</c>, ranging from -100 to 100.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The oscillator at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? AroonOsc(double[] prices, int period = 14)
  {
    Guard.Period(period);
    AroonResult? latest = Latest(prices, period);
    return latest is null ? null : latest.Value.Up - latest.Value.Down;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price. The monotonic deques carry each extreme's
  // tick along with its value, so the days-since-extreme falls out the
  // same way the batch Window walk produces it for AroonSeries; Init's
  // series equals the batch series exactly and AroonNext continues it
  // bit for bit.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="AroonNext"/>. Not thread-safe.</summary>
  public sealed class AroonState
  {
    internal AroonState(int period)
    {
      Period = period;
      Max = new ExtremeState(period + 1, isMax: true);
      Min = new ExtremeState(period + 1, isMax: false);
    }

    internal int Period { get; }

    internal int Tick { get; set; } = -1;

    internal ExtremeState Max { get; }

    internal ExtremeState Min { get; }
  }

  /// <summary>
  /// Warms up streaming Aroon state from history (closing prices).
  ///
  /// Returns the state ready to continue the series one price at a time
  /// in O(1) via <see cref="AroonNext"/>, along with the full Up / Down
  /// series of the history (identical to <see cref="AroonSeries"/>).
  /// History needs at least <c>period + 1</c> prices.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period + 1</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (AroonState State, AroonResult?[] Values) AroonInit(double[] history, int period = 14)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period + 1, "aroon_init");

    var state = new AroonState(period);
    var values = new AroonResult?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = AroonNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming Aroon state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The Aroon Up / Down of the current window, or <c>null</c> while it is still filling.</returns>
  public static AroonResult? AroonNext(AroonState state, double price)
  {
    state.Tick++;
    (int Tick, double Value)? maxPair = state.Max.Push(price);
    (int Tick, double Value)? minPair = state.Min.Push(price);

    // Both extremes fill at the same tick, so both are null together.
    if (maxPair is null)
    {
      return null;
    }

    int windowStart = state.Tick - state.Period;
    double up = 100.0 * (maxPair.Value.Tick - windowStart) / state.Period;
    double down = 100.0 * (minPair!.Value.Tick - windowStart) / state.Period;
    return new AroonResult(up, down);
  }
}

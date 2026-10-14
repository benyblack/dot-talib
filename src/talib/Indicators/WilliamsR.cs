namespace Talib.Indicators;

/// <summary>
/// Williams %R (https://en.wikipedia.org/wiki/Williams_%25R).
///
/// <c>%R = (Highest High - Close) / (Highest High - Lowest Low) * -100</c>
///
/// Ranges from -100 (close at the window low) to 0 (close at the window
/// high). Prices are always given in chronological order, oldest first,
/// and the series is returned aligned with the input. <c>null</c> is
/// returned when there is not enough data or the window has no range
/// (flat prices), since %R is then undefined.
/// </summary>
public static class WilliamsR
{
  /// <summary>Calculates the latest Williams %R.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The %R at the latest point, or <c>null</c> when there is not enough data or the window has no range.</returns>
  public static double? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double highest = double.MinValue;
    double lowest = double.MaxValue;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      if (prices[i] > highest)
      {
        highest = prices[i];
      }

      if (prices[i] < lowest)
      {
        lowest = prices[i];
      }
    }

    if (highest == lowest)
    {
      return null;
    }

    return (highest - prices[^1]) / (highest - lowest) * -100;
  }

  /// <summary>
  /// Calculates the full Williams %R series.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry is
  /// %R of the window ending at that point. Windows with no range
  /// produce <c>null</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The null-padded %R series.</returns>
  public static double?[] WilliamsRSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    var (maxes, mins) = Window.MaxesMins(prices, period);
    for (int i = 0; i < maxes.Length; i++)
    {
      double highest = maxes[i];
      double lowest = mins[i];
      result[i + period - 1] = highest != lowest
          ? (highest - prices[i + period - 1]) / (highest - lowest) * -100
          : null;
    }

    return result;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price, rolling extremes via monotonic deques.
  // Init replays the history through the same deque walk the batch
  // series runs, so its series equals the batch series exactly and
  // Next continues it bit for bit.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="WilliamsRNext"/>. Not thread-safe.</summary>
  public sealed class WilliamsRState
  {
    internal WilliamsRState(int period)
    {
      Period = period;
      Max = new ExtremeState(period, isMax: true);
      Min = new ExtremeState(period, isMax: false);
    }

    internal int Period { get; }

    internal ExtremeState Max { get; }

    internal ExtremeState Min { get; }
  }

  /// <summary>
  /// Warms up streaming Williams %R state from history.
  ///
  /// Returns the state ready to continue the series one price at a time
  /// in O(1) via <see cref="WilliamsRNext"/>, along with the full %R
  /// series of the history (identical to <see cref="WilliamsRSeries"/>).
  /// History needs at least <c>period</c> prices.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (WilliamsRState State, double?[] Values) WilliamsRInit(double[] history, int period = 14)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "williams_r_init");

    var state = new WilliamsRState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = WilliamsRNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming Williams %R state by one price, in O(1). A flat window (no range) yields <c>null</c>, like the batch series.</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The %R of the current window, or <c>null</c> while it is still filling or has no range.</returns>
  public static double? WilliamsRNext(WilliamsRState state, double price)
  {
    (int Tick, double Value)? maxPair = state.Max.Push(price);
    (int Tick, double Value)? minPair = state.Min.Push(price);

    // Both extremes fill at the same tick, so both are null together.
    if (maxPair is null || minPair is null)
    {
      return null;
    }

    double highest = maxPair.Value.Value;
    double lowest = minPair.Value.Value;
    if (highest != lowest)
    {
      return (highest - price) / (highest - lowest) * -100;
    }

    return null;
  }
}

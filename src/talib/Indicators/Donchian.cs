namespace Talib.Indicators;

/// <summary>
/// Donchian Channels (https://en.wikipedia.org/wiki/Donchian_channel).
///
/// upper = Highest High over the window, lower = Lowest Low over the
/// window, middle = (upper + lower) / 2.
///
/// Bars carry at least a High and a Low. Prices are always given in
/// chronological order, oldest first, and the series is returned aligned
/// with the input. <c>null</c> is returned when there is not enough
/// data.
/// </summary>
public static class Donchian
{
  /// <summary>The Donchian channels of one window.</summary>
  /// <param name="Upper">The window's highest high.</param>
  /// <param name="Middle">The midprice, <c>(upper + lower) / 2</c>.</param>
  /// <param name="Lower">The window's lowest low.</param>
  public sealed record DonchianResult(double Upper, double Middle, double Lower);

  /// <summary>Calculates the latest Donchian Channels.</summary>
  /// <param name="bars">Bars, oldest first; only High and Low are read.</param>
  /// <param name="period">Lookback window, a positive integer (default 20).</param>
  /// <returns>The channels at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static DonchianResult? Latest(Bar[] bars, int period = 20)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double highest = double.MinValue;
    double lowest = double.MaxValue;
    for (int i = bars.Length - period; i < bars.Length; i++)
    {
      if (bars[i].High > highest)
      {
        highest = bars[i].High;
      }

      if (bars[i].Low < lowest)
      {
        lowest = bars[i].Low;
      }
    }

    return new DonchianResult(highest, (highest + lowest) / 2, lowest);
  }

  /// <summary>
  /// Calculates the full Donchian Channels series.
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry
  /// holds the upper, middle and lower channel of the window ending at
  /// that point.
  /// </summary>
  /// <param name="bars">Bars, oldest first; only High and Low are read.</param>
  /// <param name="period">Lookback window, a positive integer (default 20).</param>
  /// <returns>The null-padded channel series.</returns>
  public static DonchianResult?[] DonchianSeries(Bar[] bars, int period = 20)
  {
    Guard.Period(period);
    var result = new DonchianResult?[bars.Length];
    if (bars.Length < period)
    {
      return result;
    }

    var highs = new double[bars.Length];
    var lows = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      highs[i] = bars[i].High;
      lows[i] = bars[i].Low;
    }

    // The upper line is the rolling max of the highs and the lower
    // line the rolling min of the lows, so only one side of each
    // batch extremes pass is used.
    var (maxes, _) = Window.MaxesMins(highs, period);
    var (_, mins) = Window.MaxesMins(lows, period);
    for (int i = 0; i < maxes.Length; i++)
    {
      result[i + period - 1] = new DonchianResult(maxes[i], (maxes[i] + mins[i]) / 2, mins[i]);
    }

    return result;
  }

  /// <summary>
  /// Calculates the Midprice of the <c>period</c> newest bars:
  /// <c>(highest high + lowest low) / 2</c> — the middle Donchian
  /// channel line on its own.
  /// </summary>
  /// <param name="bars">Bars, oldest first; only High and Low are read.</param>
  /// <param name="period">Lookback window, a positive integer (default 20).</param>
  /// <returns>The midprice at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Midprice(Bar[] bars, int period = 20)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double highest = double.MinValue;
    double lowest = double.MaxValue;
    for (int i = bars.Length - period; i < bars.Length; i++)
    {
      if (bars[i].High > highest)
      {
        highest = bars[i].High;
      }

      if (bars[i].Low < lowest)
      {
        lowest = bars[i].Low;
      }
    }

    return (highest + lowest) / 2;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per bar, rolling extremes via monotonic deques.
  // Init replays the history through the same deque walk the batch
  // series runs, so its series equals the batch series exactly and
  // Next continues it bit for bit.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="DonchianNext"/>. Not thread-safe.</summary>
  public sealed class DonchianState
  {
    internal DonchianState(int period)
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
  /// Warms up streaming Donchian Channels state from history.
  ///
  /// Returns the state ready to continue the series one bar at a time
  /// in O(1) via <see cref="DonchianNext"/>, along with the full
  /// channel series of the history (identical to
  /// <see cref="DonchianSeries"/>). History needs at least
  /// <c>period</c> bars.
  /// </summary>
  /// <param name="history">Bars, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer (default 20).</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (DonchianState State, DonchianResult?[] Values) DonchianInit(Bar[] history, int period = 20)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "donchian_init");

    var state = new DonchianState(period);
    var values = new DonchianResult?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = DonchianNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming Donchian Channels state by one bar, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="bar">The incoming bar; only High and Low are read.</param>
  /// <returns>The channels of the current window, or <c>null</c> while it is still filling.</returns>
  public static DonchianResult? DonchianNext(DonchianState state, Bar bar)
  {
    (int Tick, double Value)? maxPair = state.Max.Push(bar.High);
    (int Tick, double Value)? minPair = state.Min.Push(bar.Low);

    // Both extremes fill at the same tick, so both are null together.
    if (maxPair is null || minPair is null)
    {
      return null;
    }

    double highest = maxPair.Value.Value;
    double lowest = minPair.Value.Value;
    return new DonchianResult(highest, (highest + lowest) / 2, lowest);
  }
}

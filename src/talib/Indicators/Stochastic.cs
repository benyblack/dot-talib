namespace Talib.Indicators;

/// <summary>
/// Stochastic oscillator
/// (<see href="https://en.wikipedia.org/wiki/Stochastic_oscillator">Wikipedia</see>).
///
/// <c>%K = (Close - Lowest Low) / (Highest High - Lowest Low) * 100</c>
/// and <c>%D = SMA(%K, d_period)</c>. <c>null</c> is returned when there
/// is not enough data or the window has no range (flat prices), since
/// <c>%K</c> is then undefined.
///
/// Prices are always given in chronological order, oldest first. Note
/// that with a plain price list, close, high and low all come from the
/// same series; for proper high/low support an OHLC input shape is
/// needed.
/// </summary>
public static class Stochastic
{
  /// <summary>The slowed <c>%K</c> and <c>%D</c> values at a single point.</summary>
  public readonly record struct StochPoint(double? K, double? D);

  /// <summary>The slowed <c>%K</c> and <c>%D</c> series, each aligned with the input prices.</summary>
  public sealed record StochResult(double?[] K, double?[] D);

  /// <summary>
  /// Calculates
  /// <c>%K = (Current Close - Lowest Low) / (Highest High - Lowest Low) * 100</c>
  /// over the <c>period</c> newest prices. A zero-range (flat) window
  /// has an undefined <c>%K</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>
  /// The <c>%K</c> at the latest point, or <c>null</c> when there is
  /// not enough data or the window has no range.
  /// </returns>
  public static double? K(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double lowest = double.MaxValue;
    double highest = double.MinValue;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      if (prices[i] < lowest)
      {
        lowest = prices[i];
      }

      if (prices[i] > highest)
      {
        highest = prices[i];
      }
    }

    if (highest == lowest)
    {
      return null;
    }

    return (prices[^1] - lowest) / (highest - lowest) * 100;
  }

  /// <summary>
  /// Calculates the full <c>%K</c> series. Returns an array aligned
  /// with <paramref name="prices"/>: the first <c>period - 1</c>
  /// entries are <c>null</c>, and every later entry is <c>%K</c> of
  /// the window ending at that point. Windows with no range (flat
  /// prices) produce <c>null</c>, since <c>%K</c> is then undefined.
  /// For example, <c>KSeries([1, 6, 2, 5, 3], 3)</c> is
  /// <c>[null, null, 20.0, 75.0, 33.33333333333333]</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded <c>%K</c> series.</returns>
  public static double?[] KSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    var (maxes, mins) = Window.MaxesMins(prices, period);
    for (int k = 0; k < maxes.Length; k++)
    {
      double highest = maxes[k];
      double lowest = mins[k];
      result[k + period - 1] = highest != lowest
          ? (prices[k + period - 1] - lowest) / (highest - lowest) * 100
          : null;
    }

    return result;
  }

  /// <summary>
  /// Calculates <c>%D</c>: the <paramref name="dPeriod"/>-period
  /// simple moving average of <c>%K</c>. For example,
  /// <c>D([1, 6, 2, 5, 3, 7, 4], 5, 3)</c> is <c>60.0</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period"><c>%K</c> lookback window.</param>
  /// <param name="dPeriod">SMA period applied to <c>%K</c>.</param>
  /// <returns>
  /// The <c>%D</c> at the latest point, or <c>null</c> when there is
  /// not enough data or any of the last <c>dPeriod</c> <c>%K</c>
  /// values is undefined.
  /// </returns>
  public static double? D(double[] prices, int period = 14, int dPeriod = 3)
  {
    Guard.Period(period);
    Guard.Period(dPeriod);
    if (prices.Length < period + dPeriod - 1)
    {
      return null;
    }

    double?[] last = KSeries(prices, period)[^dPeriod..];
    double sum = 0;
    foreach (double? k in last)
    {
      if (k is null)
      {
        return null;
      }

      sum += k.Value;
    }

    return sum / dPeriod;
  }

  /// <summary>
  /// Calculates the slow stochastic (TA-Lib STOCH): the slowed
  /// <c>%K</c> line is the SMA of the fast <c>%K</c> over
  /// <paramref name="slowing"/> bars, and <c>%D</c> is the SMA of the
  /// slowed <c>%K</c>. For example, <c>Slow([1, 6, 2, 5, 3, 7, 4], 3, 2, 2)</c>
  /// gives <c>K = 62.5</c> and <c>D = 64.5833</c> (rounded to 4).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period"><c>%K</c> lookback window.</param>
  /// <param name="slowing">SMA period applied to fast <c>%K</c>.</param>
  /// <param name="dPeriod">SMA period applied to the slowed <c>%K</c>.</param>
  /// <returns>
  /// The slowed <c>%K</c> and <c>%D</c> at the latest point; each is
  /// <c>null</c> when its own data requirement is not met.
  /// </returns>
  public static StochPoint Slow(double[] prices, int period = 14, int slowing = 3, int dPeriod = 3)
  {
    Guard.Period(period);
    Guard.Period(slowing);
    Guard.Period(dPeriod);
    if (prices.Length < period + slowing + dPeriod - 2)
    {
      return new StochPoint(null, null);
    }

    double[] kFast = KSeries(prices, period)
        .Where(v => v.HasValue)
        .Select(v => v!.Value)
        .ToArray();
    if (kFast.Length < slowing)
    {
      return new StochPoint(null, null);
    }

    double[] kSlow = SmaValues(kFast, slowing);
    if (kSlow.Length < dPeriod)
    {
      return new StochPoint(kSlow[^1], null);
    }

    double[] dSlow = SmaValues(kSlow, dPeriod);
    return new StochPoint(kSlow[^1], dSlow[^1]);
  }

  /// <summary>
  /// Calculates the full slow-stochastic series (TA-Lib STOCH): the
  /// slowed <c>%K</c> line is the SMA of the fast <c>%K</c> over
  /// <paramref name="slowing"/> bars, and <c>%D</c> is the SMA of the
  /// slowed <c>%K</c>. Bars where the fast <c>%K</c> is undefined
  /// (unfilled or flat window) are skipped by the smoothers entirely,
  /// so their outputs are <c>null</c> there, exactly like the
  /// streaming walk.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period"><c>%K</c> lookback window.</param>
  /// <param name="slowing">SMA period applied to fast <c>%K</c>.</param>
  /// <param name="dPeriod">SMA period applied to the slowed <c>%K</c>.</param>
  /// <returns>The null-padded slowed <c>%K</c> and <c>%D</c> series.</returns>
  public static StochResult SlowSeries(double[] prices, int period = 14, int slowing = 3, int dPeriod = 3)
  {
    Guard.Period(period);
    Guard.Period(slowing);
    Guard.Period(dPeriod);
    var kSeries = new double?[prices.Length];
    var dSeries = new double?[prices.Length];
    if (prices.Length < period)
    {
      return new StochResult(kSeries, dSeries);
    }

    var (maxes, mins) = Window.MaxesMins(prices, period);
    var slow = new SumState(slowing);
    var d = new SumState(dPeriod);
    for (int k = 0; k < maxes.Length; k++)
    {
      double highest = maxes[k];
      double lowest = mins[k];
      if (highest == lowest)
      {
        continue;
      }

      double kFast = (prices[k + period - 1] - lowest) / (highest - lowest) * 100;
      double? sum = slow.Push(kFast);
      if (sum is null)
      {
        continue;
      }

      double kSlow = sum.Value / slowing;
      kSeries[k + period - 1] = kSlow;
      double? dSum = d.Push(kSlow);
      dSeries[k + period - 1] = dSum.HasValue ? dSum.Value / dPeriod : null;
    }

    return new StochResult(kSeries, dSeries);
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price, rolling extremes via monotonic deques.
  // Init replays the history through the same arithmetic the batch
  // functions run, so the series continue the batch results bit for
  // bit. A flat window leaves %K undefined — KNext returns null for
  // it, and like the batch slow lines the smoothers skip those bars
  // entirely.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="KNext"/>. Not thread-safe.</summary>
  public sealed class KState
  {
    internal KState(int period)
    {
      Period = period;
      Max = new ExtremeState(period, true);
      Min = new ExtremeState(period, false);
    }

    internal int Period { get; }

    internal ExtremeState Max { get; }

    internal ExtremeState Min { get; }
  }

  /// <summary>Streaming state for <see cref="SlowNext"/>. Not thread-safe.</summary>
  public sealed class SlowState
  {
    internal SlowState(int period, int slowing, int dPeriod)
    {
      K = new KState(period);
      Slowing = slowing;
      DPeriod = dPeriod;
      Slow = new SumState(slowing);
      D = new SumState(dPeriod);
    }

    internal KState K { get; }

    internal int Slowing { get; }

    internal int DPeriod { get; }

    internal SumState Slow { get; }

    internal SumState D { get; }
  }

  /// <summary>
  /// Warms up streaming fast-<c>%K</c> state from history: returns the
  /// state and the full <c>%K</c> series of the history (identical to
  /// <see cref="KSeries"/>), continuing in O(1) via
  /// <see cref="KNext"/>. History needs at least <c>period</c> prices.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (KState State, double?[] Values) KInit(double[] history, int period = 14)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "k_init");

    var state = new KState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = KNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>
  /// Advances streaming fast-<c>%K</c> state by one price, in O(1). A
  /// flat window (no range) yields <c>null</c>, like the batch series.
  /// </summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>
  /// The <c>%K</c> as of this price, or <c>null</c> while the window
  /// is still filling or has no range.
  /// </returns>
  public static double? KNext(KState state, double price)
  {
    (int Tick, double Value)? maxPair = state.Max.Push(price);
    (int Tick, double Value)? minPair = state.Min.Push(price);
    if (maxPair is null)
    {
      return null;
    }

    double highest = maxPair.Value.Value;
    double lowest = minPair!.Value.Value;
    return highest != lowest
        ? (price - lowest) / (highest - lowest) * 100
        : null;
  }

  /// <summary>
  /// Warms up streaming slow-stochastic state (TA-Lib STOCH) from
  /// history: fast <c>%K</c> over <paramref name="period"/>, slowed by
  /// an SMA of <paramref name="slowing"/>, <c>%D</c> an SMA of
  /// <paramref name="dPeriod"/> over the slowed line. Returns the
  /// state and the two aligned series (identical to
  /// <see cref="SlowSeries"/>), continuing in O(1) via
  /// <see cref="SlowNext"/>. History needs at least
  /// <c>period + slowing + dPeriod - 2</c> prices.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period + slowing + dPeriod - 2</c> of them.</param>
  /// <param name="period"><c>%K</c> lookback window.</param>
  /// <param name="slowing">SMA period applied to fast <c>%K</c>.</param>
  /// <param name="dPeriod">SMA period applied to the slowed <c>%K</c>.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (SlowState State, StochResult Values) SlowInit(
      double[] history,
      int period = 14,
      int slowing = 3,
      int dPeriod = 3)
  {
    Guard.Period(period);
    Guard.Period(slowing);
    Guard.Period(dPeriod);
    Guard.Warmup(history.Length, period + slowing + dPeriod - 2, "slow_init");

    var state = new SlowState(period, slowing, dPeriod);
    var kSeries = new double?[history.Length];
    var dSeries = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      StochPoint point = SlowNext(state, history[i]);
      kSeries[i] = point.K;
      dSeries[i] = point.D;
    }

    return (state, new StochResult(kSeries, dSeries));
  }

  /// <summary>Advances streaming slow-stochastic state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>
  /// The slowed <c>%K</c> and <c>%D</c> as of this price. Both are
  /// <c>null</c> while the fast <c>%K</c> is still undefined (unfilled
  /// or flat window) or the slowing window has not filled;
  /// <c>%D</c> alone stays <c>null</c> until its own window fills.
  /// </returns>
  public static StochPoint SlowNext(SlowState state, double price)
  {
    double? k = KNext(state.K, price);
    if (k is null)
    {
      return new StochPoint(null, null);
    }

    double? sum = state.Slow.Push(k.Value);
    if (sum is null)
    {
      return new StochPoint(null, null);
    }

    double kSlow = sum.Value / state.Slowing;
    double? dSum = state.D.Push(kSlow);
    return new StochPoint(kSlow, dSum.HasValue ? dSum.Value / state.DPeriod : null);
  }

  // SMA over a null-free series, from the first full window on — the
  // arithmetic of Ma.SmaSeries (fill sum then slide), unpadded.
  private static double[] SmaValues(double[] values, int period)
  {
    var result = new double[values.Length - period + 1];
    double sum = 0;
    for (int i = 0; i < period; i++)
    {
      sum += values[i];
    }

    result[0] = sum / period;
    for (int i = period; i < values.Length; i++)
    {
      sum = sum - values[i - period] + values[i];
      result[i - period + 1] = sum / period;
    }

    return result;
  }
}

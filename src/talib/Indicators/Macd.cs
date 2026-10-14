namespace Talib.Indicators;

/// <summary>
/// MACD indicator (<see href="https://en.wikipedia.org/wiki/MACD">Wikipedia</see>).
///
/// Calculates the MACD line, the signal line and the histogram. Prices
/// are always given in chronological order, oldest first. Every function
/// returns <c>null</c> when there is not enough data.
///
/// The MACD line at a point is defined once <c>max(fast, slow)</c>
/// prices have been seen, but — keeping the behavior of earlier
/// releases — the scalar functions require twice that much history (and
/// <see cref="MacdAll"/> derives its guards from the same rule) so that
/// the EMA seed has had time to wash out.
/// </summary>
public static class Macd
{
  /// <summary>The MACD line, signal line and histogram values at a single point.</summary>
  public readonly record struct MacdPoint(double? Macd, double? Signal, double? Histogram);

  /// <summary>The MACD line, signal line and histogram series, each aligned with the input prices.</summary>
  public sealed record MacdResult(double?[] Macd, double?[] Signal, double?[] Histogram);

  /// <summary>
  /// Calculates the latest MACD line value:
  /// <c>EMA(prices, fast) - EMA(prices, slow)</c>. For example,
  /// <c>Line([1, 4, 3, 5, 2, 6], 3, 2)</c> rounds to <c>0.4861</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Period of the fast EMA.</param>
  /// <param name="slow">Period of the slow EMA.</param>
  /// <returns>
  /// The MACD line at the latest point, or <c>null</c> when there are
  /// fewer than <c>max(fast, slow) * 2</c> prices.
  /// </returns>
  public static double? Line(double[] prices, int fast = 12, int slow = 26)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    if (prices.Length < slow * 2 || prices.Length < fast * 2)
    {
      return null;
    }

    double[] values = SeededValues(prices, Math.Min(fast, slow), Math.Max(fast, slow));
    return values[^1];
  }

  /// <summary>
  /// Calculates the full MACD line series from one fast/slow EMA pass.
  /// Returns an array aligned with <paramref name="prices"/>: entries
  /// before index <c>max(fast, slow) - 1</c> are <c>null</c>, every
  /// later entry is the MACD line at that point. From
  /// <c>max(fast, slow) * 2</c> bars on, the seed has washed out and
  /// this equals <c>EMA(fast) - EMA(slow)</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Period of the fast EMA.</param>
  /// <param name="slow">Period of the slow EMA.</param>
  /// <returns>The null-padded MACD line series.</returns>
  public static double?[] MacdSeries(double[] prices, int fast = 12, int slow = 26)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    var result = new double?[prices.Length];
    int slowPeriod = Math.Max(fast, slow);
    int fastPeriod = Math.Min(fast, slow);
    if (prices.Length < slowPeriod)
    {
      return result;
    }

    double[] values = SeededValues(prices, fastPeriod, slowPeriod);
    for (int i = 0; i < values.Length; i++)
    {
      result[i + slowPeriod - 1] = values[i];
    }

    return result;
  }

  /// <summary>
  /// Calculates the latest signal line value: an EMA with the given
  /// <paramref name="period"/> over the MACD series of the last
  /// <c>period * 2</c> windows. For example,
  /// <c>Signal([5, 1, 6, 4, 7, 3, 8, 2, 9], 2, 3, 2)</c> rounds to
  /// <c>0.3372</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">EMA period applied to the MACD series.</param>
  /// <param name="fast">Period of the fast MACD EMA.</param>
  /// <param name="slow">Period of the slow MACD EMA.</param>
  /// <returns>
  /// The signal line at the latest point, or <c>null</c> when there
  /// are fewer than <c>max(fast, slow) * 2 + period * 2 - 1</c> prices
  /// (every MACD value in the signal window must be defined).
  /// </returns>
  public static double? Signal(double[] prices, int period = 9, int fast = 12, int slow = 26)
  {
    Guard.Period(period);
    Guard.Period(fast);
    Guard.Period(slow);
    if (prices.Length < Math.Max(fast, slow) * 2 + period * 2 - 1)
    {
      return null;
    }

    double[] values = SeededValues(prices, Math.Min(fast, slow), Math.Max(fast, slow));
    return SignalFromValues(values, period);
  }

  /// <summary>
  /// Calculates the latest MACD histogram value: MACD line minus
  /// signal line. For example,
  /// <c>Histogram([5, 1, 6, 4, 7, 3, 8, 2, 9], 3, 2, 2)</c> rounds to
  /// <c>0.316</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="macdFast">Fast EMA period of the MACD line.</param>
  /// <param name="macdSlow">Slow EMA period of the MACD line.</param>
  /// <param name="signalPeriod">EMA period of the signal line.</param>
  /// <returns>The histogram at the latest point, or <c>null</c> when either input value is undefined.</returns>
  public static double? Histogram(double[] prices, int macdFast = 12, int macdSlow = 26, int signalPeriod = 9)
  {
    Guard.Period(macdFast);
    Guard.Period(macdSlow);
    Guard.Period(signalPeriod);
    return MacdAll(prices, macdFast, macdSlow, signalPeriod).Histogram;
  }

  /// <summary>
  /// Calculates the MACD line, signal line and histogram of the latest
  /// point in a single pass over the prices (one fast/slow EMA walk
  /// shared by all three values). Each value is <c>null</c> when its
  /// own data requirement is not met, so e.g. the MACD line can be
  /// defined while the signal line is still <c>null</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Period of the fast MACD EMA.</param>
  /// <param name="slow">Period of the slow MACD EMA.</param>
  /// <param name="signalPeriod">EMA period of the signal line.</param>
  /// <returns>The three latest values as a <see cref="MacdPoint"/>.</returns>
  public static MacdPoint MacdAll(double[] prices, int fast = 12, int slow = 26, int signalPeriod = 9)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    Guard.Period(signalPeriod);

    int needed = Math.Max(fast, slow) * 2;
    double? macdValue = null;
    double? signalValue = null;
    if (prices.Length >= Math.Max(fast, slow))
    {
      double[] series = SeededValues(prices, Math.Min(fast, slow), Math.Max(fast, slow));
      if (prices.Length >= needed)
      {
        macdValue = series[^1];
      }

      if (prices.Length >= needed + signalPeriod * 2 - 1)
      {
        signalValue = SignalFromValues(series, signalPeriod);
      }
    }

    double? histogramValue = macdValue.HasValue && signalValue.HasValue
        ? macdValue.Value - signalValue.Value
        : null;
    return new MacdPoint(macdValue, signalValue, histogramValue);
  }

  /// <summary>
  /// MACD with the TA-Lib MACDFIX signal convention: the signal line
  /// is the EMA over the whole MACD series starting at its first
  /// defined value (rather than a trailing window). This is also the
  /// convention the streaming state continues. For example,
  /// <c>MacdFix([1, 4, 3, 5, 2, 6], 3, 2, 2).Macd</c> rounds to
  /// <c>0.4861</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Period of the fast MACD EMA.</param>
  /// <param name="slow">Period of the slow MACD EMA.</param>
  /// <param name="signalPeriod">EMA period of the signal line.</param>
  /// <returns>The three latest values as a <see cref="MacdPoint"/>.</returns>
  public static MacdPoint MacdFix(double[] prices, int fast = 12, int slow = 26, int signalPeriod = 9)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    Guard.Period(signalPeriod);
    if (prices.Length < Math.Max(fast, slow))
    {
      return new MacdPoint(null, null, null);
    }

    double[] macdLine = SeededValues(prices, Math.Min(fast, slow), Math.Max(fast, slow));
    double? macdValue = macdLine[^1];
    double? signalValue = macdLine.Length >= signalPeriod
        ? Ma.EmaSeries(macdLine, signalPeriod)[^1]
        : null;
    double? histogramValue = macdValue.HasValue && signalValue.HasValue
        ? macdValue.Value - signalValue.Value
        : null;
    return new MacdPoint(macdValue, signalValue, histogramValue);
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price. Init replays the history through the
  // same seeded fast/slow walk as the batch series and the same
  // whole-series signal EMA as MacdFix (the MACDFIX convention — the
  // natural one to continue a feed from, and deliberately different
  // from the trailing-window batch signal), so the three series
  // continue the batch results bit for bit. As everywhere, fast and
  // slow are normalized so the slow line is the longer one; history
  // needs at least slow + signalPeriod - 1 prices.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="MacdNext"/>. Not thread-safe.</summary>
  public sealed class MacdState
  {
    internal MacdState(int fast, int slow, int signalPeriod)
    {
      Fast = fast;
      Slow = slow;
      SignalPeriod = signalPeriod;
      FastK = 2.0 / (fast + 1);
      SlowK = 2.0 / (slow + 1);
      SignalK = 2.0 / (signalPeriod + 1);
      Prefix = new Queue<double>(slow);
      SignalPrefix = new Queue<double>(signalPeriod);
    }

    internal int Fast { get; }

    internal int Slow { get; }

    internal int SignalPeriod { get; }

    internal double FastK { get; }

    internal double SlowK { get; }

    internal double SignalK { get; }

    internal Queue<double> Prefix { get; }

    internal int PrefixCount { get; set; }

    internal double? F { get; set; }

    internal double? S { get; set; }

    internal Queue<double> SignalPrefix { get; }

    internal int SignalCount { get; set; }

    internal double? Sig { get; set; }
  }

  /// <summary>
  /// Warms up streaming MACD state from history. Returns the state and
  /// the three aligned series of the history: the MACD line equals
  /// <see cref="MacdSeries"/>, and the signal line is the whole-series
  /// EMA of it (the <see cref="MacdFix"/> convention). History needs
  /// at least <c>slow + signalPeriod - 1</c> prices.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>slow + signalPeriod - 1</c> of them.</param>
  /// <param name="fast">Period of the fast MACD EMA.</param>
  /// <param name="slow">Period of the slow MACD EMA.</param>
  /// <param name="signalPeriod">EMA period of the signal line.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (MacdState State, MacdResult Values) MacdInit(
      double[] history,
      int fast = 12,
      int slow = 26,
      int signalPeriod = 9)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    Guard.Period(signalPeriod);
    if (fast > slow)
    {
      (fast, slow) = (slow, fast);
    }

    Guard.Warmup(history.Length, slow + signalPeriod - 1, "macd_init");

    var state = new MacdState(fast, slow, signalPeriod);
    var macd = new double?[history.Length];
    var signal = new double?[history.Length];
    var histogram = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      MacdPoint point = MacdNext(state, history[i]);
      macd[i] = point.Macd;
      signal[i] = point.Signal;
      histogram[i] = point.Histogram;
    }

    return (state, new MacdResult(macd, signal, histogram));
  }

  /// <summary>Advances streaming MACD state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>
  /// The MACD line, signal line and histogram as of this price; the
  /// signal line (and histogram) is <c>null</c> until its seed window
  /// has filled.
  /// </returns>
  public static MacdPoint MacdNext(MacdState state, double price)
  {
    if (state.PrefixCount < state.Slow - 1)
    {
      state.Prefix.Enqueue(price);
      state.PrefixCount++;
      return new MacdPoint(null, null, null);
    }

    if (state.PrefixCount == state.Slow - 1)
    {
      // The slow EMA is seeded with the average of the first slow
      // prices and the fast EMA with the average of the last fast
      // of those, exactly like the batch series.
      state.Prefix.Enqueue(price);
      state.PrefixCount++;
      double slowSeed = 0;
      double fastSeed = 0;
      int skip = state.Slow - state.Fast;
      int i = 0;
      foreach (double p in state.Prefix)
      {
        slowSeed += p;
        if (i++ >= skip)
        {
          fastSeed += p;
        }
      }

      slowSeed /= state.Slow;
      fastSeed /= state.Fast;
      state.F = fastSeed;
      state.S = slowSeed;
      return SignalStep(state, fastSeed - slowSeed);
    }

    double f = (price - state.F!.Value) * state.FastK + state.F.Value;
    double s = (price - state.S!.Value) * state.SlowK + state.S.Value;
    state.F = f;
    state.S = s;
    return SignalStep(state, f - s);
  }

  // The MACDFIX signal walk: seeded with the SMA of the first
  // signalPeriod MACD values, then stepped with
  // sig + k * (macd - sig).
  private static MacdPoint SignalStep(MacdState state, double macd)
  {
    if (state.SignalCount < state.SignalPeriod - 1)
    {
      state.SignalPrefix.Enqueue(macd);
      state.SignalCount++;
      return new MacdPoint(macd, null, null);
    }

    if (state.SignalCount == state.SignalPeriod - 1)
    {
      state.SignalPrefix.Enqueue(macd);
      state.SignalCount++;
      double sum = 0;
      foreach (double m in state.SignalPrefix)
      {
        sum += m;
      }

      double signal = sum / state.SignalPeriod;
      state.Sig = signal;
      return new MacdPoint(macd, signal, macd - signal);
    }

    double next = state.Sig!.Value + state.SignalK * (macd - state.Sig.Value);
    state.Sig = next;
    return new MacdPoint(macd, next, macd - next);
  }

  // One fast/slow EMA walk with TA-Lib's MACD seeding: the slow EMA is
  // seeded with the average of the first slow prices and the fast EMA
  // with the average of the last fast of those (both windows end at
  // index slow - 1), after which the two advance in lockstep. Assumes
  // fast <= slow and prices.Length >= slow.
  private static double[] SeededValues(double[] prices, int fast, int slow)
  {
    double slowSeed = 0;
    for (int i = 0; i < slow; i++)
    {
      slowSeed += prices[i];
    }

    slowSeed /= slow;

    double fastSeed = 0;
    for (int i = slow - fast; i < slow; i++)
    {
      fastSeed += prices[i];
    }

    fastSeed /= fast;

    double fastK = 2.0 / (fast + 1);
    double slowK = 2.0 / (slow + 1);

    var values = new double[prices.Length - slow + 1];
    double f = fastSeed;
    double s = slowSeed;
    values[0] = fastSeed - slowSeed;
    for (int i = slow; i < prices.Length; i++)
    {
      f = (prices[i] - f) * fastK + f;
      s = (prices[i] - s) * slowK + s;
      values[i - slow + 1] = f - s;
    }

    return values;
  }

  // EMA over the last period * 2 MACD values that satisfy the scalar
  // MACD data requirement — the trailing window of the batch signal.
  private static double? SignalFromValues(double[] values, int period)
  {
    return Ma.Ema(values[^Math.Min(2 * period, values.Length)..], period);
  }
}

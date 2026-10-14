namespace Talib.Indicators;

/// <summary>
/// Average True Range <see href="https://en.wikipedia.org/wiki/Average_true_range"/>.
///
/// The True Range of a bar is <c>max(high - low, |high - prev_close|,
/// |low - prev_close|)</c> (just <c>high - low</c> for the first bar).
/// ATR is Wilder's smoothed average (RMA) of the True Range series,
/// seeded with the SMA of the first <c>period</c> True Ranges — the
/// standard TA-Lib convention.
///
/// Bars are always given in chronological order, oldest first. Series
/// functions return results aligned with the input, and all functions
/// return <c>null</c> when there is not enough data. Only the High, Low
/// and Close of each bar are read.
/// </summary>
public static class Atr
{
  /// <summary>Calculates the latest Average True Range.</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The ATR at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    var series = AtrSeries(bars, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full ATR series.
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// <c>period</c> entries are <c>null</c>, and every later entry is
  /// the Wilder-smoothed average of the True Ranges as of that point.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded ATR series.</returns>
  public static double?[] AtrSeries(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[bars.Length];
    if (bars.Length < period + 1)
    {
      return result;
    }

    // The seed uses the close-based TRs of bars 1..period, so the RMA
    // runs over the True Ranges with the first (close-less) one
    // dropped and the null shifted back onto bar 0 — exactly the
    // TA-Lib alignment.
    double[] trs = TrueRanges(bars);
    double seedSum = 0;
    for (int i = 1; i <= period; i++)
    {
      seedSum += trs[i];
    }

    double rma = seedSum / period;
    result[period] = rma;
    for (int i = period + 1; i < bars.Length; i++)
    {
      rma = (rma * (period - 1) + trs[i]) / period;
      result[i] = rma;
    }

    return result;
  }

  /// <summary>
  /// The True Range of every bar: <c>max(high - low, |high - prev_close|,
  /// |low - prev_close|)</c> (<c>high - low</c> for the first bar).
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <returns>The True Range of each bar, aligned with the input.</returns>
  public static double[] TrueRange(Bar[] bars)
  {
    return TrueRanges(bars);
  }

  /// <summary>The Normalized ATR: <c>ATR / close * 100</c>, in percent.</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The NATR at the latest point, or <c>null</c> when there is not enough data or the close is zero.</returns>
  public static double? Natr(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double close = bars[^1].Close;
    if (close == 0)
    {
      return null;
    }

    double? atr = Latest(bars, period);
    return atr.HasValue ? atr.Value / close * 100 : null;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per bar. AtrInit warms the state up by replaying
  // the history through the same True-Range/RMA arithmetic AtrSeries
  // runs, so its series equals the batch series exactly and AtrNext
  // continues it bit for bit. Like the batch functions, the first bar
  // only contributes its close (its close-less True Range is dropped
  // by the seeding).
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="AtrNext"/>. Not thread-safe.</summary>
  public sealed class AtrState
  {
    internal AtrState(int period)
    {
      Period = period;
      Rma = new Ma.RmaState(period);
    }

    internal int Period { get; }

    internal double? PrevClose { get; set; }

    internal Ma.RmaState Rma { get; }
  }

  /// <summary>
  /// Warms up streaming ATR state from history: returns the state and
  /// the series (identical to <see cref="AtrSeries"/>), continuing it
  /// one bar at a time in O(1) via <see cref="AtrNext"/>.
  /// </summary>
  /// <param name="history">Bars, oldest first; at least <c>period + 1</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (AtrState State, double?[] Values) AtrInit(Bar[] history, int period = 14)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period + 1, "atr_init");

    var state = new AtrState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = AtrNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming ATR state by one bar, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="bar">The incoming bar.</param>
  /// <returns>The ATR as of this bar, or <c>null</c> while the seed window is still filling.</returns>
  public static double? AtrNext(AtrState state, Bar bar)
  {
    if (state.PrevClose is null)
    {
      state.PrevClose = bar.Close;
      return null;
    }

    double prevClose = state.PrevClose.Value;
    double tr = Math.Max(bar.High - bar.Low, Math.Max(Math.Abs(bar.High - prevClose), Math.Abs(bar.Low - prevClose)));
    double? value = Ma.RmaNext(state.Rma, tr);
    state.PrevClose = bar.Close;
    return value;
  }

  private static double[] TrueRanges(Bar[] bars)
  {
    var trs = new double[bars.Length];
    if (bars.Length == 0)
    {
      return trs;
    }

    trs[0] = bars[0].High - bars[0].Low;
    for (int i = 1; i < bars.Length; i++)
    {
      double high = bars[i].High;
      double low = bars[i].Low;
      double prevClose = bars[i - 1].Close;
      trs[i] = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
    }

    return trs;
  }
}

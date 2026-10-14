namespace Talib.Indicators;

/// <summary>
/// Ultimate Oscillator
/// (<see href="https://en.wikipedia.org/wiki/Ultimate_oscillator">Wikipedia</see>).
///
/// With the buying pressure <c>BP = close - min(low, prev_close)</c>
/// and the true range
/// <c>TR = max(high, prev_close) - min(low, prev_close)</c>, the
/// oscillator blends three averages:
///
/// <c>100 * (4 * A(short) + 2 * A(middle) + A(long)) / 7</c>
///
/// where <c>A(p) = SMA(BP, p) / SMA(TR, p)</c>. Bars are always given
/// in chronological order, oldest first; values are defined once the
/// longest window fills, and every function returns <c>null</c> when
/// there is not enough data. Only the High, Low and Close of each bar
/// are read.
/// </summary>
public static class UltimateOsc
{
  /// <summary>Calculates the latest Ultimate Oscillator value.</summary>
  /// <param name="bars">Bars, oldest first; High, Low and Close are read.</param>
  /// <param name="shortPeriod">Short window, a positive integer.</param>
  /// <param name="middlePeriod">Middle window, a positive integer.</param>
  /// <param name="longPeriod">Long window, a positive integer.</param>
  /// <returns>
  /// The oscillator at the latest point, or <c>null</c> when there is
  /// not enough data or the averaged true range is zero.
  /// </returns>
  public static double? Latest(Bar[] bars, int shortPeriod = 7, int middlePeriod = 14, int longPeriod = 28)
  {
    Guard.Period(shortPeriod);
    Guard.Period(middlePeriod);
    Guard.Period(longPeriod);
    var series = UltimateOscSeries(bars, shortPeriod, middlePeriod, longPeriod);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full Ultimate Oscillator series, defined from index
  /// <c>long - 1</c> on (once the longest window fills).
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the
  /// leading entries are <c>null</c> until the longest window fills,
  /// and every later entry blends the three averaged buying-pressure
  /// ratios of the window ending at that point. A zero averaged true
  /// range leaves the entry <c>null</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first; High, Low and Close are read.</param>
  /// <param name="shortPeriod">Short window, a positive integer.</param>
  /// <param name="middlePeriod">Middle window, a positive integer.</param>
  /// <param name="longPeriod">Long window, a positive integer.</param>
  /// <returns>The null-padded Ultimate Oscillator series.</returns>
  public static double?[] UltimateOscSeries(Bar[] bars, int shortPeriod = 7, int middlePeriod = 14, int longPeriod = 28)
  {
    Guard.Period(shortPeriod);
    Guard.Period(middlePeriod);
    Guard.Period(longPeriod);
    var result = new double?[bars.Length];
    if (bars.Length < longPeriod)
    {
      return result;
    }

    // Buying pressure and true range of every bar; the first bar has
    // no previous close, so it stands in for itself.
    var bp = new double[bars.Length];
    var tr = new double[bars.Length];
    bp[0] = bars[0].Close - bars[0].Low;
    tr[0] = bars[0].High - bars[0].Low;
    for (int i = 1; i < bars.Length; i++)
    {
      double prevClose = bars[i - 1].Close;
      bp[i] = bars[i].Close - Math.Min(bars[i].Low, prevClose);
      tr[i] = Math.Max(bars[i].High, prevClose) - Math.Min(bars[i].Low, prevClose);
    }

    // Ratio of the averaged buying pressure to the averaged true range
    // per window, then all three aligned on their common (newest) tail.
    double?[] ratiosShort = WindowRatios(bp, tr, shortPeriod);
    double?[] ratiosMiddle = WindowRatios(bp, tr, middlePeriod);
    double?[] ratiosLong = WindowRatios(bp, tr, longPeriod);
    int count = Math.Min(ratiosShort.Length, Math.Min(ratiosMiddle.Length, ratiosLong.Length));

    int offset = bars.Length - count;
    for (int i = 0; i < count; i++)
    {
      double? a = ratiosShort[ratiosShort.Length - count + i];
      double? b = ratiosMiddle[ratiosMiddle.Length - count + i];
      double? c = ratiosLong[ratiosLong.Length - count + i];
      result[offset + i] = a is null || b is null || c is null
          ? null
          : 100 * (4 * a.Value + 2 * b.Value + c.Value) / 7;
    }

    return result;
  }

  // The BP-to-TR ratio per window: the SMA averages of both series
  // (the unpadded tail of Ma.SmaSeries) divided entry by entry, null
  // where the averaged true range is zero.
  private static double?[] WindowRatios(double[] bp, double[] tr, int period)
  {
    if (bp.Length < period)
    {
      return Array.Empty<double?>();
    }

    double?[] bpAvgs = Ma.SmaSeries(bp, period);
    double?[] trAvgs = Ma.SmaSeries(tr, period);
    var ratios = new double?[bp.Length - period + 1];
    for (int i = 0; i < ratios.Length; i++)
    {
      double bpAvg = bpAvgs[i + period - 1]!.Value;
      double trAvg = trAvgs[i + period - 1]!.Value;
      ratios[i] = trAvg == 0 ? null : bpAvg / trAvg;
    }

    return ratios;
  }
}

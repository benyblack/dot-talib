namespace Talib.Indicators;

/// <summary>
/// Rolling Volume Weighted Average Price
/// (<see href="https://en.wikipedia.org/wiki/Volume-weighted_average_price">Wikipedia</see>).
///
/// This is the rolling VWAP: over each lookback window it is
/// <c>sum(TP * volume) / sum(volume)</c> with the typical price
/// <c>TP = (high + low + close) / 3</c> — not the session-anchored
/// variant that resets every trading day.
///
/// Bars are always given in chronological order, oldest first. Series
/// functions return results aligned with the input, and all functions
/// return <c>null</c> when there is not enough data or the window's
/// volume sums to zero. Only the High, Low, Close and Volume of each
/// bar are read.
/// </summary>
public static class Vwap
{
  /// <summary>Calculates the latest rolling VWAP.</summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>
  /// The VWAP at the latest point, or <c>null</c> when there is not
  /// enough data or the window's volume sums to zero.
  /// </returns>
  public static double? Latest(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double pv = 0;
    double v = 0;
    for (int i = bars.Length - period; i < bars.Length; i++)
    {
      Bar bar = bars[i];
      pv = pv + TypicalPrice(bar) * bar.Volume;
      v = v + bar.Volume;
    }

    return v == 0 ? null : pv / v;
  }

  /// <summary>
  /// Calculates the full rolling VWAP series in a single pass (sliding
  /// price-volume and volume sums).
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry
  /// is the volume-weighted average price of the window ending at that
  /// point. A window whose volume sums to zero leaves the entry
  /// <c>null</c> (the VWAP is undefined).
  /// </summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded rolling VWAP series.</returns>
  public static double?[] VwapSeries(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[bars.Length];
    if (bars.Length < period)
    {
      return result;
    }

    var pv = new double[bars.Length];
    var vols = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      pv[i] = TypicalPrice(bars[i]) * bars[i].Volume;
      vols[i] = bars[i].Volume;
    }

    double pvSum = 0;
    double vSum = 0;
    for (int i = 0; i < period; i++)
    {
      pvSum += pv[i];
      vSum += vols[i];
    }

    result[period - 1] = vSum == 0 ? null : pvSum / vSum;
    for (int i = period; i < bars.Length; i++)
    {
      double newPvSum = pvSum - pv[i - period] + pv[i];
      double newVSum = vSum - vols[i - period] + vols[i];
      result[i] = newVSum == 0 ? null : newPvSum / newVSum;
      pvSum = newPvSum;
      vSum = newVSum;
    }

    return result;
  }

  private static double TypicalPrice(Bar bar) => (bar.High + bar.Low + bar.Close) / 3;
}

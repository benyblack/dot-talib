namespace Talib.Indicators;

/// <summary>
/// Commodity Channel Index
/// (<see href="https://en.wikipedia.org/wiki/Commodity_channel_index">Wikipedia</see>).
///
/// With the typical price <c>TP = (high + low + close) / 3</c>:
///
/// <c>CCI = (TP - SMA(TP, period)) / (0.015 * mean_deviation)</c>
///
/// where the mean deviation is the average of <c>|TP - SMA|</c> across
/// the window. Bars are always given in chronological order, oldest
/// first. Series functions return results aligned with the input, and
/// all functions return <c>null</c> when there is not enough data or
/// the deviation is zero. Only the High, Low and Close of each bar are
/// read.
/// </summary>
public static class Cci
{
  /// <summary>Calculates the latest CCI value.</summary>
  /// <param name="bars">Bars, oldest first; High, Low and Close are read.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>
  /// The CCI at the latest point, or <c>null</c> when there is not
  /// enough data or the mean deviation is zero.
  /// </returns>
  public static double? Latest(Bar[] bars, int period = 20)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    var window = new double[period];
    double sum = 0;
    for (int i = 0; i < period; i++)
    {
      window[i] = TypicalPrice(bars[bars.Length - period + i]);
      sum += window[i];
    }

    double mean = sum / period;

    double deviationSum = 0;
    for (int i = 0; i < period; i++)
    {
      deviationSum += Math.Abs(window[i] - mean);
    }

    double deviation = deviationSum / period;
    if (deviation == 0)
    {
      return null;
    }

    return (window[period - 1] - mean) / (0.015 * deviation);
  }

  /// <summary>
  /// Calculates the full CCI series.
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry
  /// is the CCI of the window ending at that point. A window with a
  /// zero mean deviation (flat typical prices) leaves the entry
  /// <c>null</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first; High, Low and Close are read.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded CCI series.</returns>
  public static double?[] CciSeries(Bar[] bars, int period = 20)
  {
    Guard.Period(period);
    var result = new double?[bars.Length];
    if (bars.Length < period)
    {
      return result;
    }

    var tps = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      tps[i] = TypicalPrice(bars[i]);
    }

    double sum = 0;
    for (int i = 0; i < period; i++)
    {
      sum += tps[i];
    }

    // The mean rides a sliding sum, but the mean deviation is read
    // straight from the window (like TA-Lib, which also walks it).
    for (int i = period - 1; i < bars.Length; i++)
    {
      double mean = sum / period;

      double deviationSum = 0;
      for (int j = i - period + 1; j <= i; j++)
      {
        deviationSum += Math.Abs(tps[j] - mean);
      }

      double deviation = deviationSum / period;
      if (deviation != 0)
      {
        result[i] = (tps[i] - mean) / (0.015 * deviation);
      }

      if (i < bars.Length - 1)
      {
        sum = sum - tps[i - period + 1] + tps[i + 1];
      }
    }

    return result;
  }

  private static double TypicalPrice(Bar bar) => (bar.High + bar.Low + bar.Close) / 3;
}

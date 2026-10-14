namespace Talib.Indicators;

/// <summary>
/// On-Balance Volume (https://en.wikipedia.org/wiki/On-balance_volume).
///
/// A running total of volume, added when the close rises and subtracted
/// when it falls. Matching TA-Lib, the line is seeded with the first
/// bar's volume; the first bar itself carries no change.
///
/// Bars carry at least a Close and a Volume, given in chronological
/// order, oldest first; the series is returned aligned with the input.
/// </summary>
public static class Obv
{
  /// <summary>Calculates the latest On-Balance Volume.</summary>
  /// <param name="bars">Bars, oldest first; only Close and Volume are read.</param>
  /// <returns>The OBV at the latest point, or <c>null</c> for empty input.</returns>
  public static double? Latest(Bar[] bars)
  {
    double?[] series = ObvSeries(bars);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full On-Balance Volume series.
  ///
  /// Returns an array aligned with <paramref name="bars"/> (the first
  /// entry is the first bar's volume, as in TA-Lib).
  /// </summary>
  /// <param name="bars">Bars, oldest first; only Close and Volume are read.</param>
  /// <returns>The OBV series.</returns>
  public static double?[] ObvSeries(Bar[] bars)
  {
    var result = new double?[bars.Length];
    if (bars.Length == 0)
    {
      return result;
    }

    double prevClose = bars[0].Close;
    double total = bars[0].Volume;
    result[0] = total;
    for (int i = 1; i < bars.Length; i++)
    {
      double close = bars[i].Close;
      double delta = close > prevClose
          ? bars[i].Volume
          : close < prevClose ? -bars[i].Volume : 0;
      total = total + delta;
      result[i] = total;
      prevClose = close;
    }

    return result;
  }
}

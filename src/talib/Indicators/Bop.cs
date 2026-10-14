namespace Talib.Indicators;

/// <summary>
/// Balance of Power: <c>(close - open) / (high - low)</c>.
///
/// Ranges from -1 (close at the low after opening at the high) to 1.
/// Zero-range bars give <c>null</c>. Bars are always given in
/// chronological order, oldest first. Only the Open, High, Low and
/// Close of each bar are read.
/// </summary>
public static class Bop
{
  /// <summary>Calculates the Balance of Power of the latest bar.</summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>
  /// The BOP of the latest bar, or <c>null</c> for empty input or a
  /// zero-range latest bar.
  /// </returns>
  public static double? Latest(Bar[] bars)
  {
    var series = BopSeries(bars);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the Balance of Power of every bar. Returns an array
  /// aligned with <paramref name="bars"/>; zero-range bars leave the
  /// entry <c>null</c> (the balance of power is undefined).
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The BOP series.</returns>
  public static double?[] BopSeries(Bar[] bars)
  {
    var result = new double?[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      Bar bar = bars[i];
      double range = bar.High - bar.Low;
      result[i] = range == 0 ? null : (bar.Close - bar.Open) / range;
    }

    return result;
  }
}

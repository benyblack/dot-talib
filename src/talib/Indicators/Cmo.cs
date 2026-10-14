namespace Talib.Indicators;

/// <summary>
/// Chande Momentum Oscillator.
///
/// <c>CMO = 100 * (sum of gains - sum of losses) / (sum of gains + sum of losses)</c>
/// over the lookback window.
///
/// Prices are always given in chronological order, oldest first, and the
/// series is returned aligned with the input. <c>null</c> is returned
/// when there is not enough data or the window has no movement.
/// </summary>
public static class Cmo
{
  /// <summary>Calculates the latest Chande Momentum Oscillator value.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The CMO at the latest point, or <c>null</c> when there is not enough data or the window has no movement.</returns>
  public static double? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    var series = CmoSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full CMO series.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period</c> entries are <c>null</c>, and every later entry is the
  /// oscillator of the window ending at that point. Windows with no
  /// movement produce <c>null</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The null-padded CMO series.</returns>
  public static double?[] CmoSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length <= period)
    {
      return result;
    }

    // The oscillator runs over the price changes; the seed window is the
    // oldest `period` changes, then the sums slide one change at a time.
    var changes = new double[prices.Length - 1];
    for (int i = 0; i < changes.Length; i++)
    {
      changes[i] = prices[i + 1] - prices[i];
    }

    double gains = 0;
    double losses = 0;
    for (int i = 0; i < period; i++)
    {
      gains += Math.Max(changes[i], 0);
      losses += Math.Max(-changes[i], 0);
    }

    result[period] = CmoValue(gains, losses);
    int leaving = 0;
    for (int i = period; i < changes.Length; i++)
    {
      double change = changes[i];
      double old = changes[leaving++];
      gains = gains - Math.Max(old, 0) + Math.Max(change, 0);
      losses = losses - Math.Max(-old, 0) + Math.Max(-change, 0);
      result[i + 1] = CmoValue(gains, losses);
    }

    return result;
  }

  private static double? CmoValue(double gains, double losses)
  {
    if (gains == 0 && losses == 0)
    {
      return null;
    }

    return 100 * (gains - losses) / (gains + losses);
  }
}

namespace Talib.Indicators;

/// <summary>
/// Absolute Price Oscillator: <c>EMA(fast) - EMA(slow)</c>.
///
/// Prices are always given in chronological order, oldest first, and the
/// series is returned aligned with the input. <c>null</c> is returned
/// when there is not enough data. For the percentage variant see
/// <see cref="Ppo"/>.
/// </summary>
public static class Apo
{
  /// <summary>Calculates the latest Absolute Price Oscillator value. For example, <c>Latest([1, 2, 3, 4, 5, 6, 7, 8], 2, 4)</c> is <c>1.0</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Fast EMA period, a positive integer (default 12).</param>
  /// <param name="slow">Slow EMA period, a positive integer (default 26).</param>
  /// <returns>The APO at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(double[] prices, int fast = 12, int slow = 26)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    if (prices.Length < slow)
    {
      return null;
    }

    return Ma.Ema(prices, fast) - Ma.Ema(prices, slow);
  }

  /// <summary>
  /// Calculates the full APO series, defined once the slow EMA exists
  /// (index <c>slow - 1</c> on).
  ///
  /// Returns an array aligned with <paramref name="prices"/>: entries
  /// before the slow EMA's first value are <c>null</c>, and every later
  /// entry is the fast EMA minus the slow EMA at that point.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Fast EMA period, a positive integer (default 12).</param>
  /// <param name="slow">Slow EMA period, a positive integer (default 26).</param>
  /// <returns>The null-padded APO series.</returns>
  public static double?[] ApoSeries(double[] prices, int fast = 12, int slow = 26)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    double?[] fastSeries = Ma.EmaSeries(prices, fast);
    double?[] slowSeries = Ma.EmaSeries(prices, slow);

    var result = new double?[prices.Length];
    for (int i = 0; i < prices.Length; i++)
    {
      double? f = fastSeries[i];
      double? s = slowSeries[i];
      result[i] = f.HasValue && s.HasValue ? f.Value - s.Value : null;
    }

    return result;
  }
}

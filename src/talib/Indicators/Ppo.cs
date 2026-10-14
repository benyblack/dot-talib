namespace Talib.Indicators;

/// <summary>
/// Percentage Price Oscillator:
/// <c>(EMA(fast) - EMA(slow)) / EMA(slow) * 100</c>.
///
/// Prices are always given in chronological order, oldest first, and the
/// series is returned aligned with the input. <c>null</c> is returned
/// when there is not enough data. For the absolute variant see
/// <see cref="Apo"/>.
/// </summary>
public static class Ppo
{
  /// <summary>Calculates the latest Percentage Price Oscillator value, in percent. For example, <c>Latest([1, 2, 3, 4, 5, 6, 7, 8], 2, 4)</c> rounds to <c>15.3846</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Fast EMA period, a positive integer (default 12).</param>
  /// <param name="slow">Slow EMA period, a positive integer (default 26).</param>
  /// <returns>The PPO at the latest point, in percent, or <c>null</c> when there is not enough data or the slow EMA is zero.</returns>
  public static double? Latest(double[] prices, int fast = 12, int slow = 26)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    if (prices.Length < slow)
    {
      return null;
    }

    double? slowEma = Ma.Ema(prices, slow);
    if (slowEma == 0)
    {
      return null;
    }

    return (Ma.Ema(prices, fast) - slowEma) / slowEma * 100;
  }

  /// <summary>
  /// Calculates the full PPO series, defined once the slow EMA exists
  /// (index <c>slow - 1</c> on).
  ///
  /// Returns an array aligned with <paramref name="prices"/>: entries
  /// before the slow EMA's first value are <c>null</c>, and every later
  /// entry is the fast EMA minus the slow EMA as a percentage of the
  /// slow EMA at that point. A zero slow EMA produces <c>null</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fast">Fast EMA period, a positive integer (default 12).</param>
  /// <param name="slow">Slow EMA period, a positive integer (default 26).</param>
  /// <returns>The null-padded PPO series.</returns>
  public static double?[] PpoSeries(double[] prices, int fast = 12, int slow = 26)
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
      result[i] = f.HasValue && s.HasValue && s.Value != 0
          ? (f.Value - s.Value) / s.Value * 100
          : null;
    }

    return result;
  }
}

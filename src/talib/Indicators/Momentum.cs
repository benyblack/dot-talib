namespace Talib.Indicators;

/// <summary>
/// Momentum (https://en.wikipedia.org/wiki/Momentum_(technical_analysis)).
///
/// <c>MOM = price - price_period_bars_ago</c>
///
/// Prices are always given in chronological order, oldest first, and the
/// series is returned aligned with the input. <c>null</c> is returned
/// when there is not enough data.
/// </summary>
public static class Momentum
{
  /// <summary>Calculates the latest Momentum.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The MOM at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    return prices[^1] - prices[prices.Length - period - 1];
  }

  /// <summary>
  /// Calculates the full Momentum series in a single pass.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the
  /// first <c>period</c> entries are <c>null</c>, and every later entry
  /// is the price change over <c>period</c> bars.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The null-padded MOM series.</returns>
  public static double?[] MomentumSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    for (int i = period; i < prices.Length; i++)
    {
      result[i] = prices[i] - prices[i - period];
    }

    return result;
  }
}

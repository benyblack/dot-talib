namespace Talib.Indicators;

/// <summary>
/// Rate of Change (https://en.wikipedia.org/wiki/Momentum_(technical_analysis)).
///
/// <c>ROC = (price / price_period_bars_ago - 1) * 100</c>
///
/// Prices are always given in chronological order, oldest first, and the
/// series is returned aligned with the input. <c>null</c> is returned
/// when there is not enough data. Prices must be non-zero for the
/// division to be defined.
/// </summary>
public static class Roc
{
  /// <summary>Calculates the latest Rate of Change, in percent.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The ROC at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    double basePrice = prices[prices.Length - period - 1];
    return (prices[^1] / basePrice - 1) * 100;
  }

  /// <summary>
  /// Calculates the full Rate of Change series in a single pass.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the
  /// first <c>period</c> entries are <c>null</c>, and every later entry
  /// is the rate of change over <c>period</c> bars.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The null-padded ROC series.</returns>
  public static double?[] RocSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    for (int i = period; i < prices.Length; i++)
    {
      result[i] = (prices[i] / prices[i - period] - 1) * 100;
    }

    return result;
  }

  /// <summary>Calculates the latest Rate of change Percentage: <c>price / lagged price - 1</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The ROCP at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Rocp(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    return prices[^1] / prices[prices.Length - period - 1] - 1;
  }

  /// <summary>Calculates the latest Rate of change Ratio: <c>price / lagged price</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The ROCR at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Rocr(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    return prices[^1] / prices[prices.Length - period - 1];
  }

  /// <summary>Calculates the latest Rate of change Ratio scaled by 100: <c>100 * price / lagged price</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">How many bars back to compare against, a positive integer (default 14).</param>
  /// <returns>The ROCR100 at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Rocr100(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    return 100 * prices[^1] / prices[prices.Length - period - 1];
  }
}

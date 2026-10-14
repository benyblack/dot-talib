namespace Talib.Indicators;

/// <summary>
/// Stochastic RSI: the fast stochastic of the RSI series.
///
/// <c>%K = (RSI - lowest RSI) / (highest RSI - lowest RSI) * 100</c> over
/// the stochastic window. Prices are always given in chronological
/// order, oldest first, and the series is returned aligned with the
/// input. <c>null</c> is returned when there is not enough data or the
/// RSI window has no range.
/// </summary>
public static class StochRsi
{
  /// <summary>Calculates the latest Stochastic RSI (fast %K).</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="rsiPeriod">RSI lookback window, a positive integer.</param>
  /// <param name="stochPeriod">Stochastic window over the RSI series, a positive integer.</param>
  /// <returns>
  /// The %K at the latest point, or <c>null</c> when there is not enough
  /// data or the RSI window has no range.
  /// </returns>
  public static double? Latest(double[] prices, int rsiPeriod = 14, int stochPeriod = 5)
  {
    Guard.Period(rsiPeriod);
    Guard.Period(stochPeriod);
    if (prices.Length < rsiPeriod + stochPeriod)
    {
      return null;
    }

    var series = StochRsiSeries(prices, rsiPeriod, stochPeriod);
    return series[^1];
  }

  /// <summary>
  /// Calculates the full Stochastic RSI series, defined from index
  /// <c>rsiPeriod + stochPeriod - 1</c> on.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the
  /// lookback entries are <c>null</c>, and every later entry is
  /// <c>(RSI - lowest RSI) / (highest RSI - lowest RSI) * 100</c> of the
  /// stochastic window ending at that point — <c>null</c> when that
  /// window has no range.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="rsiPeriod">RSI lookback window, a positive integer.</param>
  /// <param name="stochPeriod">Stochastic window over the RSI series, a positive integer.</param>
  /// <returns>The null-padded %K series.</returns>
  public static double?[] StochRsiSeries(double[] prices, int rsiPeriod = 14, int stochPeriod = 5)
  {
    Guard.Period(rsiPeriod);
    Guard.Period(stochPeriod);
    var result = new double?[prices.Length];

    double[] rsis = RsiValues(prices, rsiPeriod);
    if (rsis.Length < stochPeriod)
    {
      return result;
    }

    var (maxes, mins) = Window.MaxesMins(rsis, stochPeriod);
    int offset = prices.Length - maxes.Length;
    for (int i = 0; i < maxes.Length; i++)
    {
      double highest = maxes[i];
      double lowest = mins[i];
      double last = rsis[i + stochPeriod - 1];
      result[offset + i] = highest != lowest ? (last - lowest) / (highest - lowest) * 100 : null;
    }

    return result;
  }

  // The defined RSI values only (no null padding), oldest first — the
  // stochastic runs over these, so undefined (flat-window) RSIs are
  // dropped like the Elixir port's Enum.reject(&is_nil/1).
  private static double[] RsiValues(double[] prices, int rsiPeriod)
  {
    double?[] series = Rsi.RsiSeries(prices, rsiPeriod);

    int count = 0;
    for (int i = 0; i < series.Length; i++)
    {
      if (series[i].HasValue)
      {
        count++;
      }
    }

    var rsis = new double[count];
    int j = 0;
    for (int i = 0; i < series.Length; i++)
    {
      if (series[i].HasValue)
      {
        rsis[j++] = series[i]!.Value;
      }
    }

    return rsis;
  }
}

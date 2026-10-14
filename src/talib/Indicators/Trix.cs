namespace Talib.Indicators;

/// <summary>
/// TRIX <see href="https://en.wikipedia.org/wiki/TRIX"/>.
///
/// A triple-smoothed EMA rate of change: the percent change of an EMA
/// applied three times over. Values are close to zero in flat trends; the
/// signal is usually its own EMA (not provided here).
///
/// Prices are always given in chronological order, oldest first, and the
/// series is returned aligned with the input. <c>null</c> is returned
/// when there is not enough data.
/// </summary>
public static class Trix
{
  /// <summary>
  /// Calculates the latest TRIX value, in percent. At least
  /// <c>3 * (period - 1) + 2</c> prices are required for the triple EMA
  /// to have two defined values to compare; otherwise <c>null</c> is
  /// returned.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">EMA period, a positive integer.</param>
  /// <returns>The TRIX at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < 3 * (period - 1) + 2)
    {
      return null;
    }

    var series = TrixSeries(prices, period);
    return series[^1];
  }

  /// <summary>
  /// Calculates the full TRIX series.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>3 * (period - 1) + 1</c> entries are <c>null</c>, and every later
  /// entry is the one-bar percent change of the triple EMA.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">EMA period, a positive integer.</param>
  /// <returns>The null-padded TRIX series.</returns>
  public static double?[] TrixSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];

    // Each EMA stage runs over the unpadded values of the previous one
    // and lags it by period - 1 bars, so the triple EMA is defined from
    // bar 3 * (period - 1) on.
    double[] e1 = EmaStage(prices, period);
    double[] e2 = EmaStage(e1, period);
    double[] e3 = EmaStage(e2, period);

    int count = e3.Length <= 1 ? 0 : e3.Length - 1;
    int offset = prices.Length - count;
    for (int i = 0; i < count; i++)
    {
      result[offset + i] = (e3[i + 1] / e3[i] - 1) * 100;
    }

    return result;
  }

  // One unpadded EMA stage; an empty stage when there is not enough data
  // to seed one, so the chain simply stays undefined.
  private static double[] EmaStage(double[] values, int period)
  {
    return values.Length < period ? Array.Empty<double>() : Ma.EmaValues(values, period);
  }
}

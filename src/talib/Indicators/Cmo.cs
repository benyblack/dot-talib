namespace Talib.Indicators;

/// <summary>
/// Chande Momentum Oscillator, TA-Lib convention:
/// <c>CMO = 100 * (avg gain - avg loss) / (avg gain + avg loss)</c> over
/// Wilder-smoothed average gains and losses (the same smoothing RSI uses —
/// TA-Lib's CMO is exactly <c>2 * RSI - 100</c>).
///
/// Note: the textbook (and Elixir-reference) formulation runs the same
/// quotient over plain sliding-window sums of gains and losses; this
/// implementation follows the C library, verified value by value in the
/// audit.
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
    var series = CmoSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full CMO series with Wilder smoothing.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period</c> entries are <c>null</c>, and every later entry is the
  /// oscillator of the smoothed averages as of that point. Windows with
  /// no movement at all produce <c>null</c>.
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

    // The seed averages cover the oldest `period` price changes, then
    // slide the Wilder way: avg = (avg * (period - 1) + change) / period.
    double gainSum = 0;
    double lossSum = 0;
    for (int i = 1; i <= period; i++)
    {
      double change = prices[i] - prices[i - 1];
      gainSum += Math.Max(change, 0);
      lossSum += Math.Max(-change, 0);
    }

    double gainAvg = gainSum / period;
    double lossAvg = lossSum / period;
    result[period] = CmoValue(gainAvg, lossAvg);
    for (int i = period + 1; i < prices.Length; i++)
    {
      double change = prices[i] - prices[i - 1];
      gainAvg = (gainAvg * (period - 1) + Math.Max(change, 0)) / period;
      lossAvg = (lossAvg * (period - 1) + Math.Max(-change, 0)) / period;
      result[i] = CmoValue(gainAvg, lossAvg);
    }

    return result;
  }

  private static double? CmoValue(double gainAvg, double lossAvg)
  {
    if (gainAvg == 0 && lossAvg == 0)
    {
      return null;
    }

    return 100 * ((gainAvg - lossAvg) / (gainAvg + lossAvg));
  }
}

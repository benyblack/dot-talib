namespace Talib.Indicators;

/// <summary>
/// Rolling population standard deviation.
///
/// Prices are always given in chronological order, oldest first. Series
/// functions return results aligned with the input, the first
/// <c>period - 1</c> entries being <c>null</c>. All scalar functions
/// return <c>null</c> when there is not enough data.
/// </summary>
public static class StdDev
{
  /// <summary>
  /// Calculates the population standard deviation of the <c>period</c>
  /// newest prices (two-pass formula, dividing by <c>period</c>).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The standard deviation at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(double[] prices, int period = 50)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double sum = 0;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      sum += prices[i];
    }

    double mean = sum / period;
    double total = 0;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      total = total + (prices[i] - mean) * (prices[i] - mean);
    }

    return Math.Sqrt(total / period);
  }

  /// <summary>
  /// Calculates the full rolling standard deviation series in a single
  /// pass (sliding window sum and sum of squares).
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry
  /// is the population standard deviation of the window ending at that
  /// point.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The null-padded standard deviation series.</returns>
  public static double?[] StdDevSeries(double[] prices, int period = 50)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    double sum = 0;
    for (int i = 0; i < period; i++)
    {
      sum += prices[i];
    }

    double sqSum = 0;
    for (int i = 0; i < period; i++)
    {
      sqSum = sqSum + prices[i] * prices[i];
    }

    result[period - 1] = PopStdDev(sum, sqSum, period);
    for (int i = period; i < prices.Length; i++)
    {
      double leaving = prices[i - period];
      sum = sum - leaving + prices[i];
      sqSum = sqSum - leaving * leaving + prices[i] * prices[i];
      result[i] = PopStdDev(sum, sqSum, period);
    }

    return result;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price. StdDevInit warms the state up by
  // replaying the history through the same sliding sums StdDevSeries
  // keeps, so its series equals the batch series exactly and
  // StdDevNext continues it bit for bit.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="StdDevNext"/>. Not thread-safe.</summary>
  public sealed class StdDevState
  {
    internal StdDevState(int period)
    {
      Period = period;
      Sums = new SumState(period);
      Squares = new SumState(period);
    }

    internal int Period { get; }

    internal SumState Sums { get; }

    internal SumState Squares { get; }
  }

  /// <summary>
  /// Warms up streaming standard deviation state from history: returns
  /// the state and the series (identical to <see cref="StdDevSeries"/>),
  /// continuing it one price at a time in O(1) via
  /// <see cref="StdDevNext"/>.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (StdDevState State, double?[] Values) StdDevInit(double[] history, int period = 50)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "std_dev_init");

    var state = new StdDevState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = StdDevNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming standard deviation state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The standard deviation of the current window, or <c>null</c> while it is still filling.</returns>
  public static double? StdDevNext(StdDevState state, double price)
  {
    double? sum = state.Sums.Push(price);
    double? squares = state.Squares.Push(price * price);
    return sum.HasValue ? PopStdDev(sum.Value, squares!.Value, state.Period) : null;
  }

  private static double PopStdDev(double sum, double sqSum, int period)
  {
    double variance = Math.Max(sqSum - sum * sum / period, 0);
    return Math.Sqrt(variance / period);
  }
}

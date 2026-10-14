namespace Talib.Indicators;

/// <summary>
/// Bollinger Bands <see href="https://en.wikipedia.org/wiki/Bollinger_Bands"/>.
///
/// * middle = SMA(close, period)
/// * upper = middle + mult * StdDev(close, period)
/// * lower = middle - mult * StdDev(close, period)
///
/// Prices are always given in chronological order, oldest first. Every
/// function returns <c>null</c> when there is not enough data.
/// </summary>
public static class Bbands
{
  /// <summary>One Bollinger Bands reading.</summary>
  /// <param name="Upper">The upper band: <c>Middle + mult * deviation</c>.</param>
  /// <param name="Middle">The middle band: the SMA of the window.</param>
  /// <param name="Lower">The lower band: <c>Middle - mult * deviation</c>.</param>
  public readonly record struct BbandsResult(double Upper, double Middle, double Lower);

  /// <summary>Calculates the latest Bollinger Bands.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">SMA lookback window, a positive integer.</param>
  /// <param name="mult">Standard deviation multiplier.</param>
  /// <returns>The bands at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static BbandsResult? Latest(double[] prices, int period = 20, double mult = 2)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double[] window = prices[^period..];
    double middle = Ma.Sma(window, period)!.Value;
    double deviation = StdDev.Latest(window, period)!.Value;
    return new BbandsResult(middle + mult * deviation, middle, middle - mult * deviation);
  }

  /// <summary>
  /// Calculates the full Bollinger Bands series in a single pass.
  ///
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry
  /// holds the bands of the window ending at that point.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">SMA lookback window, a positive integer.</param>
  /// <param name="mult">Standard deviation multiplier.</param>
  /// <returns>The null-padded bands series.</returns>
  public static BbandsResult?[] BbandsSeries(double[] prices, int period = 20, double mult = 2)
  {
    Guard.Period(period);
    var result = new BbandsResult?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    // One pass with the same sliding sum and sum of squares the
    // StdDev walk keeps, so each bar costs O(1) instead of re-reading
    // its window: middle from the sliding sum, deviation from the
    // sliding sum of squares (StdDevSeries's arithmetic exactly).
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

    result[period - 1] = Band(sum, sqSum, period, mult);
    for (int i = period; i < prices.Length; i++)
    {
      double leaving = prices[i - period];
      sum = sum - leaving + prices[i];
      sqSum = sqSum - leaving * leaving + prices[i] * prices[i];
      result[i] = Band(sum, sqSum, period, mult);
    }

    return result;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price. BbandsInit warms the state up by
  // replaying the history through the same sliding sums BbandsSeries
  // keeps, so its series equals the batch series exactly and
  // BbandsNext continues it bit for bit.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="BbandsNext"/>. Not thread-safe.</summary>
  public sealed class BbandsState
  {
    internal BbandsState(int period, double mult)
    {
      Period = period;
      Mult = mult;
      Sums = new SumState(period);
      Squares = new SumState(period);
    }

    internal int Period { get; }

    internal double Mult { get; }

    internal SumState Sums { get; }

    internal SumState Squares { get; }
  }

  /// <summary>
  /// Warms up streaming Bollinger Bands state from history: returns the
  /// state and the series (identical to <see cref="BbandsSeries"/>),
  /// continuing it one price at a time in O(1) via
  /// <see cref="BbandsNext"/>.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">SMA lookback window, a positive integer.</param>
  /// <param name="mult">Standard deviation multiplier.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (BbandsState State, BbandsResult?[] Values) BbandsInit(double[] history, int period = 20, double mult = 2)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "bbands_init");

    var state = new BbandsState(period, mult);
    var values = new BbandsResult?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = BbandsNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming Bollinger Bands state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The bands of the current window, or <c>null</c> while it is still filling.</returns>
  public static BbandsResult? BbandsNext(BbandsState state, double price)
  {
    double? sum = state.Sums.Push(price);
    double? squares = state.Squares.Push(price * price);
    return sum.HasValue ? Band(sum.Value, squares!.Value, state.Period, state.Mult) : null;
  }

  private static BbandsResult Band(double sum, double sqSum, int period, double mult)
  {
    double middle = sum / period;
    double deviation = Math.Sqrt(Math.Max(sqSum - sum * sum / period, 0) / period);
    return new BbandsResult(middle + mult * deviation, middle, middle - mult * deviation);
  }
}

namespace Talib.Indicators;

/// <summary>
/// Moving average indicator.
///
/// Calculates SMA, WMA, EMA, Wilder RMA, DEMA, TEMA and Midpoint, plus
/// streaming <c>*Init</c>/<c>*Next</c> state for maintaining a moving
/// average in O(1) as new prices arrive.
///
/// Prices are always given in chronological order, oldest first. Series
/// functions return results aligned with the input, the first
/// <c>period - 1</c> entries being <c>null</c>. All scalar functions
/// return <c>null</c> when there is not enough data.
/// </summary>
public static class Ma
{
  /// <summary>Calculates the Simple Moving Average of the <c>period</c> newest prices.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The SMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Sma(double[] prices, int period = 50)
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

    return sum / period;
  }

  /// <summary>
  /// Calculates the full SMA series in a single pass (sliding window sum).
  /// Returns an array aligned with <paramref name="prices"/>: the first
  /// <c>period - 1</c> entries are <c>null</c>, and every later entry is
  /// the simple moving average of the window ending at that point.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded SMA series.</returns>
  public static double?[] SmaSeries(double[] prices, int period = 50)
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

    result[period - 1] = sum / period;
    for (int i = period; i < prices.Length; i++)
    {
      sum = sum - prices[i - period] + prices[i];
      result[i] = sum / period;
    }

    return result;
  }

  /// <summary>
  /// Calculates the Linear Weighted Moving Average of the <c>period</c>
  /// newest prices. The newest price gets the weight <c>period</c>, the
  /// oldest price in the window gets 1.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The WMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Wma(double[] prices, int period = 50)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double weightedTotal = 0;
    for (int i = 0; i < period; i++)
    {
      weightedTotal += prices[prices.Length - period + i] * (i + 1);
    }

    return weightedTotal / (period * (period + 1) / 2.0);
  }

  /// <summary>
  /// Calculates the full WMA series in a single pass (sliding weighted
  /// and plain window sums).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded WMA series.</returns>
  public static double?[] WmaSeries(double[] prices, int period = 50)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    double denominator = period * (period + 1) / 2.0;
    double weighted = 0;
    double sum = 0;
    for (int i = 0; i < period; i++)
    {
      weighted += prices[i] * (i + 1);
      sum += prices[i];
    }

    result[period - 1] = weighted / denominator;
    for (int i = period; i < prices.Length; i++)
    {
      weighted = weighted - sum + period * prices[i];
      sum = sum - prices[i - period] + prices[i];
      result[i] = weighted / denominator;
    }

    return result;
  }

  /// <summary>
  /// Calculates the Exponential Moving Average. The series is seeded with
  /// the SMA of the oldest <c>period</c> prices, then walked forward with
  /// <c>multiplier = 2 / (period + 1)</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The EMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Ema(double[] prices, int period = 50)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double seed = SeedSum(prices, period) / period;
    double multiplier = 2.0 / (period + 1);
    double ema = seed;
    for (int i = period; i < prices.Length; i++)
    {
      ema = ema + multiplier * (prices[i] - ema);
    }

    return ema;
  }

  /// <summary>
  /// Calculates the full EMA series in a single forward pass, seeded at
  /// the SMA of the oldest <c>period</c> prices.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded EMA series.</returns>
  public static double?[] EmaSeries(double[] prices, int period = 50)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    double multiplier = 2.0 / (period + 1);
    double ema = SeedSum(prices, period) / period;
    result[period - 1] = ema;
    for (int i = period; i < prices.Length; i++)
    {
      ema = ema + multiplier * (prices[i] - ema);
      result[i] = ema;
    }

    return result;
  }

  /// <summary>
  /// Calculates Wilder's smoothed Moving Average (RMA), used by ATR, ADX
  /// and friends: seeded with the SMA of the oldest <c>period</c> prices,
  /// then <c>rma = (prev * (period - 1) + price) / period</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The RMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Rma(double[] prices, int period = 50)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double rma = SeedSum(prices, period) / period;
    for (int i = period; i < prices.Length; i++)
    {
      rma = (rma * (period - 1) + prices[i]) / period;
    }

    return rma;
  }

  /// <summary>Calculates the full Wilder RMA series in a single forward pass.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded RMA series.</returns>
  public static double?[] RmaSeries(double[] prices, int period = 50)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    double rma = SeedSum(prices, period) / period;
    result[period - 1] = rma;
    for (int i = period; i < prices.Length; i++)
    {
      rma = (rma * (period - 1) + prices[i]) / period;
      result[i] = rma;
    }

    return result;
  }

  /// <summary>Calculates the Double Exponential Moving Average: <c>2 * EMA - EMA(EMA)</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The DEMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Dema(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var series = DemaSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>Calculates the full DEMA series (<c>2 * EMA - EMA(EMA)</c>).</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded DEMA series.</returns>
  public static double?[] DemaSeries(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < 2 * period - 1)
    {
      return result;
    }

    double[] e1 = EmaValues(prices, period);
    double[] e2 = EmaValues(e1, period);
    for (int i = 0; i < e2.Length; i++)
    {
      result[i + 2 * (period - 1)] = 2 * e1[i + period - 1] - e2[i];
    }

    return result;
  }

  /// <summary>
  /// Calculates the Triple Exponential Moving Average:
  /// <c>3 * EMA - 3 * EMA(EMA) + EMA(EMA(EMA))</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The TEMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Tema(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var series = TemaSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>Calculates the full TEMA series.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded TEMA series.</returns>
  public static double?[] TemaSeries(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < 3 * period - 2)
    {
      return result;
    }

    double[] e1 = EmaValues(prices, period);
    double[] e2 = EmaValues(e1, period);
    double[] e3 = EmaValues(e2, period);
    for (int i = 0; i < e3.Length; i++)
    {
      result[i + 3 * (period - 1)] = 3 * e1[i + 2 * (period - 1)] - 3 * e2[i + period - 1] + e3[i];
    }

    return result;
  }

  /// <summary>Calculates the Midpoint of the <c>period</c> newest prices: <c>(highest + lowest) / 2</c>.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The midpoint at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Midpoint(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double highest = double.MinValue;
    double lowest = double.MaxValue;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      if (prices[i] > highest)
      {
        highest = prices[i];
      }

      if (prices[i] < lowest)
      {
        lowest = prices[i];
      }
    }

    return (highest + lowest) / 2;
  }

  /// <summary>
  /// Calculates the full Midpoint series: <c>(highest + lowest) / 2</c>
  /// over each window, in one pass.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded Midpoint series.</returns>
  public static double?[] MidpointSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    var (maxes, mins) = Window.MaxesMins(prices, period);
    for (int i = 0; i < maxes.Length; i++)
    {
      result[i + period - 1] = (maxes[i] + mins[i]) / 2;
    }

    return result;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price
  //
  // *Init warms the state up by replaying history through the same
  // sliding arithmetic the batch walks use (operation for operation), so
  // the returned series equals the batch series exactly and *Next
  // continues it bit for bit. The states are opaque — build them with
  // *Init, advance them with *Next, keep them as-is.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="SmaNext"/>. Not thread-safe.</summary>
  public sealed class SmaState
  {
    internal SmaState(int period)
    {
      Period = period;
      Sum = new SumState(period);
    }

    internal int Period { get; }

    internal SumState Sum { get; }
  }

  /// <summary>Streaming state for <see cref="EmaNext"/>. Not thread-safe.</summary>
  public sealed class EmaState
  {
    internal EmaState(int period)
    {
      Period = period;
      Multiplier = 2.0 / (period + 1);
      Seed = new SumState(period);
    }

    internal int Period { get; }

    internal double Multiplier { get; }

    internal double? Prev { get; set; }

    internal SumState Seed { get; }
  }

  /// <summary>Streaming state for <see cref="WmaNext"/>. Not thread-safe.</summary>
  public sealed class WmaState
  {
    internal WmaState(int period)
    {
      Period = period;
      Denominator = period * (period + 1) / 2.0;
      Sum = new SumState(period);
    }

    internal int Period { get; }

    internal double Denominator { get; }

    internal double Numerator { get; set; }

    internal int Count { get; set; }

    internal SumState Sum { get; }
  }

  /// <summary>Streaming state for <see cref="RmaNext"/>. Not thread-safe.</summary>
  public sealed class RmaState
  {
    internal RmaState(int period)
    {
      Period = period;
      Seed = new SumState(period);
    }

    internal int Period { get; }

    internal double? Prev { get; set; }

    internal SumState Seed { get; }
  }

  /// <summary>
  /// Warms up streaming SMA state from history. Returns the state ready
  /// to continue the series one price at a time in O(1) via
  /// <see cref="SmaNext"/>, along with the full SMA series of the
  /// history (identical to <see cref="SmaSeries"/>).
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (SmaState State, double?[] Values) SmaInit(double[] history, int period = 50)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "sma_init");

    var state = new SmaState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = SmaNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming SMA state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The SMA of the current window, or <c>null</c> while it is still filling.</returns>
  public static double? SmaNext(SmaState state, double price)
  {
    double? sum = state.Sum.Push(price);
    return sum.HasValue ? sum.Value / state.Period : null;
  }

  /// <summary>
  /// Warms up streaming EMA state from history: returns the state and
  /// the series (identical to <see cref="EmaSeries"/>), continuing in
  /// O(1) via <see cref="EmaNext"/>.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (EmaState State, double?[] Values) EmaInit(double[] history, int period = 50)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "ema_init");

    var state = new EmaState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = EmaNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming EMA state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The EMA as of this price, or <c>null</c> while the seed window is still filling.</returns>
  public static double? EmaNext(EmaState state, double price)
  {
    if (state.Prev is null)
    {
      double? sum = state.Seed.Push(price);
      if (sum is null)
      {
        return null;
      }

      double value = sum.Value / state.Period;
      state.Prev = value;
      return value;
    }

    double next = state.Prev.Value + state.Multiplier * (price - state.Prev.Value);
    state.Prev = next;
    return next;
  }

  /// <summary>
  /// Warms up streaming WMA state from history: returns the state and
  /// the series (identical to <see cref="WmaSeries"/>), continuing in
  /// O(1) via <see cref="WmaNext"/>.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (WmaState State, double?[] Values) WmaInit(double[] history, int period = 50)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "wma_init");

    var state = new WmaState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = WmaNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming WMA state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The WMA of the current window, or <c>null</c> while it is still filling.</returns>
  public static double? WmaNext(WmaState state, double price)
  {
    if (state.Count < state.Period)
    {
      state.Sum.Push(price);
      state.Numerator = state.Numerator + price * (state.Count + 1);
      state.Count++;
      if (state.Count == state.Period)
      {
        return state.Numerator / state.Denominator;
      }

      return null;
    }

    double windowSum = state.Sum.CurrentSum;
    state.Sum.Push(price);
    state.Numerator = state.Numerator - windowSum + state.Period * price;
    return state.Numerator / state.Denominator;
  }

  /// <summary>
  /// Warms up streaming Wilder RMA state from history: returns the state
  /// and the series (identical to <see cref="RmaSeries"/>), continuing
  /// in O(1) via <see cref="RmaNext"/>.
  /// </summary>
  /// <param name="history">Prices, oldest first; at least <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (RmaState State, double?[] Values) RmaInit(double[] history, int period = 50)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period, "rma_init");

    var state = new RmaState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = RmaNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming Wilder RMA state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>The RMA as of this price, or <c>null</c> while the seed window is still filling.</returns>
  public static double? RmaNext(RmaState state, double price)
  {
    if (state.Prev is null)
    {
      double? sum = state.Seed.Push(price);
      if (sum is null)
      {
        return null;
      }

      double value = sum.Value / state.Period;
      state.Prev = value;
      return value;
    }

    double next = (state.Prev.Value * (state.Period - 1) + price) / state.Period;
    state.Prev = next;
    return next;
  }

  // The EMA values only (no null padding), from the first defined bar
  // on — the stage inputs of the composite averages.
  internal static double[] EmaValues(double[] prices, int period)
  {
    double multiplier = 2.0 / (period + 1);
    var values = new double[prices.Length - period + 1];
    double ema = SeedSum(prices, period) / period;
    values[0] = ema;
    for (int i = period; i < prices.Length; i++)
    {
      ema = ema + multiplier * (prices[i] - ema);
      values[i - period + 1] = ema;
    }

    return values;
  }

  private static double SeedSum(double[] prices, int period)
  {
    double sum = 0;
    for (int i = 0; i < period; i++)
    {
      sum += prices[i];
    }

    return sum;
  }
}

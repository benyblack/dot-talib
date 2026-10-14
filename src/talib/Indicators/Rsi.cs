namespace Talib.Indicators;

/// <summary>
/// Relative Strength Index
/// (<see href="https://en.wikipedia.org/wiki/Relative_strength_index">Wikipedia</see>).
///
/// Gain and loss averages use Wilder's smoothing (an RMA seeded with the
/// SMA of the first <c>period</c> changes), matching TA-Lib. <c>null</c>
/// is returned when there is not enough data or the window has no
/// movement (RSI is undefined).
///
/// Prices are always given in chronological order, oldest first. Series
/// functions return results aligned with the input; scalar functions
/// return <c>null</c> when there is not enough data.
/// </summary>
public static class Rsi
{
  /// <summary>
  /// Calculates RSI for each point of a price series in a single pass
  /// (Wilder-smoothed gain/loss averages).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>
  /// An array aligned with <paramref name="prices"/>: the first
  /// <c>period</c> entries are <c>null</c>, and every later entry is
  /// the RSI of the window ending at that point. For example,
  /// <c>RsiSeries([1, 2, 3, 4, 5], 3)</c> is
  /// <c>[null, null, null, 100.0, 100.0]</c>.
  /// </returns>
  public static double?[] RsiSeries(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length <= period)
    {
      return result;
    }

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
    result[period] = RsiFromSums(gainAvg, lossAvg);
    for (int i = period + 1; i < prices.Length; i++)
    {
      double change = prices[i] - prices[i - 1];
      gainAvg = (gainAvg * (period - 1) + Math.Max(change, 0)) / period;
      lossAvg = (lossAvg * (period - 1) + Math.Max(-change, 0)) / period;
      result[i] = RsiFromSums(gainAvg, lossAvg);
    }

    return result;
  }

  /// <summary>
  /// Calculates RSI over the <c>period</c> newest price changes, with
  /// Wilder's smoothing (matching TA-Lib). For example,
  /// <c>Latest([1, 2, 3, 4, 5, 6], 3)</c> is <c>100.0</c>, and
  /// <c>Latest([6, 1, 4, 2, 5, 3], 3)</c> rounds to <c>39.4737</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The RSI at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(double[] prices, int period = 14)
  {
    Guard.Period(period);
    var series = RsiSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>Wilder-smoothed average gain over the <c>period</c> newest price changes.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The average gain at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? AverageGain(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    return WilderAverages(prices, period).GainAvg;
  }

  /// <summary>Wilder-smoothed average loss over the <c>period</c> newest price changes.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The average loss at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? AverageLoss(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length <= period)
    {
      return null;
    }

    return WilderAverages(prices, period).LossAvg;
  }

  /// <summary>Sum of gains across consecutive prices, oldest first.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>The total gains; <c>0</c> when there are fewer than two prices.</returns>
  public static double Gain(double[] prices)
  {
    double sum = 0;
    for (int i = 1; i < prices.Length; i++)
    {
      sum += Math.Max(prices[i] - prices[i - 1], 0);
    }

    return sum;
  }

  /// <summary>Sum of losses across consecutive prices, oldest first.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>The total losses; <c>0</c> when there are fewer than two prices.</returns>
  public static double Loss(double[] prices)
  {
    double sum = 0;
    for (int i = 1; i < prices.Length; i++)
    {
      sum += Math.Max(-(prices[i] - prices[i - 1]), 0);
    }

    return sum;
  }

  // ------------------------------------------------------------------
  // Streaming: O(1) per price. Init replays the history through the
  // same Wilder walk the batch series runs, so its values equal the
  // batch series exactly and Next continues it bit for bit. A window
  // with no movement at all makes RSI undefined — Next returns null
  // there, like the batch series does.
  // ------------------------------------------------------------------

  /// <summary>Streaming state for <see cref="RsiNext"/>. Not thread-safe.</summary>
  public sealed class RsiState
  {
    internal RsiState(int period)
    {
      Period = period;
      Gains = new SumState(period);
      Losses = new SumState(period);
    }

    internal int Period { get; }

    internal double? Prev { get; set; }

    internal SumState Gains { get; }

    internal SumState Losses { get; }

    internal double? GainAvg { get; set; }

    internal double? LossAvg { get; set; }
  }

  /// <summary>
  /// Warms up streaming RSI state from history. Returns the state ready
  /// to continue the series one price at a time in O(1) via
  /// <see cref="RsiNext"/>, along with the full RSI series of the
  /// history (identical to <see cref="RsiSeries"/>). History needs more
  /// than <c>period</c> prices (the first <c>period</c> changes form
  /// the seed).
  /// </summary>
  /// <param name="history">Prices, oldest first; more than <c>period</c> of them.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The warmed state and its warm-up series.</returns>
  public static (RsiState State, double?[] Values) RsiInit(double[] history, int period = 14)
  {
    Guard.Period(period);
    Guard.Warmup(history.Length, period + 1, "rsi_init");

    var state = new RsiState(period);
    var values = new double?[history.Length];
    for (int i = 0; i < history.Length; i++)
    {
      values[i] = RsiNext(state, history[i]);
    }

    return (state, values);
  }

  /// <summary>Advances streaming RSI state by one price, in O(1).</summary>
  /// <param name="state">The state to advance.</param>
  /// <param name="price">The incoming price.</param>
  /// <returns>
  /// The RSI as of this price, or <c>null</c> while the seed window is
  /// still filling or the window has no movement.
  /// </returns>
  public static double? RsiNext(RsiState state, double price)
  {
    if (state.Prev is null)
    {
      state.Prev = price;
      return null;
    }

    double change = price - state.Prev.Value;
    if (state.GainAvg is null)
    {
      double? gainSum = state.Gains.Push(Math.Max(change, 0));
      double? lossSum = state.Losses.Push(Math.Max(-change, 0));
      state.Prev = price;

      if (gainSum is null)
      {
        return null;
      }

      double gainAvg = gainSum.Value / state.Period;
      double lossAvg = lossSum!.Value / state.Period;
      state.GainAvg = gainAvg;
      state.LossAvg = lossAvg;
      return RsiFromSums(gainAvg, lossAvg);
    }

    double gainAvgNext = (state.GainAvg.Value * (state.Period - 1) + Math.Max(change, 0)) / state.Period;
    double lossAvgNext = (state.LossAvg!.Value * (state.Period - 1) + Math.Max(-change, 0)) / state.Period;
    state.Prev = price;
    state.GainAvg = gainAvgNext;
    state.LossAvg = lossAvgNext;
    return RsiFromSums(gainAvgNext, lossAvgNext);
  }

  // Wilder-smoothed gain/loss averages for the latest point.
  private static (double GainAvg, double LossAvg) WilderAverages(double[] prices, int period)
  {
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
    for (int i = period + 1; i < prices.Length; i++)
    {
      double change = prices[i] - prices[i - 1];
      gainAvg = (gainAvg * (period - 1) + Math.Max(change, 0)) / period;
      lossAvg = (lossAvg * (period - 1) + Math.Max(-change, 0)) / period;
    }

    return (gainAvg, lossAvg);
  }

  // RSI from the smoothed averages: undefined (null) when the window
  // has no movement, 100 when nothing was lost, otherwise
  // 100 - 100 / (1 + gain / loss).
  private static double? RsiFromSums(double gainAvg, double lossAvg)
  {
    if (gainAvg == 0 && lossAvg == 0)
    {
      return null;
    }

    if (lossAvg == 0)
    {
      return 100.0;
    }

    return 100.0 - 100.0 / (1.0 + gainAvg / lossAvg);
  }
}

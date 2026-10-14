namespace Talib.Indicators;

/// <summary>The moving-average types <see cref="Ma.MovingAverage"/> and <see cref="Ma.Mavp"/> dispatch to (TA-Lib MA).</summary>
public enum MaType
{
  /// <summary>Simple Moving Average.</summary>
  Sma,

  /// <summary>Exponential Moving Average.</summary>
  Ema,

  /// <summary>Linear Weighted Moving Average.</summary>
  Wma,

  /// <summary>Double Exponential Moving Average.</summary>
  Dema,

  /// <summary>Triple Exponential Moving Average.</summary>
  Tema,

  /// <summary>Triangular Moving Average.</summary>
  Trima,

  /// <summary>Kaufman's Adaptive Moving Average.</summary>
  Kama,

  /// <summary>Tim Tillson's T3 Moving Average.</summary>
  T3,
}

/// <summary>
/// Moving average indicator.
///
/// Calculates SMA, WMA, EMA, Wilder RMA, DEMA, TEMA, TRIMA, KAMA, T3 and
/// Midpoint, plus the generic <see cref="MovingAverage"/> dispatcher, the
/// variable-period <see cref="Mavp"/> and streaming <c>*Init</c>/<c>*Next</c>
/// state for maintaining a moving average in O(1) as new prices arrive
/// (TRIMA, KAMA, T3, the dispatcher and MAVP are batch-only).
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

  /// <summary>
  /// Calculates the Triangular Moving Average: an SMA applied twice, over
  /// the half (odd period) or half/half-plus-one (even period) windows.
  /// For example, <c>Trima([1, 2, 3, 4, 5, 6, 7], 5)</c> is <c>5.0</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The TRIMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Trima(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var series = TrimaSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full TRIMA series: the SMA of the first-stage SMA
  /// values (the half windows), null-padded with <c>period - 1</c>
  /// entries.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded TRIMA series.</returns>
  public static double?[] TrimaSeries(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < period)
    {
      return result;
    }

    int first;
    int second;
    if (period % 2 == 1)
    {
      first = (period + 1) / 2;
      second = first;
    }
    else
    {
      first = period / 2;
      second = period / 2 + 1;
    }

    double[] values = DefinedValues(SmaSeries(prices, first));
    values = DefinedValues(SmaSeries(values, second));
    for (int i = 0; i < values.Length; i++)
    {
      result[i + period - 1] = values[i];
    }

    return result;
  }

  /// <summary>
  /// Calculates Kaufman's Adaptive Moving Average: an exponential average
  /// whose smoothing constant adapts to the efficiency ratio (net change
  /// over path length) of the last <c>period</c> prices.
  ///
  /// The smoothing constants are fixed like TA-Lib's (fastest 2/3,
  /// slowest 2/31) regardless of the period, and at <c>period</c> 1 the
  /// series is the input itself. At least <c>period + 1</c> prices are
  /// required.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The KAMA at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Kama(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var series = KamaSeries(prices, period);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full KAMA series, null-padded with <c>period</c>
  /// entries (one more than the other averages: the efficiency ratio
  /// needs <c>period</c> one-day changes to its left).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The nil-padded KAMA series.</returns>
  public static double?[] KamaSeries(double[] prices, int period = 30)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];

    // At period 1 the series is the input itself.
    if (period == 1)
    {
      for (int i = 0; i < prices.Length; i++)
      {
        result[i] = prices[i] * 1.0;
      }

      return result;
    }

    if (prices.Length < period + 1)
    {
      return result;
    }

    // The efficiency-ratio sum over the first `period` one-day changes,
    // and the trailing run of exactly-flat changes (TA-Lib resets the
    // sum when a whole window has gone flat to drop rounding residue).
    double sumRoc = 0.0;
    int nullRun = 0;
    for (int i = 0; i < period; i++)
    {
      double diff = prices[i] - prices[i + 1];
      sumRoc += Math.Abs(diff);
      nullRun = diff == 0.0 ? nullRun + 1 : 0;
    }

    double prevKama = prices[period - 1];

    // The value that left the window at the previous slide, and the
    // index of the one leaving it now — the add-then-subtract
    // volatility slide pairs them. The first loop iteration re-seeds
    // both, exactly like the reference walk.
    double trailingValue = prices[0];
    int trailingIdx = 1;

    for (int i = period; i < prices.Length; i++)
    {
      double price = prices[i];
      double roc;
      if (i == period)
      {
        roc = price - prices[0];
        trailingValue = prices[0];
        trailingIdx = 1;
      }
      else
      {
        double leaving = prices[trailingIdx];
        roc = price - leaving;

        sumRoc = sumRoc - Math.Abs(trailingValue - leaving) + Math.Abs(price - prices[i - 1]);

        nullRun = price - prices[i - 1] == 0.0 ? nullRun + 1 : 0;
        if (nullRun >= period)
        {
          // A whole window has gone flat: drop the rounding residue.
          nullRun = period;
          sumRoc = 0.0;
        }

        trailingValue = leaving;
        trailingIdx++;
      }

      double efficiency = sumRoc <= 0.0 || sumRoc <= roc
          ? 1.0
          : Math.Min(Math.Abs(roc / sumRoc), 1.0);

      double sc0 = efficiency * KamaConstDiff + KamaConstMax;
      double sc = sc0 * sc0;

      double value = (price - prevKama) * sc + prevKama;
      result[i] = value;
      prevKama = value;
    }

    return result;
  }

  /// <summary>
  /// Calculates Tim Tillson's T3 moving average: six chained EMAs
  /// combined with the given volume factor. At least <c>6 * period - 5</c>
  /// prices are required. For example, <c>T3([1..14], 2)</c> rounds to
  /// <c>13.55</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">EMA period of the six stages, a positive integer.</param>
  /// <param name="vfactor">The volume factor (TA-Lib's default is 0.7).</param>
  /// <returns>The T3 at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? T3(double[] prices, int period = 30, double vfactor = 0.7)
  {
    Guard.Period(period);
    var series = T3Series(prices, period, vfactor);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full T3 series: six chained EMA stages combined with
  /// the volume-factor coefficients, null-padded with
  /// <c>6 * (period - 1)</c> entries.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">EMA period of the six stages, a positive integer.</param>
  /// <param name="vfactor">The volume factor (TA-Lib's default is 0.7).</param>
  /// <returns>The nil-padded T3 series.</returns>
  public static double?[] T3Series(double[] prices, int period = 30, double vfactor = 0.7)
  {
    Guard.Period(period);
    var result = new double?[prices.Length];
    if (prices.Length < 6 * period - 5)
    {
      return result;
    }

    // Six chained EMA stages, e1 the closest to the prices and e6 the
    // most smoothed.
    var stages = new double[6][];
    double[] prev = prices;
    for (int s = 0; s < 6; s++)
    {
      stages[s] = EmaValues(prev, period);
      prev = stages[s];
    }

    // Coefficients exactly as TA-Lib builds them (same operation order,
    // so the values agree bit for bit).
    double temp = vfactor * vfactor;
    double c1 = 0 - temp * vfactor;
    double c2 = 3.0 * (temp - c1);
    double c3 = (0 - 6.0) * temp - 3.0 * (vfactor - c1);
    double c4 = 3.0 * temp + (3.0 * vfactor + 1.0 - c1);

    double[] e6 = stages[5];
    for (int i = 0; i < e6.Length; i++)
    {
      result[i + 6 * (period - 1)] =
          c4 * stages[2][i + 3 * (period - 1)]
          + (c3 * stages[3][i + 2 * (period - 1)] + (c1 * e6[i] + c2 * stages[4][i + period - 1]));
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

  /// <summary>
  /// The generic moving-average dispatcher (TA-Lib <c>MA</c>): computes
  /// the moving average of the given <paramref name="type"/> — one of
  /// the <see cref="MaType"/> values. For example,
  /// <c>MovingAverage([1, 2, 3, 4, 5, 6, 7, 8], 4, MaType.Ema)</c>
  /// rounds to <c>6.5</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <param name="type">The type of moving average to compute.</param>
  /// <returns>The moving average at the latest point, or <c>null</c> when there is not enough data.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="type"/> is not a defined <see cref="MaType"/> value.
  /// </exception>
  public static double? MovingAverage(double[] prices, int period, MaType type)
  {
    return type switch
    {
      MaType.Sma => Sma(prices, period),
      MaType.Ema => Ema(prices, period),
      MaType.Wma => Wma(prices, period),
      MaType.Dema => Dema(prices, period),
      MaType.Tema => Tema(prices, period),
      MaType.Trima => Trima(prices, period),
      MaType.Kama => Kama(prices, period),
      MaType.T3 => T3(prices, period),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, "unknown MA type"),
    };
  }

  /// <summary>
  /// Calculates the variable-period moving average (TA-Lib <c>MAVP</c>):
  /// the value at each point uses the period given for that point,
  /// clamped into <c>[minPeriod, maxPeriod]</c> (both defaulting to 2 and
  /// 30) and truncated toward zero. Only <see cref="MaType.Sma"/> and
  /// <see cref="MaType.Ema"/> are supported. For example,
  /// <c>Mavp([1, 2, 3, 4, 5, 6], [2, 2, 2, 2, 2, 2])</c> ends at
  /// <c>5.5</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="periods">
  /// Per-point periods as numbers, zipped with <paramref name="prices"/>:
  /// only <c>min(prices.Length, periods.Length)</c> points are produced.
  /// </param>
  /// <param name="minPeriod">The lower clamp bound, a positive integer.</param>
  /// <param name="maxPeriod">The upper clamp bound, a positive integer.</param>
  /// <param name="type">The moving-average type, Sma or Ema.</param>
  /// <returns>
  /// The variable-period series, aligned with the input; an entry is
  /// <c>null</c> while fewer than its (clamped) period prices have been
  /// seen.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// A period bound is not a positive integer, or <paramref name="type"/>
  /// is neither Sma nor Ema (the latter raised per point, once a point
  /// has enough data for its period).
  /// </exception>
  public static double?[] Mavp(
      double[] prices,
      double[] periods,
      int minPeriod = 2,
      int maxPeriod = 30,
      MaType type = MaType.Sma)
  {
    Guard.Period(minPeriod);
    Guard.Period(maxPeriod);
    int count = Math.Min(prices.Length, periods.Length);
    var result = new double?[count];
    for (int index = 0; index < count; index++)
    {
      // Clamped like the reference: max(min) then min(max), truncated.
      int period = (int)Math.Truncate(Math.Min(Math.Max(periods[index], minPeriod), maxPeriod));
      if (index + 1 < period)
      {
        continue;
      }

      switch (type)
      {
        case MaType.Sma:
          {
            double sum = 0;
            for (int i = index + 1 - period; i <= index; i++)
            {
              sum += prices[i];
            }

            result[index] = sum / period;
            break;
          }

        case MaType.Ema:
          result[index] = Ema(prices[..(index + 1)], period);
          break;

        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, "unknown MAVP type");
      }
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

  // The defined (non-null) values of a padded series — the composite
  // averages' stage input.
  internal static double[] DefinedValues(double?[] series)
  {
    var values = new double[series.Length];
    int count = 0;
    foreach (double? value in series)
    {
      if (value.HasValue)
      {
        values[count++] = value.Value;
      }
    }

    return values[..count];
  }

  // KAMA mirroring TA-Lib: the smoothing constants are fixed (fastest
  // 2/3, slowest 2/31 — not a function of the period), the efficiency
  // ratio is clamped to 1 (and set to 1 on a flat window), and the
  // volatility sum slides by add-then-subtract like the C code, so the
  // series matches TA-Lib bar for bar.
  private const double KamaConstMax = 2.0 / (30.0 + 1.0);
  private const double KamaConstDiff = 2.0 / (2.0 + 1.0) - KamaConstMax;

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

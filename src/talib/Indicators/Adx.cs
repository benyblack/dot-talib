namespace Talib.Indicators;

/// <summary>
/// Average Directional Index
/// (<see href="https://en.wikipedia.org/wiki/Average_directional_movement_index">Wikipedia</see>).
///
/// <para>
///   <c>+DM</c> / <c>-DM</c>: the up-/down-move that exceeds the opposite
///   move on a bar. <c>+DI</c> / <c>-DI</c>:
///   <c>100 * RMA(+DM) / RMA(TR)</c> and its mirror (RMA = Wilder
///   smoothing). <c>DX = 100 * |+DI - -DI| / (+DI + -DI)</c>, and
///   <c>ADX = RMA(DX)</c>, seeded with the SMA of the first
///   <c>period</c> DX values.
/// </para>
///
/// The directional lines are defined from index <c>period - 1</c> on and
/// ADX from index <c>2 * period - 2</c> (i.e. <c>2 * period - 1</c>
/// bars). Bars are always given in chronological order, oldest first,
/// and only the High, Low and Close of each bar are read. Scalars return
/// <c>null</c> when there is not enough data.
/// </summary>
public static class Adx
{
  /// <summary>The +DI, -DI and ADX values at a single point.</summary>
  public sealed record AdxPoint(double? PlusDi, double? MinusDi, double? Adx);

  /// <summary>Calculates the latest +DI (positive directional index).</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The +DI at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? PlusDi(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double?[] lines = DirectionalLines(bars, period);
    return lines[^1];
  }

  /// <summary>Calculates the latest -DI (negative directional index).</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The -DI at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? MinusDi(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double?[] lines = NegativeDirectionalLines(bars, period);
    return lines[^1];
  }

  /// <summary>
  /// Calculates the latest ADX value: Wilder's smoothed average of DX.
  ///
  /// At least <c>2 * period - 1</c> bars are required for the DX seed
  /// window; otherwise <c>null</c> is returned.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The ADX at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < 2 * period - 1)
    {
      return null;
    }

    double[]? values = AdxValues(bars, period);
    return values is null ? null : values[^1];
  }

  /// <summary>
  /// Calculates +DI, -DI and ADX of the latest point in a single pass.
  ///
  /// Returns an <see cref="AdxPoint"/> with <c>PlusDi</c>, <c>MinusDi</c>
  /// and <c>Adx</c>. Each value is <c>null</c> when its own data
  /// requirement is not met, so the directional lines can be defined
  /// while ADX is still <c>null</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The +DI, -DI and ADX of the latest point.</returns>
  public static AdxPoint AdxAll(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    double?[] plus = DirectionalLines(bars, period);
    double?[] minus = NegativeDirectionalLines(bars, period);

    double? plusDi = plus.Length == 0 ? null : plus[^1];
    double? minusDi = minus.Length == 0 ? null : minus[^1];
    double[]? adxValues = AdxValues(bars, period);
    double? adx = adxValues is null ? null : adxValues[^1];

    return new AdxPoint(plusDi, minusDi, adx);
  }

  /// <summary>
  /// Calculates the latest DX value: <c>100 * |+DI - -DI| / (+DI + -DI)</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The DX at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Dx(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double[] values = DxValues(bars, period);
    return values.Length == 0 ? null : values[^1];
  }

  /// <summary>
  /// Calculates the latest ADX Rating: the average of the current ADX
  /// and the ADX <c>period - 1</c> bars ago.
  ///
  /// At least <c>3 * period - 2</c> bars are required; otherwise
  /// <c>null</c> is returned.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer (default 14).</param>
  /// <returns>The ADXR at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Adxr(Bar[] bars, int period = 14)
  {
    Guard.Period(period);
    if (bars.Length < 3 * period - 2)
    {
      return null;
    }

    double[]? values = AdxValues(bars, period);
    return values is null ? null : (values[^1] + values[values.Length - period]) / 2;
  }

  /// <summary>
  /// The raw +DM per bar (the up-move that exceeds the opposite
  /// down-move); the first bar has no previous bar to compare against
  /// and is 0.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <returns>The +DM of each bar, aligned with the input.</returns>
  public static double[] PlusDm(Bar[] bars)
  {
    var (plus, _) = DmSeries(bars);
    return plus;
  }

  /// <summary>The raw -DM per bar; the first bar is 0.</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <returns>The -DM of each bar, aligned with the input.</returns>
  public static double[] MinusDm(Bar[] bars)
  {
    var (_, minus) = DmSeries(bars);
    return minus;
  }

  // The smoothed +DI series: 100 * RMA(+DM) / RMA(TR) per bar, null
  // while either RMA is still filling or the smoothed TR is zero.
  private static double?[] DirectionalLines(Bar[] bars, int period)
  {
    var (plus, _) = DmSeries(bars);
    return DiSeries(Ma.RmaSeries(plus, period), Ma.RmaSeries(TrueRangeSeries(bars), period));
  }

  // The smoothed -DI series: 100 * RMA(-DM) / RMA(TR) per bar.
  private static double?[] NegativeDirectionalLines(Bar[] bars, int period)
  {
    var (_, minus) = DmSeries(bars);
    return DiSeries(Ma.RmaSeries(minus, period), Ma.RmaSeries(TrueRangeSeries(bars), period));
  }

  // 100 * dm / tr per bar; null where either smoothed input is null or
  // the smoothed TR is zero.
  private static double?[] DiSeries(double?[] dmSeries, double?[] trSeries)
  {
    var result = new double?[dmSeries.Length];
    for (int i = 0; i < dmSeries.Length; i++)
    {
      double? dm = dmSeries[i];
      double? tr = trSeries[i];
      result[i] = dm.HasValue && tr.HasValue && tr.Value != 0 ? 100 * dm.Value / tr.Value : null;
    }

    return result;
  }

  // The True Range of every bar; the first bar has no previous close.
  private static double[] TrueRangeSeries(Bar[] bars)
  {
    var trs = new double[bars.Length];
    if (bars.Length == 0)
    {
      return trs;
    }

    trs[0] = bars[0].High - bars[0].Low;
    for (int i = 1; i < bars.Length; i++)
    {
      double high = bars[i].High;
      double low = bars[i].Low;
      double prevClose = bars[i - 1].Close;
      trs[i] = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
    }

    return trs;
  }

  // The raw +DM/-DM per bar; the first bar has no previous bar to
  // compare against and is 0.
  private static (double[] Plus, double[] Minus) DmSeries(Bar[] bars)
  {
    var plus = new double[bars.Length];
    var minus = new double[bars.Length];
    for (int i = 1; i < bars.Length; i++)
    {
      double up = bars[i].High - bars[i - 1].High;
      double down = bars[i - 1].Low - bars[i].Low;

      if (up > down && up > 0)
      {
        plus[i] = up;
      }
      else if (down > up && down > 0)
      {
        minus[i] = down;
      }
    }

    return (plus, minus);
  }

  // The DX series, defined from index period - 1 on: pairs where both
  // directional lines are defined and their sum is nonzero, mapped to
  // 100 * |p - m| / (p + m).
  private static double[] DxValues(Bar[] bars, int period)
  {
    double?[] plus = DirectionalLines(bars, period);
    double?[] minus = NegativeDirectionalLines(bars, period);

    var dx = new List<double>();
    for (int i = 0; i < plus.Length; i++)
    {
      double? p = plus[i];
      double? m = minus[i];
      if (p is null || m is null)
      {
        continue;
      }

      double sum = p.Value + m.Value;
      if (sum == 0)
      {
        continue;
      }

      dx.Add(100 * Math.Abs(p.Value - m.Value) / sum);
    }

    return dx.ToArray();
  }

  // The ADX series: the Wilder RMA of the DX values, the leading nulls
  // rejected; null when fewer than period DX values are defined.
  private static double[]? AdxValues(Bar[] bars, int period)
  {
    double[] dx = DxValues(bars, period);
    if (dx.Length < period)
    {
      return null;
    }

    return Ma.RmaSeries(dx, period).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
  }
}

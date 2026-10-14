namespace Talib.Indicators;

/// <summary>
/// Chaikin Accumulation/Distribution line and oscillator.
///
/// With the Close Location Value
/// <c>CLV = ((close - low) - (high - close)) / (high - low)</c>:
///
/// <list type="bullet">
/// <item><c>AD</c> accumulates <c>CLV * volume</c> across bars
/// (zero-range bars contribute nothing)</item>
/// <item><c>ADOSC = EMA(CLV * volume, fast) - EMA(CLV * volume, slow)</c></item>
/// </list>
///
/// Bars are always given in chronological order, oldest first, and all
/// functions return <c>null</c> when there is not enough data. Only
/// the High, Low, Close and Volume of each bar are read.
/// </summary>
public static class ChaikinAd
{
  /// <summary>Calculates the latest value of the Chaikin A/D line.</summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <returns>The A/D line at the latest point, or <c>null</c> for empty input.</returns>
  public static double? Latest(Bar[] bars)
  {
    var series = AdSeries(bars);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the Chaikin A/D line for every bar: the running total
  /// of <c>CLV * volume</c>, starting from zero at the first bar.
  ///
  /// Returns an array aligned with <paramref name="bars"/>; zero-range
  /// bars contribute a flow of exactly <c>0</c> to the total.
  /// </summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <returns>The A/D line series.</returns>
  public static double?[] AdSeries(Bar[] bars)
  {
    var result = new double?[bars.Length];
    double total = 0.0;
    for (int i = 0; i < bars.Length; i++)
    {
      total = total + MoneyFlow(bars[i]);
      result[i] = total;
    }

    return result;
  }

  /// <summary>
  /// Calculates the Chaikin A/D oscillator:
  /// <c>EMA(flow, fast) - EMA(flow, slow)</c>, defined once the slow
  /// EMA fills (from bar <c>slow - 1</c> on).
  /// </summary>
  /// <param name="bars">Bars, oldest first; High, Low, Close and Volume are read.</param>
  /// <param name="fast">Fast EMA period, a positive integer.</param>
  /// <param name="slow">Slow EMA period, a positive integer.</param>
  /// <returns>
  /// The A/D oscillator at the latest point, or <c>null</c> when there
  /// is not enough data — fewer bars than <paramref name="slow"/>
  /// (or than <paramref name="fast"/>, when it is the longer period).
  /// </returns>
  public static double? Oscillator(Bar[] bars, int fast = 3, int slow = 10)
  {
    Guard.Period(fast);
    Guard.Period(slow);
    if (bars.Length < slow)
    {
      return null;
    }

    var flows = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      flows[i] = MoneyFlow(bars[i]);
    }

    double? fastEma = Ma.Ema(flows, fast);
    double? slowEma = Ma.Ema(flows, slow);
    if (fastEma is null || slowEma is null)
    {
      return null;
    }

    return fastEma.Value - slowEma.Value;
  }

  // The per-bar money flow: the Close Location Value times the volume.
  // A zero-range (high == low) bar is defined to contribute 0.0.
  private static double MoneyFlow(Bar bar)
  {
    double range = bar.High - bar.Low;
    if (range == 0)
    {
      return 0.0;
    }

    double clv = (bar.Close - bar.Low - (bar.High - bar.Close)) / range;
    return clv * bar.Volume;
  }
}

namespace Talib.Indicators;

/// <summary>
/// Keltner Channels <see href="https://en.wikipedia.org/wiki/Keltner_channel"/>.
///
/// * middle = EMA(close, period)
/// * upper = middle + mult * ATR(period)
/// * lower = middle - mult * ATR(period)
///
/// The EMA is seeded at bar <c>period - 1</c> while the ATR is seeded at
/// bar <c>period</c>, so the series is defined from bar <c>period</c> on.
/// Bars are always given in chronological order, oldest first, and every
/// function returns <c>null</c> when there is not enough data. Only the
/// High, Low and Close of each bar are read.
/// </summary>
public static class Keltner
{
  /// <summary>One Keltner Channels reading.</summary>
  /// <param name="Upper">The upper channel: <c>Middle + mult * ATR</c>.</param>
  /// <param name="Middle">The middle channel: the EMA of the closes.</param>
  /// <param name="Lower">The lower channel: <c>Middle - mult * ATR</c>.</param>
  public readonly record struct KeltnerResult(double Upper, double Middle, double Lower);

  /// <summary>Calculates the latest Keltner Channels.</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">EMA/ATR lookback window, a positive integer.</param>
  /// <param name="mult">ATR multiplier.</param>
  /// <returns>The channels at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static KeltnerResult? Latest(Bar[] bars, int period = 20, double mult = 2)
  {
    Guard.Period(period);
    if (bars.Length < period)
    {
      return null;
    }

    double middle = Ma.Ema(Closes(bars), period)!.Value;
    double? atr = Atr.Latest(bars, period);
    if (atr is null)
    {
      // Exactly `period` bars: the EMA is defined but the ATR is not
      // yet (it needs period + 1), so there is no deviation — null,
      // like every other insufficient-data case.
      return null;
    }

    double deviation = mult * atr.Value;
    return new KeltnerResult(middle + deviation, middle, middle - deviation);
  }

  /// <summary>
  /// Calculates the full Keltner Channels series: the EMA of the closes
  /// combined with the ATR series, a channel set wherever both are
  /// defined.
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// <c>period</c> entries are <c>null</c> (the ATR lags the EMA seed by
  /// one bar), and every later entry holds the channels of that bar.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="period">EMA/ATR lookback window, a positive integer.</param>
  /// <param name="mult">ATR multiplier.</param>
  /// <returns>The null-padded channels series.</returns>
  public static KeltnerResult?[] KeltnerSeries(Bar[] bars, int period = 20, double mult = 2)
  {
    Guard.Period(period);
    var result = new KeltnerResult?[bars.Length];

    double?[] emas = Ma.EmaSeries(Closes(bars), period);
    double?[] atrs = Atr.AtrSeries(bars, period);
    for (int i = 0; i < bars.Length; i++)
    {
      if (emas[i] is null || atrs[i] is null)
      {
        continue;
      }

      double middle = emas[i]!.Value;
      double atr = atrs[i]!.Value;
      result[i] = new KeltnerResult(middle + mult * atr, middle, middle - mult * atr);
    }

    return result;
  }

  private static double[] Closes(Bar[] bars)
  {
    var closes = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      closes[i] = bars[i].Close;
    }

    return closes;
  }
}

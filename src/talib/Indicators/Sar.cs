namespace Talib.Indicators;

/// <summary>
/// Parabolic SAR (<see href="https://en.wikipedia.org/wiki/Parabolic_SAR">Wikipedia</see>).
///
/// The trend starts up when the second close is above the first, and the
/// acceleration factor starts at <c>afStart</c> (default 0.02), stepping
/// by <c>afStep</c> (default 0.02) on every new extreme, capped at
/// <c>afMax</c> (default 0.2). Bars are always given in chronological
/// order, oldest first, and the series is returned aligned with the
/// input; values are defined from the second bar on. Only the High, Low
/// and Close of each bar are read.
/// </summary>
public static class Sar
{
  /// <summary>
  /// Calculates the latest Parabolic SAR value. For example, on the bars
  /// <c>(10, 9, 9.5), (11, 9.5, 10.5), (12, 10, 11.5), (11.5, 9.5, 10),
  /// (12.5, 10.5, 12)</c> (high, low, close) it rounds to <c>9.2352</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The SAR at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Latest(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2)
  {
    var series = SarSeries(bars, afStart, afStep, afMax);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// Calculates the full Parabolic SAR series.
  ///
  /// Returns an array aligned with <paramref name="bars"/>: the first
  /// entry is <c>null</c> (the trend needs two bars), every later entry
  /// is the SAR stop for that bar. The stop is clamped against the last
  /// two bars' opposite extremes, and the trend reverses (resetting the
  /// stop to the extreme point) when penetrated.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The null-padded SAR series.</returns>
  public static double?[] SarSeries(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2)
  {
    if (bars.Length == 0)
    {
      return Array.Empty<double?>();
    }

    var result = new double?[bars.Length];
    if (bars.Length == 1)
    {
      return result;
    }

    // State: the last two bars, the trend, the SAR stop and the extreme
    // point. The trend starts up when the second close is above the first.
    Bar first = bars[0];
    Bar second = bars[1];
    bool isUp = second.Close > first.Close;
    double sar;
    double ep;
    if (isUp)
    {
      sar = Math.Min(first.Low, second.Low);
      ep = Math.Max(first.High, second.High);
    }
    else
    {
      sar = Math.Max(first.High, second.High);
      ep = Math.Min(first.Low, second.Low);
    }

    result[1] = sar;
    double af = afStart;
    for (int i = 2; i < bars.Length; i++)
    {
      Bar prev2 = bars[i - 2];
      Bar prev1 = bars[i - 1];
      Bar bar = bars[i];
      if (isUp)
      {
        double newSar = Math.Min(sar + af * (ep - sar), Math.Min(prev1.Low, prev2.Low));
        if (bar.Low < newSar)
        {
          // Reversal: the stop jumps to the extreme point, the trend
          // flips and the acceleration resets to the step.
          result[i] = ep;
          isUp = false;
          sar = ep;
          ep = bar.High;
          af = afStep;
        }
        else
        {
          (double newEp, double newAf) = Accelerate(bar.High, ep, af, afStep, afMax);
          sar = newSar;
          ep = newEp;
          af = newAf;
          result[i] = newSar;
        }
      }
      else
      {
        double newSar = Math.Max(sar + af * (ep - sar), Math.Max(prev1.High, prev2.High));
        if (bar.High > newSar)
        {
          // Reversal: the stop jumps to the extreme point, the trend
          // flips and the acceleration resets to the step.
          result[i] = ep;
          isUp = true;
          sar = ep;
          ep = bar.Low;
          af = afStep;
        }
        else
        {
          (double newEp, double newAf) = Decelerate(bar.Low, ep, af, afStep, afMax);
          sar = newSar;
          ep = newEp;
          af = newAf;
          result[i] = newSar;
        }
      }
    }

    return result;
  }

  private static (double Ep, double Af) Accelerate(double high, double ep, double af, double afStep, double afMax)
  {
    if (high > ep)
    {
      return (high, Math.Min(af + afStep, afMax));
    }

    return (ep, af);
  }

  private static (double Ep, double Af) Decelerate(double low, double ep, double af, double afStep, double afMax)
  {
    if (low < ep)
    {
      return (low, Math.Min(af + afStep, afMax));
    }

    return (ep, af);
  }

  /// <summary>
  /// The extended Parabolic SAR (TA-Lib SAREXT) with the standard start,
  /// step and maximum acceleration factors. For example, on the bars
  /// <c>(10, 9, 9.5), (11, 9.5, 10.5), (12, 10, 11.5)</c> (high, low,
  /// close) it is <c>9</c>.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The extended SAR at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? SarExt(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2)
  {
    var series = SarExtSeries(bars, afStart, afStep, afMax);
    return series.Length == 0 ? null : series[^1];
  }

  /// <summary>
  /// The full extended Parabolic SAR (TA-Lib SAREXT) series with the
  /// standard start, step and maximum acceleration factors — the same
  /// series as <see cref="SarSeries"/>.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The null-padded extended SAR series.</returns>
  public static double?[] SarExtSeries(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2)
  {
    return SarSeries(bars, afStart, afStep, afMax);
  }
}

namespace Talib.Indicators;

/// <summary>
/// Parabolic SAR (<see href="https://en.wikipedia.org/wiki/Parabolic_SAR">Wikipedia</see>),
/// following the TA-Lib C reference exactly (verified value by value in
/// the audit).
///
/// The initial trend comes from +DM/-DM between the first two bars (a tie
/// defaults long); the first bar is consumed — its low (uptrend) or high
/// (downtrend) is the SAR of the second bar, and the extreme point starts
/// at the second bar's extreme. The acceleration factor starts at
/// <c>afStart</c> (default 0.02), stepping by <c>afStep</c> on every new
/// extreme, capped at <c>afMax</c> (default 0.2); reversals reset it and
/// output the extreme point. Stops are computed with a fused
/// multiply-add and clamped against the last two bars' opposite
/// extremes.
///
/// Note: the Elixir reference seeds the trend from closes and uses plain
/// multiply-add; this implementation follows the C library. Bars are
/// always given in chronological order, oldest first, and the series is
/// returned aligned with the input; values are defined from the second
/// bar on. Only the High and Low of each bar are read.
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
  /// is the SAR stop for that bar.
  /// </summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The null-padded SAR series.</returns>
  public static double?[] SarSeries(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2)
  {
    var result = new double?[bars.Length];
    if (bars.Length < 2)
    {
      return result;
    }

    // The C reference uses one acceleration as both the start/reset value
    // and the increment, clamped against the maximum; the split API keeps
    // them separately configurable but clamps the start the same way.
    double accel = afStart > afMax ? afMax : afStart;
    double af = accel;

    // Initial direction from +DM/-DM between the first two bars: -DM > 0
    // means short, anything else (including a tie) long.
    double upMove = bars[1].High - bars[0].High;
    double downMove = bars[0].Low - bars[1].Low;
    bool isLong = !(downMove > upMove && downMove > 0);

    // Consume the first bar: its low (long) or high (short) is the SAR of
    // the second bar; the extreme point starts at the second bar.
    double ep;
    double sar;
    if (isLong)
    {
      ep = bars[1].High;
      sar = bars[0].Low;
    }
    else
    {
      ep = bars[1].Low;
      sar = bars[0].High;
    }

    for (int i = 1; i < bars.Length; i++)
    {
      // Like the C reference's first-iteration "cheat", the previous bar
      // is the current bar on the very first pass, so the clamp ranges
      // start at bar 1 (not bar 0).
      int prevIdx = Math.Max(i - 1, 1);
      double prevHigh = bars[prevIdx].High;
      double prevLow = bars[prevIdx].Low;
      double newHigh = bars[i].High;
      double newLow = bars[i].Low;

      if (isLong)
      {
        if (newLow <= sar)
        {
          // Switch to short: output the extreme point, clamped into the
          // last two bars' highs, then restart from the new low.
          isLong = false;
          sar = ep;
          if (sar < prevHigh)
          {
            sar = prevHigh;
          }

          if (sar < newHigh)
          {
            sar = newHigh;
          }

          result[i] = sar;
          af = accel;
          ep = newLow;
          sar = Math.FusedMultiplyAdd(af, ep - sar, sar);
          if (sar < prevHigh)
          {
            sar = prevHigh;
          }

          if (sar < newHigh)
          {
            sar = newHigh;
          }
        }
        else
        {
          // No switch: output the stop calculated last iteration, then
          // advance the extreme and acceleration.
          result[i] = sar;
          if (newHigh > ep)
          {
            ep = newHigh;
            af += afStep;
            if (af > afMax)
            {
              af = afMax;
            }
          }

          sar = Math.FusedMultiplyAdd(af, ep - sar, sar);
          if (sar > prevLow)
          {
            sar = prevLow;
          }

          if (sar > newLow)
          {
            sar = newLow;
          }
        }
      }
      else if (newHigh >= sar)
      {
        // Switch to long: output the extreme point, clamped into the
        // last two bars' lows, then restart from the new high.
        isLong = true;
        sar = ep;
        if (sar > prevLow)
        {
          sar = prevLow;
        }

        if (sar > newLow)
        {
          sar = newLow;
        }

        result[i] = sar;
        af = accel;
        ep = newHigh;
        sar = Math.FusedMultiplyAdd(af, ep - sar, sar);
        if (sar > prevLow)
        {
          sar = prevLow;
        }

        if (sar > newLow)
        {
          sar = newLow;
        }
      }
      else
      {
        result[i] = sar;
        if (newLow < ep)
        {
          ep = newLow;
          af += afStep;
          if (af > afMax)
          {
            af = afMax;
          }
        }

        sar = Math.FusedMultiplyAdd(af, ep - sar, sar);
        if (sar < prevHigh)
        {
          sar = prevHigh;
        }

        if (sar < newHigh)
        {
          sar = newHigh;
        }
      }
    }

    return result;
  }

  /// <summary>Calculates the latest extended Parabolic SAR (same defaults as <see cref="Latest"/>).</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The SAR at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? SarExt(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2) =>
      Latest(bars, afStart, afStep, afMax);

  /// <summary>Calculates the full extended Parabolic SAR series (same defaults as <see cref="SarSeries"/>).</summary>
  /// <param name="bars">Bars, oldest first.</param>
  /// <param name="afStart">Initial acceleration factor (default 0.02).</param>
  /// <param name="afStep">Acceleration factor increment (default 0.02).</param>
  /// <param name="afMax">Acceleration factor cap (default 0.2).</param>
  /// <returns>The null-padded SAR series.</returns>
  public static double?[] SarExtSeries(Bar[] bars, double afStart = 0.02, double afStep = 0.02, double afMax = 0.2) =>
      SarSeries(bars, afStart, afStep, afMax);
}

namespace Talib.Indicators;

/// <summary>
/// MESA Adaptive Moving Average (TA-Lib MAMA), translated from the
/// TA-Lib C reference implementation of John Ehlers' MESA machinery.
///
/// The alpha of the exponential average adapts to the phase rate of the
/// Hilbert-transformed price: it moves at the fast limit when the phase
/// rate is at or below one degree per bar (trending) and slows toward
/// the slow limit as the phase rate rises. FAMA is MAMA itself averaged
/// again with half the alpha.
///
/// Like the 32-lookback Hilbert family (<see cref="Ht.DcPeriodSeries"/>,
/// <see cref="Ht.PhasorSeries"/>), the first 32 entries of each series
/// are <c>null</c>; TA-Lib's configurable unstable-period machinery is
/// not mirrored, and there is no streaming form. With fewer than 64
/// prices every series is all <c>null</c>.
/// </summary>
public static class Mama
{
  /// <summary>The MAMA and FAMA series, each aligned with the input prices.</summary>
  /// <param name="Mama">The MESA adaptive moving average series.</param>
  /// <param name="Fama">The follow adaptive moving average series: MAMA averaged again with half the alpha.</param>
  public sealed record MamaResult(double?[] Mama, double?[] Fama);

  /// <summary>
  /// Calculates the MAMA and FAMA series. For example, on the ramp
  /// <c>1..100</c> the latest MAMA value rounds to <c>95.7327</c> (FAMA
  /// to <c>90.7448</c>).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="fastLimit">Maximum alpha, between 0.01 and 0.99 (default 0.5).</param>
  /// <param name="slowLimit">Minimum alpha, between 0.01 and 0.99 (default 0.05).</param>
  /// <returns>
  /// The MAMA and FAMA series aligned with <paramref name="prices"/>:
  /// the first 32 entries of each are <c>null</c>, and every entry is
  /// <c>null</c> when there are fewer than 64 prices.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="fastLimit"/> or <paramref name="slowLimit"/> is not
  /// between 0.01 and 0.99.
  /// </exception>
  public static MamaResult MamaSeries(double[] prices, double fastLimit = 0.5, double slowLimit = 0.05)
  {
    if (fastLimit < 0.01 || fastLimit > 0.99)
    {
      throw new ArgumentOutOfRangeException(nameof(fastLimit), fastLimit, "fastLimit must be between 0.01 and 0.99");
    }

    if (slowLimit < 0.01 || slowLimit > 0.99)
    {
      throw new ArgumentOutOfRangeException(nameof(slowLimit), slowLimit, "slowLimit must be between 0.01 and 0.99");
    }

    var mamaSeries = new double?[prices.Length];
    var famaSeries = new double?[prices.Length];
    if (prices.Length < Ht.MinPrices)
    {
      return new MamaResult(mamaSeries, famaSeries);
    }

    var core = new Ht.CoreState(prices, Ht.WarmupShort);
    double prevPhase = 0.0;
    double mama = 0.0;
    double fama = 0.0;
    while (core.Next(out Ht.CoreBar bar))
    {
      (double alpha, double phase) = AdaptiveAlpha(bar.Q1, bar.I1Prev3, prevPhase, fastLimit, slowLimit);

      mama = (1 - alpha) * mama + alpha * bar.Price;
      double half = alpha * 0.5;
      fama = (1 - half) * fama + half * mama;

      if (bar.Today >= Ht.LookbackShort)
      {
        mamaSeries[bar.Today] = mama;
        famaSeries[bar.Today] = fama;
      }

      prevPhase = phase;
    }

    return new MamaResult(mamaSeries, famaSeries);
  }

  // The adaptive alpha from the phase rate: fast while the delta phase
  // stays under one degree per bar, otherwise fastLimit / delta clamped
  // from below at slowLimit.
  private static (double Alpha, double Phase) AdaptiveAlpha(
      double q1,
      double i1Prev3,
      double prevPhase,
      double fastLimit,
      double slowLimit)
  {
    double phase = i1Prev3 != 0.0
        ? Math.Atan(q1 / i1Prev3) * Ht.Rad2Deg
        : 0.0;

    double delta = prevPhase - phase;
    double alpha;
    if (delta < 1.0)
    {
      alpha = fastLimit;
    }
    else
    {
      double clamped = fastLimit / delta;
      alpha = clamped < slowLimit ? slowLimit : clamped;
    }

    return (alpha, phase);
  }
}

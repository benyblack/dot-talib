namespace Talib.Indicators;

/// <summary>
/// Hilbert-transform cycle indicators (TA-Lib Cycle Indicators group),
/// translated from the TA-Lib C reference implementation of John Ehlers'
/// MESA machinery ("Rocket Science for Traders").
///
/// Every function takes prices oldest first and returns a series aligned
/// with the input: the first 63 entries are <c>null</c> — the fixed warmup
/// of the 4-bar WMA pre-smoother and the Hilbert transformer. The
/// dominant-cycle-period and phasor family warms the WMA with 12 bars and
/// emits from bar 32, as in TA-Lib; the rest use 37/63. TA-Lib's
/// configurable unstable-period machinery, which shifts where outputs
/// begin, is not mirrored, and there is no streaming form. With fewer
/// than 64 prices every series is all <c>null</c>.
/// </summary>
public static class Ht
{
  /// <summary>The null-padding warmup of the 63-lookback family: bars 0..36 emit <c>null</c>.</summary>
  internal const int Warmup = 37;

  /// <summary>The first bar with a defined value in the 63-lookback family.</summary>
  internal const int Lookback = 63;

  /// <summary>The null-padding warmup of the 32-lookback family: bars 0..11 emit <c>null</c>.</summary>
  internal const int WarmupShort = 12;

  /// <summary>The first bar with a defined value in the 32-lookback family.</summary>
  internal const int LookbackShort = 32;

  /// <summary>The minimum number of prices every indicator needs, as in the TA-Lib reference.</summary>
  internal const int MinPrices = 64;

  // The fixed Hilbert-transformer coefficients of the C reference.
  private const double A = 0.0962;
  private const double B = 0.5769;

  /// <summary>Degrees per radian, computed as <c>45 / atan(1)</c> like the reference.</summary>
  internal static readonly double Rad2Deg = 45 / Math.Atan(1);

  // Radians per degree: atan(1) / 45 (pi / 180).
  private static readonly double Deg2Rad = Math.Atan(1) / 45;

  // A full turn in radians: atan(1) * 8 (2 * pi).
  private static readonly double Deg2RadBy360 = Math.Atan(1) * 8;

  /// <summary>The in-phase (I1) and quadrature (Q1) series, each aligned with the input prices.</summary>
  /// <param name="Inphase">The in-phase component series.</param>
  /// <param name="Quadrature">The quadrature component series.</param>
  public sealed record PhasorResult(double?[] Inphase, double?[] Quadrature);

  /// <summary>The sine wave and its 45-degree lead, each aligned with the input prices.</summary>
  /// <param name="Sine">The sine wave series.</param>
  /// <param name="LeadSine">The lead sine series: the sine wave 45 degrees ahead.</param>
  public sealed record SineResult(double?[] Sine, double?[] LeadSine);

  /// <summary>
  /// The Hilbert Transform - Instantaneous Trendline: the raw price
  /// averaged over the dominant cycle and blended with its own recent
  /// values. For example, on the ramp <c>1..100</c> the latest value
  /// rounds to <c>76.85</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>
  /// The trendline series aligned with <paramref name="prices"/>: the
  /// first 63 entries are <c>null</c>, and every entry is <c>null</c>
  /// when there are fewer than 64 prices.
  /// </returns>
  public static double?[] TrendlineSeries(double[] prices)
  {
    var result = new double?[prices.Length];
    if (prices.Length < MinPrices)
    {
      return result;
    }

    var core = new CoreState(prices, Warmup);
    double t1 = 0;
    double t2 = 0;
    double t3 = 0;
    while (core.Next(out CoreBar bar))
    {
      double avg = RawAverage(prices, bar.Today, bar.DcpInt);
      double value = (2.0 * t2 + (4.0 * avg + 3.0 * t1) + t3) / 10.0;
      if (bar.Today >= Lookback)
      {
        result[bar.Today] = value;
      }

      t3 = t2;
      t2 = t1;
      t1 = avg;
    }

    return result;
  }

  /// <summary>
  /// The Hilbert Transform - Dominant Cycle Period. For example, on the
  /// ramp <c>1..100</c> the latest value rounds to <c>42.5223</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>
  /// The dominant cycle period series aligned with
  /// <paramref name="prices"/>: the first 32 entries are <c>null</c> (the
  /// 12-bar warmup family, as in TA-Lib), and every entry is
  /// <c>null</c> when there are fewer than 64 prices.
  /// </returns>
  public static double?[] DcPeriodSeries(double[] prices)
  {
    var result = new double?[prices.Length];
    if (prices.Length < MinPrices)
    {
      return result;
    }

    var core = new CoreState(prices, WarmupShort);
    while (core.Next(out CoreBar bar))
    {
      if (bar.Today >= LookbackShort)
      {
        result[bar.Today] = bar.SmoothPeriod;
      }
    }

    return result;
  }

  /// <summary>
  /// The Hilbert Transform - Dominant Cycle Phase, in degrees. For
  /// example, on the ramp <c>1..100</c> the latest value rounds to
  /// <c>183.9149</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>
  /// The dominant cycle phase series aligned with
  /// <paramref name="prices"/>: the first 63 entries are <c>null</c>, and
  /// every entry is <c>null</c> when there are fewer than 64 prices.
  /// </returns>
  public static double?[] DcPhaseSeries(double[] prices)
  {
    var result = new double?[prices.Length];
    if (prices.Length < MinPrices)
    {
      return result;
    }

    var core = new CoreState(prices, Warmup);
    var sp = new double[50];
    int spIdx = 0;
    double dcPhase = 0;
    while (core.Next(out CoreBar bar))
    {
      sp[spIdx] = bar.Smoothed;
      dcPhase = DominantCyclePhase(sp, spIdx, bar.SmoothPeriod, dcPhase, bar.DcpInt);
      if (bar.Today >= Lookback)
      {
        result[bar.Today] = dcPhase;
      }

      spIdx = spIdx == 49 ? 0 : spIdx + 1;
    }

    return result;
  }

  /// <summary>
  /// The Hilbert Transform - Phasor Components: the in-phase (I1) and
  /// quadrature (Q1) series. For example, on the ramp <c>1..100</c> the
  /// latest quadrature value rounds to <c>-1.9195</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>
  /// The two phasor component series aligned with
  /// <paramref name="prices"/>: the first 32 entries of each are
  /// <c>null</c> (the 12-bar warmup family, as in TA-Lib), and every
  /// entry is <c>null</c> when there are fewer than 64 prices.
  /// </returns>
  public static PhasorResult PhasorSeries(double[] prices)
  {
    var inphase = new double?[prices.Length];
    var quadrature = new double?[prices.Length];
    if (prices.Length >= MinPrices)
    {
      var core = new CoreState(prices, WarmupShort);
      while (core.Next(out CoreBar bar))
      {
        if (bar.Today >= LookbackShort)
        {
          inphase[bar.Today] = bar.I1Prev3;
          quadrature[bar.Today] = bar.Q1;
        }
      }
    }

    return new PhasorResult(inphase, quadrature);
  }

  /// <summary>
  /// The Hilbert Transform - SineWave: the sine wave and its lead (45
  /// degrees ahead). For example, on the ramp <c>1..100</c> the latest
  /// sine value rounds to <c>-0.0683</c>.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>
  /// The sine and lead sine series aligned with <paramref name="prices"/>:
  /// the first 63 entries of each are <c>null</c>, and every entry is
  /// <c>null</c> when there are fewer than 64 prices.
  /// </returns>
  public static SineResult SineSeries(double[] prices)
  {
    var sine = new double?[prices.Length];
    var leadSine = new double?[prices.Length];
    if (prices.Length < MinPrices)
    {
      return new SineResult(sine, leadSine);
    }

    var core = new CoreState(prices, Warmup);
    var sp = new double[50];
    int spIdx = 0;
    double dcPhase = 0;
    while (core.Next(out CoreBar bar))
    {
      sp[spIdx] = bar.Smoothed;
      dcPhase = DominantCyclePhase(sp, spIdx, bar.SmoothPeriod, dcPhase, bar.DcpInt);
      if (bar.Today >= Lookback)
      {
        sine[bar.Today] = Math.Sin(dcPhase * Deg2Rad);
        leadSine[bar.Today] = Math.Sin((dcPhase + 45) * Deg2Rad);
      }

      spIdx = spIdx == 49 ? 0 : spIdx + 1;
    }

    return new SineResult(sine, leadSine);
  }

  /// <summary>
  /// The Hilbert Transform - Trend vs Cycle Mode: 1 when the price is
  /// trending, 0 when it is cycling. For example, on the ramp
  /// <c>1..100</c> the latest value is <c>1</c> (a pure trend).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <returns>
  /// The mode series aligned with <paramref name="prices"/>: the first
  /// 63 entries are <c>null</c>, and every entry is <c>null</c> when
  /// there are fewer than 64 prices.
  /// </returns>
  public static int?[] TrendModeSeries(double[] prices)
  {
    var result = new int?[prices.Length];
    if (prices.Length < MinPrices)
    {
      return result;
    }

    var core = new CoreState(prices, Warmup);
    var sp = new double[50];
    int spIdx = 0;
    double dcPhase = 0;
    double prevDcPhase = 0;
    double prevSine = 0;
    double prevLeadSine = 0;
    double daysInTrend = 0;
    double t1 = 0;
    double t2 = 0;
    double t3 = 0;
    while (core.Next(out CoreBar bar))
    {
      sp[spIdx] = bar.Smoothed;
      double smoothPeriod = bar.SmoothPeriod;
      dcPhase = DominantCyclePhase(sp, spIdx, smoothPeriod, dcPhase, bar.DcpInt);
      double avg = RawAverage(prices, bar.Today, bar.DcpInt);
      double trendline = (2.0 * t2 + (4.0 * avg + 3.0 * t1) + t3) / 10.0;
      double sine = Math.Sin(dcPhase * Deg2Rad);
      double leadSine = Math.Sin((dcPhase + 45) * Deg2Rad);
      bool crossed = (sine > leadSine && prevSine <= prevLeadSine)
          || (sine < leadSine && prevSine >= prevLeadSine);
      daysInTrend = (crossed ? 0.0 : daysInTrend) + 1.0;
      int trend = crossed || daysInTrend < 0.5 * smoothPeriod ? 0 : 1;
      double deltaPhase = dcPhase - prevDcPhase;
      if (smoothPeriod != 0.0
          && deltaPhase > 0.67 * 360.0 / smoothPeriod
          && deltaPhase < 1.5 * 360.0 / smoothPeriod)
      {
        trend = 0;
      }

      if (trendline != 0.0 && Math.Abs((sp[spIdx] - trendline) / trendline) >= 0.015)
      {
        trend = 1;
      }

      if (bar.Today >= Lookback)
      {
        result[bar.Today] = trend;
      }

      spIdx = spIdx == 49 ? 0 : spIdx + 1;
      prevDcPhase = dcPhase;
      prevSine = sine;
      prevLeadSine = leadSine;
      t3 = t2;
      t2 = t1;
      t1 = avg;
    }

    return result;
  }

  // ------------------------------------------------------------------
  // The shared MESA core: the 4-bar WMA pre-smoother plus the odd/even
  // Hilbert transformers, advanced one bar at a time. The arithmetic is
  // exactly the C reference's (same multiplication order, same smoothing
  // constants), so the bars stay identical value for value; the per-bar
  // payload carries every intermediate the cycle indicators (including
  // MAMA) read. The warmup length selects the TA-Lib family: 37 bars for
  // the 63-lookback indicators, 12 for the 32-lookback ones.
  // ------------------------------------------------------------------

  /// <summary>One bar of the MESA core stream.</summary>
  internal readonly record struct CoreBar(
      int Today,
      double Price,
      double Smoothed,
      double Q1,
      double I1Prev3,
      int DcpInt,
      double SmoothPeriod);

  /// <summary>The mutable MESA core state behind every cycle indicator.</summary>
  internal sealed class CoreState
  {
    private readonly double[] prices;

    // WMA pre-smoother: sliding plain and weighted sums.
    private double sub;
    private double sum;
    private double trailing;
    private int trailingIdx;

    // The ring index shared by all odd/even transformer stages (3 slots).
    private int hilbertIdx;

    // The eight transformer stages: detrender, Q1, jI and jQ, each on its
    // own odd and even circular buffer.
    private readonly Stage detrenderOdd = new();
    private readonly Stage detrenderEven = new();
    private readonly Stage q1Odd = new();
    private readonly Stage q1Even = new();
    private readonly Stage jiOdd = new();
    private readonly Stage jiEven = new();
    private readonly Stage jqOdd = new();
    private readonly Stage jqEven = new();

    // Complex phasor smoothing and the dominant cycle period.
    private double prevQ2;
    private double prevI2;
    private double re;
    private double im;
    private double period;
    private double smoothPeriod;

    // I1 delay lines: the detrender delayed three bars, one two-deep
    // delay line per parity handing its values to the other.
    private double i1EvenPrev3;
    private double i1EvenPrev2;
    private double i1OddPrev3;
    private double i1OddPrev2;

    private int today;
    private int last;

    /// <summary>Warms the core up from the first <paramref name="warmup"/> prices (12 or 37).</summary>
    /// <param name="prices">Prices, oldest first; at least 64 of them.</param>
    /// <param name="warmup">The number of warmup bars: 37 or 12.</param>
    internal CoreState(double[] prices, int warmup)
    {
      this.prices = prices;
      today = warmup;
      last = prices.Length - 1;
      WarmUp(warmup - 3);
    }

    /// <summary>Advances the core by one bar; returns false once the stream is exhausted.</summary>
    /// <param name="bar">The advanced bar's payload.</param>
    /// <returns>Whether a bar was produced.</returns>
    internal bool Next(out CoreBar bar)
    {
      if (today > last)
      {
        bar = default;
        return false;
      }

      double price = prices[today];
      double adjustedPrevPeriod = 0.075 * period + 0.54;

      // 4-bar WMA pre-smoother update.
      sub = sub + price - trailing;
      double sum0 = sum + price * 4.0;
      trailing = prices[trailingIdx];
      double smoothed = sum0 * 0.1;
      sum = sum0 - sub;
      trailingIdx++;

      (double detrender, double q1, double i1Prev3, double q2, double i2) =
          HilbertBar(today, smoothed, adjustedPrevPeriod);

      // Dominant cycle period from the smoothed complex phasor.
      double nextRe = 0.8 * re + 0.2 * (i2 * prevI2 + q2 * prevQ2);
      double nextIm = 0.8 * im + 0.2 * (i2 * prevQ2 - q2 * prevI2);

      double prevPeriod = period;
      double nextPeriod = nextIm != 0.0 && nextRe != 0.0
          ? 360.0 / (Math.Atan(nextIm / nextRe) * Rad2Deg)
          : prevPeriod;
      nextPeriod = Math.Min(nextPeriod, 1.5 * prevPeriod);
      nextPeriod = Math.Max(nextPeriod, 0.67 * prevPeriod);
      nextPeriod = Math.Max(nextPeriod, 6);
      nextPeriod = Math.Min(nextPeriod, 50);
      nextPeriod = nextPeriod * 0.2 + prevPeriod * 0.8;

      double nextSmoothPeriod = 0.67 * smoothPeriod + 0.33 * nextPeriod;

      re = nextRe;
      im = nextIm;
      prevQ2 = q2;
      prevI2 = i2;
      period = nextPeriod;
      smoothPeriod = nextSmoothPeriod;

      bar = new CoreBar(
          today,
          price,
          smoothed,
          q1,
          i1Prev3,
          (int)Math.Truncate(nextSmoothPeriod + 0.5),
          nextSmoothPeriod);
      today++;
      return true;
    }

    // The warmup: 3 unrolled plus `extra` rolled WMA steps starting at
    // index 0 (extra = 34 leaves `today` at 37; 9 leaves it at 12).
    private void WarmUp(int extra)
    {
      double p0 = prices[0];
      double p1 = prices[1];
      double p2 = prices[2];

      sub = p0 + p1 + p2;
      sum = p0 + p1 * 2 + p2 * 3;
      trailing = 0.0;
      for (int idx = 0; idx < extra; idx++)
      {
        double value = prices[idx + 3];
        sub = sub + value - trailing;
        sum = sum + value * 4.0;
        trailing = prices[trailingIdx];
        sum = sum - sub;
        trailingIdx = idx + 1;
      }
    }

    // The odd/even Hilbert transformers for one bar: detrender, Q1, jI and
    // jQ on the parity's circular buffers, then Q2/I2 and the I1 delays.
    private (double Detrender, double Q1, double I1Prev3, double Q2, double I2) HilbertBar(
        int today,
        double smoothed,
        double adjustedPrevPeriod)
    {
      bool even = today % 2 == 0;

      double detrender = Transformer(even ? detrenderEven : detrenderOdd, smoothed, adjustedPrevPeriod);
      double q1 = Transformer(even ? q1Even : q1Odd, detrender, adjustedPrevPeriod);
      double i1Prev3 = even ? i1EvenPrev3 : i1OddPrev3;
      double ji = Transformer(even ? jiEven : jiOdd, i1Prev3, adjustedPrevPeriod);
      double jq = Transformer(even ? jqEven : jqOdd, q1, adjustedPrevPeriod);

      hilbertIdx = hilbertIdx == 2 ? 0 : hilbertIdx + 1;

      double q2 = 0.2 * (q1 + ji) + 0.8 * prevQ2;
      double i2 = 0.2 * (i1Prev3 - jq) + 0.8 * prevI2;

      // I1 is the detrender delayed by three bars; each parity keeps its
      // own two-deep delay line and hands the value over on the next bar
      // of the opposite parity.
      if (even)
      {
        i1OddPrev3 = i1OddPrev2;
        i1OddPrev2 = detrender;
      }
      else
      {
        i1EvenPrev3 = i1EvenPrev2;
        i1EvenPrev2 = detrender;
      }

      return (detrender, q1, i1Prev3, q2, i2);
    }

    // One Hilbert-transformer stage on the odd or even circular buffer,
    // exactly as the C reference computes it.
    private double Transformer(Stage stage, double input, double adjustedPrevPeriod)
    {
      double[] buffer = stage.Buffer;

      double temp = A * input;
      double value = -buffer[hilbertIdx];
      buffer[hilbertIdx] = temp;
      value = value + temp;
      value = value - stage.Prev;
      double newPrev = B * stage.Input;
      value = value + newPrev;
      value = value * adjustedPrevPeriod;
      stage.Prev = newPrev;
      stage.Input = input;
      return value;
    }
  }

  /// <summary>One odd/even Hilbert-transformer stage: a 3-slot circular buffer plus its previous output-input pair.</summary>
  internal sealed class Stage
  {
    internal double[] Buffer { get; } = new double[3];

    internal double Prev { get; set; }

    internal double Input { get; set; }
  }

  // Average of the raw price over the dominant cycle, ending at `today`
  // (0.0 while the period rounds down to nothing, as in the C code).
  private static double RawAverage(double[] prices, int today, int dcpInt)
  {
    if (dcpInt < 1)
    {
      return 0.0;
    }

    double total = 0.0;
    for (int i = 0; i < dcpInt; i++)
    {
      total += prices[today - i];
    }

    return total / dcpInt;
  }

  // The phase (in degrees) of the dominant cycle over the smooth-price
  // ring, walking back from `spIdx` over `dcpInt` bars.
  private static double DominantCyclePhase(double[] sp, int spIdx, double smoothPeriod, double dcPhase, int dcpInt)
  {
    double realPart = 0.0;
    double imagPart = 0.0;
    if (dcpInt >= 1)
    {
      int idx = spIdx;
      for (int i = 0; i < dcpInt; i++)
      {
        double angle = i * Deg2RadBy360 / dcpInt;
        double value = sp[idx];
        realPart = realPart + Math.Sin(angle) * value;
        imagPart = imagPart + Math.Cos(angle) * value;
        idx = idx == 0 ? 49 : idx - 1;
      }
    }

    if (Math.Abs(imagPart) > 0)
    {
      dcPhase = Math.Atan(realPart / imagPart) * Rad2Deg;
    }
    else
    {
      if (realPart < 0)
      {
        dcPhase = dcPhase - 90;
      }
      else if (realPart > 0)
      {
        dcPhase = dcPhase + 90;
      }
    }

    dcPhase = dcPhase + 90 + 360 / smoothPeriod;
    if (imagPart < 0)
    {
      dcPhase = dcPhase + 180;
    }

    return dcPhase > 315 ? dcPhase - 360 : dcPhase;
  }
}

namespace Talib.Indicators;

/// <summary>
/// Candlestick pattern recognition: all 61 TA-Lib CDL functions, scored
/// per bar (+100 bullish / -100 bearish / 0 none, with TA-Lib's ±200
/// hikkake confirmations and ±80 equal-touch levels), plus
/// <see cref="Scan"/> for scoring many patterns over one shared candle
/// context.
///
/// Bars are always given in chronological order, oldest first. The
/// candle settings judge each bar against the 10 bars before it (the
/// trailing average windows); pattern windows evaluate only from the
/// pattern's lookback bar on, zero-padded before that.
/// </summary>
public static partial class Cdl
{
  /// <summary>
  /// A prebuilt candle context: pass it to any pattern (or
  /// <see cref="Scan"/>) to compute the trailing sums once for a whole
  /// set of patterns.
  /// </summary>
  public sealed class Ctx
  {
    internal Ctx(Bar[] raw, CtxBar[] bars)
    {
      Raw = raw;
      Bars = bars;
    }

    internal Bar[] Raw { get; }

    internal CtxBar[] Bars { get; }
  }

  // The per-bar candle fields plus the trailing average-window sums the
  // candle settings read.
  internal sealed class CtxBar
  {
    internal double O = 0, H = 0, L = 0, C = 0;
    internal double Body = 0, Up = 0, Lo = 0;
    internal double Body10 = 0, Hl10 = 0, Shadow10 = 0, Hl5 = 0;
  }

  // Pattern dispatch: resolved by name through the (Ctx) overloads and
  // cached, so pattern files need no registration.
  private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Func<Ctx, int[]>> Dispatch = new();

  private static Func<Ctx, int[]> Resolve(string name) =>
      Dispatch.GetOrAdd(name, n =>
      {
        var method = typeof(Cdl).GetMethod(
            n,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            new[] { typeof(Ctx) },
            modifiers: null)
            ?? throw new ArgumentException($"unknown CDL pattern: {n}", nameof(name));
        return (Func<Ctx, int[]>)method.CreateDelegate(typeof(Func<Ctx, int[]>));
      });

  /// <summary>All 61 pattern names, in TA-Lib order.</summary>
  public static readonly string[] PatternNames =
  {
      "TwoCrows", "ThreeBlackCrows", "ThreeInside", "ThreeLineStrike",
      "ThreeOutside", "ThreeStarsInSouth", "ThreeWhiteSoldiers",
      "AbandonedBaby", "AdvanceBlock", "BeltHold", "Breakaway",
      "ClosingMarubozu", "ConcealBabysWall", "Counterattack",
      "DarkCloudCover", "Doji", "DojiStar", "DragonflyDoji", "Engulfing",
      "EveningDojiStar", "EveningStar", "GapSideSideWhite",
      "GravestoneDoji", "Hammer", "HangingMan", "Harami", "HaramiCross",
      "HighWave", "Hikkake", "HikkakeMod", "HomingPigeon",
      "Identical3Crows", "InNeck", "InvertedHammer", "Kicking",
      "KickingByLength", "LadderBottom", "LongLeggedDoji", "LongLine",
      "Marubozu", "MatchingLow", "MatHold", "MorningDojiStar",
      "MorningStar", "OnNeck", "Piercing", "RickshawMan",
      "RiseFall3Methods", "SeparatingLines", "ShootingStar", "ShortLine",
      "SpinningTop", "StalledPattern", "StickSandwich", "Takuri",
      "TasukiGap", "Thrusting", "Tristar", "Unique3River",
      "UpsideGap2Crows", "XsideGap3Methods",
  };

  /// <summary>
  /// Builds the candle context of <paramref name="bars"/> once, for
  /// reuse across several pattern calls (each pattern called with plain
  /// bars rebuilds it).
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The shared candle context.</returns>
  public static Ctx Context(Bar[] bars) => new(bars, BuildContext(bars));

  /// <summary>
  /// Scores several patterns over one shared context — equivalent to
  /// calling each pattern on <paramref name="bars"/>, without rebuilding
  /// the trailing sums per pattern. With no pattern list, scores all 61.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <param name="patterns">Pattern names to score (from <see cref="PatternNames"/>); all when null.</param>
  /// <returns>A map from pattern name to its score series.</returns>
  public static Dictionary<string, int[]> Scan(Bar[] bars, string[]? patterns = null)
  {
    var ctx = Context(bars);
    var result = new Dictionary<string, int[]>((patterns ?? PatternNames).Length);
    foreach (string name in patterns ?? PatternNames)
    {
      result[name] = Resolve(name)(ctx);
    }

    return result;
  }

  internal static CtxBar[] Wrap(Ctx ctx) => ctx.Bars;

  internal static CtxBar[] Wrap(Bar[] bars) => BuildContext(bars);

  internal static Bar[] RawOf(Ctx ctx) => ctx.Raw;

  internal static Bar[] RawOf(Bar[] bars) => bars;

  private static CtxBar[] BuildContext(Bar[] bars)
  {
    var ctx = new CtxBar[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      Bar bar = bars[i];
      ctx[i] = new CtxBar
      {
        O = bar.Open,
        H = bar.High,
        L = bar.Low,
        C = bar.Close,
        Body = Math.Abs(bar.Close - bar.Open),
        Up = bar.High - Math.Max(bar.Open, bar.Close),
        Lo = Math.Min(bar.Open, bar.Close) - bar.Low,
      };
    }

    double[] body10 = Sums(ctx, 10, b => b.Body);
    double[] hl10 = Sums(ctx, 10, b => b.H - b.L);
    double[] shadow10 = Sums(ctx, 10, b => b.Up + b.Lo);
    double[] hl5 = Sums(ctx, 5, b => b.H - b.L);

    for (int i = 0; i < ctx.Length; i++)
    {
      ctx[i].Body10 = body10[i];
      ctx[i].Hl10 = hl10[i];
      ctx[i].Shadow10 = shadow10[i];
      ctx[i].Hl5 = hl5[i];
    }

    return ctx;
  }

  // sums[k] = the values summed over the n bars before k (0.0 for the
  // first n bars — no pattern reads them, its lookback covers the
  // warmup); after the fill the window slides by adding the entering
  // value and subtracting the leaving one.
  private static double[] Sums(CtxBar[] ctx, int n, Func<CtxBar, double> value)
  {
    var values = new double[ctx.Length];
    for (int i = 0; i < ctx.Length; i++)
    {
      values[i] = value(ctx[i]);
    }

    var result = new double[ctx.Length];
    if (ctx.Length <= n)
    {
      return result;
    }

    double seed = 0;
    for (int i = 0; i < n; i++)
    {
      seed += values[i];
    }

    result[n] = seed;
    for (int k = n + 1; k < values.Length; k++)
    {
      result[k] = result[k - 1] + (values[k] - values[k - n]);
    }

    return result;
  }

  // ------------------------------------------------------------------
  // Candle settings, each the TA_CANDLEAVERAGE of its row in TA-Lib's
  // default table.
  // ------------------------------------------------------------------

  internal static bool White(CtxBar b) => b.C >= b.O;

  internal static bool Black(CtxBar b) => b.C < b.O;

  internal static double MaxB(CtxBar b) => Math.Max(b.O, b.C);

  internal static double MinB(CtxBar b) => Math.Min(b.O, b.C);

  internal static bool BodyLong(CtxBar b) => b.Body > b.Body10 / 10;

  internal static bool BodyShort(CtxBar b) => b.Body < b.Body10 / 10;

  internal static bool DojiBody(CtxBar b) => b.Body <= 0.1 * (b.Hl10 / 10);

  internal static double ShadowLong(CtxBar b) => 1.0 * b.Body;

  internal static double ShadowVeryLong(CtxBar b) => 2.0 * b.Body;

  internal static double ShadowShort(CtxBar b) => b.Shadow10 / 10 / 2.0;

  internal static double ShadowVeryShort(CtxBar b) => 0.1 * (b.Hl10 / 10);

  internal static double Near(CtxBar b) => 0.2 * (b.Hl5 / 5);

  internal static double Far(CtxBar b) => 0.6 * (b.Hl5 / 5);

  internal static double EqualTol(CtxBar b) => 0.05 * (b.Hl5 / 5);

  internal static bool GapUp(CtxBar b, CtxBar prev) => MinB(b) > MaxB(prev);

  internal static bool GapDown(CtxBar b, CtxBar prev) => MaxB(b) < MinB(prev);

  // ------------------------------------------------------------------
  // Scoring: windows of `window` context bars, evaluated only from the
  // pattern's lookback bar on (the C library outputs nothing before
  // it), zero-padded before that. The eval callback receives the window
  // oldest-first.
  // ------------------------------------------------------------------

  internal static int[] Score(CtxBar[] ctx, int lookback, int window, Func<CtxBar[], int> eval)
  {
    var result = new int[ctx.Length];
    for (int k = Math.Max(lookback, window - 1); k < ctx.Length; k++)
    {
      var w = new CtxBar[window];
      Array.Copy(ctx, k - window + 1, w, 0, window);
      result[k] = eval(w);
    }

    return result;
  }

  /// <summary>
  /// Two Crows: two black candles gapping up after a long white one, the
  /// third closing inside the first body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] TwoCrows(Bar[] bars) => TwoCrows(Context(bars));

  /// <summary>Two Crows over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] TwoCrows(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return White(a) && BodyLong(a) && Black(b) && GapUp(b, a) && Black(c)
            && c.O < b.O && c.O > b.C && c.C > a.O && c.C < a.C
            ? -100
            : 0;
      });

  /// <summary>
  /// Three Black Crows: three long black candles opening inside the prior
  /// body, closing successively lower with very short lower shadows.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeBlackCrows(Bar[] bars) => ThreeBlackCrows(Context(bars));

  /// <summary>Three Black Crows over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeBlackCrows(Ctx ctx) =>
      Score(Wrap(ctx), 13, 4, w =>
      {
        var (a, b, c, d) = (w[0], w[1], w[2], w[3]);
        return White(a) && Black(b) && Black(c) && Black(d)
            && b.O < a.O && b.O > a.C && c.O < b.O && c.O > b.C
            && a.H > b.C && b.C > c.C && c.C > d.C
            && c.Lo < ShadowVeryShort(c) && d.Lo < ShadowVeryShort(d) && b.Lo < ShadowVeryShort(b)
            ? -100
            : 0;
      });

  /// <summary>
  /// Three Inside: a long candle, a short candle engulfed by it, then a
  /// candle closing out in the opposite direction.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeInside(Bar[] bars) => ThreeInside(Context(bars));

  /// <summary>Three Inside over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeInside(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool opposite = (White(a) && Black(c) && c.C < a.O) || (Black(a) && White(c) && c.C > a.O);
        return MaxB(b) < MaxB(a) && MinB(b) > MinB(a) && opposite && BodyLong(a) && BodyShort(b)
            ? (White(a) ? -100 : 100)
            : 0;
      });

  /// <summary>
  /// Three Line Strike: three same-color candles each opening near the
  /// prior body, undone by a fourth that opens beyond and closes across
  /// all three.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeLineStrike(Bar[] bars) => ThreeLineStrike(Context(bars));

  /// <summary>Three Line Strike over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeLineStrike(Ctx ctx) =>
      Score(Wrap(ctx), 8, 4, w =>
      {
        var (a, b, c, d) = (w[0], w[1], w[2], w[3]);
        bool same = White(a) == White(b) && White(b) == White(c) && White(d) != White(c);
        bool direction = White(c)
            ? c.C > b.C && b.C > a.C && d.O > c.C && d.C < a.O
            : c.C < b.C && b.C < a.C && d.O < c.C && d.C > a.O;
        return same && b.O >= MinB(a) - Near(a) && b.O <= MaxB(a) + Near(a)
            && c.O >= MinB(b) - Near(b) && c.O <= MaxB(b) + Near(b) && direction
            ? (White(c) ? 100 : -100)
            : 0;
      });
}

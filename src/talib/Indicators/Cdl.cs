using System.Numerics;

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
  // candle settings read. A struct, so a context is one contiguous
  // allocation and the windows copy by value.
  internal struct CtxBar
  {
    internal double O, H, L, C;
    internal double Body, Up, Lo;
    internal double Body10, Hl10, Shadow10, Hl5;
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

    // Pass 1: the per-bar candle fields. Every operation is
    // element-wise, so the vector lanes compute exactly the scalar
    // expression per element — bit-identical by construction.
    int width = Vector<double>.Count;
    Span<double> open = stackalloc double[width];
    Span<double> high = stackalloc double[width];
    Span<double> low = stackalloc double[width];
    Span<double> close = stackalloc double[width];
    Span<double> body = stackalloc double[width];
    Span<double> up = stackalloc double[width];
    Span<double> lo = stackalloc double[width];
    int i = 0;
    for (; i + width <= bars.Length; i += width)
    {
      for (int j = 0; j < width; j++)
      {
        Bar bar = bars[i + j];
        open[j] = bar.Open;
        high[j] = bar.High;
        low[j] = bar.Low;
        close[j] = bar.Close;
      }

      var bodies = Vector.Abs(new Vector<double>(close) - new Vector<double>(open));
      var ups = new Vector<double>(high) - Vector.Max(new Vector<double>(open), new Vector<double>(close));
      var los = Vector.Min(new Vector<double>(open), new Vector<double>(close)) - new Vector<double>(low);
      bodies.CopyTo(body);
      ups.CopyTo(up);
      los.CopyTo(lo);
      for (int j = 0; j < width; j++)
      {
        ctx[i + j] = new CtxBar
        {
          O = open[j],
          H = high[j],
          L = low[j],
          C = close[j],
          Body = body[j],
          Up = up[j],
          Lo = lo[j],
        };
      }
    }

    for (; i < bars.Length; i++)
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

    // Pass 2: the four trailing sums in one walk. Each running sum
    // keeps the exact arithmetic of the standalone walk it replaces
    // (a seed summed left to right, then `previous + (entering -
    // leaving)` per bar), so the values are bit-identical; only the
    // passes are fused — one walk over the context instead of four
    // walks each allocating an intermediate values array.
    int n = ctx.Length;
    if (n > 5)
    {
      double hl5 = 0;
      for (int k = 0; k < 5; k++)
      {
        hl5 += ctx[k].H - ctx[k].L;
      }

      ctx[5].Hl5 = hl5;
      for (int k = 6; k < Math.Min(11, n); k++)
      {
        hl5 += (ctx[k - 1].H - ctx[k - 1].L) - (ctx[k - 6].H - ctx[k - 6].L);
        ctx[k].Hl5 = hl5;
      }

      if (n > 10)
      {
        double body10 = 0, hl10 = 0, shadow10 = 0;
        for (int k = 0; k < 10; k++)
        {
          body10 += ctx[k].Body;
          hl10 += ctx[k].H - ctx[k].L;
          shadow10 += ctx[k].Up + ctx[k].Lo;
        }

        ctx[10].Body10 = body10;
        ctx[10].Hl10 = hl10;
        ctx[10].Shadow10 = shadow10;

        for (int k = 11; k < n; k++)
        {
          CtxBar entering = ctx[k - 1];
          CtxBar leaving = ctx[k - 11];
          body10 += entering.Body - leaving.Body;
          hl10 += (entering.H - entering.L) - (leaving.H - leaving.L);
          shadow10 += (entering.Up + entering.Lo) - (leaving.Up + leaving.Lo);
          ctx[k].Body10 = body10;
          ctx[k].Hl10 = hl10;
          ctx[k].Shadow10 = shadow10;

          hl5 += (entering.H - entering.L) - (ctx[k - 6].H - ctx[k - 6].L);
          ctx[k].Hl5 = hl5;
        }
      }
    }

    return ctx;
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

    // One scratch window, refilled per bar: the eval callback sees the
    // window's bars oldest-first, copied by value — no per-bar
    // allocation.
    var w = new CtxBar[window];
    for (int k = Math.Max(lookback, window - 1); k < ctx.Length; k++)
    {
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

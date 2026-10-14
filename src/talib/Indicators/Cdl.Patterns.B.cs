namespace Talib.Indicators;

public static partial class Cdl
{
  /// <summary>
  /// Identical Three Crows: three black candles with very short lower
  /// shadows, each opening at the prior close and closing lower.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Identical3Crows(Bar[] bars) => Identical3Crows(Context(bars));

  /// <summary>Identical Three Crows over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Identical3Crows(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return Black(a) && a.Lo < ShadowVeryShort(a)
            && Black(b) && b.Lo < ShadowVeryShort(b)
            && Black(c) && c.Lo < ShadowVeryShort(c)
            && a.C > b.C && b.C > c.C
            && b.O <= a.C + EqualTol(a) && b.O >= a.C - EqualTol(a)
            && c.O <= b.C + EqualTol(b) && c.O >= b.C - EqualTol(b)
            ? -100
            : 0;
      });

  /// <summary>
  /// In Neck: a long black, then a white opening below its low and
  /// closing marginally into its body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] InNeck(Bar[] bars) => InNeck(Context(bars));

  /// <summary>In Neck over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] InNeck(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return Black(a) && BodyLong(a) && White(b) && b.O < a.L
            && b.C <= a.C + EqualTol(a) && b.C >= a.C
            ? -100
            : 0;
      });

  /// <summary>
  /// Inverted Hammer: after a gap down, a small body with a long upper
  /// shadow and a very short lower shadow.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] InvertedHammer(Bar[] bars) => InvertedHammer(Context(bars));

  /// <summary>Inverted Hammer over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] InvertedHammer(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return GapDown(b, a) && BodyShort(b) && b.Up > ShadowLong(b)
            && b.Lo < ShadowVeryShort(b)
            ? 100
            : 0;
      });

  /// <summary>
  /// Kicking: two opposite marubozu candles separated by a body gap.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Kicking(Bar[] bars) => Kicking(Context(bars));

  /// <summary>Kicking over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Kicking(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        bool IsMarubozu(CtxBar x) => BodyLong(x) && x.Up < ShadowVeryShort(x) && x.Lo < ShadowVeryShort(x);
        bool gapped = (Black(a) && b.L > a.H) || (White(a) && b.H < a.L);
        return White(a) != White(b) && IsMarubozu(a) && IsMarubozu(b) && gapped
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Kicking (determined by length): as <see cref="Kicking(Bar[])"/>,
  /// scored by the color of the longer body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] KickingByLength(Bar[] bars) => KickingByLength(Context(bars));

  /// <summary>Kicking (determined by length) over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] KickingByLength(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        bool IsMarubozu(CtxBar x) => BodyLong(x) && x.Up < ShadowVeryShort(x) && x.Lo < ShadowVeryShort(x);
        bool gapped = (Black(a) && b.L > a.H) || (White(a) && b.H < a.L);
        var winner = b.Body > a.Body ? b : a;
        return White(a) != White(b) && IsMarubozu(a) && IsMarubozu(b) && gapped
            ? (White(winner) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Ladder Bottom: three blacks with lower opens and closes, a black
  /// with an upper shadow, then a white closing above its high.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] LadderBottom(Bar[] bars) => LadderBottom(Context(bars));

  /// <summary>Ladder Bottom over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] LadderBottom(Ctx ctx) =>
      Score(Wrap(ctx), 14, 5, w =>
      {
        var (a, b, c, d, e) = (w[0], w[1], w[2], w[3], w[4]);
        return Black(a) && Black(b) && Black(c)
            && a.O > b.O && b.O > c.O && a.C > b.C && b.C > c.C
            && Black(d) && d.Up > ShadowVeryShort(d)
            && White(e) && e.O > d.O && e.C > d.H
            ? 100
            : 0;
      });

  /// <summary>
  /// Long Legged Doji: a doji with at least one shadow longer than its
  /// own body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] LongLeggedDoji(Bar[] bars) => LongLeggedDoji(Context(bars));

  /// <summary>Long Legged Doji over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] LongLeggedDoji(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return DojiBody(b) && (b.Lo > ShadowLong(b) || b.Up > ShadowLong(b))
            ? 100
            : 0;
      });

  /// <summary>
  /// Long Line: a long body with short shadows on both ends.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] LongLine(Bar[] bars) => LongLine(Context(bars));

  /// <summary>Long Line over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] LongLine(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return BodyLong(b) && b.Up < ShadowShort(b) && b.Lo < ShadowShort(b)
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Marubozu: a long body with very short shadows on both ends.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Marubozu(Bar[] bars) => Marubozu(Context(bars));

  /// <summary>Marubozu over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Marubozu(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return BodyLong(b) && b.Up < ShadowVeryShort(b) && b.Lo < ShadowVeryShort(b)
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Matching Low: two black candles closing at the same level.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MatchingLow(Bar[] bars) => MatchingLow(Context(bars));

  /// <summary>Matching Low over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MatchingLow(Ctx ctx) =>
      Score(Wrap(ctx), 6, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return Black(a) && Black(b)
            && b.C <= a.C + EqualTol(a) && b.C >= a.C - EqualTol(a)
            ? 100
            : 0;
      });

  /// <summary>
  /// Mat Hold: a long white, a gap and three small drifting candles
  /// holding within it, then a white closing above the reaction highs.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MatHold(Bar[] bars) => MatHold(Context(bars));

  /// <summary>Mat Hold over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MatHold(Ctx ctx) =>
      Score(Wrap(ctx), 14, 5, w =>
      {
        var (a, b, c, d, e) = (w[0], w[1], w[2], w[3], w[4]);
        return White(a) && Black(b) && White(e) && GapUp(b, a)
            && MinB(c) < a.C && MinB(d) < a.C
            && MinB(c) > a.C - a.Body * 0.5
            && MinB(d) > a.C - a.Body * 0.5
            && MaxB(c) < b.O && MaxB(d) < MaxB(c)
            && e.O > d.C && e.C > Math.Max(Math.Max(b.H, c.H), d.H)
            && BodyLong(a) && BodyShort(b) && BodyShort(c) && BodyShort(d)
            ? 100
            : 0;
      });

  /// <summary>
  /// Morning Doji Star: a long black, a doji gapping down, then a white
  /// closing well within the first body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MorningDojiStar(Bar[] bars) => MorningDojiStar(Context(bars));

  /// <summary>Morning Doji Star over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MorningDojiStar(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return Black(a) && White(c) && GapDown(b, a)
            && c.C > a.Body * 0.3 + a.C
            && BodyLong(a) && DojiBody(b) && !BodyShort(c)
            ? 100
            : 0;
      });

  /// <summary>
  /// Morning Star: a long black, a short candle gapping down, then a
  /// white closing well within the first body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MorningStar(Bar[] bars) => MorningStar(Context(bars));

  /// <summary>Morning Star over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] MorningStar(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return Black(a) && White(c) && GapDown(b, a)
            && c.C > a.Body * 0.3 + a.C
            && BodyLong(a) && BodyShort(b) && !BodyShort(c)
            ? 100
            : 0;
      });

  /// <summary>
  /// On Neck: a long black, then a white opening below its low and
  /// closing at its low.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] OnNeck(Bar[] bars) => OnNeck(Context(bars));

  /// <summary>On Neck over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] OnNeck(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return Black(a) && BodyLong(a) && White(b) && b.O < a.L
            && b.C <= a.L + EqualTol(a) && b.C >= a.L - EqualTol(a)
            ? -100
            : 0;
      });

  /// <summary>
  /// Piercing Line: a long black, then a white opening below its low and
  /// closing above the midpoint of its body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Piercing(Bar[] bars) => Piercing(Context(bars));

  /// <summary>Piercing Line over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Piercing(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return Black(a) && BodyLong(a) && White(b) && BodyLong(b)
            && b.O < a.L && b.C < a.O && b.C > a.Body * 0.5 + a.C
            ? 100
            : 0;
      });

  /// <summary>
  /// Rickshaw Man: a doji with long shadows on both sides and the body
  /// near the midpoint of the range.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] RickshawMan(Bar[] bars) => RickshawMan(Context(bars));

  /// <summary>Rickshaw Man over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] RickshawMan(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        double midpoint = b.L + (b.H - b.L) / 2;
        return DojiBody(b) && b.Lo > ShadowLong(b) && b.Up > ShadowLong(b)
            && MinB(b) <= midpoint + Near(b) && MaxB(b) >= midpoint - Near(b)
            ? 100
            : 0;
      });

  /// <summary>
  /// Rising/Falling Three Methods: a long candle, three small bodies
  /// drifting within it, then a long candle closing the move.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] RiseFall3Methods(Bar[] bars) => RiseFall3Methods(Context(bars));

  /// <summary>Rising/Falling Three Methods over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] RiseFall3Methods(Ctx ctx) =>
      Score(Wrap(ctx), 14, 5, w =>
      {
        var (a, b, c, d, e) = (w[0], w[1], w[2], w[3], w[4]);
        bool colors = White(a) != White(b) && White(b) == White(c) && White(c) == White(d)
            && White(d) != White(e);
        bool hold = (MinB(b) < a.H && MaxB(b) > a.L)
            && (MinB(c) < a.H && MaxB(c) > a.L)
            && (MinB(d) < a.H && MaxB(d) > a.L);
        bool drift = White(a)
            ? c.C < b.C && d.C < c.C && e.O > d.C && e.C > a.C
            : c.C > b.C && d.C > c.C && e.O < d.C && e.C < a.C;
        return colors && hold && drift
            && BodyLong(a) && BodyShort(b) && BodyShort(c) && BodyShort(d) && BodyLong(e)
            ? (White(a) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Separating Lines: two opposite candles opening at the same level,
  /// the second a belt-hold candle.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] SeparatingLines(Bar[] bars) => SeparatingLines(Context(bars));

  /// <summary>Separating Lines over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] SeparatingLines(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        bool belt = (White(b) && b.Lo < ShadowVeryShort(b)) || (Black(b) && b.Up < ShadowVeryShort(b));
        return White(a) != White(b)
            && b.O <= a.O + EqualTol(a) && b.O >= a.O - EqualTol(a)
            && BodyLong(b) && belt
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Shooting Star: after a body gap up, a small body with a long upper
  /// shadow and a very short lower shadow.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ShootingStar(Bar[] bars) => ShootingStar(Context(bars));

  /// <summary>Shooting Star over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ShootingStar(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return GapUp(b, a) && BodyShort(b) && b.Up > ShadowLong(b)
            && b.Lo < ShadowVeryShort(b)
            ? -100
            : 0;
      });

  /// <summary>
  /// Short Line: a short body with short shadows on both ends.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ShortLine(Bar[] bars) => ShortLine(Context(bars));

  /// <summary>Short Line over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ShortLine(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return BodyShort(b) && b.Up < ShadowShort(b) && b.Lo < ShadowShort(b)
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Spinning Top: a short body with both shadows longer than the body
  /// itself.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] SpinningTop(Bar[] bars) => SpinningTop(Context(bars));

  /// <summary>Spinning Top over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] SpinningTop(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return b.Up > b.Body && b.Lo > b.Body && BodyShort(b)
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Stalled Pattern: two long whites advancing, then a small body
  /// riding on the second candle's shoulder.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] StalledPattern(Bar[] bars) => StalledPattern(Context(bars));

  /// <summary>Stalled Pattern over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] StalledPattern(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return White(a) && White(b) && White(c)
            && c.C > b.C && b.C > a.C
            && BodyLong(a) && BodyLong(b)
            && b.Up < ShadowVeryShort(b)
            && b.O > a.O && b.O <= a.C + Near(a)
            && BodyShort(c)
            && c.O >= b.C - c.Body - Near(b)
            ? -100
            : 0;
      });

  /// <summary>
  /// Stick Sandwich: two blacks around a white whose low holds above the
  /// first close, the closes matching.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] StickSandwich(Bar[] bars) => StickSandwich(Context(bars));

  /// <summary>Stick Sandwich over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] StickSandwich(Ctx ctx) =>
      Score(Wrap(ctx), 7, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return Black(a) && White(b) && Black(c) && b.L > a.C
            && c.C <= a.C + EqualTol(a) && c.C >= a.C - EqualTol(a)
            ? 100
            : 0;
      });

  /// <summary>
  /// Takuri: a doji with a very short upper shadow and a lower shadow
  /// longer than twice the body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Takuri(Bar[] bars) => Takuri(Context(bars));

  /// <summary>Takuri over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Takuri(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return DojiBody(b) && b.Up < ShadowVeryShort(b) && b.Lo > ShadowVeryLong(b)
            ? 100
            : 0;
      });

  /// <summary>
  /// Tasuki Gap: a gap then a candle opening inside the gap-closing
  /// direction with a near-equal body, closing inside the gap.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] TasukiGap(Bar[] bars) => TasukiGap(Context(bars));

  /// <summary>Tasuki Gap over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] TasukiGap(Ctx ctx) =>
      Score(Wrap(ctx), 7, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool up = GapUp(b, a) && White(b) && Black(c)
            && c.O < b.C && c.O > b.O && c.C < b.O && c.C > MaxB(a)
            && Math.Abs(b.Body - c.Body) < Near(b);
        bool down = GapDown(b, a) && Black(b) && White(c)
            && c.O < b.O && c.O > b.C && c.C > b.O && c.C < MinB(a)
            && Math.Abs(b.Body - c.Body) < Near(b);
        return up || down
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Thrusting: a long black, then a white opening below its low and
  /// closing into but under the midpoint of its body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Thrusting(Bar[] bars) => Thrusting(Context(bars));

  /// <summary>Thrusting over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Thrusting(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return Black(a) && BodyLong(a) && White(b) && b.O < a.L
            && b.C > a.C + EqualTol(a) && b.C <= a.Body * 0.5 + a.C
            ? -100
            : 0;
      });

  /// <summary>
  /// Tristar: three dojis, the second gapping away and the third failing
  /// to extend the gap.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Tristar(Bar[] bars) => Tristar(Context(bars));

  /// <summary>Tristar over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Tristar(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return DojiBody(a) && b.Body <= 0.1 * (a.Hl10 / 10) && c.Body <= 0.1 * (a.Hl10 / 10)
            ? (GapUp(b, a) && MaxB(c) < MaxB(b) ? -100
                : GapDown(b, a) && MinB(c) > MinB(b) ? 100
                : 0)
            : 0;
      });

  /// <summary>
  /// Unique Three River: a long black, a black harami with a lower low,
  /// then a small white opening off the low.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Unique3River(Bar[] bars) => Unique3River(Context(bars));

  /// <summary>Unique Three River over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Unique3River(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return Black(a) && Black(b) && White(c)
            && b.C > a.C && b.O <= a.O && b.L < a.L && c.O > b.L
            && BodyLong(a) && BodyShort(c)
            ? 100
            : 0;
      });

  /// <summary>
  /// Upside Gap Two Crows: a long white, then two blacks gapping up with
  /// the third engulfing the second but closing above the first.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] UpsideGap2Crows(Bar[] bars) => UpsideGap2Crows(Context(bars));

  /// <summary>Upside Gap Two Crows over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] UpsideGap2Crows(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return White(a) && BodyLong(a) && Black(b) && BodyShort(b)
            && GapUp(b, a) && Black(c)
            && c.O > b.O && c.C < b.C && c.C > a.C
            ? -100
            : 0;
      });

  /// <summary>
  /// Upside/Downside Gap Three Methods: a gap between two same-color
  /// candles, partially closed by a third of the opposite color.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] XsideGap3Methods(Bar[] bars) => XsideGap3Methods(Context(bars));

  /// <summary>Upside/Downside Gap Three Methods over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] XsideGap3Methods(Ctx ctx) =>
      Score(Wrap(ctx), 2, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool gapped = (White(a) && GapUp(b, a)) || (Black(a) && GapDown(b, a));
        return White(a) == White(b) && White(b) != White(c)
            && c.O < MaxB(b) && c.O > MinB(b)
            && c.C < MaxB(a) && c.C > MinB(a) && gapped
            ? (White(a) ? 100 : -100)
            : 0;
      });
}

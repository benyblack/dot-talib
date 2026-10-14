namespace Talib.Indicators;

public static partial class Cdl
{
  /// <summary>
  /// Three Outside: an engulfing candle followed by a further close in
  /// the engulfing direction.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeOutside(Bar[] bars) => ThreeOutside(Context(bars));

  /// <summary>Three Outside over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeOutside(Ctx ctx) =>
      Score(Wrap(ctx), 3, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool bull = White(b) && Black(a) && b.C > a.O && b.O < a.C && c.C > b.C;
        bool bear = Black(b) && White(a) && b.O > a.C && b.C < a.O && c.C < b.C;
        return bull || bear ? (White(b) ? 100 : -100) : 0;
      });

  /// <summary>
  /// Three Stars in the South: three black candles — long with a long
  /// lower shadow, smaller inside it, then a small marubozu engulfed by
  /// the second.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeStarsInSouth(Bar[] bars) => ThreeStarsInSouth(Context(bars));

  /// <summary>Three Stars in the South over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeStarsInSouth(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return Black(a) && Black(b) && Black(c) && BodyLong(a)
            && a.Lo > ShadowLong(a)
            && b.Body < a.Body && b.O > a.C && b.O <= a.H && b.L < a.C && b.L >= a.L
            && b.Lo > ShadowVeryShort(b)
            && !BodyShort(c) && c.Lo < ShadowVeryShort(c)
            && c.Up < ShadowVeryShort(c) && c.L > b.L && c.H < b.H
            ? 100
            : 0;
      });

  /// <summary>
  /// Three Advancing White Soldiers: three white candles with consecutive
  /// higher closes, each opening near the prior body, none far shorter
  /// than its predecessor.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeWhiteSoldiers(Bar[] bars) => ThreeWhiteSoldiers(Context(bars));

  /// <summary>Three Advancing White Soldiers over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ThreeWhiteSoldiers(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return White(a) && a.Up < ShadowVeryShort(a) && White(b)
            && b.Up < ShadowVeryShort(b) && White(c) && c.Up < ShadowVeryShort(c)
            && c.C > b.C && b.C > a.C
            && b.O > a.O && b.O <= a.C + Near(a)
            && c.O > b.O && c.O <= b.C + Near(b)
            && b.Body > a.Body - Far(a) && c.Body > b.Body - Far(b)
            && !BodyShort(c)
            ? 100
            : 0;
      });

  /// <summary>
  /// Abandoned Baby: a long candle, a doji gapping away from it, and a
  /// third candle gapping beyond the doji to close well within the first
  /// body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] AbandonedBaby(Bar[] bars) => AbandonedBaby(Context(bars));

  /// <summary>Abandoned Baby over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] AbandonedBaby(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool bull = White(a) && Black(c) && c.C < a.C - a.Body * 0.3 && b.L > a.H && c.H < b.L;
        bool bear = Black(a) && White(c) && c.C > a.Body * 0.3 + a.C && b.H < a.L && c.L > b.H;
        return BodyLong(a) && DojiBody(b) && !BodyShort(c) && (bull || bear)
            ? (White(c) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Advancing Block: three white candles with higher closes but stalling
  /// bodies or upper shadows.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] AdvanceBlock(Bar[] bars) => AdvanceBlock(Context(bars));

  /// <summary>Advancing Block over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] AdvanceBlock(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool blocked = (b.Body < a.Body - Far(a) && c.Body < b.Body + Near(b))
            || c.Body < b.Body - Far(b)
            || (c.Body < b.Body && b.Body < a.Body && (c.Up > ShadowShort(c) || b.Up > ShadowShort(b)))
            || (c.Body < b.Body && c.Up > ShadowLong(c));
        return White(a) && White(b) && White(c)
            && c.C > b.C && b.C > a.C
            && b.O > a.O && b.O <= a.C + Near(a)
            && c.O > b.O && c.O <= b.C + Near(b)
            && BodyLong(a) && a.Up < ShadowShort(a) && blocked
            ? -100
            : 0;
      });

  /// <summary>
  /// Belt Hold: a long-body candle with a very short shadow on the
  /// closing end.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] BeltHold(Bar[] bars) => BeltHold(Context(bars));

  /// <summary>Belt Hold over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] BeltHold(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        bool shadowed = (White(b) && b.Lo < ShadowVeryShort(b))
            || (Black(b) && b.Up < ShadowVeryShort(b));
        return BodyLong(b) && shadowed
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Breakaway: a gap and three drifting candles, then a long candle
  /// closing back inside the gap.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Breakaway(Bar[] bars) => Breakaway(Context(bars));

  /// <summary>Breakaway over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Breakaway(Ctx ctx) =>
      Score(Wrap(ctx), 14, 5, w =>
      {
        var (a, b, c, d, e) = (w[0], w[1], w[2], w[3], w[4]);
        bool colors = White(a) == White(b) && White(b) == White(d) && White(e) != White(d);
        bool bear = Black(a) && GapDown(b, a) && c.H < b.H && c.L < b.L
            && d.H < c.H && d.L < c.L && e.C > b.O && e.C < a.C;
        bool bull = White(a) && GapUp(b, a) && c.H > b.H && c.L > b.L
            && d.H > c.H && d.L > c.L && e.C < b.O && e.C > a.C;
        return colors && BodyLong(a) && (bear || bull)
            ? (White(e) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Closing Marubozu: a long body with a very short shadow only on the
  /// closing end.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ClosingMarubozu(Bar[] bars) => ClosingMarubozu(Context(bars));

  /// <summary>Closing Marubozu over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ClosingMarubozu(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        bool shadowed = (White(b) && b.Up < ShadowVeryShort(b))
            || (Black(b) && b.Lo < ShadowVeryShort(b));
        return BodyLong(b) && shadowed
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Concealing Baby Swallow: two marubozu blacks, a gapping black with
  /// an upper shadow into the prior body, engulfed by the fourth black.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ConcealBabysWall(Bar[] bars) => ConcealBabysWall(Context(bars));

  /// <summary>Concealing Baby Swallow over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] ConcealBabysWall(Ctx ctx) =>
      Score(Wrap(ctx), 13, 4, w =>
      {
        var (a, b, c, d) = (w[0], w[1], w[2], w[3]);

        bool Marubozu(CtxBar x) => x.Lo < ShadowVeryShort(x) && x.Up < ShadowVeryShort(x);

        return Black(a) && Black(b) && Black(c) && Black(d)
            && Marubozu(a) && Marubozu(b)
            && c.Lo < ShadowVeryShort(c) && GapDown(c, b)
            && c.Up > ShadowVeryShort(c) && c.H > b.C
            && d.H > c.H && d.L < c.L
            ? 100
            : 0;
      });

  /// <summary>
  /// Counterattack: two opposite long candles closing at the same level.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Counterattack(Bar[] bars) => Counterattack(Context(bars));

  /// <summary>Counterattack over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Counterattack(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return White(a) != White(b) && BodyLong(a) && BodyLong(b)
            && b.C <= a.C + EqualTol(a) && b.C >= a.C - EqualTol(a)
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Dark Cloud Cover: a long white candle, then a black opening above
  /// its high and closing well within its body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] DarkCloudCover(Bar[] bars) => DarkCloudCover(Context(bars));

  /// <summary>Dark Cloud Cover over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] DarkCloudCover(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return White(a) && BodyLong(a) && Black(b) && b.O > a.H && b.C > a.O
            && b.C < a.C - a.Body * 0.5
            ? -100
            : 0;
      });

  /// <summary>
  /// Doji: a body no wider than 10% of the average high-low range of the
  /// 10 previous candles.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Doji(Bar[] bars) => Doji(Context(bars));

  /// <summary>Doji over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Doji(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return DojiBody(b) ? 100 : 0;
      });

  /// <summary>
  /// Doji Star: a long candle then a doji gapping away from its body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] DojiStar(Bar[] bars) => DojiStar(Context(bars));

  /// <summary>Doji Star over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] DojiStar(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        bool gapped = (White(a) && GapUp(b, a)) || (Black(a) && GapDown(b, a));
        return BodyLong(a) && DojiBody(b) && gapped
            ? (White(a) ? -100 : 100)
            : 0;
      });

  /// <summary>
  /// Dragonfly Doji: a doji with a very short upper shadow and a lower
  /// shadow beyond the very-short band.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] DragonflyDoji(Bar[] bars) => DragonflyDoji(Context(bars));

  /// <summary>Dragonfly Doji over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] DragonflyDoji(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return DojiBody(b) && b.Up < ShadowVeryShort(b) && b.Lo > ShadowVeryShort(b)
            ? 100
            : 0;
      });

  /// <summary>
  /// Engulfing: a candle whose body engulfs the previous opposite-color
  /// body — ±80 when the touching bodies are merely equal.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/±80/0, zero-padded through the lookback).</returns>
  public static int[] Engulfing(Bar[] bars) => Engulfing(Context(bars));

  /// <summary>Engulfing over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/±80/0, zero-padded through the lookback).</returns>
  public static int[] Engulfing(Ctx ctx) =>
      Score(Wrap(ctx), 2, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        bool bull = White(b) && Black(a)
            && ((b.C >= a.O && b.O < a.C) || (b.C > a.O && b.O <= a.C));
        bool bear = Black(b) && White(a)
            && ((b.O >= a.C && b.C < a.O) || (b.O > a.C && b.C <= a.O));

        if (!(bull || bear))
        {
          return 0;
        }

        return b.O != a.C && b.C != a.O ? (White(b) ? 100 : -100) : (White(b) ? 80 : -80);
      });

  /// <summary>
  /// Evening Doji Star: a long white, a doji gapping up, then a black
  /// closing well within the first body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] EveningDojiStar(Bar[] bars) => EveningDojiStar(Context(bars));

  /// <summary>Evening Doji Star over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] EveningDojiStar(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return White(a) && Black(c) && GapUp(b, a)
            && c.C < a.C - a.Body * 0.3
            && BodyLong(a) && DojiBody(b) && !BodyShort(c)
            ? -100
            : 0;
      });

  /// <summary>
  /// Evening Star: a long white, a short candle gapping up, then a black
  /// closing well within the first body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] EveningStar(Bar[] bars) => EveningStar(Context(bars));

  /// <summary>Evening Star over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] EveningStar(Ctx ctx) =>
      Score(Wrap(ctx), 12, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        return White(a) && Black(c) && GapUp(b, a)
            && c.C < a.C - a.Body * 0.3
            && BodyLong(a) && BodyShort(b) && !BodyShort(c)
            ? -100
            : 0;
      });

  /// <summary>
  /// Upside/Downside Gap Side-by-Side White Lines: two white candles on
  /// the same side of a gap, of near-equal body and open.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] GapSideSideWhite(Bar[] bars) => GapSideSideWhite(Context(bars));

  /// <summary>Upside/Downside Gap Side-by-Side White Lines over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] GapSideSideWhite(Ctx ctx) =>
      Score(Wrap(ctx), 7, 3, w =>
      {
        var (a, b, c) = (w[0], w[1], w[2]);
        bool both = (GapUp(b, a) && GapUp(c, a)) || (GapDown(b, a) && GapDown(c, a));
        return both && White(b) && White(c)
            && c.Body >= b.Body - Near(b) && c.Body <= b.Body + Near(b)
            && c.O >= b.O - EqualTol(b) && c.O <= b.O + EqualTol(b)
            ? (GapUp(b, a) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Gravestone Doji: a doji with a very short lower shadow and an upper
  /// shadow beyond the very-short band.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] GravestoneDoji(Bar[] bars) => GravestoneDoji(Context(bars));

  /// <summary>Gravestone Doji over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] GravestoneDoji(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return DojiBody(b) && b.Lo < ShadowVeryShort(b) && b.Up > ShadowVeryShort(b)
            ? 100
            : 0;
      });

  /// <summary>
  /// Hammer: a small body near the prior candle's lows with a long lower
  /// shadow and a very short upper shadow.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Hammer(Bar[] bars) => Hammer(Context(bars));

  /// <summary>Hammer over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] Hammer(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return BodyShort(b) && b.Lo > ShadowLong(b) && b.Up < ShadowVeryShort(b)
            && MinB(b) <= a.L + Near(a)
            ? 100
            : 0;
      });

  /// <summary>
  /// Hanging Man: a small body near the prior candle's highs with a long
  /// lower shadow and a very short upper shadow.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] HangingMan(Bar[] bars) => HangingMan(Context(bars));

  /// <summary>Hanging Man over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] HangingMan(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return BodyShort(b) && b.Lo > ShadowLong(b) && b.Up < ShadowVeryShort(b)
            && MinB(b) >= a.H - Near(a)
            ? -100
            : 0;
      });

  /// <summary>
  /// Harami: a long candle then a short body inside it — ±80 when the
  /// short body merely touches the long body's ends.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/±80/0, zero-padded through the lookback).</returns>
  public static int[] Harami(Bar[] bars) => Harami(Context(bars));

  /// <summary>Harami over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/±80/0, zero-padded through the lookback).</returns>
  public static int[] Harami(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);

        if (!(BodyLong(a) && BodyShort(b)))
        {
          return 0;
        }

        if (MaxB(b) < MaxB(a) && MinB(b) > MinB(a))
        {
          return White(a) ? -100 : 100;
        }

        return MaxB(b) <= MaxB(a) && MinB(b) >= MinB(a)
            ? (White(a) ? -80 : 80)
            : 0;
      });

  /// <summary>
  /// Harami Cross: a long candle then a doji inside it — ±80 when the
  /// doji merely touches the long body's ends.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/±80/0, zero-padded through the lookback).</returns>
  public static int[] HaramiCross(Bar[] bars) => HaramiCross(Context(bars));

  /// <summary>Harami Cross over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/±80/0, zero-padded through the lookback).</returns>
  public static int[] HaramiCross(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);

        if (!(BodyLong(a) && DojiBody(b)))
        {
          return 0;
        }

        if (MaxB(b) < MaxB(a) && MinB(b) > MinB(a))
        {
          return White(a) ? -100 : 100;
        }

        return MaxB(b) <= MaxB(a) && MinB(b) >= MinB(a)
            ? (White(a) ? -80 : 80)
            : 0;
      });

  /// <summary>
  /// High Wave: a short body with both shadows longer than twice the
  /// body.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] HighWave(Bar[] bars) => HighWave(Context(bars));

  /// <summary>High Wave over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] HighWave(Ctx ctx) =>
      Score(Wrap(ctx), 10, 1, w =>
      {
        var b = w[0];
        return BodyShort(b) && b.Up > ShadowVeryLong(b) && b.Lo > ShadowVeryLong(b)
            ? (White(b) ? 100 : -100)
            : 0;
      });

  /// <summary>
  /// Hikkake: an inside bar resolved by a breakout bar, with a ±200
  /// confirmation when a close crosses the setup's extreme within three
  /// bars. Reads the raw bars, not the candle context.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/±200/0, zero-padded through the lookback).</returns>
  public static int[] Hikkake(Bar[] bars)
  {
    int size = bars.Length;
    int cd = 0, result = 0;
    double savedHigh = 0.0, savedLow = 0.0;

    for (int i = 2; i <= Math.Min(4, size - 1); i++)
    {
      (_, cd, result, savedHigh, savedLow) = HikkakeStep(bars, i, cd, result, savedHigh, savedLow);
    }

    var scores = new int[size];
    for (int i = 5; i < size; i++)
    {
      (scores[i], cd, result, savedHigh, savedLow) = HikkakeStep(bars, i, cd, result, savedHigh, savedLow);
    }

    return scores;
  }

  /// <summary>Hikkake over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context; the raw bars are read.</param>
  /// <returns>The per-bar scores (+100/-100/±200/0, zero-padded through the lookback).</returns>
  public static int[] Hikkake(Ctx ctx) => Hikkake(RawOf(ctx));

  // One bar of the shared hikkake state machine: detection has
  // priority, confirmation follows while the counter is live.
  private static (int Score, int Cd, int Result, double SavedHigh, double SavedLow) HikkakeStep(
      Bar[] bars, int i, int cd, int result, double savedHigh, double savedLow)
  {
    Bar p2 = bars[i - 2], p1 = bars[i - 1], p0 = bars[i];

    bool detected = p1.High < p2.High && p1.Low > p2.Low
        && ((p0.High < p1.High && p0.Low < p1.Low) || (p0.High > p1.High && p0.Low > p1.Low));

    int score;
    if (detected)
    {
      result = p0.High < p1.High ? 100 : -100;
      score = result;
      cd = 4;
      savedHigh = p1.High;
      savedLow = p1.Low;
    }
    else
    {
      bool confirmed = cd > 0
          && ((result > 0 && p0.Close > savedHigh) || (result < 0 && p0.Close < savedLow));

      score = confirmed ? Confirmed(result) : 0;
      if (confirmed)
      {
        cd = 0;
      }
    }

    if (cd > 0)
    {
      cd--;
    }

    return (score, cd, result, savedHigh, savedLow);
  }

  /// <summary>
  /// Modified Hikkake: two consecutive inside bars with the second
  /// closing near its extreme, resolved by a breakout bar, with the same
  /// ±200 confirmation.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/±200/0, zero-padded through the lookback).</returns>
  public static int[] HikkakeMod(Bar[] bars) => HikkakeMod(Context(bars));

  /// <summary>Modified Hikkake over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/±200/0, zero-padded through the lookback).</returns>
  public static int[] HikkakeMod(Ctx ctx)
  {
    CtxBar[] bars = Wrap(ctx);
    int size = bars.Length;
    if (size <= 10)
    {
      return new int[size];
    }

    int count = 0, result = 0;
    double patternHigh = 0.0, patternLow = 0.0;

    for (int i = 7; i <= 9; i++)
    {
      (_, count, result, patternHigh, patternLow) = HikkakeModStep(bars, i, count, result, patternHigh, patternLow);
    }

    var scores = new int[size];
    for (int i = 10; i < size; i++)
    {
      (scores[i], count, result, patternHigh, patternLow) =
          HikkakeModStep(bars, i, count, result, patternHigh, patternLow);
    }

    return scores;
  }

  // One bar of the shared modified-hikkake state machine: detection has
  // priority, confirmation follows while the counter is live.
  private static (int Score, int Count, int Result, double PatternHigh, double PatternLow) HikkakeModStep(
      CtxBar[] bars, int i, int count, int result, double patternHigh, double patternLow)
  {
    CtxBar p3 = bars[i - 3], p2 = bars[i - 2], p1 = bars[i - 1], p0 = bars[i];

    bool detected = p2.H < p3.H && p2.L > p3.L && p1.H < p2.H && p1.L > p2.L
        && ((p0.H < p1.H && p0.L < p1.L && p2.C <= p2.L + Near(p2))
            || (p0.H > p1.H && p0.L > p1.L && p2.C >= p2.H - Near(p2)));

    int score;
    if (detected)
    {
      result = p0.H < p1.H ? 100 : -100;
      score = result;
      count = 4;
      patternHigh = p1.H;
      patternLow = p1.L;
    }
    else
    {
      bool confirmed = count > 0
          && ((result > 0 && p0.C > patternHigh) || (result < 0 && p0.C < patternLow));

      score = confirmed ? Confirmed(result) : 0;
      if (confirmed)
      {
        count = 0;
      }
    }

    if (count > 0)
    {
      count--;
    }

    return (score, count, result, patternHigh, patternLow);
  }

  private static int Confirmed(int result) => result + Math.Sign(result) * 100;

  /// <summary>
  /// Homing Pigeon: a long black candle then a smaller black body inside
  /// it.
  /// </summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] HomingPigeon(Bar[] bars) => HomingPigeon(Context(bars));

  /// <summary>Homing Pigeon over a prebuilt context.</summary>
  /// <param name="ctx">The shared candle context.</param>
  /// <returns>The per-bar scores (+100/-100/0, zero-padded through the lookback).</returns>
  public static int[] HomingPigeon(Ctx ctx) =>
      Score(Wrap(ctx), 11, 2, w =>
      {
        var (a, b) = (w[0], w[1]);
        return Black(a) && Black(b) && BodyLong(a) && BodyShort(b)
            && b.O < a.O && b.C > a.C
            ? 100
            : 0;
      });
}

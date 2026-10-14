using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class CdlTestsA : CdlFixture
{
  [Fact]
  public void Score_conventions_on_hand_made_bars()
  {
    // The candle settings judge each bar against the 10 bars before it,
    // so the crafted patterns sit after full warmup windows.
    Bar[] warm = Enumerable.Range(0, 11)
        .Select(_ => new Bar(100.0, 102.0, 99.0, 101.0, 0))
        .ToArray();

    Bar[] doji = warm.Take(10).ToArray();
    var dojiBar = new Bar(101.0, 102.0, 99.0, 101.01, 0);
    Assert.All(Cdl.Doji(doji.Append(dojiBar).Concat(warm).ToArray()).Take(10), s => Assert.Equal(0, s));
    Assert.Equal(100, Cdl.Doji(doji.Append(dojiBar).ToArray())[^1]);

    var hammer = new Bar(99.0, 99.6, 96.5, 99.5, 0);
    Assert.Equal(100, Cdl.Hammer(warm.Append(hammer).ToArray())[^1]);

    var hangingMan = new Bar(102.5, 103.1, 100.0, 103.0, 0);
    Assert.Equal(-100, Cdl.HangingMan(warm.Append(hangingMan).ToArray())[^1]);

    var marubozu = new Bar(100.0, 103.05, 99.95, 103.0, 0);
    Assert.Equal(100, Cdl.ClosingMarubozu(warm.Take(10).Append(marubozu).ToArray())[^1]);

    // Engulfing evaluates from the third bar on (lookback 2).
    var lead = new Bar(10.0, 10.5, 9.5, 10.2, 0);

    Assert.Equal(new[] { 0, 0, 100 }, Cdl.Engulfing(new[]
    {
        lead,
        new Bar(12.0, 12.5, 11.5, 11.8, 0),
        new Bar(11.5, 13.0, 11.0, 12.9, 0),
    }));

    Assert.Equal(new[] { 0, 0, 100 }, Cdl.Engulfing(new[]
    {
        lead,
        new Bar(11.0, 11.2, 9.8, 10.0, 0),
        new Bar(9.9, 12.0, 9.7, 11.9, 0),
    }));

    // A body merely touching the prior close scores 80, not 100.
    Assert.Equal(new[] { 0, 0, -80 }, Cdl.Engulfing(new[]
    {
        lead,
        new Bar(10.0, 12.5, 9.5, 12.0, 0),
        new Bar(12.0, 12.1, 9.4, 9.5, 0),
    }));
  }

  [Fact]
  public void Hikkake_reports_the_plus_minus_200_confirmation_levels()
  {
    var bars = new[]
    {
        new Bar(1.0, 10.0, 0.0, 5.0, 0),
        new Bar(1.0, 9.0, 1.0, 5.0, 0),
        new Bar(1.0, 8.0, 0.5, 5.0, 0),
        new Bar(1.0, 8.5, 0.6, 5.0, 0),
        new Bar(1.0, 8.4, 0.7, 5.0, 0),
        new Bar(1.0, 8.6, 0.65, 8.0, 0),
    };

    Assert.Equal(new[] { 0, 0, 0, 0, 0, 0 }, Cdl.Hikkake(bars));

    var confirmed = bars.ToArray();
    confirmed[5] = new Bar(1.0, 8.6, 0.65, 9.5, 0);
    Assert.Equal(new[] { 0, 0, 0, 0, 0, 200 }, Cdl.Hikkake(confirmed));
  }

  [Fact]
  public void Patterns_match_the_reference_counts_on_the_mixed_fixture()
  {
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ThreeOutside(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ThreeStarsInSouth(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ThreeWhiteSoldiers(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.AbandonedBaby(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.AdvanceBlock(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 56, [100] = 4 }, Counts(Cdl.BeltHold(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Breakaway(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 8, [0] = 41, [100] = 11 }, Counts(Cdl.ClosingMarubozu(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ConcealBabysWall(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Counterattack(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.DarkCloudCover(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 50, [100] = 10 }, Counts(Cdl.Doji(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.DojiStar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.DragonflyDoji(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Engulfing(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.EveningDojiStar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.EveningStar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.GapSideSideWhite(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.GravestoneDoji(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Hammer(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.HangingMan(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 1, [0] = 58, [100] = 1 }, Counts(Cdl.Harami(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 1, [0] = 59 }, Counts(Cdl.HaramiCross(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 47, [100] = 13 }, Counts(Cdl.HighWave(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 1, [0] = 52, [100] = 4, [200] = 3 }, Counts(Cdl.Hikkake(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.HikkakeMod(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 59, [100] = 1 }, Counts(Cdl.HomingPigeon(Bars)));
  }
}

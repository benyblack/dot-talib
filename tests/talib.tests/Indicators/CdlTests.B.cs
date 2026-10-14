using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class CdlTestsB : CdlFixture
{
  [Fact]
  public void Patterns_match_the_reference_counts_on_the_mixed_fixture()
  {
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Identical3Crows(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.InNeck(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.InvertedHammer(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Kicking(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.KickingByLength(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.LadderBottom(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 50, [100] = 10 }, Counts(Cdl.LongLeggedDoji(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 2, [0] = 53, [100] = 5 }, Counts(Cdl.LongLine(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 56, [100] = 4 }, Counts(Cdl.Marubozu(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 59, [100] = 1 }, Counts(Cdl.MatchingLow(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.MatHold(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.MorningDojiStar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.MorningStar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.OnNeck(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Piercing(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 52, [100] = 8 }, Counts(Cdl.RickshawMan(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.RiseFall3Methods(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 59, [100] = 1 }, Counts(Cdl.SeparatingLines(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ShootingStar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 1, [0] = 57, [100] = 2 }, Counts(Cdl.ShortLine(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 3, [0] = 41, [100] = 16 }, Counts(Cdl.SpinningTop(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.StalledPattern(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.StickSandwich(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Takuri(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.TasukiGap(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Thrusting(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Tristar(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.Unique3River(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.UpsideGap2Crows(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.XsideGap3Methods(Bars)));
  }
}

using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public abstract class CdlFixture
{
  // A deterministic mixed fixture (gaps, dojis, long shadows, marubozu
  // bars); every count below is cross-checked against the independent
  // reference implementation of the same definitions.
  protected static readonly Bar[] Bars =
  {
      new(100.0, 101.0, 97.8, 96.8, 0),
      new(102.4327, 104.1327, 99.6327, 100.0327, 0),
      new(103.9563, 106.3563, 100.4563, 102.3563, 0),
      new(104.0896, 107.1896, 102.4896, 104.0896, 0),
      new(103.005, 106.805, 100.805, 103.005, 0),
      new(98.9477, 102.4477, 96.1477, 99.7477, 0),
      new(100.3853, 102.0853, 99.3853, 101.9853, 0),
      new(99.0526, 101.4526, 97.4526, 101.4526, 0),
      new(98.6062, 101.7062, 96.4062, 101.8062, 0),
      new(101.0504, 103.0504, 101.0504, 103.0504, 0),
      new(103.471, 104.471, 102.471, 101.071, 0),
      new(104.9645, 106.6645, 103.3645, 103.3645, 0),
      new(105.0638, 107.4638, 102.8638, 104.2638, 0),
      new(103.9573, 107.4573, 101.1573, 103.9573, 0),
      new(98.9006, 102.7006, 97.9006, 99.7006, 0),
      new(95.3609, 96.3609, 93.7609, 95.3609, 0),
      new(98.0625, 99.7625, 94.5625, 100.4625, 0),
      new(99.6456, 102.0456, 96.8456, 102.8456, 0),
      new(102.1009, 105.2009, 101.1009, 98.9009, 0),
      new(104.5087, 108.3087, 102.9087, 102.1087, 0),
      new(107.9718, 108.9718, 105.7718, 106.3718, 0),
      new(102.5372, 104.5372, 102.5372, 104.5372, 0),
      new(101.4094, 103.8094, 100.4094, 101.4094, 0),
      new(99.8538, 102.9538, 96.3538, 100.6538, 0),
      new(98.8373, 102.6373, 96.6373, 100.4373, 0),
      new(96.5731, 97.5731, 93.7731, 98.9731, 0),
      new(100.6855, 102.3855, 99.6855, 103.8855, 0),
      new(103.1513, 105.5513, 101.5513, 103.1513, 0),
      new(102.0459, 105.1459, 99.8459, 99.6459, 0),
      new(103.4783, 107.2783, 100.6783, 101.8783, 0),
      new(103.51, 104.51, 100.01, 102.71, 0),
      new(102.3612, 104.0612, 100.7612, 102.3612, 0),
      new(100.8073, 103.2073, 98.6073, 101.6073, 0),
      new(101.8144, 103.8144, 101.8144, 103.8144, 0),
      new(100.0846, 103.8846, 99.0846, 102.4846, 0),
      new(95.7259, 96.7259, 94.1259, 98.9259, 0),
      new(100.7016, 102.4016, 98.5016, 97.5016, 0),
      new(103.0825, 106.5825, 99.5825, 100.6825, 0),
      new(104.4839, 107.5839, 103.4839, 102.8839, 0),
      new(104.482, 108.282, 102.882, 104.482, 0),
      new(103.3127, 104.3127, 101.1127, 103.3127, 0),
      new(101.7612, 103.4612, 98.9612, 102.5612, 0),
      new(97.2923, 99.6923, 96.2923, 98.8923, 0),
      new(97.5969, 100.6969, 95.9969, 99.9969, 0),
      new(99.2669, 103.0669, 95.7669, 102.4669, 0),
      new(99.2519, 101.2519, 99.2519, 101.2519, 0),
      new(106.1185, 107.8185, 105.1185, 103.7185, 0),
      new(105.4887, 107.8887, 103.8887, 103.8887, 0),
      new(105.4533, 108.5533, 103.2533, 104.6533, 0),
      new(100.764, 104.564, 97.964, 100.764, 0),
      new(99.2155, 100.2155, 98.2155, 100.0155, 0),
      new(98.271, 99.971, 94.771, 98.271, 0),
      new(98.61, 101.01, 96.41, 101.01, 0),
      new(100.3083, 103.8083, 97.5083, 103.5083, 0),
      new(102.8022, 106.6022, 101.8022, 99.6022, 0),
      new(102.6539, 103.6539, 101.0539, 100.2539, 0),
      new(102.9927, 104.6927, 100.7927, 101.3927, 0),
      new(102.9239, 104.9239, 102.9239, 104.9239, 0),
      new(101.7152, 104.8152, 98.2152, 101.7152, 0),
      new(102.17, 105.97, 100.57, 102.97, 0),
  };

  protected static Dictionary<int, int> Counts(int[] scores) =>
      scores.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

}

public class CdlTests : CdlFixture
{
  [Fact]
  public void Warmup_bars_of_multi_bar_patterns_score_zero()
  {
    Assert.Equal(new[] { 0, 0 }, Cdl.TwoCrows(Bars[..2]));
  }

  [Fact]
  public void Patterns_match_the_reference_counts_on_the_mixed_fixture()
  {
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.TwoCrows(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ThreeBlackCrows(Bars)));
    Assert.Equal(new Dictionary<int, int> { [-100] = 1, [0] = 59 }, Counts(Cdl.ThreeInside(Bars)));
    Assert.Equal(new Dictionary<int, int> { [0] = 60 }, Counts(Cdl.ThreeLineStrike(Bars)));
  }

  [Fact]
  public void Context_overloads_equal_the_plain_bar_calls()
  {
    var ctx = Cdl.Context(Bars);
    Assert.Equal(Cdl.TwoCrows(Bars), Cdl.TwoCrows(ctx));
    Assert.Equal(Cdl.ThreeInside(Bars), Cdl.ThreeInside(ctx));
  }

  [Fact]
  public void Scan_scores_named_patterns_over_one_shared_context()
  {
    var result = Cdl.Scan(Bars, new[] { "TwoCrows", "ThreeInside", "ThreeLineStrike" });
    Assert.Equal(Cdl.TwoCrows(Bars), result["TwoCrows"]);
    Assert.Equal(Cdl.ThreeInside(Bars), result["ThreeInside"]);
    Assert.Equal(Cdl.ThreeLineStrike(Bars), result["ThreeLineStrike"]);
  }

  [Fact]
  public void Scan_scores_every_pattern_by_default()
  {
    var result = Cdl.Scan(Bars);
    Assert.Equal(Cdl.PatternNames.Length, result.Count);
    Assert.Equal(Cdl.ThreeInside(Bars), result["ThreeInside"]);
    Assert.Equal(Cdl.XsideGap3Methods(Bars), result["XsideGap3Methods"]);
  }
}

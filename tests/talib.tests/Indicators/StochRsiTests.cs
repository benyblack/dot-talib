using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class StochRsiTests
{
  // The 100-price fixture from earlier releases was newest-first; it is
  // reversed here into chronological order (values are unchanged).
  private static readonly double[] Prices = new[]
  {
        1330.95, 1334.65, 1340, 1338.7, 1319.6, 1305.75, 1303.8, 1309.6, 1296.95,
        1254.25, 1266.2, 1266, 1270.7, 1244.55, 1233.7, 1244.5, 1227.05, 1225.05,
        1235.75, 1246.55, 1248.3, 1245.7, 1250.5, 1242.95, 1228.5, 1246.9, 1258.2,
        1250.2, 1253.15, 1233.8, 1255.15, 1266.5, 1259.9, 1257.75, 1273.3, 1272.2,
        1272.3, 1264.65, 1258.9, 1235.15, 1254.6, 1258.35, 1293.2, 1309.15, 1281.1,
        1276.1, 1286.7, 1287.45, 1293.2, 1307.35, 1345.6, 1300.6, 1299.6, 1299.4,
        1301.8, 1303.65, 1308.95, 1319.25, 1319.2, 1336.75, 1313.3, 1309, 1295.3,
        1302.15, 1302.7, 1314.15, 1334.1, 1333.95, 1399.65, 1430.1, 1438.65, 1429.9,
        1399.95, 1396.75, 1395.9, 1378.35, 1372.7, 1382.05, 1389.2, 1408.7, 1386.3,
        1399.1, 1356, 1348.7, 1345.35, 1341.2, 1342.7, 1350.7, 1322.9, 1291.65,
        1287.65, 1297.7, 1307.4, 1308.05, 1305.15, 1306.95, 1318.15, 1317.55,
        1279.15, 1208.2,
    }.Reverse().ToArray();

  [Fact]
  public void Latest_returns_null_without_enough_data()
  {
    Assert.Null(StochRsi.Latest(Prices, 14, 200));
    Assert.Equal(0.0, Round(StochRsi.Latest(Prices, 14, 5), 4));
  }

  [Fact]
  public void StochRsiSeries_aligns_with_the_input()
  {
    double?[] series = StochRsi.StochRsiSeries(Prices, 14, 5);
    Assert.Equal(Prices.Length, series.Length);
    Assert.Equal(18, series.Count(v => v is null));
  }

  [Fact]
  public void Series_and_scalar_agree_at_the_latest_point()
  {
    Assert.Equal(StochRsi.StochRsiSeries(Prices, 14, 5)[^1], StochRsi.Latest(Prices, 14, 5));
  }

  [Fact]
  public void Batch_matches_the_doctest_walks()
  {
    double[] prices = { 3, 5, 2, 6, 1, 4, 5, 3, 6, 2 };
    Assert.Equal(0.0, StochRsi.Latest(prices, 3, 3));

    double?[] expected = { null, null, null, null, null, 60.9677, 100.0, 0.0, 100.0, 0.0 };
    Assert.Equal(expected, Round(StochRsi.StochRsiSeries(prices, 3, 3), 4));
  }

  [Fact]
  public void A_flat_rsi_window_has_no_range()
  {
    // A strictly rising series keeps RSI pinned at 100, so every
    // stochastic window is flat (highest == lowest) and %K is null.
    double[] rising = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    Assert.All(StochRsi.StochRsiSeries(rising, 3, 3), v => Assert.Null(v));
    Assert.Null(StochRsi.Latest(rising, 3, 3));

    // A constant series never defines RSI at all — same null outcome.
    double[] flat = { 5, 5, 5, 5, 5, 5, 5, 5 };
    Assert.All(StochRsi.StochRsiSeries(flat, 3, 3), v => Assert.Null(v));
    Assert.Null(StochRsi.Latest(flat, 3, 3));
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(StochRsi.StochRsiSeries(Array.Empty<double>(), 3, 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => StochRsi.Latest(Prices, 0, 5));
    Assert.Throws<ArgumentOutOfRangeException>(() => StochRsi.Latest(Prices, 14, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => StochRsi.StochRsiSeries(Prices, -1, 5));
    Assert.Throws<ArgumentOutOfRangeException>(() => StochRsi.StochRsiSeries(Prices, 14, -2));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);

  private static double?[] Round(double?[] values, int digits) =>
      values.Select(v => Round(v, digits)).ToArray();
}

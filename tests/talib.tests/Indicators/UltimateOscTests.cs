using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class UltimateOscTests
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

  // Deterministic OHLCV bars derived from the price fixture.
  private static readonly Bar[] Bars = BarsFrom(Prices);

  [Fact]
  public void Latest_returns_null_without_enough_data()
  {
    Assert.Null(UltimateOsc.Latest(Bars, 7, 14, 200));
    Assert.Equal(67.2266, Round(UltimateOsc.Latest(Bars), 4));
  }

  [Fact]
  public void UltimateOscSeries_aligns_with_the_input()
  {
    double?[] series = UltimateOsc.UltimateOscSeries(Bars);
    Assert.Equal(Bars.Length, series.Length);
    Assert.Equal(27, series.Count(v => v is null));
    Assert.Equal(67.2266, Round(series[^1], 4));
    Assert.Equal(UltimateOsc.Latest(Bars), series[^1]);
  }

  [Fact]
  public void Matches_the_doctest_walk()
  {
    var bars = Enumerable.Range(1, 5)
        .Select(i => new Bar(
            Open: 0,
            High: i + 1.0,
            Low: i - 1.0,
            Close: i * 1.0,
            Volume: 0))
        .ToArray();

    Assert.Equal(50.0, UltimateOsc.Latest(bars, 2, 3, 4));
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(UltimateOsc.UltimateOscSeries(Array.Empty<Bar>()));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => UltimateOsc.Latest(Bars, 0, 14, 28));
    Assert.Throws<ArgumentOutOfRangeException>(() => UltimateOsc.Latest(Bars, 7, 0, 28));
    Assert.Throws<ArgumentOutOfRangeException>(() => UltimateOsc.UltimateOscSeries(Bars, 7, 14, -1));
  }

  private static Bar[] BarsFrom(double[] prices) =>
      prices
          .Select((price, i) => new Bar(
              0,
              price + 1 + i % 3,
              price - 1 - (i + 1) % 3,
              price,
              1000 + 100 * (i % 5)))
          .ToArray();

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

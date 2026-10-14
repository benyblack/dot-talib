using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class SarTests
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

  // Deterministic OHLC bars derived from the price fixture.
  private static readonly Bar[] Bars = BarsFrom(Prices);

  [Fact]
  public void Latest_returns_null_without_enough_data()
  {
    Assert.Null(Sar.Latest(Array.Empty<Bar>(), 0.02, 0.02, 0.2));
    Assert.Null(Sar.Latest(new[] { new Bar(0, 12, 8, 10, 0) }));
    Assert.Equal(new double?[] { null }, Sar.SarSeries(new[] { new Bar(0, 12, 8, 10, 0) }));
  }

  [Fact]
  public void Series_aligns_with_the_input()
  {
    double?[] series = Sar.SarSeries(Bars);
    Assert.Equal(Bars.Length, series.Length);
    Assert.Null(series[0]);

    // Recorded from the TA-Lib C library: the first bar is consumed (its
    // low seeds the second bar's SAR — here 1206.2) and the trend comes
    // from +DM/-DM, not closes.
    Assert.Equal(1206.2, Round(series[1], 4));
    Assert.Equal(1277.2732, Round(series[^1], 4));
    Assert.Equal(Sar.Latest(Bars)!.Value, series[^1]!.Value);
  }

  [Fact]
  public void Series_returns_empty_for_empty_input()
  {
    Assert.Empty(Sar.SarSeries(Array.Empty<Bar>()));
  }

  [Fact]
  public void SarExt_shares_the_base_SAR_behaviour()
  {
    Assert.Equal(Sar.Latest(Bars)!.Value, Sar.SarExt(Bars)!.Value);
    Assert.Equal(Sar.Latest(Bars, 0.02, 0.02, 0.2)!.Value, Sar.SarExt(Bars, 0.02, 0.02, 0.2)!.Value);
    Assert.Equal(Sar.SarSeries(Bars), Sar.SarExtSeries(Bars));
  }

  [Fact]
  public void Batch_matches_the_doctest_walks()
  {
    var bars = new[]
    {
        new Bar(0, 10, 9, 9.5, 0),
        new Bar(0, 11, 9.5, 10.5, 0),
        new Bar(0, 12, 10, 11.5, 0),
    };

    // Values recorded from the TA-Lib C library: 9.04 = 9 + 0.02 * (11 - 9).
    Assert.Equal(new double?[] { null, 9.0, 9.04 }, Sar.SarSeries(bars));
    Assert.Equal(9.04, Sar.SarExt(bars));

    var walk = bars.Concat(new[]
    {
            new Bar(0, 11.5, 9.5, 10.0, 0),
            new Bar(0, 12.5, 10.5, 12.0, 0),
        })
        .ToArray();

    Assert.Equal(9.2721, Round(Sar.Latest(walk), 4));
  }

  [Fact]
  public void Trend_reverses_when_the_stop_is_penetrated()
  {
    // A rising market whose fourth bar plunges below the up stop: the
    // trend flips down and the stop jumps to the extreme point.
    var bars = new[]
    {
        new Bar(0, 10, 9, 9.5, 0),
        new Bar(0, 11, 9.5, 10.5, 0),
        new Bar(0, 12, 10, 11.5, 0),
        new Bar(0, 11.5, 8.5, 9.0, 0),
    };

    double?[] series = Sar.SarSeries(bars);
    Assert.Equal(new double?[] { null, 9.0, 9.04, 12.0 }, series);
    Assert.Equal(12.0, Sar.Latest(bars));
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

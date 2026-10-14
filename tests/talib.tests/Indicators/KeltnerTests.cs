using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class KeltnerTests
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

  // Bars derived from the prices exactly like the Elixir fixture:
  // high = price + 1 + i % 3, low = price - 1 - (i + 1) % 3,
  // close = price, volume = 1000 + 100 * (i % 5).
  private static readonly Bar[] Bars = BuildBars(Prices);

  [Fact]
  public void Latest_returns_null_without_enough_data()
  {
    Assert.Null(Keltner.Latest(Bars, 200, 2));

    var channels = Keltner.Latest(Bars, 14, 2)!.Value;
    Assert.True(Math.Abs(channels.Upper - 1330.0) < 0.01);
    Assert.True(Math.Abs(channels.Middle - 1304.53) < 0.01);
    Assert.True(Math.Abs(channels.Lower - 1279.06) < 0.01);
  }

  [Fact]
  public void Latest_returns_null_while_the_atr_is_still_undefined()
  {
    // Exactly `period` bars define the EMA but not the ATR (it needs
    // period + 1), so no channels yet. The Elixir original raises nil
    // arithmetic here; the port returns null like every other
    // insufficient-data case, agreeing with the series.
    var bars = new[]
    {
        new Bar(0, 12, 8, 10, 0),
        new Bar(0, 11, 9, 10, 0),
        new Bar(0, 13, 9, 12, 0),
    };

    Assert.Null(Keltner.Latest(bars, 3, 2));
    Assert.Null(Keltner.KeltnerSeries(bars, 3, 2)[^1]);
  }

  [Fact]
  public void KeltnerSeries_aligns_with_the_input_and_ends_at_the_scalar_channels()
  {
    Keltner.KeltnerResult?[] series = Keltner.KeltnerSeries(Bars, 14, 2);
    Assert.Equal(Bars.Length, series.Length);
    Assert.Equal(14, series.Count(v => v is null));

    var last = series[^1]!.Value;
    var scalar = Keltner.Latest(Bars, 14, 2)!.Value;
    Assert.Equal(scalar.Upper, last.Upper);
    Assert.Equal(scalar.Middle, last.Middle);
    Assert.Equal(scalar.Lower, last.Lower);
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(Keltner.KeltnerSeries(Array.Empty<Bar>(), 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    // mult is a double parameter in C#, so the Elixir "mult must be a
    // number" guard is enforced by the type system instead.
    Assert.Throws<ArgumentOutOfRangeException>(() => Keltner.Latest(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Keltner.KeltnerSeries(Bars, -1));
  }

  [Fact]
  public void Batch_matches_the_doctest_walk()
  {
    var bars = new[]
    {
        new Bar(0, 12, 8, 10, 0),
        new Bar(0, 11, 9, 10, 0),
        new Bar(0, 13, 9, 12, 0),
    };

    var channels = Keltner.Latest(bars, 2, 2)!.Value;
    Assert.Equal(17.3333, Round(channels.Upper, 4));
    Assert.Equal(11.3333, Round(channels.Middle, 4));
    Assert.Equal(5.3333, Round(channels.Lower, 4));

    Assert.Equal(2, Keltner.KeltnerSeries(bars, 2).Count(v => v is null));
  }

  private static Bar[] BuildBars(double[] prices)
  {
    var bars = new Bar[prices.Length];
    for (int i = 0; i < prices.Length; i++)
    {
      double price = prices[i];
      bars[i] = new Bar(0, price + 1 + i % 3, price - 1 - (i + 1) % 3, price, 1000 + 100 * (i % 5));
    }

    return bars;
  }

  private static double Round(double value, int digits) =>
      Math.Round(value, digits, MidpointRounding.AwayFromZero);
}

using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class AdxTests
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

  // Deterministic OHLC bars derived from the price fixture, exactly like
  // the Elixir reference suite.
  private static readonly Bar[] Bars = BarsFrom(Prices);

  [Fact]
  public void PlusDi_and_MinusDi_match_the_reference()
  {
    Assert.Null(Adx.PlusDi(Bars, 200));
    Assert.Null(Adx.MinusDi(Bars, 200));
    Assert.Equal(55.353, Round(Adx.PlusDi(Bars, 14), 4));
    Assert.Equal(28.8769, Round(Adx.MinusDi(Bars, 14), 4));
  }

  [Fact]
  public void Latest_needs_two_periods_minus_one_bars()
  {
    Assert.Null(Adx.Latest(Bars[..26], 14));
    Assert.Equal(21.3288, Round(Adx.Latest(Bars, 14), 4));
  }

  [Fact]
  public void AdxAll_computes_all_three_values_and_matches_the_scalars()
  {
    Adx.AdxPoint all = Adx.AdxAll(Bars, 14);

    Assert.True(Math.Abs(all.PlusDi!.Value - Adx.PlusDi(Bars, 14)!.Value) < 1.0e-9);
    Assert.True(Math.Abs(all.MinusDi!.Value - Adx.MinusDi(Bars, 14)!.Value) < 1.0e-9);
    Assert.True(Math.Abs(all.Adx!.Value - Adx.Latest(Bars, 14)!.Value) < 1.0e-9);
  }

  [Fact]
  public void AdxAll_returns_per_value_nulls_while_data_accumulates()
  {
    // 20 bars: enough for the directional lines (14 needed) but not for
    // ADX (27 needed).
    Adx.AdxPoint partial = Adx.AdxAll(Bars[..20], 14);

    Assert.NotNull(partial.PlusDi);
    Assert.NotNull(partial.MinusDi);
    Assert.Null(partial.Adx);
  }

  [Fact]
  public void Dx_Adxr_and_the_raw_Dm_match_the_reference()
  {
    Assert.Equal(31.4331, Round(Adx.Dx(Bars, 14), 4));
    Assert.Null(Adx.Adxr(Bars[..39], 14));
    Assert.Equal(17.6999, Round(Adx.Adxr(Bars, 14), 4));

    double[] plus = Adx.PlusDm(Bars);
    double[] minus = Adx.MinusDm(Bars);
    Assert.Equal(Bars.Length, plus.Length);
    Assert.Equal(0.0, plus[0]);
    Assert.Equal(Bars.Length, minus.Length);
    Assert.Equal(0.0, minus[0]);
  }

  [Fact]
  public void Doctest_walks_match_the_reference()
  {
    var bars = new[]
    {
            new Bar(0, 10, 5, 7, 0),
            new Bar(0, 11, 6, 9, 0),
            new Bar(0, 12, 7, 8, 0),
            new Bar(0, 11, 8, 10, 0),
            new Bar(0, 13, 9, 12, 0),
        };

    Assert.Equal(22.807, Round(Adx.PlusDi(bars, 3), 4));
    Assert.Equal(0.0, Adx.MinusDi(bars, 3));
    Assert.Equal(100.0, Adx.Latest(bars, 3));

    Adx.AdxPoint all = Adx.AdxAll(bars, 3);
    Assert.Equal(22.807, Round(all.PlusDi, 4));
    Assert.Equal(0.0, all.MinusDi);
    Assert.Equal(100.0, all.Adx);

    var threeBars = new[]
    {
            new Bar(0, 10, 5, 7, 0),
            new Bar(0, 11, 6, 9, 0),
            new Bar(0, 12, 7, 8, 0),
        };
    Assert.Equal(100.0, Round(Adx.Dx(threeBars, 2), 4));

    var fourBars = new[]
    {
            new Bar(0, 10, 5, 7, 0),
            new Bar(0, 11, 6, 9, 0),
            new Bar(0, 12, 7, 8, 0),
            new Bar(0, 12, 8, 10, 0),
        };
    Assert.Equal(100.0, Round(Adx.Adxr(fourBars, 2), 4));

    Assert.Equal(new double[] { 0, 1, 1 }, Adx.PlusDm(threeBars));
    Assert.Equal(new double[] { 0, 0, 0 }, Adx.MinusDm(threeBars));
  }

  [Fact]
  public void Empty_input_returns_null()
  {
    Assert.Null(Adx.PlusDi(Array.Empty<Bar>(), 3));
    Assert.Null(Adx.MinusDi(Array.Empty<Bar>(), 3));
    Assert.Null(Adx.Latest(Array.Empty<Bar>(), 3));
    Assert.Null(Adx.Dx(Array.Empty<Bar>(), 3));
    Assert.Null(Adx.Adxr(Array.Empty<Bar>(), 3));
    Assert.Empty(Adx.PlusDm(Array.Empty<Bar>()));
    Assert.Empty(Adx.MinusDm(Array.Empty<Bar>()));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Adx.Latest(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Adx.PlusDi(Bars, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Adx.MinusDi(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Adx.AdxAll(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Adx.Dx(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Adx.Adxr(Bars, 0));
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

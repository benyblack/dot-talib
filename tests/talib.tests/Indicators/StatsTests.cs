using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class StatsTests
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

  // Deterministic market series derived from the price fixture.
  private static readonly double[] Market = Prices
      .Select((price, i) => price + 20 * Math.Sin(0.3 * i))
      .ToArray();

  [Fact]
  public void Variance_returns_null_without_enough_data()
  {
    Assert.Null(Stats.Variance(Prices, 200));
    Assert.Equal(0.25, Stats.Variance(new[] { 1.0, 2, 3, 4 }, 2));
    Assert.Equal(1006.4367, Round(Stats.Variance(Prices, 14), 4));
  }

  [Fact]
  public void Linear_regression_family_returns_null_without_enough_data()
  {
    Assert.Null(Stats.LinearReg(Prices, 200));
    Assert.Null(Stats.Slope(Prices, 200));
    Assert.Null(Stats.Intercept(Prices, 200));
    Assert.Null(Stats.Angle(Prices, 200));
    Assert.Null(Stats.Tsf(Prices, 200));
  }

  [Fact]
  public void Linear_regression_family_matches_the_fixture()
  {
    Assert.Equal(7.3571, Round(Stats.Slope(Prices, 14), 4));
    Assert.Equal(1250.8714, Round(Stats.Intercept(Prices, 14), 4));
    Assert.Equal(1346.5143, Round(Stats.LinearReg(Prices, 14), 4));
    Assert.Equal(1353.8714, Round(Stats.Tsf(Prices, 14), 4));
    Assert.Equal(82.2597, Round(Stats.Angle(Prices, 14), 4));
  }

  [Fact]
  public void Linear_regression_family_matches_the_doctest_walk()
  {
    double[] prices = { 1, 2, 3, 4, 5 };

    Assert.Equal(1.0, Stats.Slope(prices, 3));
    Assert.Equal(3.0, Stats.Intercept(prices, 3));
    Assert.Equal(5.0, Stats.LinearReg(prices, 3));
    Assert.Equal(6.0, Stats.Tsf(prices, 3));
    Assert.Equal(45.0, Stats.Angle(prices, 3));
  }

  [Fact]
  public void Beta_and_Correl_match_the_fixture()
  {
    // Returns-based beta (TA-Lib), recorded from the C library; the
    // price-space regression the Elixir reference used gives 0.6104.
    Assert.Equal(1.0037, Round(Stats.Beta(Prices, Market, 14), 4));
    Assert.Equal(0.938, Round(Stats.Correl(Prices, Market, 14), 4));

    // A linear market (y = 2x in levels) has beta 1 in returns.
    Assert.Equal(1.0, Stats.Correl(new[] { 1.0, 2, 3, 4 }, new[] { 2.0, 4, 6, 8 }, 3));
    Assert.Equal(1.0, Stats.Beta(new[] { 1.0, 2, 3, 4 }, new[] { 2.0, 4, 6, 8 }, 3));
  }

  [Fact]
  public void Beta_and_Correl_return_null_without_enough_data_or_zero_variance()
  {
    Assert.Null(Stats.Beta(Prices, new[] { 1.0, 2 }, 14));
    Assert.Null(Stats.Correl(new[] { 1.0, 2 }, Prices, 14));

    // Correlation is undefined on a flat series; beta on flat prices is
    // a degenerate returns regression whose defined reading is 0 in the
    // C library (not null), and a flat market (y) gives slope 0.
    double[] flat = { 5.0, 5, 5, 5 };
    Assert.Null(Stats.Correl(new[] { 1.0, 2, 3, 4 }, flat, 3));
    Assert.Equal(0.0, Stats.Beta(flat, new[] { 1.0, 2, 3, 4 }, 3));
    Assert.Equal(0.0, Stats.Beta(new[] { 1.0, 2, 3, 4 }, flat, 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Variance(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Slope(Prices, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.LinearReg(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Intercept(Prices, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Angle(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Tsf(Prices, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Beta(Prices, Market, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stats.Correl(Prices, Market, -1));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

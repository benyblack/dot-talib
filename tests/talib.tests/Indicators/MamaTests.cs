using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class MamaTests
{
  private static readonly double[] Ramp = Enumerable.Range(1, 100).Select(i => i * 1.0).ToArray();

  // Chronological fixture (the newest-first fixture of the Elixir
  // reference, reversed; values are unchanged).
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
  public void Verified_last_values_on_the_ramp()
  {
    Mama.MamaResult values = Mama.MamaSeries(Ramp);
    Assert.Equal(95.7327, Round(values.Mama[^1], 4));
    Assert.Equal(90.7448, Round(values.Fama[^1], 4));
  }

  [Fact]
  public void Verified_last_values_on_the_price_fixture()
  {
    Mama.MamaResult values = Mama.MamaSeries(Prices);
    Assert.Equal(1321.3113, Round(values.Mama[^1], 4));
    Assert.Equal(1283.2815, Round(values.Fama[^1], 4));
  }

  [Fact]
  public void Series_align_with_the_input_and_null_pad_the_warmup()
  {
    Mama.MamaResult values = Mama.MamaSeries(Prices);
    Assert.Equal(100, values.Mama.Length);
    Assert.Equal(100, values.Fama.Length);
    Assert.Equal(32, values.Mama.Count(v => v is null));
    Assert.Equal(32, values.Fama.Count(v => v is null));
    Assert.Null(values.Mama[31]);
    Assert.NotNull(values.Mama[32]);
    Assert.NotNull(values.Fama[32]);
  }

  [Fact]
  public void Defaults_match_the_explicit_limits()
  {
    Mama.MamaResult withDefaults = Mama.MamaSeries(Prices);
    Mama.MamaResult explicitLimits = Mama.MamaSeries(Prices, 0.5, 0.05);
    Assert.Equal(explicitLimits.Mama, withDefaults.Mama);
    Assert.Equal(explicitLimits.Fama, withDefaults.Fama);
  }

  [Fact]
  public void Insufficient_history_yields_all_nulls()
  {
    Mama.MamaResult values = Mama.MamaSeries(Enumerable.Range(1, 10).Select(i => i * 1.0).ToArray());
    Assert.Equal(10, values.Mama.Length);
    Assert.Equal(10, values.Fama.Length);
    Assert.All(values.Mama, v => Assert.Null(v));
    Assert.All(values.Fama, v => Assert.Null(v));
  }

  [Fact]
  public void Limits_are_validated_like_talib()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Mama.MamaSeries(Prices, 0.005));
    Assert.Throws<ArgumentOutOfRangeException>(() => Mama.MamaSeries(Prices, 0.5, 1.5));
    Assert.Throws<ArgumentOutOfRangeException>(() => Mama.MamaSeries(Prices, 0.991));
    Assert.Throws<ArgumentOutOfRangeException>(() => Mama.MamaSeries(Prices, 0.5, 0.009));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

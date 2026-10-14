using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class HtTests
{
  // The ramp exercises a pure trend (period pegged high, trend mode 1).
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
  public void Series_align_with_the_input_and_null_pad_the_warmup()
  {
    // The 63-lookback family...
    foreach (double?[] series in new[]
    {
      Ht.TrendlineSeries(Ramp),
      Ht.DcPhaseSeries(Ramp),
      Ht.SineSeries(Ramp).Sine,
      Ht.SineSeries(Ramp).LeadSine,
    })
    {
      Assert.Equal(100, series.Length);
      Assert.Equal(63, series.Count(v => v is null));
      Assert.All(series[..63], v => Assert.Null(v));
      Assert.All(series[63..], v => Assert.NotNull(v));
    }

    int?[] modes = Ht.TrendModeSeries(Ramp);
    Assert.Equal(100, modes.Length);
    Assert.Equal(63, modes.Count(v => v is null));
    Assert.All(modes[..63], v => Assert.Null(v));
    Assert.All(modes[63..], v => Assert.NotNull(v));

    // ...and the 32-lookback family, as in TA-Lib.
    foreach (double?[] series in new[] { Ht.DcPeriodSeries(Ramp), Ht.PhasorSeries(Ramp).Inphase, Ht.PhasorSeries(Ramp).Quadrature })
    {
      Assert.Equal(100, series.Length);
      Assert.Equal(32, series.Count(v => v is null));
      Assert.All(series[..32], v => Assert.Null(v));
      Assert.All(series[32..], v => Assert.NotNull(v));
    }
  }

  [Fact]
  public void Insufficient_history_yields_all_nulls()
  {
    double[] shortPrices = Enumerable.Range(1, 63).Select(i => i * 1.0).ToArray();
    Assert.All(Ht.TrendlineSeries(shortPrices), v => Assert.Null(v));
    Assert.All(Ht.DcPeriodSeries(shortPrices), v => Assert.Null(v));
    Assert.All(Ht.TrendModeSeries(shortPrices), v => Assert.Null(v));

    Ht.SineResult empty = Ht.SineSeries(Array.Empty<double>());
    Assert.Empty(empty.Sine);
    Assert.Empty(empty.LeadSine);
  }

  [Fact]
  public void Exactly_64_prices_define_the_last_value_only()
  {
    double[] minPrices = Enumerable.Range(1, 64).Select(i => i * 1.0).ToArray();
    Assert.Equal(63, Ht.TrendlineSeries(minPrices).Count(v => v is null));
    Assert.NotNull(Ht.TrendlineSeries(minPrices)[^1]);
    Assert.Equal(32, Ht.DcPeriodSeries(minPrices).Count(v => v is null));
    Assert.NotNull(Ht.DcPeriodSeries(minPrices)[^1]);
  }

  // The last values of both fixtures are cross-checked against the
  // TA-Lib (C) output on the same fixtures, matching to 1e-13 or better.
  [Fact]
  public void Verified_last_values_on_the_ramp()
  {
    Assert.Equal(76.85, Round(Ht.TrendlineSeries(Ramp)[^1], 4));
    Assert.Equal(42.5223, Round(Ht.DcPeriodSeries(Ramp)[^1], 4));
    Assert.Equal(183.9149, Round(Ht.DcPhaseSeries(Ramp)[^1], 4));
    Assert.Equal(6.7514, Round(Ht.PhasorSeries(Ramp).Inphase[^1], 4));
    Assert.Equal(-1.9195, Round(Ht.PhasorSeries(Ramp).Quadrature[^1], 4));
    Assert.Equal(-0.0683, Round(Ht.SineSeries(Ramp).Sine[^1], 4));
    Assert.Equal(-0.7537, Round(Ht.SineSeries(Ramp).LeadSine[^1], 4));
    Assert.Equal(1, Ht.TrendModeSeries(Ramp)[^1]!.Value);
  }

  [Fact]
  public void Verified_last_values_on_the_price_fixture()
  {
    Assert.Equal(1275.3293, Round(Ht.TrendlineSeries(Prices)[^1], 4));
    Assert.Equal(21.2511, Round(Ht.DcPeriodSeries(Prices)[^1], 4));
    Assert.Equal(169.2707, Round(Ht.DcPhaseSeries(Prices)[^1], 4));
    Assert.Equal(26.6549, Round(Ht.PhasorSeries(Prices).Inphase[^1], 4));
    Assert.Equal(-10.2152, Round(Ht.PhasorSeries(Prices).Quadrature[^1], 4));
    Assert.Equal(0.1862, Round(Ht.SineSeries(Prices).Sine[^1], 4));
    Assert.Equal(-0.5631, Round(Ht.SineSeries(Prices).LeadSine[^1], 4));
  }

  [Fact]
  public void Invariants()
  {
    double[] sine = Ht.SineSeries(Prices).Sine.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
    Assert.All(sine, v => Assert.True(-1.0 <= v && v <= 1.0));

    double[] periods = Ht.DcPeriodSeries(Prices).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
    Assert.All(periods, v => Assert.True(v > 0.0 && v <= 50.0));

    int?[] modes = Ht.TrendModeSeries(Prices);
    Assert.All(modes.Where(v => v.HasValue), v => Assert.True(v!.Value is 0 or 1));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

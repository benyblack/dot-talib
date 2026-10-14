using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class StdDevTests
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
    Assert.Null(StdDev.Latest(Prices, 200));
    Assert.Equal(39.5503, Round(StdDev.Latest(Prices, 20), 4));
  }

  [Fact]
  public void StdDevSeries_aligns_with_the_input_and_agrees_with_Latest()
  {
    double?[] series = StdDev.StdDevSeries(Prices, 20);
    Assert.Equal(Prices.Length, series.Length);
    Assert.Equal(19, series.Count(v => v is null));
    Assert.True(Math.Abs(series[^1]!.Value - StdDev.Latest(Prices, 20)!.Value) < 1.0e-9);
  }

  [Fact]
  public void StdDevSeries_of_a_constant_series_is_zero()
  {
    Assert.Equal(new double?[] { null, 0.0, 0.0, 0.0 }, StdDev.StdDevSeries(new[] { 5.0, 5, 5, 5 }, 2));
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(StdDev.StdDevSeries(Array.Empty<double>(), 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => StdDev.Latest(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => StdDev.StdDevSeries(Prices, -1));
  }

  [Fact]
  public void Batch_matches_the_doctest_walks()
  {
    Assert.Equal(1.118034, Round(StdDev.Latest(new[] { 1.0, 2, 3, 4 }, 4), 6));
    Assert.Equal(1.0, StdDev.Latest(new[] { 2.0, 4, 6, 8 }, 2));
    Assert.Equal(new double?[] { null, 1.0, 1.0, 1.0 }, StdDev.StdDevSeries(new[] { 2.0, 4, 6, 8 }, 2));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    var (state, values) = StdDev.StdDevInit(new[] { 2.0, 4, 6, 8 }, 2);
    Assert.Equal(new double?[] { null, 1.0, 1.0, 1.0 }, values);
    Assert.Equal(1.0, StdDev.StdDevNext(state, 10));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight points must equal the batch series
    // exactly, and stepping the remaining points must continue it
    // exactly — the bit-for-bit streaming contract.
    double[] history = { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
    CheckStreaming(history, 3);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => StdDev.StdDevInit(new[] { 1.0, 2 }, 3));
  }

  private static void CheckStreaming(double[] history, int period)
  {
    var (state, warmupValues) = StdDev.StdDevInit(history[..8], period);
    double?[] full = StdDev.StdDevSeries(history, period);
    Assert.Equal(full[..8], warmupValues);

    var streamed = history[8..].Select(price => StdDev.StdDevNext(state, price)).ToArray();
    Assert.Equal(full[8..], streamed);
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

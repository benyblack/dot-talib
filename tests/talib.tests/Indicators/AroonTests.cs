using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class AroonTests
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
    Assert.Null(Aroon.Latest(Prices, 200));

    Aroon.AroonResult latest = Aroon.Latest(Prices, 14)!.Value;
    Assert.Equal(85.7143, Round(latest.Up, 4));
    Assert.Equal(0.0, Round(latest.Down, 4));
  }

  [Fact]
  public void AroonSeries_aligns_with_the_input()
  {
    Aroon.AroonResult?[] series = Aroon.AroonSeries(Prices, 14);
    Assert.Equal(Prices.Length, series.Length);
    Assert.Equal(14, series.Count(v => v is null));
    Assert.Equal(Aroon.Latest(Prices, 14), series[^1]);
  }

  [Fact]
  public void AroonOsc_matches_the_reference()
  {
    Assert.Null(Aroon.AroonOsc(Prices, 200));
    Assert.Equal(85.7143, Round(Aroon.AroonOsc(Prices, 14), 4));
  }

  [Fact]
  public void Batch_matches_the_doctest_walks()
  {
    double[] walk = { 5, 3, 8, 2, 6, 9, 4 };

    Aroon.AroonResult latest = Aroon.Latest(walk, 3)!.Value;
    Assert.Equal(66.66666666666667, latest.Up);
    Assert.Equal(0.0, latest.Down);

    Aroon.AroonResult?[] series = Aroon.AroonSeries(walk, 3);
    Assert.Null(series[2]);
    Assert.Equal(new Aroon.AroonResult(100.0, 33.333333333333336), series[5]);
    Assert.Equal(new Aroon.AroonResult(66.66666666666667, 0.0), series[6]);

    Assert.Equal(66.6667, Round(Aroon.AroonOsc(walk, 3), 4));
  }

  [Fact]
  public void Earliest_tie_wins()
  {
    // The window max 9 appears twice; only a strictly better value
    // replaces the extreme, so Up counts days since the FIRST 9.
    double[] walk = { 2, 9, 1, 9, 3 };

    Aroon.AroonResult latest = Aroon.Latest(walk, 3)!.Value;
    Assert.Equal(0.0, latest.Up);
    Assert.Equal(33.333333333333336, latest.Down);

    Aroon.AroonResult?[] series = Aroon.AroonSeries(walk, 3);
    Assert.Equal(new Aroon.AroonResult(33.333333333333336, 66.66666666666667), series[3]);
    Assert.Equal(new Aroon.AroonResult(0.0, 33.333333333333336), series[4]);
  }

  [Fact]
  public void Series_returns_empty_for_empty_input()
  {
    Assert.Empty(Aroon.AroonSeries(Array.Empty<double>(), 3));
    Assert.Null(Aroon.Latest(Array.Empty<double>(), 3));
    Assert.Null(Aroon.AroonOsc(Array.Empty<double>(), 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Aroon.Latest(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Aroon.AroonSeries(Prices, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Aroon.AroonOsc(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Aroon.AroonInit(Prices[..4], 0));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    var (state, values) = Aroon.AroonInit(new[] { 5.0, 3, 8, 2, 6, 9, 4 }, 3);
    Assert.Null(values[2]);
    Assert.Equal(new Aroon.AroonResult(100.0, 33.333333333333336), values[5]);
    Assert.Equal(new Aroon.AroonResult(66.66666666666667, 0.0), values[6]);
    Assert.Equal(new Aroon.AroonResult(100.0, 66.66666666666667), Aroon.AroonNext(state, 10));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight points must equal the batch series
    // exactly, and stepping the remaining points must continue it
    // exactly — the bit-for-bit streaming contract. The run [5, 5, 5, 4]
    // also exercises equal values on the sliding deques.
    double[] history = { 5, 9, 3, 8, 2, 6, 5, 5, 5, 4, 7, 6 };

    CheckStreaming(history, 3);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => Aroon.AroonInit(new[] { 5.0, 9, 3 }, 3));
  }

  private static void CheckStreaming(double[] history, int period)
  {
    var (state, warmupValues) = Aroon.AroonInit(history[..8], period);
    Aroon.AroonResult?[] full = Aroon.AroonSeries(history, period);
    Assert.Equal(full[..8], warmupValues);

    var streamed = history[8..].Select(price => Aroon.AroonNext(state, price)).ToArray();
    Assert.Equal(full[8..], streamed);
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

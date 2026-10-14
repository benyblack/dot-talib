using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class BbandsTests
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
    Assert.Null(Bbands.Latest(Prices, 200, 2));
    Assert.Null(Bbands.Latest(Prices, 200));

    var bands = Bbands.Latest(Prices, 20, 2)!;
    Assert.True(Math.Abs(bands.Upper - 1358.82) < 0.01);
    Assert.True(Math.Abs(bands.Middle - 1279.71) < 0.01);
    Assert.True(Math.Abs(bands.Lower - 1200.61) < 0.01);
  }

  [Fact]
  public void BbandsSeries_aligns_with_the_input_and_ends_at_the_scalar_bands()
  {
    Bbands.BbandsResult?[] series = Bbands.BbandsSeries(Prices, 20, 2);
    Assert.Equal(Prices.Length, series.Length);
    Assert.Equal(19, series.Count(v => v is null));

    var last = series[^1]!;
    var scalar = Bbands.Latest(Prices, 20, 2)!;
    Assert.True(Math.Abs(last.Upper - scalar.Upper) < 1.0e-9);
    Assert.True(Math.Abs(last.Middle - scalar.Middle) < 1.0e-9);
    Assert.True(Math.Abs(last.Lower - scalar.Lower) < 1.0e-9);
  }

  [Fact]
  public void A_constant_series_collapses_onto_the_SMA()
  {
    Assert.Equal(new Bbands.BbandsResult(5.0, 5.0, 5.0), Bbands.Latest(new[] { 5.0, 5, 5, 5 }, 4, 2));
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(Bbands.BbandsSeries(Array.Empty<double>(), 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    // mult is a double parameter in C#, so the Elixir "mult must be a
    // number" guard is enforced by the type system instead.
    Assert.Throws<ArgumentOutOfRangeException>(() => Bbands.Latest(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Bbands.BbandsSeries(Prices, -1));
  }

  [Fact]
  public void Batch_matches_the_doctest_walks()
  {
    var bands = Bbands.Latest(new[] { 1.0, 2, 3, 4, 5 }, 4, 2)!;
    Assert.Equal(5.7361, Round(bands.Upper, 4));
    Assert.Equal(3.5, Round(bands.Middle, 4));
    Assert.Equal(1.2639, Round(bands.Lower, 4));

    Assert.Equal(3, Bbands.BbandsSeries(new[] { 1.0, 2, 3, 4, 5 }, 4, 2).Count(v => v is null));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    var (state, values) = Bbands.BbandsInit(new[] { 1.0, 2, 3, 4, 5 }, 4, 2);
    Assert.Equal(new Bbands.BbandsResult(5.73606797749979, 3.5, 1.2639320225002102), values[^1]!);

    var next = Bbands.BbandsNext(state, 6)!;
    Assert.Equal(6.7361, Round(next.Upper, 4));
    Assert.Equal(4.5, next.Middle);
    Assert.Equal(2.2639, Round(next.Lower, 4));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight points must equal the batch series
    // exactly (element-wise, band by band), and stepping the remaining
    // points must continue it exactly — the bit-for-bit streaming
    // contract.
    double[] history = { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
    CheckStreaming(history, 3, 2);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => Bbands.BbandsInit(new[] { 1.0, 2, 3 }, 4));
  }

  private static void CheckStreaming(double[] history, int period, double mult)
  {
    var (state, warmupValues) = Bbands.BbandsInit(history[..8], period, mult);
    var full = Bbands.BbandsSeries(history, period, mult);
    Assert.Equal(full[..8], warmupValues);

    var streamed = history[8..].Select(price => Bbands.BbandsNext(state, price)).ToArray();
    Assert.Equal(full[8..], streamed);
  }

  private static double Round(double value, int digits) =>
      Math.Round(value, digits, MidpointRounding.AwayFromZero);
}

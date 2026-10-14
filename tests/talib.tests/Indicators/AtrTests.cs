using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class AtrTests
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
    Assert.Null(Atr.Latest(Bars, 200));
    Assert.Equal(12.7337, Round(Atr.Latest(Bars, 14), 4));
  }

  [Fact]
  public void AtrSeries_aligns_with_the_input()
  {
    double?[] series = Atr.AtrSeries(Bars, 14);
    Assert.Equal(Bars.Length, series.Length);
    Assert.Equal(14, series.Count(v => v is null));
    Assert.Equal(Atr.Latest(Bars, 14)!.Value, series[^1]!.Value);
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(Atr.AtrSeries(Array.Empty<Bar>(), 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Atr.Latest(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Atr.AtrSeries(Bars, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Atr.Natr(Bars, 0));
  }

  [Fact]
  public void TrueRange_and_Natr_match_the_reference()
  {
    double[] trs = Atr.TrueRange(Bars);
    Assert.Equal(Bars.Length, trs.Length);
    Assert.Equal(Bars[0].High - Bars[0].Low, trs[0]);

    Assert.Null(Atr.Natr(Bars, 200));
    Assert.Equal(0.9567, Round(Atr.Natr(Bars, 14), 4));
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

    Assert.Equal(new double[] { 4, 2, 4 }, Atr.TrueRange(bars));
    Assert.Equal(3.0, Atr.Latest(bars, 2));
    Assert.Equal(25.0, Round(Atr.Natr(bars, 2), 4));
    Assert.Equal(new double?[] { null, null, 3.0 }, Atr.AtrSeries(bars, 2));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    var bars = new[]
    {
            new Bar(0, 12, 8, 10, 0),
            new Bar(0, 11, 9, 10, 0),
            new Bar(0, 13, 9, 12, 0),
        };

    var (state, values) = Atr.AtrInit(bars, 2);
    Assert.Equal(new double?[] { null, null, 3.0 }, values);
    Assert.Equal(3.5, Atr.AtrNext(state, new Bar(0, 14, 10, 11, 0)));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight bars must equal the batch series
    // exactly, and stepping the remaining bars must continue it
    // exactly — the bit-for-bit streaming contract.
    Bar[] history = BarsFrom(Enumerable.Range(1, 12).Select(i => (double)i).ToArray());
    CheckStreaming(history, 3);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    var bars = new[] { new Bar(0, 12, 8, 10, 0), new Bar(0, 11, 9, 10, 0) };
    Assert.Throws<ArgumentException>(() => Atr.AtrInit(bars, 2));
  }

  private static void CheckStreaming(Bar[] history, int period)
  {
    var (state, warmupValues) = Atr.AtrInit(history[..8], period);
    double?[] full = Atr.AtrSeries(history, period);
    Assert.Equal(full[..8], warmupValues);

    var streamed = history[8..].Select(bar => Atr.AtrNext(state, bar)).ToArray();
    Assert.Equal(full[8..], streamed);
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

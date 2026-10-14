using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class DonchianTests
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

  // Bars derived from the fixture prices: high = price + 1 + i % 3,
  // low = price - 1 - (i + 1) % 3 (the window extremes can land on
  // values that print as integers).
  private static readonly Bar[] Bars = Prices
      .Select((price, i) => new Bar(
          Open: 0,
          High: price + 1 + i % 3,
          Low: price - 1 - (i + 1) % 3,
          Close: price,
          Volume: 1000 + 100 * (i % 5)))
      .ToArray();

  [Fact]
  public void Calculate_returns_null_without_enough_data()
  {
    Assert.Null(Donchian.Latest(Bars, 200));

    Donchian.DonchianResult channels = Donchian.Latest(Bars, 14)!;
    Assert.Equal(1342.0, channels.Upper);
    Assert.Equal(1292.78, Round(channels.Middle, 2));
    Assert.Equal(1243.55, channels.Lower);
  }

  [Fact]
  public void Series_aligns_with_the_input()
  {
    Donchian.DonchianResult?[] series = Donchian.DonchianSeries(Bars, 14);
    Assert.Equal(Bars.Length, series.Length);
    Assert.Equal(13, series.Count(v => v is null));
    Assert.All(series[..13], value => Assert.Null(value));
    Assert.All(series[13..], value => Assert.NotNull(value));
  }

  [Fact]
  public void Series_agrees_exactly_with_Latest_at_the_latest_point()
  {
    Assert.Equal(Donchian.Latest(Bars, 14), Donchian.DonchianSeries(Bars, 14)[^1]);
  }

  [Fact]
  public void Midprice_is_the_middle_channel_line()
  {
    Assert.Equal(1292.78, Round(Donchian.Midprice(Bars, 14), 2));
  }

  [Fact]
  public void Scalar_and_series_match_the_doctest_walks()
  {
    Bar[] bars =
    {
            new(Open: 0, High: 12, Low: 8, Close: 10, Volume: 0),
            new(0, 11, 9, 10, 0),
            new(0, 13, 9, 12, 0),
        };

    Assert.Equal(new Donchian.DonchianResult(Upper: 13, Middle: 10.5, Lower: 8), Donchian.Latest(bars, 3));
    Assert.Equal(10.5, Donchian.Midprice(bars, 3));

    Donchian.DonchianResult?[] expected =
    {
            null,
            new Donchian.DonchianResult(Upper: 12, Middle: 10.0, Lower: 8),
            new Donchian.DonchianResult(Upper: 13, Middle: 11.0, Lower: 9),
        };
    Assert.Equal(expected, Donchian.DonchianSeries(bars, 2));
  }

  [Fact]
  public void Series_returns_empty_for_empty_input()
  {
    Assert.Empty(Donchian.DonchianSeries(Array.Empty<Bar>(), 3));
    Assert.Null(Donchian.Latest(Array.Empty<Bar>(), 3));
    Assert.Null(Donchian.Midprice(Array.Empty<Bar>(), 3));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Donchian.Latest(Bars, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Donchian.DonchianSeries(Bars, -1));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    Bar[] bars =
    {
            new(Open: 0, High: 12, Low: 8, Close: 10, Volume: 0),
            new(0, 11, 9, 10, 0),
            new(0, 13, 9, 12, 0),
        };

    var (state, values) = Donchian.DonchianInit(bars, 2);
    Donchian.DonchianResult?[] expected =
    {
            null,
            new Donchian.DonchianResult(Upper: 12, Middle: 10.0, Lower: 8),
            new Donchian.DonchianResult(Upper: 13, Middle: 11.0, Lower: 9),
        };
    Assert.Equal(expected, values);
    Assert.Equal(new Donchian.DonchianResult(Upper: 14, Middle: 10.5, Lower: 7), Donchian.DonchianNext(state, new Bar(0, 14, 7, 0, 0)));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight bars must equal the batch series
    // exactly, and stepping the remaining bars must continue it
    // exactly — the bit-for-bit streaming contract.
    Bar[] history = new[] { 5.0, 9, 3, 8, 2, 6, 5, 5, 5, 4, 7, 6 }
        .Select((price, i) => new Bar(
            Open: 0,
            High: price + 1 + i % 3,
            Low: price - 1 - (i + 1) % 3,
            Close: price,
            Volume: 0))
        .ToArray();

    var (state, warmupValues) = Donchian.DonchianInit(history[..8], 3);
    Donchian.DonchianResult?[] full = Donchian.DonchianSeries(history, 3);
    Assert.Equal(full[..8], warmupValues);

    var streamed = history[8..].Select(bar => Donchian.DonchianNext(state, bar)).ToArray();
    Assert.Equal(full[8..], streamed);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => Donchian.DonchianInit(
        new[] { new Bar(0, 12, 8, 0, 0), new Bar(0, 11, 9, 0, 0) }, 3));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

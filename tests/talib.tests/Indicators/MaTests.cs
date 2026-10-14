using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class MaTests
{
  // Chronological series (oldest first) — the single input convention.
  private static readonly double[] Series =
  {
        44.3289, 44.3189, 44.3389, 44.0902, 44.1497, 43.6124, 44.3278, 44.8264,
        45.0955, 45.4245, 45.8433, 46.0826, 45.8931, 46.0328, 45.614, 46.282,
    };

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
  public void Sma_returns_null_without_enough_data()
  {
    Assert.Null(Ma.Sma(Series, 50));
    Assert.Null(Ma.Sma(Series));
    Assert.Equal(45.5422, Round(Ma.Sma(Series, 10), 4));
    Assert.Equal(1292.35, Round(Ma.Sma(new[] { 1281.90, 1277.90, 1289.80, 1297.00, 1304.70 }, 4), 2));
  }

  [Fact]
  public void SmaSeries_returns_a_null_padded_rolling_series()
  {
    double?[] expected =
    {
            null, null, 44.3289, 44.2493, 44.1929, 43.9508, 44.03, 44.2555, 44.7499,
            45.1155, 45.4544, 45.7835, 45.9397, 46.0028, 45.8466, 45.9763,
        };
    Assert.Equal(expected, Round(Ma.SmaSeries(Series, 3), 4));
  }

  [Fact]
  public void SmaSeries_agrees_with_Sma_at_the_latest_point()
  {
    var series = Ma.SmaSeries(Series, 10);
    Assert.True(Math.Abs(series[^1]!.Value - Ma.Sma(Series, 10)!.Value) < 1.0e-9);
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(Ma.SmaSeries(Array.Empty<double>(), 3));
    Assert.Empty(Ma.EmaSeries(Array.Empty<double>(), 3));
  }

  [Fact]
  public void Wma_weights_the_newest_price_highest()
  {
    Assert.Null(Ma.Wma(Series, 50));
    Assert.Null(Ma.Wma(Series));
    Assert.Equal(45.8098, Round(Ma.Wma(Series, 10), 4));

    var series = Ma.WmaSeries(Series, 10);
    Assert.True(Math.Abs(series[^1]!.Value - Ma.Wma(Series, 10)!.Value) < 1.0e-9);
  }

  [Fact]
  public void Ema_seeds_with_the_sma_of_the_first_window()
  {
    Assert.Null(Ma.Ema(Series, 50));
    Assert.Equal(1328.45, Round(Ma.Ema(Prices, 5), 2));
    Assert.Equal(1306.72, Round(Ma.Ema(Prices, 13), 2));
  }

  [Fact]
  public void EmaSeries_returns_a_null_padded_series()
  {
    double?[] expected =
    {
            null, null, 44.3289, 44.2096, 44.1796, 43.896, 44.1119, 44.4692, 44.7823,
            45.1034, 45.4734, 45.778, 45.8355, 45.9342, 45.7741, 46.028,
        };
    Assert.Equal(expected, Round(Ma.EmaSeries(Series, 3), 4));
  }

  [Fact]
  public void EmaSeries_agrees_exactly_with_Ema_at_the_latest_point()
  {
    Assert.Equal(Ma.Ema(Series, 3)!.Value, Ma.EmaSeries(Series, 3)[^1]!.Value);
  }

  [Fact]
  public void Rma_uses_wilder_smoothing()
  {
    Assert.Null(Ma.Rma(Series, 50));
    Assert.Equal(1288.0976, Round(Ma.Rma(Prices, 14), 4));

    double?[] series = Ma.RmaSeries(Prices, 14);
    Assert.Equal(Prices.Length, series.Length);
    Assert.Equal(13, series.Count(v => v is null));
    Assert.Equal(Ma.Rma(Prices, 14)!.Value, series[^1]!.Value);
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.Sma(Series, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.Ema(Series, -3));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.SmaSeries(Series, 0));
  }

  [Fact]
  public void Overlap_stages_dema_tema_midpoint()
  {
    Assert.Equal(1331.9805, Round(Ma.Dema(Prices, 14), 4));
    Assert.Equal(1345.4243, Round(Ma.Tema(Prices, 14), 4));
    Assert.Equal(1292.275, Round(Ma.Midpoint(Prices, 14), 4));

    Assert.Equal(Prices.Length, Ma.DemaSeries(Prices, 14).Length);
  }

  [Fact]
  public void Streaming_sma_matches_the_doctest_walk()
  {
    var (state, values) = Ma.SmaInit(new[] { 1.0, 2, 3, 4, 5 }, 3);
    double?[] expected = { null, null, 2.0, 3.0, 4.0 };
    Assert.Equal(expected, values);
    Assert.Equal(5.0, Ma.SmaNext(state, 6));
  }

  [Fact]
  public void Streaming_ema_matches_the_doctest_walk()
  {
    var (state, values) = Ma.EmaInit(new[] { 1.0, 2, 3, 4, 5 }, 2);
    double?[] expected = { null, 1.5, 2.5, 3.5, 4.5 };
    Assert.Equal(expected, values);
    Assert.Equal(5.5, Ma.EmaNext(state, 6));
  }

  [Fact]
  public void Streaming_wma_and_rma_match_the_doctest_walks()
  {
    var (wmaState, wmaValues) = Ma.WmaInit(new[] { 1.0, 2, 3, 4, 5 }, 3);
    Assert.Equal(5.3333, Round(Ma.WmaNext(wmaState, 6), 4));

    var (rmaState, rmaValues) = Ma.RmaInit(new[] { 1.0, 2, 3, 4, 5 }, 3);
    double?[] expected = { null, null, 2.0, 2.6666666666666665, 3.444444444444444 };
    Assert.Equal(expected, rmaValues);
    Assert.Equal(4.2963, Round(Ma.RmaNext(rmaState, 6), 4));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight points must equal the batch series
    // exactly, and stepping the remaining points must continue it
    // exactly — the bit-for-bit streaming contract.
    double[] history = { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    CheckStreaming(history, 3, Ma.SmaInit, Ma.SmaNext, Ma.SmaSeries);
    CheckStreaming(history, 3, Ma.EmaInit, Ma.EmaNext, Ma.EmaSeries);
    CheckStreaming(history, 3, Ma.WmaInit, Ma.WmaNext, Ma.WmaSeries);
    CheckStreaming(history, 3, Ma.RmaInit, Ma.RmaNext, Ma.RmaSeries);
  }

  private static void CheckStreaming<TState>(
      double[] history,
      int period,
      Func<double[], int, (TState State, double?[] Values)> init,
      Func<TState, double, double?> next,
      Func<double[], int, double?[]> batch)
  {
    var (state, warmupValues) = init(history[..8], period);
    double?[] full = batch(history, period);
    Assert.Equal(full[..8], warmupValues);

    var streamed = history[8..].Select(price => next(state, price)).ToArray();
    Assert.Equal(full[8..], streamed);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => Ma.SmaInit(new[] { 1.0, 2 }, 3));
    Assert.Throws<ArgumentException>(() => Ma.EmaInit(new[] { 1.0, 2 }, 3));
    Assert.Throws<ArgumentException>(() => Ma.WmaInit(new[] { 1.0, 2 }, 3));
    Assert.Throws<ArgumentException>(() => Ma.RmaInit(new[] { 1.0, 2 }, 3));
  }

  [Fact]
  public void Overlap_additions_trima_kama_t3()
  {
    Assert.Equal(1300.0545, Round(Ma.Trima(Prices, 14), 4));
    Assert.Equal(1313.4113, Round(Ma.Kama(Prices, 14), 4));
    Assert.Equal(1279.787, Round(Ma.T3(Prices, 14), 4));

    Assert.Equal(Prices.Length, Ma.TrimaSeries(Prices, 14).Length);
    Assert.Equal(13, Ma.TrimaSeries(Prices, 14).Count(v => v is null));
    // KAMA lags one bar further: the efficiency ratio needs period
    // one-day changes to its left.
    Assert.Equal(14, Ma.KamaSeries(Prices, 14).Count(v => v is null));
    Assert.Equal(6 * (14 - 1), Ma.T3Series(Prices, 14).Count(v => v is null));
  }

  [Fact]
  public void Trima_matches_the_doctest_walk()
  {
    double[] walk = { 1, 2, 3, 4, 5, 6, 7 };
    Assert.Equal(5.0, Round(Ma.Trima(walk, 5), 4));

    double?[] expected = { null, null, null, null, 3.0, 4.0, 5.0 };
    Assert.Equal(expected, Round(Ma.TrimaSeries(walk, 5), 4));
  }

  [Fact]
  public void Kama_matches_the_doctest_walk()
  {
    double[] walk = { 1, 2, 3, 4, 5, 6, 7, 8 };
    Assert.Equal(6.7868, Round(Ma.Kama(walk, 2), 4));

    double?[] expected = { null, null, 2.4444, 3.1358, 3.9643, 4.8691, 5.8162, 6.7868 };
    Assert.Equal(expected, Round(Ma.KamaSeries(walk, 2), 4));

    // At period 1 the series is the input itself.
    Assert.Equal(new double?[] { 1.0, 2.0, 3.0 }, Ma.KamaSeries(new[] { 1.0, 2.0, 3.0 }, 1));
  }

  [Fact]
  public void T3_matches_the_doctest_walk()
  {
    double[] walk = Enumerable.Range(1, 14).Select(i => (double)i).ToArray();
    Assert.Equal(13.55, Round(Ma.T3(walk, 2), 4));
    Assert.Equal(6, Ma.T3Series(walk, 2).Count(v => v is null));
  }

  [Fact]
  public void New_averages_return_nulls_without_enough_data()
  {
    Assert.Null(Ma.Trima(Prices[..13], 14));
    Assert.Null(Ma.Kama(Prices[..14], 14));
    Assert.Null(Ma.T3(Prices[..78], 14));

    Assert.All(Ma.TrimaSeries(Prices[..13], 14), v => Assert.Null(v));
    Assert.All(Ma.KamaSeries(Prices[..14], 14), v => Assert.Null(v));
    Assert.All(Ma.T3Series(Prices[..78], 14), v => Assert.Null(v));

    Assert.Equal(Ma.Trima(Prices, 14)!.Value, Ma.TrimaSeries(Prices, 14)[^1]!.Value);
    Assert.Equal(Ma.Kama(Prices, 14)!.Value, Ma.KamaSeries(Prices, 14)[^1]!.Value);
    Assert.Equal(Ma.T3(Prices, 14)!.Value, Ma.T3Series(Prices, 14)[^1]!.Value);
  }

  [Fact]
  public void New_averages_reject_invalid_periods()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.Trima(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.TrimaSeries(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.Kama(Prices, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.KamaSeries(Prices, -1));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.T3(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.T3Series(Prices, 0));
  }

  [Fact]
  public void MovingAverage_dispatches_to_the_named_average()
  {
    Assert.Equal(Ma.Sma(Series, 3), Ma.MovingAverage(Series, 3, MaType.Sma));
    Assert.Equal(Ma.Ema(Series, 3), Ma.MovingAverage(Series, 3, MaType.Ema));
    Assert.Equal(Ma.Wma(Series, 3), Ma.MovingAverage(Series, 3, MaType.Wma));
    Assert.Equal(Ma.Dema(Prices, 14), Ma.MovingAverage(Prices, 14, MaType.Dema));
    Assert.Equal(Ma.Tema(Prices, 14), Ma.MovingAverage(Prices, 14, MaType.Tema));
    Assert.Equal(Ma.Trima(Prices, 14), Ma.MovingAverage(Prices, 14, MaType.Trima));
    Assert.Equal(Ma.Kama(Prices, 14), Ma.MovingAverage(Prices, 14, MaType.Kama));
    Assert.Equal(Ma.T3(Prices, 14), Ma.MovingAverage(Prices, 14, MaType.T3));

    Assert.Equal(6.5, Round(Ma.MovingAverage(new[] { 1.0, 2, 3, 4, 5, 6, 7, 8 }, 4, MaType.Ema), 4));
    Assert.Null(Ma.MovingAverage(Series, 50, MaType.Sma));

    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.MovingAverage(Series, 3, (MaType)99));
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.MovingAverage(Series, 0, MaType.Sma));
  }

  [Fact]
  public void Mavp_uses_the_period_given_for_each_point()
  {
    Assert.Equal(5.5, Ma.Mavp(new[] { 1.0, 2, 3, 4, 5, 6 }, new[] { 2.0, 2, 2, 2, 2, 2 })[^1]);

    double?[] firstThree = Ma.Mavp(new[] { 1.0, 2, 3, 4, 5, 6 }, new[] { 1.0, 2, 3, 4, 5, 6 });
    Assert.Equal(new double?[] { null, 1.5, 2.0 }, firstThree[..3]);

    // EMA type: each point is the EMA of the prefix ending there.
    Assert.Equal(
        Ma.Ema(new[] { 1.0, 2, 3, 4, 5, 6 }, 2),
        Ma.Mavp(new[] { 1.0, 2, 3, 4, 5, 6 }, new[] { 2.0, 2, 2, 2, 2, 2 }, 2, 30, MaType.Ema)[^1]);
  }

  [Fact]
  public void Mavp_clamps_and_truncates_the_periods()
  {
    double?[] clamped = Ma.Mavp(new[] { 1.0, 2, 3, 4, 5, 6 }, new[] { 0.0, 100, 2.7, 2, 2, 2 });
    Assert.Equal(new double?[] { null, null, 2.5, 3.5, 4.5, 5.5 }, clamped);
  }

  [Fact]
  public void Mavp_zips_prices_and_periods_and_validates()
  {
    // Only min(prices, periods) points are produced.
    Assert.Equal(2, Ma.Mavp(new[] { 1.0, 2, 3 }, new[] { 3.0, 3 }).Length);

    // The type check happens per point, so pre-window points stay null.
    Assert.Equal(new double?[] { null }, Ma.Mavp(new[] { 1.0 }, new[] { 1.0 }, 2, 30, (MaType)99));

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        Ma.Mavp(new[] { 1.0, 2, 3 }, new[] { 2.0, 2, 2 }, 2, 30, (MaType)99));
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        Ma.Mavp(new[] { 1.0, 2, 3 }, new[] { 2.0, 2, 2 }, 0, 30));
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        Ma.Mavp(new[] { 1.0, 2, 3 }, new[] { 2.0, 2, 2 }, 2, 0));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);

  private static double?[] Round(double?[] values, int digits) =>
      values.Select(v => Round(v, digits)).ToArray();
}

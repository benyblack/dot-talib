using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class StochasticTests
{
  // Chronological fixture (the newest-first fixture of earlier
  // releases, reversed; values are unchanged).
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
  public void K_returns_the_latest_fast_k_value()
  {
    Assert.Null(Stochastic.K(Prices, 1000));
    Assert.Equal(90.52, Round(Stochastic.K(Prices), 2));
    // Zero-range (flat) windows have an undefined %K.
    Assert.Null(Stochastic.K(new[] { 5.0, 5, 5 }, 3));
  }

  [Fact]
  public void K_matches_the_doctest()
  {
    Assert.Equal(75.0, Stochastic.K(new[] { 1.0, 2, 5, 1, 4 }, 5));
  }

  [Fact]
  public void KSeries_aligns_with_the_input_and_ends_at_the_scalar_value()
  {
    double?[] kSeries = Stochastic.KSeries(Prices);
    Assert.Equal(Prices.Length, kSeries.Length);
    Assert.Equal(13, kSeries.Count(v => v is null));
    Assert.Equal(Stochastic.K(Prices)!.Value, kSeries[^1]!.Value);
  }

  [Fact]
  public void KSeries_matches_the_doctest()
  {
    double?[] expected = { null, null, 20.0, 75.0, 33.33333333333333 };
    Assert.Equal(expected, Stochastic.KSeries(new[] { 1.0, 6, 2, 5, 3 }, 3));
  }

  [Fact]
  public void D_returns_the_latest_d_value()
  {
    Assert.Null(Stochastic.D(Prices, 1000));
    Assert.Equal(95.16, Round(Stochastic.D(Prices), 2));
  }

  [Fact]
  public void D_matches_the_doctest()
  {
    Assert.Equal(60.0, Stochastic.D(new[] { 1.0, 6, 2, 5, 3, 7, 4 }, 5, 3));
  }

  [Fact]
  public void D_is_the_sma_of_the_last_d_period_k_values()
  {
    const int period = 14;
    const int dPeriod = 5;

    double sum = 0;
    foreach (double? k in Stochastic.KSeries(Prices, period).TakeLast(dPeriod))
    {
      sum += k!.Value;
    }

    Assert.Equal(sum / dPeriod, Stochastic.D(Prices, period, dPeriod));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Stochastic.K(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Stochastic.D(Prices, 14, 0));
  }

  [Fact]
  public void Slow_returns_the_talib_stoch_lines()
  {
    // The slow stochastic (TA-Lib STOCH) golden values.
    Stochastic.StochPoint values = Stochastic.Slow(Prices, 14, 3, 3);
    Assert.Equal(95.1619, Round(values.K, 4));
    Assert.Equal(97.8281, Round(values.D, 4));
  }

  [Fact]
  public void Slow_matches_the_doctest()
  {
    Stochastic.StochPoint values = Stochastic.Slow(new[] { 1.0, 6, 2, 5, 3, 7, 4 }, 3, 2, 2);
    Assert.Equal(62.5, Round(values.K, 4));
    Assert.Equal(64.5833, Round(values.D, 4));
  }

  [Fact]
  public void SlowSeries_aligns_with_the_input_and_ends_at_the_scalar_lines()
  {
    Stochastic.StochResult series = Stochastic.SlowSeries(Prices, 14, 3, 3);
    Assert.Equal(Prices.Length, series.K.Length);
    Assert.Equal(Prices.Length, series.D.Length);
    // %K defined from index period - 1 + slowing - 1, %D one
    // d_period window later.
    Assert.Equal(15, series.K.Count(v => v is null));
    Assert.Equal(17, series.D.Count(v => v is null));
    Assert.Equal(95.1619, Round(series.K[^1], 4));
    Assert.Equal(97.8281, Round(series.D[^1], 4));
  }

  [Fact]
  public void Series_return_empty_for_empty_input()
  {
    Assert.Empty(Stochastic.KSeries(Array.Empty<double>(), 3));
    Assert.Empty(Stochastic.SlowSeries(Array.Empty<double>(), 3, 2, 2).K);
    Assert.Empty(Stochastic.SlowSeries(Array.Empty<double>(), 3, 2, 2).D);
  }

  [Fact]
  public void Streaming_k_matches_the_doctest_walk()
  {
    var (state, values) = Stochastic.KInit(new[] { 1.0, 6, 2, 5, 3 }, 3);
    double?[] expected = { null, null, 20.0, 75.0, 33.33333333333333 };
    Assert.Equal(expected, values);
    Assert.Equal(100.0, Stochastic.KNext(state, 7));
  }

  [Fact]
  public void Streaming_slow_matches_the_doctest_walk()
  {
    var (state, values) = Stochastic.SlowInit(new[] { 1.0, 6, 2, 5, 3, 7, 4 }, 3, 2, 2);
    Assert.Equal(62.49999999999999, values.K[^1]);
    Assert.Equal(64.58333333333333, values.D[^1]);

    Stochastic.StochPoint point = Stochastic.SlowNext(state, 5);
    Assert.Equal(29.1667, Round(point.K, 4));
    Assert.Equal(45.8333, Round(point.D, 4));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    double[] history = Prices[..20].ToArray();

    CheckStreaming(history, 5, Stochastic.KInit, Stochastic.KNext, Stochastic.KSeries);
    CheckSlowStreaming(history, 5, 3, 2, 8);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => Stochastic.KInit(new[] { 1.0, 2 }, 3));
    // slow_init needs period + slowing + d_period - 2 prices.
    Assert.Throws<ArgumentException>(() => Stochastic.SlowInit(new[] { 1.0, 2, 3, 4 }, 3, 2, 2));
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

  private static void CheckSlowStreaming(double[] history, int period, int slowing, int dPeriod, int warmup)
  {
    var (state, result) = Stochastic.SlowInit(history[..warmup], period, slowing, dPeriod);
    Stochastic.StochResult batch = Stochastic.SlowSeries(history, period, slowing, dPeriod);
    Assert.Equal(batch.K[..warmup], result.K);
    Assert.Equal(batch.D[..warmup], result.D);

    for (int i = warmup; i < history.Length; i++)
    {
      Stochastic.StochPoint point = Stochastic.SlowNext(state, history[i]);
      Assert.Equal(batch.K[i], point.K);
      Assert.Equal(batch.D[i], point.D);
    }
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

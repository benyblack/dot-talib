using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class MacdTests
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
  public void Line_returns_the_latest_macd_line_value()
  {
    Assert.Equal(20.36, Round(Macd.Line(Prices), 2));
    Assert.Null(Macd.Line(Prices, 12, 1000));
    Assert.Null(Macd.Line(Prices, 1000, 26));
    Assert.Null(Macd.Line(Prices[..51]));
  }

  [Fact]
  public void MacdSeries_aligns_with_the_input_and_ends_at_the_scalar_value()
  {
    double?[] series = Macd.MacdSeries(Prices);
    Assert.Equal(Prices.Length, series.Length);
    // MACD defined from index max(fast, slow) - 1.
    Assert.Equal(25, series.Count(v => v is null));
    Assert.Equal(Macd.Line(Prices)!.Value, series[^1]!.Value);
  }

  [Fact]
  public void Signal_returns_the_latest_signal_line_value()
  {
    Assert.Equal(12.28, Round(Macd.Signal(Prices), 2));
    Assert.Null(Macd.Signal(Prices, 1000));
    // Every MACD value in the signal window must be defined.
    Assert.Null(Macd.Signal(Prices[..68]));
  }

  [Fact]
  public void Signal_applies_the_given_period_to_the_macd_series()
  {
    const int period = 5;

    double[] window = Macd.MacdSeries(Prices)
        // scalar-valid MACD values start at index 2 * max(fast, slow) - 1
        .Skip(51)
        .TakeLast(2 * period)
        .Select(v => v!.Value)
        .ToArray();

    Assert.Equal(Ma.Ema(window, period), Macd.Signal(Prices, period));
  }

  [Fact]
  public void Histogram_returns_macd_minus_signal()
  {
    Assert.Equal(8.08, Round(Macd.Histogram(Prices), 2));
    Assert.Null(Macd.Histogram(Prices, 12, 26, 1000));
  }

  [Fact]
  public void MacdAll_computes_all_three_values_and_matches_the_scalars()
  {
    Macd.MacdPoint all = Macd.MacdAll(Prices);

    Assert.True(Math.Abs(all.Macd!.Value - Macd.Line(Prices)!.Value) < 1.0e-9);
    Assert.True(Math.Abs(all.Signal!.Value - Macd.Signal(Prices)!.Value) < 1.0e-9);
    Assert.True(Math.Abs(all.Histogram!.Value - Macd.Histogram(Prices)!.Value) < 1.0e-9);
    Assert.True(Math.Abs(all.Histogram!.Value - (all.Macd!.Value - all.Signal!.Value)) < 1.0e-12);
  }

  [Fact]
  public void MacdAll_returns_per_value_nulls_while_data_accumulates()
  {
    // 60 prices: enough for the MACD line (52 needed) but not for
    // the signal line (69 needed).
    Macd.MacdPoint partial = Macd.MacdAll(Prices[..60]);

    Assert.NotNull(partial.Macd);
    Assert.Null(partial.Signal);
    Assert.Null(partial.Histogram);
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Macd.Line(Prices, 0, 26));
    Assert.Throws<ArgumentOutOfRangeException>(() => Macd.Signal(Prices, 9, 12, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Macd.Histogram(Prices, 12, 26, 0));
  }

  [Fact]
  public void MacdFix_uses_the_whole_series_signal_convention()
  {
    Macd.MacdPoint fix = Macd.MacdFix(Prices, 12, 26, 9);
    Assert.NotNull(fix.Macd);
    Assert.NotNull(fix.Signal);

    Assert.Equal(0.4861, Round(Macd.MacdFix(new[] { 1.0, 4, 3, 5, 2, 6 }, 3, 2, 2).Macd, 4));
  }

  [Fact]
  public void Functions_match_the_doctest_walks()
  {
    Assert.Equal(0.4861, Round(Macd.Line(new[] { 1.0, 4, 3, 5, 2, 6 }, 3, 2), 4));
    Assert.Equal(0.3372, Round(Macd.Signal(new[] { 5.0, 1, 6, 4, 7, 3, 8, 2, 9 }, 2, 3, 2), 4));
    Assert.Equal(0.316, Round(Macd.Histogram(new[] { 5.0, 1, 6, 4, 7, 3, 8, 2, 9 }, 3, 2, 2), 4));

    Macd.MacdPoint all = Macd.MacdAll(new[] { 5.0, 1, 6, 4, 7, 3, 8, 2, 9 }, 3, 2, 2);
    Assert.Equal(0.6532, Round(all.Macd, 4));
    Assert.Equal(0.3372, Round(all.Signal, 4));
    Assert.Equal(0.316, Round(all.Histogram, 4));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    var (state, values) = Macd.MacdInit(new[] { 5.0, 1, 6, 4, 7, 3, 8, 2, 9 }, 3, 2, 2);
    Assert.Equal(0.6532, Round(values.Macd[^1], 4));

    Macd.MacdPoint point = Macd.MacdNext(state, 7);
    Assert.Equal(0.2959, Round(point.Macd, 4));
    Assert.Equal(0.3165, Round(point.Signal, 4));
    Assert.Equal(-0.0206, Round(point.Histogram, 4));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // The streaming signal uses the MACDFIX convention (an EMA over
    // the whole MACD series), so the batch expectation for it is the
    // whole-series EMA re-padded onto the input alignment.
    double[] history = Prices[..20].ToArray();

    CheckMacdStreaming(history, 3, 5, 3, 8);
    CheckMacdStreaming(history, 2, 3, 2, 7);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    // slow + signal - 1 prices are needed (after fast/slow swap).
    Assert.Throws<ArgumentException>(() => Macd.MacdInit(new[] { 1.0, 2, 3, 4, 5, 6 }, 3, 6, 3));
    Assert.Throws<ArgumentException>(() => Macd.MacdInit(new[] { 1.0, 2, 3, 4 }, 2, 3, 3));
  }

  private static void CheckMacdStreaming(
      double[] history,
      int fast,
      int slow,
      int signalPeriod,
      int warmup)
  {
    var (state, result) = Macd.MacdInit(history[..warmup], fast, slow, signalPeriod);

    double?[] batchMacd = Macd.MacdSeries(history, fast, slow);
    Assert.Equal(batchMacd[..warmup], result.Macd);

    double[] macdValues = batchMacd.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
    double?[] signalOfValues = Ma.EmaSeries(macdValues, signalPeriod);
    int slowPeriod = Math.Max(fast, slow);
    var batchSignal = new double?[history.Length];
    var batchHistogram = new double?[history.Length];
    for (int i = 0; i < signalOfValues.Length; i++)
    {
      batchSignal[i + slowPeriod - 1] = signalOfValues[i];
    }

    for (int i = 0; i < history.Length; i++)
    {
      batchHistogram[i] = batchMacd[i].HasValue && batchSignal[i].HasValue
          ? batchMacd[i]!.Value - batchSignal[i]!.Value
          : null;
    }

    Assert.Equal(batchSignal[..warmup], result.Signal);
    Assert.Equal(batchHistogram[..warmup], result.Histogram);

    for (int i = warmup; i < history.Length; i++)
    {
      Macd.MacdPoint point = Macd.MacdNext(state, history[i]);
      Assert.Equal(batchMacd[i], point.Macd);
      Assert.Equal(batchSignal[i], point.Signal);
      Assert.Equal(batchHistogram[i], point.Histogram);
    }
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

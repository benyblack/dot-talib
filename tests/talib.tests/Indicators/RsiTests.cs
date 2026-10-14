using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class RsiTests
{
  // All fixtures are chronological, oldest first (the fixtures of
  // earlier releases were newest-first and have been reversed; values
  // unchanged).
  private static readonly double[] Prices = new[]
  {
        44.3289, 44.3189, 44.3389, 44.0902, 44.1497, 43.6124, 44.3278, 44.8264,
        45.0955, 45.4245, 45.8433, 46.0826, 45.8931, 46.0328, 45.614, 46.282,
        46.28,
    }.Reverse().ToArray();

  private static readonly double[] Prices3 = new[]
  {
        46.28, 46.28, 45.61, 46.03, 45.89, 46.08, 45.84, 45.42, 45.10, 44.83,
        44.33, 43.61, 44.15, 44.09, 44.34,
    }.Reverse().ToArray();

  private static readonly double[] Prices2 = new[]
  {
        70.369, 70.001, 69.745, 68.204, 67.923, 67.878, 67.99, 67.232, 66.935,
        67.642, 68.199, 67.642, 67.385, 66.973, 67.113, 67.592, 66.743, 66.494,
        65.585, 65.91, 65.795,
    }.Reverse().ToArray();

  [Fact]
  public void RsiSeries_returns_a_null_padded_rsi_series()
  {
    double[] data = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

    double?[] expected = Enumerable.Repeat<double?>(null, 14)
        .Concat(Enumerable.Repeat<double?>(100.0, 86))
        .ToArray();
    Assert.Equal(expected, Rsi.RsiSeries(data, 14));
  }

  [Fact]
  public void RsiSeries_returns_empty_for_empty_input_and_nulls_for_flat_data()
  {
    Assert.Empty(Rsi.RsiSeries(Array.Empty<double>(), 14));
    double?[] expected = { null, null, null, null, null };
    Assert.Equal(expected, Rsi.RsiSeries(new[] { 5.0, 5, 5, 5, 5 }, 3));
  }

  [Fact]
  public void RsiSeries_agrees_with_the_scalar_at_the_latest_point()
  {
    double?[] series = Rsi.RsiSeries(Prices3, 14);
    Assert.True(Math.Abs(series[^1]!.Value - Rsi.Latest(Prices3)!.Value) < 1.0e-9);
  }

  [Fact]
  public void Latest_returns_the_wilder_smoothed_rsi()
  {
    // Wilder-smoothed (TA-Lib) values, cross-checked against the
    // reference implementation.
    Assert.Equal(70.4641, Round(Rsi.Latest(Prices3), 4));
    Assert.Equal(77.0633, Round(Rsi.Latest(Prices2), 4));
    Assert.Equal(29.5353, Round(Rsi.Latest(Prices), 4));
    Assert.Null(Rsi.Latest(Array.Empty<double>(), 14));
    Assert.Null(Rsi.Latest(new[] { 1.0, 2, 3 }, 14));
    // A flat window has no relative strength: RSI is undefined.
    Assert.Null(Rsi.Latest(new[] { 5.0, 5, 5, 5, 5 }, 4));
  }

  [Fact]
  public void Periods_must_be_positive_integers()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Rsi.Latest(Prices, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => Rsi.RsiSeries(new[] { 1.0, 2, 3 }, -1));
  }

  [Fact]
  public void AverageLoss_returns_the_wilder_smoothed_average_loss()
  {
    Assert.Equal(10.0, Round(Rsi.AverageLoss(new[] { 40.0, 30, 20, 10 }, 3), 4));
    Assert.Null(Rsi.AverageLoss(new[] { 1.0, 2, 3 }));
    Assert.Null(Rsi.AverageLoss(Array.Empty<double>()));
    Assert.Equal(0.2069, Round(Rsi.AverageLoss(Prices), 4));
    Assert.Equal(1.0, Round(Rsi.AverageLoss(new[] { 4.0, 3, 2, 1 }, 2), 4));
    // A single price has no change to average.
    Assert.Null(Rsi.AverageLoss(new[] { 5.0 }, 3));
  }

  [Fact]
  public void AverageGain_returns_the_wilder_smoothed_average_gain()
  {
    Assert.Equal(10.0, Round(Rsi.AverageGain(new[] { 0.0, 10, 20, 30 }, 3), 4));
    Assert.Null(Rsi.AverageGain(Array.Empty<double>()));
    Assert.Equal(0.0867, Round(Rsi.AverageGain(Prices), 4));
    Assert.Equal(1.0, Round(Rsi.AverageGain(new[] { 1.0, 2, 3, 4 }, 2), 4));
    // A single price has no change to average.
    Assert.Null(Rsi.AverageGain(new[] { 5.0 }, 3));
  }

  [Fact]
  public void Gain_sums_the_positive_changes()
  {
    Assert.Equal(30.0, Rsi.Gain(new[] { 0.0, 10, 20, 30 }));
    Assert.Equal(0.0, Rsi.Gain(Array.Empty<double>()));
    Assert.Equal(3.0, Rsi.Gain(new[] { 1.0, 2, 3, 4 }));
    Assert.Equal(3.34, Round(Rsi.Gain(Prices3), 2));
  }

  [Fact]
  public void Loss_sums_the_negative_changes()
  {
    Assert.Equal(30.0, Rsi.Loss(new[] { 40.0, 30, 20, 10 }));
    Assert.Equal(0.0, Rsi.Loss(new[] { 1.0, 2, 3 }));
    Assert.Equal(0.0, Rsi.Loss(Array.Empty<double>()));
    Assert.Equal(3.0, Rsi.Loss(new[] { 4.0, 3, 2, 1 }));
    Assert.Equal(1.4, Round(Rsi.Loss(Prices3), 2));
  }

  [Fact]
  public void Streaming_matches_the_doctest_walk()
  {
    var (state, values) = Rsi.RsiInit(new[] { 1.0, 2, 3, 4, 5 }, 3);
    double?[] expected = { null, null, null, 100.0, 100.0 };
    Assert.Equal(expected, values);
    Assert.Equal(100.0, Rsi.RsiNext(state, 6));
  }

  [Fact]
  public void Streaming_init_replays_the_batch_series_and_next_continues_it()
  {
    // Init on the first eight points must equal the batch series
    // exactly, and stepping the remaining points must continue it
    // exactly — the bit-for-bit streaming contract.
    double[] history = { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

    CheckStreaming(history, 3, Rsi.RsiInit, Rsi.RsiNext, Rsi.RsiSeries);
  }

  [Fact]
  public void Streaming_init_throws_on_short_history()
  {
    Assert.Throws<ArgumentException>(() => Rsi.RsiInit(new[] { 1.0, 2 }, 3));
    Assert.Throws<ArgumentException>(() => Rsi.RsiInit(new[] { 1.0, 2, 3 }, 3));
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

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

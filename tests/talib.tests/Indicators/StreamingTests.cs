using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

// The streaming contract: warm up on the first 300 points, stream the
// remaining 20, and the full streamed series must equal the batch series
// over all 320 points bit for bit — exact double equality, nulls
// included. Fixtures are deterministic (a smooth trend with jitter for
// price-based indicators, LCG bars for the bar-based ones), ported from
// the Elixir reference suite.
public class StreamingTests
{
  private static readonly double[] Prices =
      Enumerable.Range(0, 320).Select(i =>
          100.0 + 50.0 * Math.Sin(i * 0.03 + 1.3) + (i + 1) % 13 * 0.7 + i * 0.001).ToArray();

  private static readonly Bar[] Bars = MakeBars(320, 12_345);

  private static Bar[] MakeBars(int count, long seed)
  {
    long Next(ref long s)
    {
      s = (s * 1_103_515_245 + 12_345) % 2_147_483_648;
      return s;
    }

    var bars = new Bar[count];
    long s = seed;
    for (int i = 0; i < count; i++)
    {
      long u1 = Next(ref s);
      long u2 = Next(ref s);
      long u3 = Next(ref s);
      long u4 = Next(ref s);

      double open = 100.0 + u1 % 5001 / 10.0;
      double close = open + (u2 % 4001 - 2000) / 100.0;
      bars[i] = new Bar(
          Open: open,
          High: Math.Max(open, close) + u3 % 2001 / 100.0,
          Low: Math.Min(open, close) - u4 % 2001 / 100.0,
          Close: close,
          Volume: 0);
    }

    return bars;
  }

  private static void CheckStreamEqualsBatch<TState>(
      double[] values,
      Func<double[], (TState State, double?[] Values)> init,
      Func<TState, double, double?> next,
      double?[] batch,
      int split = 300)
  {
    var (state, warmup) = init(values[..split]);
    Assert.Equal(batch[..split], warmup);

    var streamed = values[split..].Select(price => next(state, price)).ToArray();
    Assert.Equal(batch[split..], streamed);
  }

  private static void CheckStreamEqualsBatch<TState>(
      Bar[] values,
      Func<Bar[], (TState State, double?[] Values)> init,
      Func<TState, Bar, double?> next,
      double?[] batch,
      int split = 300)
  {
    var (state, warmup) = init(values[..split]);
    Assert.Equal(batch[..split], warmup);

    var streamed = values[split..].Select(bar => next(state, bar)).ToArray();
    Assert.Equal(batch[split..], streamed);
  }

  [Fact]
  public void Streaming_continues_the_batch_series_bit_for_bit()
  {
    CheckStreamEqualsBatch(Prices, h => Ma.SmaInit(h, 14), Ma.SmaNext, Ma.SmaSeries(Prices, 14));
    CheckStreamEqualsBatch(Prices, h => Ma.SmaInit(h, 1), Ma.SmaNext, Ma.SmaSeries(Prices, 1));
    CheckStreamEqualsBatch(Prices, h => Ma.EmaInit(h, 12), Ma.EmaNext, Ma.EmaSeries(Prices, 12));
    CheckStreamEqualsBatch(Prices, h => Ma.WmaInit(h, 7), Ma.WmaNext, Ma.WmaSeries(Prices, 7));
    CheckStreamEqualsBatch(Prices, h => Ma.RmaInit(h, 9), Ma.RmaNext, Ma.RmaSeries(Prices, 9));
    CheckStreamEqualsBatch(
        Prices, h => StdDev.StdDevInit(h, 20), StdDev.StdDevNext, StdDev.StdDevSeries(Prices, 20));
    CheckStreamEqualsBatch(Prices, h => Rsi.RsiInit(h, 14), Rsi.RsiNext, Rsi.RsiSeries(Prices, 14));
    CheckStreamEqualsBatch(
        Prices, h => WilliamsR.WilliamsRInit(h, 14), WilliamsR.WilliamsRNext,
        WilliamsR.WilliamsRSeries(Prices, 14));
    CheckStreamEqualsBatch(
        Prices, h => Stochastic.KInit(h, 14), Stochastic.KNext, Stochastic.KSeries(Prices, 14));
    CheckStreamEqualsBatch(Bars, b => Atr.AtrInit(b, 14), Atr.AtrNext, Atr.AtrSeries(Bars, 14));
  }

  [Fact]
  public void Streaming_bbands_continues_the_batch_series_bit_for_bit()
  {
    var (state, warmup) = Bbands.BbandsInit(Prices[..300], 20, 2.0);
    var batch = Bbands.BbandsSeries(Prices, 20, 2.0);
    Assert.Equal(batch[..300], warmup);

    var streamed = Prices[300..].Select(price => Bbands.BbandsNext(state, price)).ToArray();
    Assert.Equal(batch[300..], streamed);
  }

  [Fact]
  public void Streaming_donchian_continues_the_batch_series_bit_for_bit()
  {
    var (state, warmup) = Donchian.DonchianInit(Bars[..300], 20);
    var batch = Donchian.DonchianSeries(Bars, 20);
    Assert.Equal(batch[..300], warmup);

    var streamed = Bars[300..].Select(bar => Donchian.DonchianNext(state, bar)).ToArray();
    Assert.Equal(batch[300..], streamed);
  }

  [Fact]
  public void Streaming_stochastic_slow_continues_the_batch_series_bit_for_bit()
  {
    var (state, values) = Stochastic.SlowInit(Prices[..300], 14, 5, 3);
    var ext = Prices[300..].Select(price => Stochastic.SlowNext(state, price)).ToArray();

    // The streamed %K and %D must equal the batch composition exactly:
    // %K = SMA(slowing) of the fast %K values, %D = SMA(dPeriod) of %K.
    var k = values.K.Concat(ext.Select(e => e.K));
    var d = values.D.Concat(ext.Select(e => e.D));

    var expectedK = Ma.SmaSeries(Defined(Stochastic.KSeries(Prices, 14)), 5);
    var expectedD = Ma.SmaSeries(Defined(expectedK), 3);

    Assert.Equal(Defined(expectedK), Defined(k));
    Assert.Equal(Defined(expectedD), Defined(d));
  }

  [Fact]
  public void Streaming_macd_continues_the_batch_series_bit_for_bit()
  {
    // MACD warms up on the first 200 points (it needs slow + signal
    // bars); the streamed line must equal the batch line exactly, the
    // signal must equal the batch EMA(9) of the line, and the last
    // streamed point must equal MACDFIX (the streaming convention).
    var (state, values) = Macd.MacdInit(Prices[..200], 12, 26, 9);
    var ext = Prices[200..].Select(price => Macd.MacdNext(state, price)).ToArray();

    var macd = values.Macd.Concat(ext.Select(e => e.Macd)).ToArray();
    var signal = values.Signal.Concat(ext.Select(e => e.Signal)).ToArray();
    var histogram = values.Histogram.Concat(ext.Select(e => e.Histogram)).ToArray();

    var batchLine = Macd.MacdSeries(Prices, 12, 26);
    Assert.Equal(batchLine, macd);

    var expectedSignal = Ma.EmaSeries(Defined(batchLine), 9);
    Assert.Equal(Defined(expectedSignal), Defined(signal));

    var fix = Macd.MacdFix(Prices, 12, 26, 9);
    Assert.Equal(fix.Macd!.Value, macd[^1]!.Value);
    Assert.Equal(fix.Signal!.Value, signal[^1]!.Value);
    Assert.Equal(fix.Histogram!.Value, histogram[^1]!.Value);
  }

  [Fact]
  public void Insufficient_history_throws_ArgumentException()
  {
    Assert.Throws<ArgumentException>(() => Ma.SmaInit(Enumerable.Range(1, 4).Select(i => (double)i).ToArray(), 5));
    Assert.Throws<ArgumentException>(() => Rsi.RsiInit(Enumerable.Range(1, 14).Select(i => (double)i).ToArray(), 14));
    Assert.Throws<ArgumentException>(() => Atr.AtrInit(Bars[..14], 14));
    Assert.Throws<ArgumentException>(() => Macd.MacdInit(Enumerable.Range(1, 30).Select(i => (double)i).ToArray(), 12, 26, 9));
    Assert.Throws<ArgumentException>(() => Stochastic.SlowInit(new double[] { 1, 2, 3, 4 }, 3, 2, 2));
  }

  [Fact]
  public void Invalid_periods_throw_ArgumentOutOfRangeException()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Ma.SmaInit(new[] { 1.0, 2, 3 }, 0));
    Assert.Throws<ArgumentOutOfRangeException>(() => StdDev.StdDevInit(new[] { 1.0, 2, 3 }, -1));
  }

  [Fact]
  public void Flat_windows_K_and_R_are_null_and_smoothers_skip()
  {
    // Rising, then one pop, then perfectly flat: %K defined at the
    // pop, undefined (null) while flat — and the slow smoothers skip
    // the flat bars exactly like the batch walk does.
    double[] prices =
        Enumerable.Range(1, 20).Select(i => (double)i)
            .Append(30)
            .Concat(Enumerable.Repeat(30.0, 15))
            .ToArray();

    CheckStreamEqualsBatch(
        prices, h => Stochastic.KInit(h, 5), Stochastic.KNext, Stochastic.KSeries(prices, 5), split: 30);
    CheckStreamEqualsBatch(
        prices, h => WilliamsR.WilliamsRInit(h, 5), WilliamsR.WilliamsRNext,
        WilliamsR.WilliamsRSeries(prices, 5), split: 30);

    var (state, values) = Stochastic.SlowInit(prices[..30], 5, 3, 3);
    var ext = prices[30..].Select(price => Stochastic.SlowNext(state, price)).ToArray();

    var k = values.K.Concat(ext.Select(e => e.K));
    var d = values.D.Concat(ext.Select(e => e.D));

    var expectedK = Ma.SmaSeries(Defined(Stochastic.KSeries(prices, 5)), 3);
    var expectedD = Ma.SmaSeries(Defined(expectedK), 3);

    Assert.Equal(Defined(expectedK), Defined(k));
    Assert.Equal(Defined(expectedD), Defined(d));
  }

  [Fact]
  public void Rsi_returns_null_on_a_window_with_no_movement_at_all()
  {
    double[] prices =
        Enumerable.Range(1, 20).Select(i => (double)i)
            .Concat(Enumerable.Repeat(20.0, 15))
            .ToArray();

    CheckStreamEqualsBatch(prices, h => Rsi.RsiInit(h, 5), Rsi.RsiNext, Rsi.RsiSeries(prices, 5), split: 30);
  }

  [Fact]
  public void Equal_extremes_earliest_tie_wins_like_the_batch_window_walk()
  {
    // Two equal highs inside the window; the streaming deque keeps
    // the earliest occurrence, like the batch window walk.
    double[] prices = { 3, 9, 9, 4, 5, 6, 9, 2, 7, 9, 1, 8, 9, 9, 4, 6, 9, 3, 5, 9, 2, 8, 9, 4 };

    CheckStreamEqualsBatch(
        prices, h => Stochastic.KInit(h, 4), Stochastic.KNext, Stochastic.KSeries(prices, 4), split: 20);
    CheckStreamEqualsBatch(
        prices, h => WilliamsR.WilliamsRInit(h, 4), WilliamsR.WilliamsRNext,
        WilliamsR.WilliamsRSeries(prices, 4), split: 20);
  }

  private static double[] Defined(IEnumerable<double?> values) =>
      values.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
}

using System.Diagnostics;
using Talib;
using Talib.Indicators;

namespace Talib.Bench;

// Streaming benchmark: the per-tick cost of the Init/Next API vs
// recomputing the batch series — the C# twin of the Elixir project's
// bench/bench_stream.exs. Reports per-tick microseconds for the best of
// 5 folds of 10k ticks after a 100k-point warmup, next to the batch
// cost of the same series (what a per-tick recompute would pay) and the
// one-time Init warmup.
internal static class BenchStream
{
  public static void Run(int size)
  {
    const int ticks = 10_000;

    double[] prices = Benching.MakePrices(size + ticks);
    Bar[] bars = Benching.MakeBars(prices);
    double[] historyPrices = prices[..size];
    Bar[] historyBars = bars[..size];
    double[] tickPrices = prices[size..];
    Bar[] tickBars = bars[size..];

    var streams = new (string Label, Action Warm, Action Fold)[]
    {
        ("Ma.SmaNext (20)", () => Ma.SmaInit(historyPrices, 20), Fold(Ma.SmaInit(historyPrices, 20).State, tickPrices, (s, p) => Ma.SmaNext(s, p))),
        ("Ma.EmaNext (20)", () => Ma.EmaInit(historyPrices, 20), Fold(Ma.EmaInit(historyPrices, 20).State, tickPrices, (s, p) => Ma.EmaNext(s, p))),
        ("Ma.WmaNext (20)", () => Ma.WmaInit(historyPrices, 20), Fold(Ma.WmaInit(historyPrices, 20).State, tickPrices, (s, p) => Ma.WmaNext(s, p))),
        ("Ma.RmaNext (20)", () => Ma.RmaInit(historyPrices, 20), Fold(Ma.RmaInit(historyPrices, 20).State, tickPrices, (s, p) => Ma.RmaNext(s, p))),
        ("StdDev.StdDevNext (20)", () => StdDev.StdDevInit(historyPrices, 20), Fold(StdDev.StdDevInit(historyPrices, 20).State, tickPrices, (s, p) => StdDev.StdDevNext(s, p))),
        ("Bbands.BbandsNext (20,2)", () => Bbands.BbandsInit(historyPrices, 20, 2), Fold(Bbands.BbandsInit(historyPrices, 20, 2).State, tickPrices, (s, p) => Bbands.BbandsNext(s, p))),
        ("Rsi.RsiNext (14)", () => Rsi.RsiInit(historyPrices, 14), Fold(Rsi.RsiInit(historyPrices, 14).State, tickPrices, (s, p) => Rsi.RsiNext(s, p))),
        ("Atr.AtrNext (14) [bars]", () => Atr.AtrInit(historyBars, 14), Fold(Atr.AtrInit(historyBars, 14).State, tickBars, (s, b) => Atr.AtrNext(s, b))),
        ("Macd.MacdNext (12/26/9)", () => Macd.MacdInit(historyPrices, 12, 26, 9), Fold(Macd.MacdInit(historyPrices, 12, 26, 9).State, tickPrices, (s, p) => Macd.MacdNext(s, p))),
        ("Stochastic.KNext (14)", () => Stochastic.KInit(historyPrices, 14), Fold(Stochastic.KInit(historyPrices, 14).State, tickPrices, (s, p) => Stochastic.KNext(s, p))),
        ("Stochastic.SlowNext (14,5,3)", () => Stochastic.SlowInit(historyPrices, 14, 5, 3), Fold(Stochastic.SlowInit(historyPrices, 14, 5, 3).State, tickPrices, (s, p) => Stochastic.SlowNext(s, p))),
        ("WilliamsR.WilliamsRNext (14)", () => WilliamsR.WilliamsRInit(historyPrices, 14), Fold(WilliamsR.WilliamsRInit(historyPrices, 14).State, tickPrices, (s, p) => WilliamsR.WilliamsRNext(s, p))),
        ("Donchian.DonchianNext (20) [bars]", () => Donchian.DonchianInit(historyBars, 20), Fold(Donchian.DonchianInit(historyBars, 20).State, tickBars, (s, b) => Donchian.DonchianNext(s, b))),
        ("Aroon.AroonNext (14)", () => Aroon.AroonInit(historyPrices, 14), Fold(Aroon.AroonInit(historyPrices, 14).State, tickPrices, (s, p) => Aroon.AroonNext(s, p))),
    };

    foreach (var (label, warm, fold) in streams)
    {
      warm();
      fold();

      double bestUs = double.MaxValue;
      for (int r = 0; r < 5; r++)
      {
        warm();
        long t0 = Stopwatch.GetTimestamp();
        fold();
        long t1 = Stopwatch.GetTimestamp();
        bestUs = Math.Min(bestUs, (t1 - t0) * 1_000_000.0 / Stopwatch.Frequency / ticks);
      }

      Console.WriteLine($"{label}\tper-tick-us\t{Math.Round(bestUs, 4)}");
    }

    Console.WriteLine();

    // What a per-tick recompute would pay: the full batch series at
    // `size`, and the one-time warmup cost of Init.
    var recomputes = new (string Label, Action Run)[]
    {
        ("Ma.SmaSeries (20)", () => Ma.SmaSeries(historyPrices, 20)),
        ("Bbands.BbandsSeries (20,2)", () => Bbands.BbandsSeries(historyPrices, 20, 2)),
        ("Rsi.RsiSeries (14)", () => Rsi.RsiSeries(historyPrices, 14)),
        ("Atr.AtrSeries (14) [bars]", () => Atr.AtrSeries(historyBars, 14)),
        ("Macd.MacdFix (12/26/9)", () => Macd.MacdFix(historyPrices, 12, 26, 9)),
        ("Donchian.DonchianSeries (20) [bars]", () => Donchian.DonchianSeries(historyBars, 20)),
        ("Aroon.AroonSeries (14)", () => Aroon.AroonSeries(historyPrices, 14)),
    };

    foreach (var (label, run) in recomputes)
    {
      Console.WriteLine($"{label}\trecompute-ms\t{Math.Round(Benching.BestMs(5, run), 3)}");
    }

    Console.WriteLine();

    var inits = new (string Label, Action Run)[]
    {
        ("Ma.SmaInit (20)", () => Ma.SmaInit(historyPrices, 20)),
        ("Rsi.RsiInit (14)", () => Rsi.RsiInit(historyPrices, 14)),
        ("Macd.MacdInit (12/26/9)", () => Macd.MacdInit(historyPrices, 12, 26, 9)),
        ("Stochastic.SlowInit (14,5,3)", () => Stochastic.SlowInit(historyPrices, 14, 5, 3)),
    };

    foreach (var (label, run) in inits)
    {
      Console.WriteLine($"{label}\twarmup-ms\t{Math.Round(Benching.BestMs(5, run), 3)}");
    }
  }

  // The fold over `ticks` inputs through a warmed state — the per-tick
  // body. State is threaded by mutation, the Elixir twin threads it
  // through the reduce; the arithmetic is identical.
  private static Action Fold<TState, TInput>(TState state, TInput[] inputs, Action<TState, TInput> next) => () =>
  {
    foreach (TInput input in inputs)
    {
      next(state, input);
    }
  };
}

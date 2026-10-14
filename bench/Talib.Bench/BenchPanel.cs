using System.Collections.Concurrent;
using System.Diagnostics;
using Talib;
using Talib.Indicators;

namespace Talib.Bench;

// Concurrency panel benchmark: one "symbol" = the 8-indicator set below
// computed over one 100k-point series; the workload is 16 such symbols.
// It measures sequential vs Parallel.ForEach scaling on .NET, next to
// the identical panel run against the TA-Lib Python bindings
// (sequential / threads / processes) by bench/bench_panel.py — the C#
// twin of the Elixir project's bench/bench_panel.exs.
internal static class BenchPanel
{
  public static void Run(int seriesCount)
  {
    const int size = 100_000;

    var series = new double[seriesCount][];
    for (int s = 1; s <= seriesCount; s++)
    {
      series[s - 1] = Enumerable.Range(0, size)
          .Select(i => 100 + 50 * Math.Sin(i * 0.03 + s) + (i + s) % 13 * 0.7 + i * 0.001)
          .ToArray();
    }

    // The panel: the same jobs as the single-function table, per symbol.
    void Panel(double[] prices)
    {
      Bar[] bars = Benching.MakeBars(prices);

      Ma.SmaSeries(prices, 20);
      Ma.EmaSeries(prices, 20);
      Rsi.RsiSeries(prices, 14);
      Macd.MacdFix(prices, 12, 26, 9);
      Bbands.BbandsSeries(prices, 20, 2);
      Atr.AtrSeries(bars, 14);
      Ht.TrendlineSeries(prices);
      Mama.MamaSeries(prices, 0.5, 0.05);
    }

    int workers = Environment.ProcessorCount;

    void RunSequential()
    {
      foreach (double[] prices in series)
      {
        Panel(prices);
      }
    }

    void RunParallel()
    {
      Parallel.ForEach(
          series,
          new ParallelOptions { MaxDegreeOfParallelism = workers },
          Panel);
    }

    static double Median(int runs, Action action)
    {
      var times = new List<double>();
      for (int r = 0; r < runs; r++)
      {
        long t0 = Stopwatch.GetTimestamp();
        action();
        long t1 = Stopwatch.GetTimestamp();
        times.Add((t1 - t0) * 1000.0 / Stopwatch.Frequency);
      }

      times.Sort();
      return times[times.Count / 2];
    }

    // Warmup once, then the median of repeated runs.
    RunSequential();

    double seq = Median(3, RunSequential);
    double par = Median(3, RunParallel);

    Console.WriteLine($"panel-sequential\t{seriesCount}x{size}\t{Math.Round(seq, 1)}");
    Console.WriteLine($"panel-parallel\t{seriesCount}x{size}\t{Math.Round(par, 1)}");
    Console.WriteLine($"workers\t{workers}");
    Console.WriteLine($"speedup\t{Math.Round(seq / par, 2)}");
  }
}

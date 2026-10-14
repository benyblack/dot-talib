using System.Diagnostics;
using Talib;
using Talib.Indicators;

namespace Talib.Bench;

// Shared fixture builders and timing helpers, mirroring the Elixir
// project's bench/bench.exs: deterministic prices by the same formula,
// bars derived by a fixed rule, wall-clock best-of-N after warmup.
internal static class Benching
{
  public static double[] MakePrices(int size) =>
      Enumerable.Range(0, size)
          .Select(i => 100 + 50 * Math.Sin(i * 0.03) + i % 13 * 0.7 + i * 0.001)
          .ToArray();

  public static Bar[] MakeBars(double[] prices) =>
      prices
          .Select((p, i) => new Bar(
              Open: p - 0.4 - i % 3 * 0.1,
              High: p + 1 + i % 5 * 0.3,
              Low: p - 1 - (i + 2) % 4 * 0.25,
              Close: p,
              Volume: 1000 + 100 * (i % 5)))
          .ToArray();

  // One warmup pass, then the best (minimum) of `runs` timed passes, in
  // milliseconds — a min is what a clean cache-warm run achieves; the
  // Elixir harness reports the same statistic.
  public static double BestMs(int runs, Action action)
  {
    action();

    double best = double.MaxValue;
    for (int r = 0; r < runs; r++)
    {
      long t0 = Stopwatch.GetTimestamp();
      action();
      long t1 = Stopwatch.GetTimestamp();
      best = Math.Min(best, (t1 - t0) * 1000.0 / Stopwatch.Frequency);
    }

    return best;
  }

  public static void Emit(string name, int size, double ms) =>
      Console.WriteLine($"{name}\t{size}\t{Math.Round(ms, 3)}");
}

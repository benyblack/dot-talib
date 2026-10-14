using Talib;
using Talib.Indicators;

namespace Talib.Bench;

// Batch benchmark: wall-clock timings for representative functions on
// deterministic data — the C# twin of the Elixir project's
// bench/bench.exs (same jobs, same fixtures, same best-of-N statistic).
internal static class BenchBatch
{
  public static void Run(int size)
  {
    double[] prices = Benching.MakePrices(size);
    Bar[] bars = Benching.MakeBars(prices);

    var jobs = new (string Name, Action Run)[]
    {
        ("Ma.SmaSeries (20)", () => Ma.SmaSeries(prices, 20)),
        ("Ma.EmaSeries (20)", () => Ma.EmaSeries(prices, 20)),
        ("Ma.WmaSeries (20)", () => Ma.WmaSeries(prices, 20)),
        ("Ma.DemaSeries (20)", () => Ma.DemaSeries(prices, 20)),
        ("Ma.KamaSeries (20)", () => Ma.KamaSeries(prices, 20)),
        ("Ma.MidpointSeries (20)", () => Ma.MidpointSeries(prices, 20)),
        ("Rsi.RsiSeries (14)", () => Rsi.RsiSeries(prices, 14)),
        ("Macd.MacdFix (12/26/9)", () => Macd.MacdFix(prices, 12, 26, 9)),
        ("Bbands.BbandsSeries (20,2)", () => Bbands.BbandsSeries(prices, 20, 2)),
        ("StdDev.StdDevSeries (20)", () => StdDev.StdDevSeries(prices, 20)),
        ("Atr.AtrSeries (14) [bars]", () => Atr.AtrSeries(bars, 14)),
        ("Obv.ObvSeries [bars]", () => Obv.ObvSeries(bars)),
        ("Stochastic.Slow (14,3,3)", () => Stochastic.Slow(prices, 14, 3, 3)),
        ("WilliamsR.WilliamsRSeries (14)", () => WilliamsR.WilliamsRSeries(prices, 14)),
        ("Aroon.AroonSeries (14)", () => Aroon.AroonSeries(prices, 14)),
        ("Cci.CciSeries (20) [bars]", () => Cci.CciSeries(bars, 20)),
        ("Donchian.DonchianSeries (20) [bars]", () => Donchian.DonchianSeries(bars, 20)),
        ("StochRsi.StochRsiSeries (14,5)", () => StochRsi.StochRsiSeries(prices, 14, 5)),
        ("Cdl.Doji [bars]", () => Cdl.Doji(bars)),
        ("Cdl.Engulfing [bars]", () => Cdl.Engulfing(bars)),
        ("Cdl.ThreeWhiteSoldiers [bars]", () => Cdl.ThreeWhiteSoldiers(bars)),
        ("Ht.TrendlineSeries", () => Ht.TrendlineSeries(prices)),
        ("Ht.DcPhaseSeries", () => Ht.DcPhaseSeries(prices)),
        ("Mama.MamaSeries (0.5,0.05)", () => Mama.MamaSeries(prices, 0.5, 0.05)),
    };

    int runs = size >= 100_000 ? 5 : 20;
    foreach (var (name, run) in jobs)
    {
      Benching.Emit(name, size, Benching.BestMs(runs, run));
    }

    // The candle-context observation: one Scan shares the context (and
    // its trailing sums) across all 61 patterns; calling the patterns
    // one by one rebuilds it per pattern.
    double scan = Benching.BestMs(runs, () => Cdl.Scan(bars));
    double individual = Benching.BestMs(runs, () =>
    {
      foreach (string name in Cdl.PatternNames)
      {
        Cdl.Scan(bars, new[] { name });
      }
    });

    Console.WriteLine($"Cdl.Scan (all 61)\t{size}\t{Math.Round(scan, 3)}");
    Console.WriteLine($"Cdl.Scan (61x individually)\t{size}\t{Math.Round(individual, 3)}");
  }
}

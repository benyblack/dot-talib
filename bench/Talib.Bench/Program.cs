// Benchmark harness. Three dependency-free modes mirror the Elixir
// project's bench/ scripts (same jobs, same fixtures, same statistics);
// with no mode argument, the BenchmarkDotNet suite runs instead:
//
//     dotnet run --project bench/Talib.Bench -c Release -- bench [size]
//     dotnet run --project bench/Talib.Bench -c Release -- stream [size]
//     dotnet run --project bench/Talib.Bench -c Release -- panel [series-count]

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Talib;
using Talib.Indicators;

if (args.Length > 0 && args[0] is "bench" or "stream" or "panel")
{
  int size = args.Length > 1 && args[0] != "panel" ? int.Parse(args[1]) : 100_000;
  switch (args[0])
  {
    case "bench":
      Talib.Bench.BenchBatch.Run(size);
      break;
    case "stream":
      Talib.Bench.BenchStream.Run(size);
      break;
    case "panel":
      Talib.Bench.BenchPanel.Run(args.Length > 1 ? int.Parse(args[1]) : 16);
      break;
  }

  return 0;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
return 0;

[MemoryDiagnoser]
public class BatchBenchmarks
{
  private double[] _prices = null!;
  private Bar[] _bars = null!;

  [GlobalSetup]
  public void Setup()
  {
    int n = 100_000;
    _prices = new double[n];
    for (int i = 0; i < n; i++)
    {
      _prices[i] = 100 + 50 * Math.Sin(i * 0.03) + i % 13 * 0.7 + i * 0.001;
    }

    _bars = new Bar[n];
    for (int i = 0; i < n; i++)
    {
      double p = _prices[i];
      _bars[i] = new Bar(
          Open: p - 0.4 - i % 3 * 0.1,
          High: p + 1 + i % 5 * 0.3,
          Low: p - 1 - (i + 2) % 4 * 0.25,
          Close: p,
          Volume: 1000 + 100 * (i % 5));
    }
  }

  [Benchmark]
  public double?[] BatchSma() => Ma.SmaSeries(_prices, 20);

  [Benchmark]
  public double?[] BatchEma() => Ma.EmaSeries(_prices, 20);

  [Benchmark]
  public double?[] BatchRsi() => Rsi.RsiSeries(_prices, 14);

  [Benchmark]
  public double?[] BatchStdDev() => StdDev.StdDevSeries(_prices, 20);

  [Benchmark]
  public double?[] BatchMacdLine() => Macd.MacdSeries(_prices, 12, 26);

  [Benchmark]
  public Bbands.BbandsResult?[] BatchBbands() => Bbands.BbandsSeries(_prices, 20, 2);

  [Benchmark]
  public Stochastic.StochResult BatchStochSlow() => Stochastic.SlowSeries(_prices, 14, 5, 3);

  [Benchmark]
  public double?[] BatchAtr() => Atr.AtrSeries(_bars, 14);

  [Benchmark]
  public Donchian.DonchianResult?[] BatchDonchian() => Donchian.DonchianSeries(_bars, 20);
}

// The per-tick cost of the streaming API: warm up once from history,
// then advance 1000 prices per invocation — divide by 1000 for the
// per-tick figure.
[MemoryDiagnoser]
public class StreamingBenchmarks
{
  private const int Ticks = 1000;

  private double[] _history = null!;
  private double[] _ahead = null!;
  private Bar[] _barHistory = null!;
  private Bar[] _barAhead = null!;

  private Ma.SmaState _sma = null!;
  private Ma.EmaState _ema = null!;
  private Rsi.RsiState _rsi = null!;
  private Atr.AtrState _atr = null!;
  private Macd.MacdState _macd = null!;

  [GlobalSetup]
  public void Setup()
  {
    int n = 100_000;
    var prices = new double[n];
    for (int i = 0; i < n; i++)
    {
      prices[i] = 100 + 50 * Math.Sin(i * 0.03) + i % 13 * 0.7 + i * 0.001;
    }

    _history = prices[..^Ticks];
    _ahead = prices[^Ticks..];

    _barHistory = new Bar[_history.Length];
    _barAhead = new Bar[Ticks];
    Bar MakeBar(double p, int i) => new(
        Open: p - 0.4 - i % 3 * 0.1,
        High: p + 1 + i % 5 * 0.3,
        Low: p - 1 - (i + 2) % 4 * 0.25,
        Close: p,
        Volume: 1000 + 100 * (i % 5));
    for (int i = 0; i < _history.Length; i++)
    {
      _barHistory[i] = MakeBar(_history[i], i);
    }

    for (int i = 0; i < Ticks; i++)
    {
      _barAhead[i] = MakeBar(_ahead[i], _history.Length + i);
    }

    _sma = Ma.SmaInit(_history, 20).State;
    _ema = Ma.EmaInit(_history, 20).State;
    _rsi = Rsi.RsiInit(_history, 14).State;
    _atr = Atr.AtrInit(_barHistory, 14).State;
    _macd = Macd.MacdInit(_history, 12, 26, 9).State;
  }

  [Benchmark]
  public double StreamSma1000Ticks()
  {
    double last = 0;
    foreach (double price in _ahead)
    {
      last = Ma.SmaNext(_sma, price) ?? last;
    }

    return last;
  }

  [Benchmark]
  public double StreamEma1000Ticks()
  {
    double last = 0;
    foreach (double price in _ahead)
    {
      last = Ma.EmaNext(_ema, price) ?? last;
    }

    return last;
  }

  [Benchmark]
  public double StreamRsi1000Ticks()
  {
    double last = 0;
    foreach (double price in _ahead)
    {
      last = Rsi.RsiNext(_rsi, price) ?? last;
    }

    return last;
  }

  [Benchmark]
  public double StreamAtr1000Ticks()
  {
    double last = 0;
    foreach (Bar bar in _barAhead)
    {
      last = Atr.AtrNext(_atr, bar) ?? last;
    }

    return last;
  }

  [Benchmark]
  public double StreamMacd1000Ticks()
  {
    double last = 0;
    foreach (double price in _ahead)
    {
      last = Macd.MacdNext(_macd, price).Macd ?? last;
    }

    return last;
  }
}

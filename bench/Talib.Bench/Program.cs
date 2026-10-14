// Performance benchmarks: batch series at 100k points and the O(1)
// streaming steps, on the same deterministic fixture shape the audit
// uses. Run with:
//
//     dotnet run --project bench/Talib.Bench -c Release
//     dotnet run --project bench/Talib.Bench -c Release -- --filter *Streaming*

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Talib;
using Talib.Indicators;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

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

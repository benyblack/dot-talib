# Talib (dot-talib)

Technical analysis library for C# / .NET, with TA-Lib-compatible values.

This is a revival of a 2019-era sketch, rebuilt on the conventions proven
by the [Elixir sister library](https://github.com/benyblack/talib) — whose
algorithms were verified value-by-value against the real TA-Lib C
library — and this port re-runs that audit against the C library
directly (see [Audit](#audit)). **All 133 TA-Lib functions are
covered** — see [PARITY.md](PARITY.md) for the function-by-function map
with each function's agreement figure.

## Indicators

- Overlap studies: SMA, EMA (SMA-seeded), WMA, Wilder RMA, DEMA, TEMA,
  TRIMA, KAMA, T3, Midpoint, Bollinger Bands, Donchian channels
  (+ Midprice), Parabolic SAR (+ extended), Keltner channels, the
  generic `MovingAverage` dispatcher and variable-period MAVP
- Momentum: RSI (Wilder), MACD (line/signal/histogram, `MacdFix`,
  `MacdExt`), Stochastic (fast and slow), Williams %R,
  ROC/ROCP/ROCR/ROCR100, Momentum, CMO (Wilder), APO, PPO, Aroon
  (+ oscillator), StochRSI, TRIX, ADX/DMI (+DI, -DI, DX, ADXR, raw DM),
  BOP, CCI, Ultimate Oscillator
- Volatility: population StdDev, Variance, ATR (+ NATR, True Range)
- Volume: OBV, MFI, Chaikin A/D line and oscillator, rolling VWAP
- Statistics: linear regression (value/slope/intercept/angle), time
  series forecast, Beta (returns-based), Correlation
- Price transform: average, median, typical, weighted close
- Cycles: the full Hilbert-transform family (trendline, DC period/phase,
  phasor, sine, trend mode) and MAMA/FAMA
- Candlestick patterns: all 61 TA-Lib CDL functions via `Cdl`, scored
  per bar (+100 bullish / -100 bearish / 0, with TA-Lib's ±200 hikkake
  confirmations and ±80 equal-touch levels), integer-exact against the
  C library; `Cdl.Scan` scores many patterns over one shared context

Each family comes as a batch scalar (`Latest` — the value at the newest
point), a batch series (`...Series` — aligned with the input,
`null`-padded during lookback) and, for 14 families, an O(1)
[streaming](#streaming) state.

## Installation

The package is not on NuGet (yet). Reference the project directly:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/dot-talib/src/talib/talib.csproj" />
</ItemGroup>
```

Requires `net10.0`.

## Conventions

- Prices and bars are always given in **chronological order, oldest
  first**.
- Series functions return arrays aligned with the input, `null`-padded
  while the lookback window fills; scalar functions return the value at
  the latest point.
- Every function returns `null` — never `0` — when there is not enough
  data, since `0` is a legitimate reading for several indicators.
- Lookback periods must be positive integers; anything else throws
  `ArgumentOutOfRangeException`.
- Bar-based indicators take `Talib.Bar` — a
  `readonly record struct Bar(double Open, double High, double Low,
  double Close, double Volume)` — and read only the fields they need.

## Examples

```csharp
using Talib;
using Talib.Indicators;

double[] prices = LoadPrices(); // oldest first

Ma.Sma(prices, 20);                          // scalar: latest SMA(20)
Ma.SmaSeries(prices, 20);                    // series: [null, .., smas]
Rsi.Latest(prices, 14);                      // Wilder RSI
Macd.MacdAll(prices);                        // line + signal + histogram

var bars = LoadBars();                       // Bar[] oldest first
Atr.Latest(bars, 14);
var channels = Donchian.Latest(bars, 20);    // .Upper / .Middle / .Lower
```

## Streaming

Batch functions walk their whole input — the wrong shape for a live
feed. Fourteen families (the MAs, StdDev, Bollinger Bands, ATR, RSI,
MACD, Stochastic fast and slow, Williams %R, Donchian, Aroon) also come
as O(1) streaming state: warm up once from history, then advance one
bar at a time.

```csharp
// Warm up from history; values equals the batch series exactly
var (state, values) = Ma.SmaInit(history, 20);

// One new price, one constant-time step — ~4 ns, zero allocation
double? sma = Ma.SmaNext(state, price);
```

Keep the state in your feed handler and advance it as ticks arrive; the
states are small mutable objects, documented as not thread-safe. Because
`Init` replays the batch arithmetic operation for operation, the
streamed series continues the batch series **bit for bit** — the same
values the audit checked against the C library. `StreamingTests`
asserts that equality exactly, per family, including flat windows and
equal-extreme ties.

## Audit

`bench/Talib.AuditDump` writes deterministic fixtures and every
function's output to files; `bench/compare_audit.py` runs the same rows
through the real TA-Lib C library (via its Python bindings) and compares
value by value:

```
dotnet run --project bench/Talib.AuditDump -c Release -- tmp 500 42
python bench/compare_audit.py tmp
```

Verified against TA-Lib 0.8.2: **bit-exact** for SMA, EMA, DEMA, TEMA,
the MACD line, BBands middle, fast %K, slow %K/%D, Williams %R,
Midpoint, OBV, both Donchian channels, CMO, APO, PPO, TRIX, SAR,
Keltner middle, the ADX family's latest values, BOP, AD, the median /
typical / weighted-close price transforms and Beta; **max relative
error from 1.5e-14 (WMA) to 4.3e-12 (StdDev/CCI)** for the rest. The
streaming rows match with the same figures as their batch twins. The
MACD line and stochastic rows differ only in leading alignment: this
library defines outputs from the bar where the math starts, so some
rows begin earlier than TA-Lib's unified outputs; on the shared bars
the values agree exactly.

The audit also catches inherited divergences from the Elixir reference
(whose audit never dumped those functions): CMO and SAR in 0.3.0, and
Beta (TA-Lib regresses returns, not prices) and the Chaikin oscillator
(EMA over the cumulative A/D line, not the flows) in 0.4.0 — all four
rewritten here from the C source and verified (see PARITY.md,
"Divergences from the Elixir reference"). Since 0.5.0 the audit dumps
every one of the 133 functions, including all 61 candlestick patterns
(integer-exact) and the full Hilbert family (trendline bit-exact, the
rest within 1.3e-13).

## Benchmarks

`bench/Talib.Bench` (BenchmarkDotNet), 100k points, .NET 10:

| Batch (100k points) | Median | | Streaming (per 1000 ticks) | Median |
|---|---:|---|---|---:|
| SMA(20) | 215 µs | | SMA | 3.7 µs |
| EMA(20) | 284 µs | | EMA | 3.2 µs |
| StdDev(20) | 430 µs | | RSI | 5.2 µs |
| RSI(14) | 596 µs | | ATR | 4.7 µs |
| MACD line | 616 µs | | MACD | 4.2 µs |
| Stoch slow | 1,430 µs | | | |
| ATR(14) | 941 µs | | | |
| BBands(20, 2) | 4,224 µs | | | |
| Donchian(20) | 4,788 µs | | | |

Streaming steps allocate nothing; batch series allocate their output
arrays (plus per-window records for the multi-output families).

## Roadmap

Full TA-Lib parity is done. Next candidates: publishing to NuGet, and
streaming state for more families (the Hilbert walk is inherently
stateful, so `Ht`/`Mama` streaming would be a natural fit). The Elixir
library also deserves the four upstream fixes this library already
applied — CMO, SAR, Beta and the Chaikin oscillator (see PARITY.md).
Contributions welcome.

## License

[MIT](LICENSE)

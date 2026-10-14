# Benchmarks

Talib (pure C#, .NET 10) versus TA-Lib (the C library through its
Python bindings, `ta-lib` 0.8.2), on identical deterministic data — the
C# twin of the Elixir project's `docs/BENCHMARKS.md`.

## Method

Both sides ran natively on the same Windows machine (24 logical
processors), on 100,000 data points, the minimum across repeated best-of-5
runs (single-run readings on this host vary by up to ~2x with
background load; the tables take the cleanest reading seen). The harnesses are committed: `dotnet run --project
bench/Talib.Bench -c Release -- bench [size]` for the batch table, and
`bench/bench_c.py` driving the `talib` bindings over numpy arrays for
the C side. Bars (for the OHLC functions) derive from the same price
series by a fixed formula on both sides — the same fixtures the value
audit uses. The panel runs with Server GC enabled (the bench project
sets it), the throughput configuration for parallel work.

The comparison is a ballpark, not a controlled benchmark: TA-Lib
computes over contiguous arrays in hand-tuned C with no per-element
allocation, while Talib allocates its output arrays and (for the
multi-output families) per-window records. The column to watch is the
order of magnitude.

## Results (100k points, best-of-5 ms)

| Function | Talib (C#) | TA-Lib (C) | Ratio |
| --- | ---: | ---: | ---: |
| `Ma.SmaSeries` (20) | 0.183 | 0.079 | 2.3x |
| `Ma.EmaSeries` (20) | 0.200 | 0.137 | 1.5x |
| `Ma.WmaSeries` (20) | 0.118 | 0.083 | 1.4x |
| `Ma.DemaSeries` (20) | 0.440 | 0.149 | 3.0x |
| `Ma.KamaSeries` (20) | 0.266 | 0.296 | 0.9x |
| `Ma.MidpointSeries` (20) | 0.393 | 0.108 | 3.6x |
| `Rsi.RsiSeries` (14) | 0.455 | 0.192 | 2.4x |
| `Macd.MacdFix` (12/26/9) | 0.488 | 0.363 | 1.3x |
| `Bbands.BbandsSeries` (20, 2) | 0.533 | 0.451 | 1.2x |
| `StdDev.StdDevSeries` (20) | 0.361 | 0.220 | 1.6x |
| `Atr.AtrSeries` (14) | 0.674 | 0.080 | 8.4x |
| `Obv.ObvSeries` | 0.229 | 0.040 | 5.7x |
| `Stochastic.Slow` (14, 3, 3) | 1.227 | 0.896 | 1.4x |
| `WilliamsR.WilliamsRSeries` (14) | 0.551 | 0.161 | 3.4x |
| `Aroon.AroonSeries` (14) | 1.377 | 0.566 | 2.4x |
| `Cci.CciSeries` (20) | 0.957 | 0.974 | 1.0x |
| `StochRsi.StochRsiSeries` (14, 5) | 1.305 | 1.040 | 1.3x |
| `Cdl.Doji` | 5.601 | 0.147 | 38x |
| `Cdl.Engulfing` | 8.396 | 0.055 | 153x |
| `Cdl.ThreeWhiteSoldiers` | 7.200 | 0.430 | 17x |
| `Ht.TrendlineSeries` | 9.153 | 3.776 | 2.4x |
| `Ht.DcPhaseSeries` | 24.745 | 23.234 | 1.1x |
| `Mama.MamaSeries` (0.5, 0.05) | 4.040 | 3.089 | 1.3x |

(`Donchian.DonchianSeries` (20) runs at 1.268 ms; TA-Lib has no
Donchian function to compare against.)

Takeaways (0.6.0, the allocation-driven performance release):

- The single-pass numeric families land at **1-8x the C library** —
  Bollinger Bands, MACD and Stochastic within ~40%, KAMA and CCI at or
  *faster* than C. For context, the Elixir twin of this library —
  immutable linked lists — reports 25-100x on its own host.
- 0.6.0 removed the per-window heap allocations: the multi-output
  records (`BbandsResult`, `DonchianResult`, `AroonResult`,
  `KeltnerResult`, `StochPoint`) became `readonly record struct`s, the
  CDL candle context became one contiguous struct array with a reused
  scratch window, and Donchian walks the bars once instead of twice.
  Measured: Bollinger 1.9 -> 0.53 ms (**3.6x**), Donchian 2.0 -> 1.3 ms,
  Stochastic slow 1.9 -> 1.2 ms, and scoring all 61 patterns one by one
  1.08 s -> 0.52 s (**2x**) — every value unchanged (the full audit
  re-run passes bit for bit).
- A single candlestick pattern still pays ~17-153x: each call builds
  the whole candle context, which is the API's shape — `Cdl.Scan`
  shares one context across all 61 patterns: ~74 ms for the whole set
  at 100k bars (**~7x** the individual calls, after the 0.6.0 halving).
- The Hilbert/MESA family lands at 1.2-2.5x — its fixed-layout state
  survives the port with nothing but the output arrays allocated.
- Everything is linear in the input; nothing is quadratic. At typical
  interactive sizes (hundreds to a few thousand bars), every function
  returns in microseconds.

## Concurrency: the many-symbol panel

The single-function table is one call at a time, which hides a
runtime's strength: many independent computations at once. The panel
benchmark (`-- panel [series-count]` and `bench/bench_panel.py`)
computes an 8-indicator set (SMA, EMA, RSI, MACDFIX, Bollinger, ATR,
HT trendline, MAMA) over N independent 100k-point series — a "scan
many symbols" workload — sequentially and concurrently, on both sides
of the same machine (median of 3 runs, Server GC on the C# side):

| Runtime | Sequential | Concurrent | Speedup |
| --- | ---: | ---: | ---: |
| Talib (C#), 16 symbols | 369 ms | 65 ms (24 `Parallel.ForEach` workers) | 5.7x |
| TA-Lib via Python, 16 symbols | 352 ms | 218 ms (8 threads) | 1.6x |
| TA-Lib via Python, 16 symbols | 352 ms | 72 ms (8 processes) | 4.9x |

(Repeated C# runs land between 4x and 5.7x depending on host load;
the 64-symbol panel measured 4.1x before the 0.6.0 allocation cuts
and has only improved since. The .NET row comes with no pool to
manage and no IPC.)

Honest reading:

- .NET scales the panel with a one-line change (`Parallel.ForEach`)
  and no pool to manage — up to 5.7x at 16 symbols on 24 workers, with
  Server GC absorbing the allocation storm of 128 series-returning
  calls (workstation GC caps it at ~3.4x; the panel is memory-bound
  well before 24 workers — the 0.6.0 allocation cuts help precisely
  here).
- Python threads barely move the needle (the GIL holds the C calls
  back); Python processes scale, at the cost of a pool plus spawn
  discipline. Both Python rows include rebuilding each series from its
  seed inside the worker — the same methodology as the Elixir
  project's panel, which keeps process IPC small.
- The C library's per-computation speed still wins absolute wall
  time: 72 ms beats 102 ms. Concurrency narrows a gap that is already
  only ~2x on the numeric families.

## Streaming: O(1) per tick

Every batch function walks its whole input — the wrong shape for a
live feed, where each tick would recompute 100k points to learn one
number. The streaming API (`Ma.SmaInit` + `Ma.SmaNext` and friends
across 14 indicator families) carries only the rolling state, so a
tick costs constant time (`-- stream [size]`, best of 5 folds of 10k
ticks after a 100k-point warmup):

| Streaming `Next` | Per tick | Batch recompute per tick | Ratio |
| --- | ---: | ---: | ---: |
| `Ma.SmaNext` (20) | 0.009 µs | 0.198 ms | 22,500x |
| `Ma.EmaNext` (20) | 0.020 µs | — | — |
| `Ma.WmaNext` (20) | 0.022 µs | — | — |
| `Ma.RmaNext` (20) | 0.020 µs | — | — |
| `StdDev.StdDevNext` (20) | 0.038 µs | — | — |
| `Bbands.BbandsNext` (20, 2) | 0.048 µs | 0.572 ms | 11,900x |
| `Rsi.RsiNext` (14) | 0.043 µs | 0.517 ms | 12,100x |
| `Atr.AtrNext` (14) | 0.024 µs | 0.910 ms | 37,600x |
| `Macd.MacdNext` (12/26/9) | 0.068 µs | 0.570 ms | 8,400x |
| `Stochastic.KNext` (14) | 0.057 µs | — | — |
| `Stochastic.SlowNext` (14/5/3) | 0.107 µs | — | — |
| `WilliamsR.WilliamsRNext` (14) | 0.057 µs | — | — |
| `Donchian.DonchianNext` (20) | 0.071 µs | 0.974 ms | 13,700x |
| `Aroon.AroonNext` (14) | 0.080 µs | 0.980 ms | 12,300x |

The ratio column is what a per-tick recompute would pay: four to five
orders of magnitude more than the state update. Warm-up (`Init`) costs
one batch pass — it replays the same sliding walk, threading the state
alongside (1-11 ms at 100k in this run; the 0.6.0 struct records also
removed the per-step allocation from the multi-output streaming
families) — and because it replays the
batch arithmetic operation for operation, the streamed series
continues the batch series bit for bit: the test suite folds `Next`
over hundreds of bars and compares element-wise, and the value audit
runs every streaming family against the C library the same way (each
`stream_*` row is warmed on the first 80% of the fixture, then stepped
bar by bar).

## Numerical agreement with TA-Lib

Beyond timing, the audit verifies values against the real C library
(not just a transliteration): `bench/Talib.AuditDump` dumps every
function's output on shared fixtures (a smooth series, derived OHLC
bars and pseudo-random candle bars), and `bench/compare_audit.py`
runs the same rows through the C library and compares value by value.
As of 0.5.0 the audit covers **all 133 functions**:

- **Bit-exact (max rel err 0)**: SMA, EMA, DEMA, TEMA, the MACD line,
  `MacdFix`, BBands middle, fast %K, slow %K/%D, Williams %R,
  Midpoint, OBV, both Donchian channels, CMO, APO, PPO, TRIX, SAR,
  BOP, AD, the median/typical/weighted-close transforms, Beta,
  `Ht.TrendlineSeries` and `Ht.TrendModeSeries`.
- **Bit-near (max rel err ≤ 1e-11)**: WMA (1.5e-14), KAMA (1.6e-16),
  RSI (7.3e-14), Bollinger bands (1.3e-13), StdDev (4.3e-12), ATR
  (5.2e-16), Aroon (1.7e-16), StochRSI (2.1e-13), CCI (2.6e-12),
  ADOSC (4.9e-14), the statistics family (~1e-14), the Hilbert family
  (≤1.3e-13), MAMA/FAMA (6e-16) — all starting at TA-Lib's own bar
  offsets, unstable period 0.
- **All 61 candlestick patterns are integer-exact** bar for bar,
  including hikkake's `±200` confirmation levels and the `±80`
  equal-touch levels of engulfing/harami.
- **The streaming API matches the C library with the same figures as
  its batch twins**; RMA has no standalone C function and is covered
  by the streaming-equals-batch tests, which compare element-wise
  with exact equality.

The documented remaining differences are alignment, not values: our
`MacdSeries` defines the MACD line from bar `slow - 1` on (as soon as
both EMAs exist), while TA-Lib's MACD output begins at
`slow + signal - 2` — 8 extra leading values on our side, identical
thereafter; the stochastic rows differ the same way (2 bars).

## Reproducing

```sh
dotnet run --project bench/Talib.Bench -c Release -- bench 100000
dotnet run --project bench/Talib.Bench -c Release -- stream 100000
dotnet run --project bench/Talib.Bench -c Release -- panel 16
python bench/bench_c.py 100000
python bench/bench_panel.py 16
dotnet run --project bench/Talib.AuditDump -c Release -- /tmp 500 42
python bench/compare_audit.py /tmp
```

The last two write the fixtures and a dump of every Talib output; any
environment with the `ta-lib` Python bindings and numpy compares them
against the C library over the same files (see the method description
above).

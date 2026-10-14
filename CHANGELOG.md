# Changelog

All notable changes to this project are documented here. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.5.0] - 2026-10-14

The full-parity release: **133 of 133 TA-Lib functions**, every one
dumped by the audit and verified against the real C library.

### Added

- The Hilbert-transform family (`Ht`): trendline, DC period, DC phase,
  phasor, sine (+ lead) and trend mode — the MESA core ported stage for
  stage (multistage WMA warmup, odd/even transformer stages,
  smooth-price ring, scan/align warmup suppression), with every constant
  computed by the reference's exact expression shape.
- MAMA/FAMA (`Mama.MamaSeries`) with the fast/slow limit validation.
- All 61 candlestick patterns via `Cdl`, scored per bar with TA-Lib's
  levels — including the ±200 hikkake confirmations and ±80 equal-touch
  scores most reimplementations skip — plus `Cdl.Context`/`Cdl.Scan` for
  scoring many patterns over one shared candle context.
- 17 new tests (269 total). The audit now covers **every function**:
  all 61 patterns are integer-exact bar for bar on the pseudo-random
  candle fixture, HT trendline and trend mode are bit-exact, the rest
  of the Hilbert family within 1.3e-13, MAMA/FAMA at 6e-16.

### Fixed

- The CDL trailing-sum window slid one bar late (drifting the
  candle-setting averages); caught by the tolerance-sensitive patterns
  of the final pattern batch before release, not by the exemplar tests.

## [0.4.0] - 2026-10-14

The statistics-volume release: the moving-average family completed,
momentum, volume, statistics and price-transform groups at full TA-Lib
coverage — and the audit caught two more inherited divergences.

### Added

- Ma completion: TRIMA, KAMA, T3 (the reference's fixed smoothing
  constants, flat-window reset and coefficient order preserved), the
  generic `MovingAverage` dispatcher with a `MaType` enum, and MAVP.
- `Macd.MacdExt` (any MA type for the three legs; unlocks with the new
  averages).
- `Stats` (scalar-only, like the reference): Variance, the linear
  regression family (value/slope/intercept/angle), TSF, Beta,
  Correlation.
- `PriceTransform`: average, median, typical and weighted-close prices.
- Volume and momentum: rolling VWAP (beyond the TA-Lib list), MFI,
  Chaikin A/D line and oscillator, BOP, CCI, Ultimate Oscillator.
- 59 new tests (248 total); the audit gains 24 rows — everything now
  dumped passes against the C library: bit-exact for BOP, AD,
  MEDPRICE/TYPPRICE/WCLPRICE and Beta; 1.6e-16 (KAMA) to 4.3e-12 (CCI,
  StdDev) elsewhere; MACDEXT ~1e-12 on WMA legs; the stats last values
  ~1e-14.

### Changed

- **Beta now follows the TA-Lib C library** (`ta_BETA.c`, ported
  operation for operation): the regression runs over per-bar returns
  with shift-anchored sliding sums and reseed triggers. The Elixir
  reference regresses raw prices — a different quantity (fixture golden
  0.6104 → 1.0037; degenerate windows read 0, the C library's defined
  value). The Elixir library is worth the same upstream fix.
- **The Chaikin oscillator now takes EMA(3) − EMA(10) over the
  cumulative A/D line** (the TA-Lib convention); the Elixir reference
  oscillates the per-bar flows (fixture golden 76.0517 → 56.9191).
- The compare script gained an absolute-error floor, so a 0 vs 5e-14
  value (float dust at an MFI turning point) no longer reads as a 100%
  error.

## [0.3.0] - 2026-10-14

The core-momentum release: nine more families, and the value audit —
extended to every new function — earned its keep by catching two real
divergences inherited from the Elixir reference.

### Added

- ADX/DMI family: +DI, -DI, DX, ADX, ADXR, the combined `AdxAll`, and
  the raw +DM/-DM series (scalar-only, mirroring the reference).
- Aroon (up/down, oscillator) with O(1) streaming state — the 14th
  streaming family — built on a new index-emitting `Window.Extremes`
  walk (earliest tie wins).
- Keltner channels (EMA ± mult·ATR), StochRSI, TRIX, CMO, APO, PPO,
  Parabolic SAR (+ extended), all batch scalar + series.
- 60 new tests (189 total), golden values ported from the Elixir suite
  or recorded directly from the TA-Lib C library.
- The audit now dumps and verifies 14 more rows (ADX family, Aroon
  batch + streaming, StochRSI, TRIX, CMO, APO, PPO, SAR, Keltner
  composed from EMA + ATR): all pass — bit-exact for CMO, APO, PPO,
  TRIX, SAR, Keltner middle and the ADX family's last values, ≤2.1e-13
  elsewhere (StochRSI), Aroon at 1.7e-16.

### Changed

- **CMO now follows the TA-Lib C library**: Wilder-smoothed average
  gains/losses (exactly `2 * RSI - 100`), not the textbook
  sliding-window sums. The Elixir reference diverges here — its audit
  never dumped CMO — and is worth the same upstream fix.
- **SAR now follows the TA-Lib C library** (`ta_SAR.c`): the initial
  trend comes from +DM/-DM between the first two bars (not closes), the
  first bar is consumed (its low/high is the second bar's SAR), stops
  are computed with a fused multiply-add and clamped with the C code's
  first-iteration convention, and reversals reset the acceleration to
  the start value. The Elixir reference diverges here too.

## [0.2.0] - 2026-10-14

The revival release. The library slept from 2019 (four indicators,
quadratic algorithms, a MACD whose signal and histogram were literally
`null`) until now; it is rebuilt on the conventions proven by the
[Elixir sister library](https://github.com/benyblack/talib), whose
algorithms were audited bit-for-bit against the TA-Lib C library, and
this port re-runs that audit against the real C library locally.

### Changed (breaking)

- Target framework `net10.0` (was `netstandard2.0`); nullable reference
  types and warnings-as-errors throughout.
- Complete API redesign around the Elixir library's conventions:
  chronological input (oldest first), series functions return
  `double?[]` aligned with the input and `null`-padded during lookback,
  scalars return `null` — never `0` — when there is not enough data, and
  invalid periods throw `ArgumentOutOfRangeException`. Bar-based
  indicators take a `Bar` record (open/high/low/close/volume).
- The 2019-era implementations (recursive EMA with a memoization
  dictionary keyed on array references, simple-average RSI, stochastic
  over a single price list with quadratic recomputation) are replaced by
  single-pass O(n) ports of the verified Elixir algorithms. Every
  floating-point expression keeps the reference operation order, so
  values match the Elixir library — and through it the C library —
  bit for bit where the reference does.

### Added

- Moving averages: SMA, EMA (SMA-seeded), WMA, Wilder RMA, DEMA, TEMA,
  Midpoint — scalar, series and streaming forms.
- Momentum: Wilder RSI (with average gain/loss), MACD (line, signal,
  histogram, `MacdAll`, `MacdFix`), Stochastic (fast `%K`/`%D` and
  slow), Williams %R, ROC/ROCP/ROCR/ROCR100, Momentum.
- Volatility: population StdDev, ATR (+ NATR, True Range).
- Overlap/volume: Bollinger Bands, Donchian channels (+ Midprice), OBV.
- Streaming API: O(1) `Init`/`Next` state for 13 families (the MAs,
  StdDev, BBands, RSI, ATR, MACD, fast %K, slow %K/%D, Williams %R,
  Donchian). `Init` replays the batch arithmetic operation for
  operation, so the streamed series continues the batch series bit for
  bit — asserted with exact equality in `StreamingTests` (129 tests
  total, golden values recorded from the TA-Lib C library).
- Value audit: `bench/Talib.AuditDump` dumps every function's output on
  deterministic fixtures, and `bench/compare_audit.py` (committed,
  unlike the Elixir repo's private script) runs the same rows through
  the real TA-Lib C library and compares value by value. Verified
  locally against TA-Lib 0.8.2: bit-exact for SMA, EMA, DEMA, TEMA, the
  MACD line, BBands middle, fast %K, slow %K/%D, Williams %R, Midpoint,
  OBV and both Donchian channels; max relative error from 1.5e-14 (WMA)
  to 4.3e-12 (StdDev, the same figure the Elixir library records); the
  MACD/stochastic rows differ only in leading alignment, documented in
  PARITY.md.
- Benchmarks: `bench/Talib.Bench` (BenchmarkDotNet) — batch series at
  100k points and streaming per-tick costs.
- Repository hygiene: MIT LICENSE file, .gitignore, .editorconfig,
  .gitattributes, GitHub Actions CI (build + test + format check on
  ubuntu and windows).

## [0.1.0] - 2019-06-12

Initial release: SMA, EMA, RSI, MACD and Stochastic.

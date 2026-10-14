# Changelog

All notable changes to this project are documented here. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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

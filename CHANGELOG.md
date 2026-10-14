# Changelog

All notable changes to this project are documented here. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the
project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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

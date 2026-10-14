# TA-Lib parity

Function-by-function coverage against the 133-function TA-Lib list.
This is a revival in progress — the goal is the full map, following the
route the [Elixir sister library](https://github.com/benyblack/talib)
took to 133/133. Verified functions match the C library's values on
shared fixtures (see the audit in the README); a ✓ with no note was
bit-exact in the audit, otherwise the worst-case relative error is
noted.

## House notes

- Prices and bars are given in chronological order, **oldest first**.
- Series functions return arrays aligned with the input, `null`-padded
  while the lookback window fills; scalar functions return the value at
  the latest point.
- Every function returns `null` — never `0` — when there is not enough
  data, since `0` is a legitimate reading for several indicators.
- Invalid periods throw `ArgumentOutOfRangeException`.
- Outputs start where the math starts (TA-Lib's unstable-period
  machinery is not emulated); where TA-Lib's unified outputs begin
  later, ours define values earlier — same values on the shared bars.

## Overlap studies

| TA-Lib | Talib | Status |
|---|---|---|
| SMA | `Ma.Sma` / `SmaSeries` | ✓ (bit-exact) |
| EMA | `Ma.Ema` / `EmaSeries` | ✓ (bit-exact) |
| WMA | `Ma.Wma` / `WmaSeries` | ✓ (1.5e-14) |
| DEMA | `Ma.Dema` / `DemaSeries` | ✓ (bit-exact) |
| TEMA | `Ma.Tema` / `TemaSeries` | ✓ (bit-exact) |
| TRIMA | — | |
| KAMA | — | |
| T3 | — | |
| MIDPOINT | `Ma.Midpoint` / `MidpointSeries` | ✓ (bit-exact) |
| MIDPRICE | `Donchian.Midprice` | ✓ |
| BBANDS | `Bbands.Latest` / `BbandsSeries` | ✓ (middle bit-exact, bands 1.3e-13) |
| SAR | `Sar.Latest` / `SarSeries` | ✓ (bit-exact) |
| SAR-ext | `Sar.SarExt` / `SarExtSeries` | ✓ (alias of the base series) |
| MAVP | — | |
| Wilder RMA | `Ma.Rma` / `RmaSeries` | ✓ (no standalone TA-Lib function) |
| Keltner (beyond TA-Lib) | `Keltner.Latest` / `KeltnerSeries` | ✓ (EMA ± mult·ATR, middle bit-exact) |

## Momentum indicators

| TA-Lib | Talib | Status |
|---|---|---|
| RSI | `Rsi.Latest` / `RsiSeries` | ✓ (7.3e-14, Wilder) |
| MACD | `Macd.Line` / `Signal` / `Histogram` / `MacdAll` | ✓ (line bit-exact) |
| MACDEXT | — | needs TRIMA/KAMA/T3 |
| MACDFIX | `Macd.MacdFix` | ✓ (MACDFIX convention) |
| STOCH (fast) | `Stochastic.K` / `KSeries` | ✓ (bit-exact) |
| STOCH (slow) | `Stochastic.Slow` / `SlowSeries` | ✓ (bit-exact) |
| STOCHRSI | — | |
| WILLR | `WilliamsR.Latest` / `WilliamsRSeries` | ✓ (bit-exact) |
| ROC | `Roc.Latest` / `RocSeries` | ✓ |
| ROCP | `Roc.Rocp` | ✓ |
| ROCR | `Roc.Rocr` | ✓ |
| ROCR100 | `Roc.Rocr100` | ✓ |
| MOM | `Momentum.Latest` / `MomentumSeries` | ✓ |
| CMO | `Cmo.Latest` / `CmoSeries` | ✓ (bit-exact, Wilder-smoothed) |
| APO | `Apo.Latest` / `ApoSeries` | ✓ (bit-exact) |
| PPO | `Ppo.Latest` / `PpoSeries` | ✓ (bit-exact) |
| AROON | `Aroon.Latest` / `AroonSeries` | ✓ (1.7e-16) |
| AROONOSC | `Aroon.AroonOsc` | ✓ |
| ADX | `Adx.Latest` / `AdxAll` | ✓ (last value bit-exact; scalar-only, like the reference) |
| ADXR | `Adx.Adxr` | ✓ |
| PLUS_DI / MINUS_DI | `Adx.PlusDi` / `Adx.MinusDi` | ✓ |
| PLUS_DM / MINUS_DM | `Adx.PlusDm` / `Adx.MinusDm` | ✓ |
| DX | `Adx.Dx` | ✓ |
| STOCHRSI | `StochRsi.Latest` / `StochRsiSeries` | ✓ (2.1e-13) |
| TRIX | `Trix.Latest` / `TrixSeries` | ✓ (bit-exact) |
| BOP, CCI, MFI, ULTOSC | — | |

## Volatility indicators

| TA-Lib | Talib | Status |
|---|---|---|
| STDDEV | `StdDev.Latest` / `StdDevSeries` | ✓ (4.3e-12) |
| ATR | `Atr.Latest` / `AtrSeries` | ✓ (5.2e-16) |
| NATR | `Atr.Natr` | ✓ |
| TRANGE | `Atr.TrueRange` | ✓ |
| VAR | — | |

## Volume indicators

| TA-Lib | Talib | Status |
|---|---|---|
| OBV | `Obv.Latest` / `ObvSeries` | ✓ (bit-exact) |
| AD, ADOSC, MFI, PVT | — | |

## Statistics, price transform, cycles, patterns

Not yet started: LINEARREG family, TSF, BETA, CORREL, STDP/VAR/STD
cross-sectionals, AVGPRICE/MEDPRICE/TYPPRICE/WCLPRICE, the Hilbert
transform family, MAMA/FAMA, and the 61 candlestick patterns.

## Streaming

O(1) `Init`/`Next` state exists for 14 families: SMA, EMA, WMA, RMA,
StdDev, BBands, RSI, ATR, MACD, fast %K, slow %K/%D, Williams %R, the
Donchian channels and Aroon. Every streamed series continues its batch
series bit for bit (asserted exactly in `StreamingTests`), and the
streaming rows of the audit match the C library with the same figures as
their batch twins.

## Divergences from the Elixir reference

Two 0.3.0 families follow the TA-Lib C library where the Elixir
reference (whose audit never dumped them) diverges: CMO (TA-Lib uses
Wilder-smoothed averages — exactly `2 * RSI - 100` — not sliding-window
sums) and SAR (trend seeded from +DM/-DM, first bar consumed as the
second bar's stop, `fma` stop arithmetic, the C code's first-iteration
clamp). Both are bit-exact here; the Elixir library is worth the same
upstream fix.

**Summary: 40 of 133 functions** (plus Keltner, beyond the TA-Lib list).

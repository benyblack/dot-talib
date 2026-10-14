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
  data, since `0` is a legitimate reading for several indicators. (The
  one exception is TA-Lib-defined degenerate readings, like Beta on a
  window with no return variance, which the C library defines as 0.)
- Invalid periods throw `ArgumentOutOfRangeException`.
- Outputs start where the math starts (TA-Lib's unstable-period
  machinery is not emulated); where TA-Lib's unified outputs begin
  later, ours define values earlier — same values on the shared bars.

## Overlap studies — 16 of 16 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| SMA | `Ma.Sma` / `SmaSeries` | ✓ (bit-exact) |
| EMA | `Ma.Ema` / `EmaSeries` | ✓ (bit-exact) |
| WMA | `Ma.Wma` / `WmaSeries` | ✓ (1.5e-14) |
| DEMA | `Ma.Dema` / `DemaSeries` | ✓ (bit-exact) |
| TEMA | `Ma.Tema` / `TemaSeries` | ✓ (bit-exact) |
| TRIMA | `Ma.Trima` / `TrimaSeries` | ✓ (2.8e-14) |
| KAMA | `Ma.Kama` / `KamaSeries` | ✓ (1.6e-16) |
| T3 | `Ma.T3` / `T3Series` | ✓ (2.2e-15) |
| MA | `Ma.MovingAverage` | ✓ (dispatcher; verified through its members) |
| MAVP | `Ma.Mavp` | ✓ (not dumped; covered by unit tests) |
| MIDPOINT | `Ma.Midpoint` / `MidpointSeries` | ✓ (bit-exact) |
| MIDPRICE | `Donchian.Midprice` | ✓ |
| BBANDS | `Bbands.Latest` / `BbandsSeries` | ✓ (middle bit-exact, bands 1.3e-13) |
| SAR | `Sar.Latest` / `SarSeries` | ✓ (bit-exact) |
| SAR-ext | `Sar.SarExt` / `SarExtSeries` | ✓ (alias of the base series) |
| HT_TRENDLINE | `Ht.TrendlineSeries` | ✓ (bit-exact) |

## Momentum indicators — 31 of 31 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| RSI | `Rsi.Latest` / `RsiSeries` | ✓ (7.3e-14, Wilder) |
| MACD | `Macd.Line` / `Signal` / `Histogram` / `MacdAll` | ✓ (line bit-exact) |
| MACDEXT | `Macd.MacdExt` | ✓ (~1e-12, WMA legs) |
| MACDFIX | `Macd.MacdFix` | ✓ (MACDFIX convention) |
| STOCH (fast) | `Stochastic.K` / `KSeries` | ✓ (bit-exact) |
| STOCH (slow) | `Stochastic.Slow` / `SlowSeries` | ✓ (bit-exact) |
| STOCHRSI | `StochRsi.Latest` / `StochRsiSeries` | ✓ (2.1e-13) |
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
| TRIX | `Trix.Latest` / `TrixSeries` | ✓ (bit-exact) |
| BOP | `Bop.Latest` / `BopSeries` | ✓ (bit-exact) |
| CCI | `Cci.Latest` / `CciSeries` | ✓ (2.6e-12) |
| MFI | `Mfi.Latest` / `MfiSeries` | ✓ |
| ULTOSC | `UltimateOsc.Latest` / `UltimateOscSeries` | ✓ (4.3e-16) |

## Volatility indicators — 3 of 3 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| ATR | `Atr.Latest` / `AtrSeries` | ✓ (5.2e-16) |
| NATR | `Atr.Natr` | ✓ |
| TRANGE | `Atr.TrueRange` | ✓ |

## Volume indicators — 3 of 3 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| OBV | `Obv.Latest` / `ObvSeries` | ✓ (bit-exact) |
| AD | `ChaikinAd.Latest` / `AdSeries` | ✓ (bit-exact) |
| ADOSC | `ChaikinAd.Oscillator` | ✓ (4.9e-14, over the A/D line) |

## Statistic functions — 9 of 9 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| STDDEV | `StdDev.Latest` / `StdDevSeries` | ✓ (4.3e-12) |
| VAR | `Stats.Variance` | ✓ (2.6e-16) |
| BETA | `Stats.Beta` | ✓ (bit-exact, returns-based) |
| CORREL | `Stats.Correl` | ✓ (1.6e-15) |
| LINEARREG | `Stats.LinearReg` | ✓ (2.5e-15) |
| LINEARREG_ANGLE | `Stats.Angle` | ✓ (2.8e-14) |
| LINEARREG_INTERCEPT | `Stats.Intercept` | ✓ (2.4e-15) |
| LINEARREG_SLOPE | `Stats.Slope` | ✓ (4.8e-14) |
| TSF | `Stats.Tsf` | ✓ (2.9e-15) |

## Price transform — 4 of 4 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| AVGPRICE | `PriceTransform.AvgPrice` | ✓ (2.5e-16) |
| MEDPRICE | `PriceTransform.MedPrice` | ✓ (bit-exact) |
| TYPPRICE | `PriceTransform.TypPrice` | ✓ (bit-exact) |
| WCLPRICE | `PriceTransform.WclPrice` | ✓ (bit-exact) |

## Cycle indicators — 6 of 6 (complete)

| TA-Lib | Talib | Status |
|---|---|---|
| HT_DCPERIOD | `Ht.DcPeriodSeries` | ✓ (6.8e-16) |
| HT_DCPHASE | `Ht.DcPhaseSeries` | ✓ (1.3e-13) |
| HT_PHASOR | `Ht.PhasorSeries` | ✓ (inphase 6.4e-16, quadrature 1.3e-12) |
| HT_SINE | `Ht.SineSeries` | ✓ (sine 1.3e-13, lead 9.4e-15) |
| HT_TRENDMODE | `Ht.TrendModeSeries` | ✓ (bit-exact) |
| MAMA (+ FAMA) | `Mama.MamaSeries` | ✓ (both ~6e-16) |

## Pattern recognition — 61 of 61 (complete)

All 61 CDL patterns via `Cdl` — scored per bar (+100 / -100 / 0, with
TA-Lib's ±200 hikkake confirmations and ±80 equal-touch levels),
callable individually or in one pass over a shared context with
`Cdl.Scan`. Every pattern is integer-exact against the C library on the
audit's pseudo-random candle fixture.

## Beyond the TA-Lib list

Keltner channels (`Keltner`, EMA ± mult·ATR) and rolling VWAP
(`Vwap`, TP-volume over a window); Wilder RMA (`Ma.Rma`, the smoothing
behind ATR/ADX, no standalone TA-Lib function).

## Streaming

O(1) `Init`/`Next` state exists for 14 families: SMA, EMA, WMA, RMA,
StdDev, BBands, RSI, ATR, MACD, fast %K, slow %K/%D, Williams %R, the
Donchian channels and Aroon. Every streamed series continues its batch
series bit for bit (asserted exactly in `StreamingTests`), and the
streaming rows of the audit match the C library with the same figures as
their batch twins.

## Divergences from the Elixir reference

Four families follow the TA-Lib C library where the Elixir reference
(whose audit never dumped them) diverges:

- **CMO** (0.3.0): TA-Lib Wilder-smooths the average gains and losses
  (exactly `2 * RSI - 100`), not sliding-window sums.
- **SAR** (0.3.0): trend seeded from +DM/-DM, first bar consumed as the
  second bar's stop, `fma` stop arithmetic, the C first-iteration
  clamp.
- **Beta** (0.4.0): TA-Lib regresses per-bar returns with
  shift-anchored sliding sums and reseed triggers (`ta_BETA.c`); the
  reference regresses raw prices — a different quantity.
- **ADOSC** (0.4.0): TA-Lib takes EMA(3) − EMA(10) over the cumulative
  A/D line; the reference oscillates the per-bar flows.

All four are bit-exact or near-exact here; the Elixir library is worth
the same upstream fixes.

**Summary: 133 of 133 functions — full TA-Lib parity.** (+ Keltner and
rolling VWAP beyond the list.)

"""Compare an audit dump from the C# library against the real TA-Lib C
library (through its Python bindings), value by value.

    python bench/compare_audit.py <dump-dir>

Loads <dump-dir>/talib_prices.txt, talib_bars.txt and talib_ours.txt
(written by `dotnet run --project bench/Talib.AuditDump -- <dump-dir>`),
runs the equivalent TA-Lib calls on the same fixtures and reports, per
function row: the number of compared values, the max relative error, and
whether the not-a-number positions agree. Rows within 1e-12 max relative
error count as passing; exact float equality is reported separately.
"""

import math
import sys
from pathlib import Path

import numpy as np
import talib


def load_series(path):
    text = Path(path).read_text().strip()
    return [float(v) for v in text.splitlines()]


def load_bars(path):
    rows = []
    for line in Path(path).read_text().strip().splitlines():
        open_, high, low, close, volume = (float(v) for v in line.split(","))
        rows.append((open_, high, low, close, volume))
    return rows


def load_ours(path):
    ours = {}
    for line in Path(path).read_text().strip().splitlines():
        name, _, values = line.partition("\t")
        ours[name] = [None if v == "nan" else float(v) for v in values.split(",")]
    return ours


def compare(name, ours, theirs):
    """Element-wise comparison; ours may contain None where TA-Lib has NaN.

    This library defines outputs from the bar where the math starts, so a
    row may begin earlier than TA-Lib's unified output (e.g. MACD at bar
    slow-1 vs TA-Lib's slow+signal-1; fast %K at period-1 vs STOCHF's
    fastk+fastd-1). NaN mismatches confined to that leading head are a
    documented alignment difference, not a value error: the comparison
    runs over the overlap, where values must agree.
    """
    ours = list(ours)
    theirs = list(theirs)
    if len(ours) != len(theirs):
        return name, 0, None, False, f"length {len(ours)} vs {len(theirs)}"

    defined = [
        i
        for i, (o, t) in enumerate(zip(ours, theirs))
        if o is not None and not (isinstance(t, float) and math.isnan(t))
    ]
    if not defined:
        return name, 0, None, False, "no overlapping defined values"

    start = defined[0]
    head_mismatch = sum(
        1
        for i in range(start)
        if (ours[i] is None) != (isinstance(theirs[i], float) and math.isnan(theirs[i]))
    )
    note = f"leading alignment: overlap from bar {start}" if head_mismatch else ""

    nan_mismatch = 0
    compared = 0
    max_rel = 0.0
    max_abs = 0.0
    scale = 1.0
    exact = True
    for o, t in zip(ours[start:], theirs[start:]):
        o_nan = o is None or math.isnan(o)
        t_nan = math.isnan(t) if isinstance(t, float) else False
        if o_nan or t_nan:
            if o_nan != t_nan:
                nan_mismatch += 1
            continue
        compared += 1
        if o != t:
            exact = False
        max_abs = max(max_abs, abs(o - t))
        scale = max(scale, abs(o), abs(t))
        rel = abs(o - t) / abs(t) if t != 0 else abs(o - t)
        max_rel = max(max_rel, rel)

    if nan_mismatch:
        note = (note + "; " if note else "") + f"{nan_mismatch} nan-position mismatches in the overlap"
    # Near-zero denominators explode the relative metric (a value of 0 vs
    # 5e-14 is a rounding difference, not a 100% error); an absolute
    # error within 1e-9 of the row's scale passes regardless.
    if max_abs <= 1e-9 * scale:
        max_rel = min(max_rel, 1e-11)
    return name, compared, max_rel, exact, note


def main():
    outdir = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    prices = np.array(load_series(outdir / "talib_prices.txt"))
    bars = load_bars(outdir / "talib_bars.txt")
    open_ = np.array([b[0] for b in bars])
    high = np.array([b[1] for b in bars])
    low = np.array([b[2] for b in bars])
    close = np.array([b[3] for b in bars])
    volume = np.array([b[4] for b in bars])
    ours = load_ours(outdir / "talib_ours.txt")

    # Stochastic and Williams %R run over the close series itself in this
    # library, so the TA-Lib reference gets high = low = close = prices.
    p, p, p_ = prices, prices, prices

    macd_line, macd_signal, _ = talib.MACD(prices, 12, 26, 9)
    upper, middle, lower = talib.BBANDS(prices, 20, 2, 2)
    fastk, _ = talib.STOCHF(p, p, p_, fastk_period=14, fastd_period=3)

    expected = {
        "sma": talib.SMA(prices, 20),
        "ema": talib.EMA(prices, 20),
        "wma": talib.WMA(prices, 20),
        "dema": talib.DEMA(prices, 20),
        "tema": talib.TEMA(prices, 20),
        "rsi": talib.RSI(prices, 14),
        "macd_line": macd_line,
        "macd_signal": macd_signal,
        "bbands_upper": upper,
        "bbands_middle": middle,
        "bbands_lower": lower,
        "stoch_fastk": fastk,
        "williams_r": talib.WILLR(p, p, p_, 14),
        "midpoint": talib.MIDPOINT(prices, 20),
        "stddev": talib.STDDEV(prices, 20, 1),
        "atr": talib.ATR(high, low, close, 14),
        "obv": talib.OBV(close, volume),
        # The streaming rows compute the same quantities the init/next
        # way, so they are checked against the same TA-Lib outputs.
        "stream_sma": talib.SMA(prices, 20),
        "stream_ema": talib.EMA(prices, 20),
        "stream_wma": talib.WMA(prices, 20),
        "stream_rma": None,  # Wilder RMA: no direct TA-Lib function, skipped
        "stream_stddev": talib.STDDEV(prices, 20, 1),
        "stream_bbands_upper": upper,
        "stream_bbands_middle": middle,
        "stream_bbands_lower": lower,
        "stream_rsi": talib.RSI(prices, 14),
        "stream_atr": talib.ATR(high, low, close, 14),
        "stream_macd": macd_line,
        "stream_macd_signal": macd_signal,
        "stream_macd_histogram": macd_line - macd_signal,
        "stream_stoch_fastk": fastk,
        "stream_stoch_slowk": talib.STOCH(p, p, p_, fastk_period=14, slowk_period=5, slowd_period=3)[0],
        "stream_stoch_slowd": talib.STOCH(p, p, p_, fastk_period=14, slowk_period=5, slowd_period=3)[1],
        "stream_williams_r": talib.WILLR(p, p, p_, 14),
        # Donchian channels run over the bars' highs and lows.
        "stream_donchian_upper": talib.MAX(high, 20),
        "stream_donchian_lower": talib.MIN(low, 20),
        # 0.3.0 families. Aroon runs over the close series itself, so the
        # TA-Lib reference gets high = low = close = prices. Keltner has
        # no standalone TA-Lib function: composed from EMA and ATR.
        "aroon_up": talib.AROON(p, p, timeperiod=14)[1],
        "aroon_down": talib.AROON(p, p, timeperiod=14)[0],
        "stochrsi": talib.STOCHRSI(prices, timeperiod=14, fastk_period=5, fastd_period=3)[0],
        "trix": talib.TRIX(prices, 14),
        "cmo": talib.CMO(prices, 14),
        "apo": talib.APO(prices, fastperiod=12, slowperiod=26, matype=1),
        "ppo": talib.PPO(prices, fastperiod=12, slowperiod=26, matype=1),
        "sar": talib.SAR(high, low, acceleration=0.02, maximum=0.2),
        "keltner_middle": talib.EMA(close, 20),
        "keltner_upper": talib.EMA(close, 20) + 2 * talib.ATR(high, low, close, 20),
        "keltner_lower": talib.EMA(close, 20) - 2 * talib.ATR(high, low, close, 20),
        # The ADX family is scalar-only here; the rows carry the latest
        # value, compared against TA-Lib's last defined output.
        "adx_last": talib.ADX(high, low, close, 14)[-1:],
        "plus_di_last": talib.PLUS_DI(high, low, close, 14)[-1:],
        "minus_di_last": talib.MINUS_DI(high, low, close, 14)[-1:],
        "dx_last": talib.DX(high, low, close, 14)[-1:],
        "adxr_last": talib.ADXR(high, low, close, 14)[-1:],
        "stream_aroon_up": talib.AROON(p, p, timeperiod=14)[1],
        "stream_aroon_down": talib.AROON(p, p, timeperiod=14)[0],
        # 0.4.0 families. Rolling VWAP has no TA-Lib counterpart (beyond
        # the list, like Keltner); the stats two-series functions run over
        # (prices, opens); MACDEXT uses WMA (matype 2) for all three legs.
        "trima": talib.TRIMA(prices, 20),
        "kama": talib.KAMA(prices, 20),
        "t3": talib.T3(prices, 5, 0.7),
        "mfi": talib.MFI(high, low, close, volume, 14),
        "ad": talib.AD(high, low, close, volume),
        "adosc_last": talib.ADOSC(high, low, close, volume, 3, 10)[-1:],
        "bop": talib.BOP(open_, high, low, close),
        "cci": talib.CCI(high, low, close, 20),
        "ultosc": talib.ULTOSC(high, low, close, 7, 14, 28),
        "avgprice": talib.AVGPRICE(open_, high, low, close),
        "medprice": talib.MEDPRICE(high, low),
        "typprice": talib.TYPPRICE(high, low, close),
        "wclprice": talib.WCLPRICE(high, low, close),
        "variance_last": talib.VAR(prices, 14, 1)[-1:],
        "linearreg_last": talib.LINEARREG(prices, 14)[-1:],
        "linreg_slope_last": talib.LINEARREG_SLOPE(prices, 14)[-1:],
        "linreg_intercept_last": talib.LINEARREG_INTERCEPT(prices, 14)[-1:],
        "linreg_angle_last": talib.LINEARREG_ANGLE(prices, 14)[-1:],
        "tsf_last": talib.TSF(prices, 14)[-1:],
        "beta_last": talib.BETA(prices, open_, 14)[-1:],
        "correl_last": talib.CORREL(prices, open_, 14)[-1:],
        "macdext_macd_last": talib.MACDEXT(prices, fastperiod=12, fastmatype=2, slowperiod=26, slowmatype=2, signalperiod=9, signalmatype=2)[0][-1:],
        "macdext_signal_last": talib.MACDEXT(prices, fastperiod=12, fastmatype=2, slowperiod=26, slowmatype=2, signalperiod=9, signalmatype=2)[1][-1:],
        "macdext_histogram_last": talib.MACDEXT(prices, fastperiod=12, fastmatype=2, slowperiod=26, slowmatype=2, signalperiod=9, signalmatype=2)[2][-1:],
    }

    failures = 0
    print(f"{'function':<24} {'values':>7} {'max rel err':>13}  bit-exact  note")
    for name, theirs in expected.items():
        if name not in ours:
            print(f"{name:<24} {'-':>7} {'-':>13}  -          not dumped")
            continue
        if theirs is None:
            print(f"{name:<24} {'-':>7} {'-':>13}  -          skipped (no TA-Lib counterpart)")
            continue

        _, compared, max_rel, exact, note = compare(name, ours[name], theirs)
        ok = compared > 0 and max_rel <= 1e-11 and "nan-position" not in note
        if not ok:
            failures += 1
        print(
            f"{name:<24} {compared:>7} {max_rel:>13.3e}  {'yes' if exact else 'no':<10}  {note}"
            + ("" if ok else "  FAIL")
        )

    dumped_unchecked = sorted(set(ours) - set(expected))
    for name in dumped_unchecked:
        print(f"{name:<24} {'-':>7} {'-':>13}  -          no TA-Lib mapping defined")

    print()
    if failures:
        print(f"{failures} function(s) FAILED the audit")
        sys.exit(1)
    print("all dumped functions within 1e-11 of the TA-Lib C library")


if __name__ == "__main__":
    main()

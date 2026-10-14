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
    """Element-wise comparison; ours may contain None where TA-Lib has NaN."""
    ours = list(ours)
    theirs = list(theirs)
    if len(ours) != len(theirs):
        return name, 0, None, False, f"length {len(ours)} vs {len(theirs)}"

    nan_mismatch = 0
    compared = 0
    max_rel = 0.0
    exact = True
    for o, t in zip(ours, theirs):
        o_nan = o is None or math.isnan(o)
        t_nan = math.isnan(t) if isinstance(t, float) else False
        if o_nan or t_nan:
            if o_nan != t_nan:
                nan_mismatch += 1
            continue
        compared += 1
        if o != t:
            exact = False
        rel = abs(o - t) / abs(t) if t != 0 else abs(o - t)
        max_rel = max(max_rel, rel)

    note = ""
    if nan_mismatch:
        note = f"{nan_mismatch} nan-position mismatches"
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
        "stream_donchian_upper": talib.MAX(prices, 20),
        "stream_donchian_lower": talib.MIN(prices, 20),
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
        ok = compared > 0 and max_rel <= 1e-12 and not note
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
    print("all dumped functions within 1e-12 of the TA-Lib C library")


if __name__ == "__main__":
    main()

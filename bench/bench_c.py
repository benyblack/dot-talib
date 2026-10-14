"""Batch benchmark: the TA-Lib C library side of the comparison.

Runs the same jobs as `dotnet run --project bench/Talib.Bench -- bench`
through the `talib` Python bindings over numpy arrays — the twin of the
Elixir project's Python bench script. Bars derive from the price series
by the same fixed formula as the C# harness.

    python bench_c.py [size]
"""

import math
import sys
import time

import numpy as np
import talib

size = int(sys.argv[1]) if len(sys.argv) > 1 else 100_000

prices = np.array(
    [100 + 50 * math.sin(i * 0.03) + (i % 13) * 0.7 + i * 0.001 for i in range(size)],
    dtype=np.double,
)
idx = np.arange(size)
high = prices + 1 + (idx % 5) * 0.3
low = prices - 1 - ((idx + 2) % 4) * 0.25
cdl_open = prices - 0.4 - (idx % 3) * 0.1
volume = np.array([1000 + 100 * (i % 5) for i in range(size)], dtype=np.double)

jobs = [
    ("Ma.SmaSeries (20)", lambda: talib.SMA(prices, 20)),
    ("Ma.EmaSeries (20)", lambda: talib.EMA(prices, 20)),
    ("Ma.WmaSeries (20)", lambda: talib.WMA(prices, 20)),
    ("Ma.DemaSeries (20)", lambda: talib.DEMA(prices, 20)),
    ("Ma.KamaSeries (20)", lambda: talib.KAMA(prices, 20)),
    ("Ma.MidpointSeries (20)", lambda: talib.MIDPOINT(prices, 20)),
    ("Rsi.RsiSeries (14)", lambda: talib.RSI(prices, 14)),
    ("Macd.MacdFix (12/26/9)", lambda: talib.MACDFIX(prices, 9)),
    ("Bbands.BbandsSeries (20,2)", lambda: talib.BBANDS(prices, 20, 2.0, 2.0)),
    ("StdDev.StdDevSeries (20)", lambda: talib.STDDEV(prices, 20, 1)),
    ("Atr.AtrSeries (14) [bars]", lambda: talib.ATR(high, low, prices, 14)),
    ("Obv.ObvSeries [bars]", lambda: talib.OBV(prices, volume)),
    ("Stochastic.Slow (14,3,3)", lambda: talib.STOCH(high, low, prices, 14, 3, 3)),
    ("WilliamsR.WilliamsRSeries (14)", lambda: talib.WILLR(high, low, prices, 14)),
    ("Aroon.AroonSeries (14)", lambda: talib.AROON(high, low, timeperiod=14)),
    ("Cci.CciSeries (20) [bars]", lambda: talib.CCI(high, low, prices, 20)),
    ("Donchian.DonchianSeries (20) [bars]", None),  # no TA-Lib counterpart
    ("StochRsi.StochRsiSeries (14,5)", lambda: talib.STOCHRSI(prices, timeperiod=14, fastk_period=5, fastd_period=3)),
    ("Cdl.Doji [bars]", lambda: talib.CDLDOJI(cdl_open, high, low, prices)),
    ("Cdl.Engulfing [bars]", lambda: talib.CDLENGULFING(cdl_open, high, low, prices)),
    ("Cdl.ThreeWhiteSoldiers [bars]", lambda: talib.CDL3WHITESOLDIERS(cdl_open, high, low, prices)),
    ("Ht.TrendlineSeries", lambda: talib.HT_TRENDLINE(prices)),
    ("Ht.DcPhaseSeries", lambda: talib.HT_DCPHASE(prices)),
    ("Mama.MamaSeries (0.5,0.05)", lambda: talib.MAMA(prices, 0.5, 0.05)),
]

runs = 5 if size >= 100_000 else 20
for name, job in jobs:
    if job is None:
        continue
    job()  # warmup
    best = min(
        (lambda t0=time.perf_counter(): (job(), time.perf_counter() - t0)[1])() * 1000
        for _ in range(runs)
    )
    print(f"{name}\t{size}\t{round(best, 3)}")

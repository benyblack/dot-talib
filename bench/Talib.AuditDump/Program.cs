// Value-audit fixtures and a dump of every library output, for comparison
// against the real TA-Lib C library through its Python bindings. See
// bench/compare_audit.py.
//
//     dotnet run --project bench/Talib.AuditDump -- <output-dir> [size] [seed]
//
// Writes into <output-dir>: talib_prices.txt (one price per line),
// talib_bars.txt (open,high,low,close,volume per line), talib_cdl_bars.txt
// (the LCG candlestick fixture, for the future CDL port) and talib_ours.txt
// (one `name<TAB>comma-separated values` line per function, "nan" for
// null). The Python side of the audit loads the same files, runs the
// equivalent TA-Lib calls and compares value by value.

using Talib;
using Talib.Indicators;

if (args.Length > 3)
{
  Console.Error.WriteLine("usage: Talib.AuditDump <output-dir> [size] [seed]");
  return 1;
}

string outdir = args.Length > 0 ? args[0] : ".";
int size = args.Length > 1 ? int.Parse(args[1]) : 500;
int seed = args.Length > 2 ? int.Parse(args[2]) : 42;

var prices = new double[size];
for (int i = 0; i < size; i++)
{
  prices[i] = 100 + 50 * Math.Sin(i * 0.03) + i % 13 * 0.7 + i * 0.001;
}

var bars = new Bar[size];
for (int i = 0; i < size; i++)
{
  double p = prices[i];
  bars[i] = new Bar(
      Open: p - 0.4 - i % 3 * 0.1,
      High: p + 1 + i % 5 * 0.3,
      Low: p - 1 - (i + 2) % 4 * 0.25,
      Close: p,
      Volume: 1000 + 100 * (i % 5));
}

Bar[] cdlBars = CdlBars(size, seed);

WriteLines(outdir, "talib_prices.txt", prices.Select(p => Fmt(p)));
WriteLines(outdir, "talib_bars.txt", bars.Select(BarLine));
WriteLines(outdir, "talib_cdl_bars.txt", cdlBars.Select(BarLine));

var rows = new List<(string Name, IEnumerable<double?> Values)>
{
    ("sma", Ma.SmaSeries(prices, 20)),
    ("ema", Ma.EmaSeries(prices, 20)),
    ("wma", Ma.WmaSeries(prices, 20)),
    ("dema", Ma.DemaSeries(prices, 20)),
    ("tema", Ma.TemaSeries(prices, 20)),
    ("rsi", Rsi.RsiSeries(prices, 14)),
    ("macd_line", Macd.MacdSeries(prices, 12, 26)),
    ("macd_signal", MacdSignal(prices)),
    ("bbands_upper", Bbands.BbandsSeries(prices, 20, 2).Select(b => (double?)b?.Upper)),
    ("bbands_middle", Bbands.BbandsSeries(prices, 20, 2).Select(b => (double?)b?.Middle)),
    ("bbands_lower", Bbands.BbandsSeries(prices, 20, 2).Select(b => (double?)b?.Lower)),
    ("stoch_fastk", Stochastic.KSeries(prices, 14)),
    ("williams_r", WilliamsR.WilliamsRSeries(prices, 14)),
    ("midpoint", Ma.MidpointSeries(prices, 20)),
    ("stddev", StdDev.StdDevSeries(prices, 20)),
    ("atr", Atr.AtrSeries(bars, 14)),
    ("obv", Obv.ObvSeries(bars)),
    ("aroon_up", Aroon.AroonSeries(prices, 14).Select(a => (double?)a?.Up)),
    ("aroon_down", Aroon.AroonSeries(prices, 14).Select(a => (double?)a?.Down)),
    ("stochrsi", StochRsi.StochRsiSeries(prices, 14, 5)),
    ("trix", Trix.TrixSeries(prices, 14)),
    ("cmo", Cmo.CmoSeries(prices, 14)),
    ("apo", Apo.ApoSeries(prices, 12, 26)),
    ("ppo", Ppo.PpoSeries(prices, 12, 26)),
    ("sar", Sar.SarSeries(bars, 0.02, 0.02, 0.2)),
    ("keltner_upper", Keltner.KeltnerSeries(bars, 20, 2).Select(k => (double?)k?.Upper)),
    ("keltner_middle", Keltner.KeltnerSeries(bars, 20, 2).Select(k => (double?)k?.Middle)),
    ("keltner_lower", Keltner.KeltnerSeries(bars, 20, 2).Select(k => (double?)k?.Lower)),
    // The ADX family is scalar-only in the reference library; each row
    // carries the latest value, compared against TA-Lib's last output.
    ("adx_last", new[] { Adx.Latest(bars, 14) }),
    ("plus_di_last", new[] { Adx.PlusDi(bars, 14) }),
    ("minus_di_last", new[] { Adx.MinusDi(bars, 14) }),
    ("dx_last", new[] { Adx.Dx(bars, 14) }),
    ("adxr_last", new[] { Adx.Adxr(bars, 14) }),
    // 0.4.0 families. Stats and MACDEXT are scalar-only in the reference
    // library (last-value rows); the stats two-series functions run over
    // the price fixture and the bars' opens.
    ("trima", Ma.TrimaSeries(prices, 20)),
    ("kama", Ma.KamaSeries(prices, 20)),
    ("t3", Ma.T3Series(prices, 5, 0.7)),
    ("mfi", Mfi.MfiSeries(bars, 14)),
    ("ad", ChaikinAd.AdSeries(bars)),
    ("adosc_last", new[] { ChaikinAd.Oscillator(bars, 3, 10) }),
    ("bop", Bop.BopSeries(bars)),
    ("cci", Cci.CciSeries(bars, 20)),
    ("ultosc", UltimateOsc.UltimateOscSeries(bars, 7, 14, 28)),
    ("avgprice", PriceTransform.AvgPriceSeries(bars).Select(v => (double?)v)),
    ("medprice", PriceTransform.MedPriceSeries(bars).Select(v => (double?)v)),
    ("typprice", PriceTransform.TypPriceSeries(bars).Select(v => (double?)v)),
    ("wclprice", PriceTransform.WclPriceSeries(bars).Select(v => (double?)v)),
    ("variance_last", new[] { Stats.Variance(prices, 14) }),
    ("linearreg_last", new[] { Stats.LinearReg(prices, 14) }),
    ("linreg_slope_last", new[] { Stats.Slope(prices, 14) }),
    ("linreg_intercept_last", new[] { Stats.Intercept(prices, 14) }),
    ("linreg_angle_last", new[] { Stats.Angle(prices, 14) }),
    ("tsf_last", new[] { Stats.Tsf(prices, 14) }),
    ("beta_last", new[] { Stats.Beta(prices, Opens(bars), 14) }),
    ("correl_last", new[] { Stats.Correl(prices, Opens(bars), 14) }),
    ("macdext_macd_last", new[] { Macd.MacdExt(prices, 12, 26, 9, MaType.Wma).Macd }),
    ("macdext_signal_last", new[] { Macd.MacdExt(prices, 12, 26, 9, MaType.Wma).Signal }),
    ("macdext_histogram_last", new[] { Macd.MacdExt(prices, 12, 26, 9, MaType.Wma).Histogram }),
};
rows.AddRange(StreamingRows(prices, bars));

string content = string.Join("\n", rows.Select(r => r.Name + "\t" + string.Join(",", r.Values.Select(Fmt)))) + "\n";
File.WriteAllText(Path.Combine(outdir, "talib_ours.txt"), content);

Console.WriteLine($"audit dump written to {outdir} ({size} bars, seed {seed})");
return 0;

// The MACD signal with the batch leading alignment: 25 leading nulls,
// then the EMA(9) of the defined part of the line.
static IEnumerable<double?> MacdSignal(double[] prices)
{
  double?[] line = Macd.MacdSeries(prices, 12, 26);
  double[] defined = line.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
  return Enumerable.Repeat<double?>(null, 25).Concat(Ma.EmaSeries(defined, 9));
}

static double[] Opens(Bar[] bars) => bars.Select(b => b.Open).ToArray();

// The streaming rows: identical quantities computed the Init + Next way —
// warm up on the first four fifths of the fixture, then one O(1) step per
// remaining bar — proving the streaming path emits exactly what the batch
// functions (and the C library) do.
static IEnumerable<(string Name, IEnumerable<double?> Values)> StreamingRows(double[] prices, Bar[] bars)
{
  int split = prices.Length / 5 * 4;

  IEnumerable<double?> Stream<TState, TInput>(
      TInput[] values,
      Func<TInput[], (TState State, double?[] Values)> init,
      Func<TState, TInput, double?> next)
  {
    var (state, warmup) = init(values[..split]);
    return warmup.Concat(values[split..].Select(v => next(state, v)));
  }

  var donchianWarm = Donchian.DonchianInit(bars[..split], 20);
  var donchian = donchianWarm.Values
      .Concat(bars[split..].Select(b => Donchian.DonchianNext(donchianWarm.State, b)))
      .ToArray();

  var aroon = Aroon.AroonInit(prices[..split], 14);
  var aroonExt = prices[split..].Select(p => Aroon.AroonNext(aroon.State, p)).ToArray();

  var bbands = Bbands.BbandsInit(prices[..split], 20, 2);
  var bbandsExt = prices[split..].Select(p => Bbands.BbandsNext(bbands.State, p)).ToArray();

  var macd = Macd.MacdInit(prices[..split], 12, 26, 9);
  var macdExt = prices[split..].Select(p => Macd.MacdNext(macd.State, p)).ToArray();

  var slow = Stochastic.SlowInit(prices[..split], 14, 5, 3);
  var slowExt = prices[split..].Select(p => Stochastic.SlowNext(slow.State, p)).ToArray();

  yield return ("stream_sma", Stream(prices, h => Ma.SmaInit(h, 20), Ma.SmaNext));
  yield return ("stream_ema", Stream(prices, h => Ma.EmaInit(h, 20), Ma.EmaNext));
  yield return ("stream_wma", Stream(prices, h => Ma.WmaInit(h, 20), Ma.WmaNext));
  yield return ("stream_rma", Stream(prices, h => Ma.RmaInit(h, 20), Ma.RmaNext));
  yield return ("stream_stddev", Stream(prices, h => StdDev.StdDevInit(h, 20), StdDev.StdDevNext));
  yield return ("stream_bbands_upper", bbands.Values.Select(b => (double?)b?.Upper).Concat(bbandsExt.Select(b => b?.Upper)));
  yield return ("stream_bbands_middle", bbands.Values.Select(b => (double?)b?.Middle).Concat(bbandsExt.Select(b => b?.Middle)));
  yield return ("stream_bbands_lower", bbands.Values.Select(b => (double?)b?.Lower).Concat(bbandsExt.Select(b => b?.Lower)));
  yield return ("stream_rsi", Stream(prices, h => Rsi.RsiInit(h, 14), Rsi.RsiNext));
  yield return ("stream_atr", Stream(bars, h => Atr.AtrInit(h, 14), Atr.AtrNext));
  yield return ("stream_macd", macd.Values.Macd.Concat(macdExt.Select(e => e.Macd)));
  yield return ("stream_macd_signal", macd.Values.Signal.Concat(macdExt.Select(e => e.Signal)));
  yield return ("stream_macd_histogram", macd.Values.Histogram.Concat(macdExt.Select(e => e.Histogram)));
  yield return ("stream_stoch_fastk", Stream(prices, h => Stochastic.KInit(h, 14), Stochastic.KNext));
  yield return ("stream_stoch_slowk", slow.Values.K.Concat(slowExt.Select(e => e.K)));
  yield return ("stream_stoch_slowd", slow.Values.D.Concat(slowExt.Select(e => e.D)));
  yield return ("stream_aroon_up", aroon.Values.Select(a => (double?)a?.Up).Concat(aroonExt.Select(a => a?.Up)));
  yield return ("stream_aroon_down", aroon.Values.Select(a => (double?)a?.Down).Concat(aroonExt.Select(a => a?.Down)));
  yield return ("stream_williams_r", Stream(prices, h => WilliamsR.WilliamsRInit(h, 14), WilliamsR.WilliamsRNext));
  yield return ("stream_donchian_upper", donchian.Select(d => (double?)d?.Upper));
  yield return ("stream_donchian_lower", donchian.Select(d => (double?)d?.Lower));
}

static Bar[] CdlBars(int size, int seed)
{
  long Lcg(long x) => (x * 1_103_515_245 + 12_345) % 2_147_483_648;

  var bars = new Bar[size];
  long x = seed;
  for (int i = 0; i < size; i++)
  {
    long oU = Lcg(x);
    long cU = Lcg(oU);
    long hU = Lcg(cU);
    long lU = Lcg(hU);
    x = lU;

    double open = 100.0 + oU % 5001 / 10.0;
    double close = open + (cU % 4001 - 2000) / 100.0;
    double bodyHigh = open >= close ? open : close;
    double bodyLow = open <= close ? open : close;

    bars[i] = new Bar(
        Open: open,
        High: bodyHigh + hU % 401 / 100.0,
        Low: bodyLow - lU % 401 / 100.0,
        Close: close,
        Volume: 1000);
  }

  return bars;
}

static string Fmt(double? x) => x is null ? "nan" : x.Value.ToString("R");

static string BarLine(Bar bar) =>
    string.Join(",", Fmt(bar.Open), Fmt(bar.High), Fmt(bar.Low), Fmt(bar.Close), Fmt(bar.Volume));

static void WriteLines(string outdir, string name, IEnumerable<string> lines) =>
    File.WriteAllText(Path.Combine(outdir, name), string.Join("\n", lines) + "\n");

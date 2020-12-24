using System.Linq;
using System.Collections.Generic;

namespace Talib.Indicators
{

    /// <summary>
    /// MACD indicator [Wikipedia](https://en.wikipedia.org/wiki/MACD)
    /// Calculate MACD, Signal line, and Histogram
    /// </summary>
    public class Macd
    {
        /// <summary>
        /// Calculate a single MACD
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="fast">Period of fast ema calculation</param>
        /// <param name="slow">Period of slow ema calculation</param>
        public static double? MacdSingle(double[] data, int fast = 12, int slow = 26)
        => MA.EmaSingle(data, fast) - MA.EmaSingle(data, slow);

        /// <summary>
        /// Calculate a single Signal value
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of ema calculation on macd line</param>
        /// <param name="macdFast">Period of fast ema calculation</param>
        /// <param name="macdSlow">Period of slow ema calculation</param>
        public static double? SignalSingle(double[] data, int period = 9, int macdFast = 12, int macdSlow = 26)
        {
            var macdList = new List<double>();
            for (var i = 0; i < data.Length; i++)
            {
                var selectedData = data.Take(data.Length - i).ToArray();
                var currentMacd = Macd.MacdSingle(selectedData, macdFast, macdSlow);
                if (!currentMacd.HasValue) break;
                macdList.Add(currentMacd.Value);
            }
            macdList.Reverse();
            return MA.EmaSingle(macdList.ToArray(), period);
        }

        /// <summary>
        /// Calculate a MACD Histogram value which is (MACD Line - Signal Line)
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="signalPeriod">Period of ema calculation on macd line</param>
        /// <param name="macdFast">Period of fast ema calculation</param>
        /// <param name="macdSlow">Period of slow ema calculation</param>
        /// <returns></returns>
        public static double? HistogramSingle(double[] data, int signalPeriod = 9, int macdFast = 12, int macdSlow = 26)
        => MacdSingle(data, macdFast, macdSlow) - SignalSingle(data, signalPeriod);

        /// <summary>
        /// Calculate MACD
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="signalPeriod">Period of ema calculation on macd line</param>
        /// <param name="macdFast">Period of fast ema calculation</param>
        /// <param name="macdSlow">Period of slow ema calculation</param>
        /// <return>A tuple contains macd, signal and histogram lines</return>
        public static (double?[] macd, double?[] signal, double?[] histogram) MACD(double[] data, int signalPeriod = 9, int macdFast = 12, int macdSlow = 26)
        {
            var macdList = new List<double?>();
            var signalList = new List<double?>();
            var histogramList = new List<double?>();
            for (var i = 0; i < data.Length; i++)
            {
                var selectedData = data.Take(data.Length - i).ToArray();
                var currentMacd = MacdSingle(selectedData, macdFast, macdSlow);
                var currentSignal = SignalSingle(selectedData, signalPeriod, macdFast, macdSlow);
                var currentHistogram = HistogramSingle(selectedData, signalPeriod, macdFast, macdSlow);
                macdList.Add(currentMacd);
                signalList.Add(currentSignal);
                histogramList.Add(currentHistogram);
            }

            macdList.Reverse();
            return (macdList.ToArray(), signalList.ToArray(), histogramList.ToArray());
        }
    }
}

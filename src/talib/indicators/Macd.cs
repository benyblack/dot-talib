using System.Linq;
using System.Collections.Generic;
using System;

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
        {
            var ma = new MA();
            return ma.EmaSingle(data, fast) - ma.EmaSingle(data, slow);
        }

        /// <summary>
        /// Calculate a single Signal value
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of ema calculation on macd line</param>
        /// <param name="macd_fast">Period of fast ema calculation</param>
        /// <param name="macd_slow">Period of slow ema calculation</param>
        public static double? SignalSingle(double[] data, int period = 9, int macd_fast = 12, int macd_slow = 26)
        {
            var macd_list = new List<double>();
            for (int i = 0; i < data.Length; i++)
            {
                var selected_data = data.Take(data.Length - i).ToArray();
                var current_macd = Macd.MacdSingle(selected_data, macd_fast, macd_slow);
                if (!current_macd.HasValue)
                {
                    break;
                }
                macd_list.Add(current_macd.Value);
            }
            macd_list.Reverse();
            return new MA().EmaSingle(macd_list.ToArray(), period);
        }

        /// <summary>
        /// Calculate a MACD Histogram value which is (MACD Line - Signal Line)
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="signal_period">Period of ema calculation on macd line</param>
        /// <param name="macd_fast">Period of fast ema calculation</param>
        /// <param name="macd_slow">Period of slow ema calculation</param>
        /// <returns></returns>
        public static double? HistogramSingle(double[] data, int signal_period = 9, int macd_fast = 12, int macd_slow = 26)
        {
            return MacdSingle(data, macd_fast, macd_slow) - SignalSingle(data, signal_period);
        }

        /// <summary>
        /// Calculate MACD
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="signal_period">Period of ema calculation on macd line</param>
        /// <param name="macd_fast">Period of fast ema calculation</param>
        /// <param name="macd_slow">Period of slow ema calculation</param>
        /// <return>A tuple contains macd, signal and histogram lines</return>
        public static (double?[] macd, double?[] signal, double?[] histogram) MACD(double[] data, int signal_period = 9, int macd_fast = 12, int macd_slow = 26)
        {
            var macd_list = new List<double?>();
            for (int i = 0; i < data.Length; i++)
            {
                var selected_data = data.Take(data.Length - i).ToArray();
                var current_macd = MacdSingle(selected_data, macd_fast, macd_slow);
                var current_signal = SignalSingle(selected_data, signal_period, macd_fast, macd_slow);
                var current_histogram = HistogramSingle(selected_data, signal_period, macd_fast, macd_slow);
                macd_list.Add(current_macd);
            }

            macd_list.Reverse();
            return (macd_list.ToArray(), null, null);
        }
    }
}
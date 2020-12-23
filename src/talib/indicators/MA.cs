using System;
using System.Collections.Generic;
using System.Linq;

namespace Talib.Indicators
{

    /// <summary>
    /// Moving Average indicator [Wikipedia](https://en.wikipedia.org/wiki/Moving_average)
    /// Calculate SMA and EMA
    /// </summary>
    public class MA
    {

        private bool ValidateData(double[] data, int period) =>
            (data != null && data != Array.Empty<double>() && period <= data.Length && period > 0);

        /// <summary>
        /// Calculate Simple Moving Average
        /// </summary>
        /// <param name="data">List of prices.</param>
        /// <param name="period">MA period to be calculated. It must be equal or less than size of data.</param>
        public double? SmaSingle(double[] data, int period)
        {
            if (!ValidateData(data, period)) return null;

            var sum = data.Skip(data.Length - period).Sum();
            return sum / period;
        }

        /// <summary>
        /// Calculate a list of SMA
        /// </summary>
        /// <param name="data">List of prices.</param>
        /// <param name="period">MA period to be calculated. It must be equal or less than size of data.</param>
        public double?[] SMA(double[] data, int period)
        {
            if (!ValidateData(data, period)) return null;

            var result = new double?[data.Length];
            for (int i = period; i <= data.Length; i++)
            {
                result[i - 1] = SmaSingle(data.Take(i).ToArray(), period);
            }
            return result;
        }

        /// <summary>
        /// Calculate Exponential Moving Average
        /// </summary>
        /// <param name="data">List of prices.</param>
        /// <param name="period">MA period to be calculated. It must be equal or less than size of data.</param>
        public double? EmaSingle(double[] data, int period)
        {
            if (!ValidateData(data, period)) return null;

            var multiplier = 2D / (period + 1);
            var reversed_data = data.Reverse().ToArray();

            double? EmaSingleInner(double[] innerData)
            {
                if (period == innerData.Length)
                {
                    return SmaSingle(reversed_data, period);
                }
                double? last_ema = EmaSingleInner(innerData.Skip(1).ToArray());
                return last_ema + (multiplier * (innerData.Take(1).Single() - last_ema));
            }

            return EmaSingleInner(reversed_data);
        }

        /// <summary>
        /// Calculate a list of EMA
        /// </summary>
        /// <param name="data">List of prices.</param>
        /// <param name="period">MA period to be calculated. It must be equal or less than size of data.</param>
        public double?[] EMA(double[] data, int period)
        {
            if (!ValidateData(data, period)) return null;

            var multiplier = 2D / (period + 1);
            var reversed_data = data.Reverse().ToArray();
            var emaList = new List<double?>();
            var memo = new Dictionary<double[], double?>();

            double? EmaInner(double[] innerData, int offset)
            {
                if (period == innerData.Length)
                {
                    memo[innerData] = SmaSingle(reversed_data, period);
                    return memo[innerData];
                }
                var subData = reversed_data.Skip(offset).ToArray();
                if (!memo.ContainsKey(subData))
                {
                    memo[subData] = EmaInner(subData, offset + 1);
                }
                var last_ema = memo[subData];
                var result = last_ema + (multiplier * (innerData.Take(1).Single() - last_ema));
                emaList.Add(last_ema);
                return result;
            }

            EmaInner(reversed_data, 0);
            return emaList.ToArray();
        }
    }
}
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

        /// <summary>
        /// Calculate Simple Moving Average
        /// </summary>
        /// <param name="data">List of prices.</param>
        /// <param name="period">MA period to be calculated. It must be equal or less than size of data.</param>
        public double? SmaSingle(double[] data, int period)
        {
            if (period > data.Length || period <= 0)
            {
                return null;
            }

            double sum = 0;
            for (int i = data.Length - 1; i >= data.Length - period; i--)
            {
                sum += data[i];
            }
            return sum / period;
        }

        /// <summary>
        /// Calculate a list of SMA
        /// </summary>
        /// <param name="data">List of prices.</param>
        /// <param name="period">MA period to be calculated. It must be equal or less than size of data.</param>
        public double?[] SMA(double[] data, int period)
        {
            var result = new double?[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = SmaSingle(data.Take(i + 1).ToArray(), period);
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
            if (data == null || period <= 0)
            {
                return null;
            }
            double multiplier = 2D / (period + 1);
            var reversed_data = data.Reverse().ToArray();
            double? EmaSingleInner(double[] idata)
            {
                if (period > idata.Length)
                {
                    return null;
                }
                if (period == idata.Length)
                {
                    return SmaSingle(reversed_data, period);
                }
                double? last_ema = EmaSingleInner(idata.Skip(1).ToArray());
                return last_ema + (multiplier * (idata.Take(1).Single() - last_ema));
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
            if (data == null || period <= 0)
            {
                return null;
            }
            double multiplier = 2D / (period + 1);
            var reversed_data = data.Reverse().ToArray();
            List<double?> emaList = new List<double?>();
            Dictionary<double[], double?> memo = new Dictionary<double[], double?>();
            double? EmaInner(double[] idata, int offset)
            {
                if (period > idata.Length)
                {
                    return null;
                }
                if (period == idata.Length)
                {
                    memo[idata] = SmaSingle(reversed_data, period);
                    return memo[idata];
                }
                var subData = reversed_data.Skip(offset).ToArray();
                if (!memo.ContainsKey(subData))
                {
                    memo[subData] = EmaInner(subData, offset + 1);
                }
                double? last_ema = memo[subData];
                var result = last_ema + (multiplier * (idata.Take(1).Single() - last_ema));
                emaList.Add(last_ema);
                return result;
            }
            EmaInner(reversed_data, 0);
            return emaList.ToArray();
        }
    }
}
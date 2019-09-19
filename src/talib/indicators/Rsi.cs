using System;
using System.Collections.Generic;
using System.Linq;

namespace Talib.Indicators
{

    /// <summary>
    /// RSI indicator [Wikipedia](https://en.wikipedia.org/wiki/Relative_strength_index)
    /// Calculate RSI based on price history
    /// </summary>
    public class Rsi
    {

        /// <summary>
        /// Avererage gain during the period
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double AverageGain(double[] data, int period)
        {
            if (period >= data.Length)
            {
                return 0;
            }
            double gains = 0;
            for (int i = data.Length - period - 1; i < period; i++)
            {
                if (data[i + 1] > data[i])
                {
                    gains += data[i + 1] - data[i];
                }
            }
            return gains / period;
        }

        /// <summary>
        /// Avererage loss during the period
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double AverageLoss(double[] data, int period)
        {
            if (period >= data.Length)
            {
                return 0;
            }
            double losses = 0;
            for (int i = data.Length - period - 1; i < period; i++)
            {
                if (data[i + 1] < data[i])
                {
                    losses += data[i] - data[i + 1];
                }
            }
            return losses / period;
        }

        /// <summary>
        /// Calculate a single RSI value
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double? RsiSingle(double[] data, int period)
        {
            if (period >= data.Length)
            {
                return null;
            }
            double averageLoss = AverageLoss(data, period);
            if (averageLoss == 0)
            {
                return 100;
            }
            double rs = AverageGain(data, period) / averageLoss;
            return 100D - (100D / (1D + rs));
        }

        /// <summary>
        /// Calculate RSI
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double?[] RSI(double[] data, int period)
        {
            List<double?> result = new List<double?>();
            for (int i = 0; i < data.Length; i++)
            {
                result.Add(RsiSingle(data.Take(i+1).ToArray(),period));
            }
            return result.ToArray();
        }
    }
}
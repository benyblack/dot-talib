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
        private static bool ValidateData(double[] data, int period) =>
            (data != null && data != Array.Empty<double>() && period < data.Length && period > 0);

        /// <summary>
        /// Average gain during the period
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation, must be less than length of price list</param>
        public static double AverageGain(double[] data, int period)
        {
            if (!ValidateData(data, period)) return 0;

            double gains = 0;
            var startIndex = data.Length - period + 1;
            for (int i = startIndex; i < data.Length; i++)
            {
                if (data[i] > data[i - 1])
                {
                    gains += data[i] - data[i - 1];
                }
            }
            return gains / (period - 1);
        }

        /// <summary>
        /// Average loss during the period
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation, must be less than length of price list</param>
        public static double AverageLoss(double[] data, int period)
        {
            if (!ValidateData(data, period)) return 0;

            double losses = 0;
            var startIndex = data.Length - period + 1;
            for (int i = startIndex; i < data.Length; i++)
            {
                if (data[i] < data[i - 1])
                {
                    losses += data[i - 1] - data[i];
                }
            }
            return losses / (period - 1);
        }


        /// <summary>
        /// Calculate a single RSI value
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation, must be less than length of price list</param>
        public static double? RsiSingle(double[] data, int period)
        {
            if (!ValidateData(data, period)) return null;

            var averageLoss = AverageLoss(data, period);
            if (averageLoss == 0D)
            {
                return 100;
            }
            var rs = AverageGain(data, period) / averageLoss;
            return 100D - (100D / (1D + rs));
        }

        /// <summary>
        /// Calculate RSI
        /// </summary>
        /// <param name="data">List of prices</param>
        /// <param name="period">Period of calculation</param>
        public static double?[] RSI(double[] data, int period)
        {
            return data.Select((t, i) => RsiSingle(data.Take(i + 1).ToArray(), period)).ToArray();
        }
    }
}
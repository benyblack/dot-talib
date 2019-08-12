using System;
using System.Collections.Generic;
using System.Linq;

namespace Talib.Indicators
{
    public class MA
    {

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

        public double?[] SMA(double[] data, int period)
        {
            var result = new double?[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = SmaSingle(data.Take(i + 1).ToArray(), period);
            }
            return result;
        }

        public double? EmaSingle(double[] data, int period)
        {
            if (data == null || period <= 0)
            {
                return null;
            }
            double multiplier = 2D / (period + 1);
            Array.Reverse(data);
            double? EmaSingleInner(double[] idata)
            {
                if (period > idata.Length)
                {
                    return null;
                }
                if (period == idata.Length)
                {
                    return SmaSingle(data, period);
                }
                double? last_ema = EmaSingleInner(idata.Skip(1).ToArray());
                return last_ema + (multiplier * (idata.Take(1).Single() - last_ema));
            }
            return EmaSingleInner(data);
        }

        public double?[] EMA(double[] data, int period)
        {
            if (data == null || period <= 0)
            {
                return null;
            }
            double multiplier = 2D / (period + 1);
            Array.Reverse(data);
            List<double?> emaList = new List<double?>();
            Dictionary<double[], double?> memo = new Dictionary<double[], double?>();
            double? EmaInner(double[] idata)
            {
                if (period > idata.Length)
                {
                    return null;
                }
                if (period == idata.Length)
                {
                    memo[idata] = SmaSingle(data, period);
                    return memo[idata];
                }
                var subData = idata.Skip(1).ToArray();
                if (!memo.ContainsKey(subData))
                {
                    memo[subData] = EmaInner(subData);
                }
                double? last_ema = memo[subData];
                var result = last_ema + (multiplier * (idata.Take(1).Single() - last_ema));
                emaList.Add(last_ema);
                return result;
            }
            EmaInner(data);
            emaList.Reverse();
            return emaList.ToArray();
        }
    }
}
using System;

namespace Talib.Indicators {
    public class MA {

        public double? SmaSingle (double[] data, int period) {
            if (period > data.Length || period <= 0) {
                return null;
            }
            
            double sum = 0;
            for (int i = data.Length - 1; i >= data.Length - period; i--) {
                sum += data[i];
            }
            return sum / period;
        }
    }
}
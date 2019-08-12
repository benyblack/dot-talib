using System;
using System.Linq;
using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators
{
    public class StochasticTests
    {

        [Fact]
        public void K_simplevalues_calculated()
        {
            //Given
            double[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            //When
            double?[] result = Stochastic.K(data, period: 5);
            double?[] expected = { null, null, null, null, 100, 100, 100, 100, 100, 100 };
            //Then
            Assert.Equal(expected, result);
        }

        [Fact]
        public void D_simplevalues_calculated()
        {
            //Given
            double[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            //When
            double?[] result = Stochastic.D(data, period: 5, k_period: 3);
            double?[] expected = { null, null, null, null, 100, 100, 100, 100, 100, 100 };
            //Then
            Assert.Equal(expected, result);
        }

        [Fact]
        public void K_Single_SimpleValues_Calculated()
        {
            //Given
            double[] data = { 1, 2, 3, 4, 5, 6, 5, 8, 4, 7 };

            //When
            var result = Stochastic.K_Single(data, period: 5);

            //Then
            Assert.Equal(75D, result);
        }

        [Fact]
        public void K_Single_DataLengthLessThanPeriod_ReturnZero()
        {
            //Given
            double[] data = { 1, 2, 3, 4, 5, 6, 5, 8, 4, 7 };

            //When
            var result = Stochastic.K_Single(data, period: 11);

            //Then
            Assert.Equal(0, result);
        }

        [Fact]
        public void D_Single_SimpleValues_Calculated()
        {
            //Given
            double[] data = { 1, 2, 3, 4, 5, 6, 5, 8, 4, 7 };

            //When
            var result = Stochastic.D_Single(data, period: 5, k_period: 3);

            //Then
            Assert.Equal(88.89D, Math.Round(result.Value, 2));
        }

        [Fact]
        public void D_Single_DataLengthLessThanPeriod_ReturnZero()
        {
            //Given
            double[] data = { 1, 2, 3, 4, 5, 6, 5, 8, 4, 7 };

            //When
            var result = Stochastic.D_Single(data, period: 11);

            //Then
            Assert.Equal(0, result);
        }
    }
}
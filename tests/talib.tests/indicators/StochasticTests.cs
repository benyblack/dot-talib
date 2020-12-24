using System;
using System.Linq;
using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators
{
    public class StochasticTests
    {
        private readonly double[] _dataSet1 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        private readonly double[] _dataSet2 = { 1, 2, 3, 4, 5, 6, 5, 8, 4, 7 };

        [Theory]
        [InlineData(3, null, null, 100d, 100d, 100d, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(4, null, null, null, 100d, 100d, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(5, null, null, null, null, 100d, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(6, null, null, null, null, null, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(7, null, null, null, null, null, null, 100d, 100d, 100d, 100d)]
        [InlineData(8, null, null, null, null, null, null, null, 100d, 100d, 100d)]
        public void K_simpleValues_calculated(int period , params double?[] expected)
        {
            //When
            var result = Stochastic.K(_dataSet1, period);

            //Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(3, null, null, 100d, 100d, 100d, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(4, null, null, null, 100d, 100d, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(5, null, null, null, null, 100d, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(6, null, null, null, null, null, 100d, 100d, 100d, 100d, 100d)]
        [InlineData(7, null, null, null, null, null, null, 100d, 100d, 100d, 100d)]
        [InlineData(8, null, null, null, null, null, null, null, 100d, 100d, 100d)]
        public void D_simpleValues_calculated(int period, params double?[] expected)
        {
            //When
            double?[] result = Stochastic.D(_dataSet1, period, kPeriod: 3);

            //Then
            Assert.Equal(expected, result);
        }

        [Fact]
        public void K_Single_SimpleValues_Calculated()
        {
            //When
            var result = Stochastic.K_Single(_dataSet2, period: 5);

            //Then
            Assert.Equal(75D, result);
        }

        [Fact]
        public void K_Single_DataLengthLessThanPeriod_ReturnZero()
        {
            //When
            var result = Stochastic.K_Single(_dataSet2, period: 11);

            //Then
            Assert.Equal(0, result);
        }

        [Fact]
        public void D_Single_SimpleValues_Calculated()
        {
            //When
            var result = Stochastic.D_Single(_dataSet2, period: 5, kPeriod: 3);

            //Then
            Assert.Equal(88.89D, Math.Round(result.Value, 2));
        }

        [Fact]
        public void D_Single_DataLengthLessThanPeriod_ReturnZero()
        {
            //When
            var result = Stochastic.D_Single(_dataSet2, period: 11);

            //Then
            Assert.Equal(0, result);
        }
    }
}
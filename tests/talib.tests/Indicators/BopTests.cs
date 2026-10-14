using Talib.Indicators;
using Xunit;

namespace Talib.Tests.Indicators;

public class BopTests
{
  [Fact]
  public void Latest_and_BopSeries_match_the_reference()
  {
    var bars = new[]
    {
            new Bar(Open: 9.5, High: 10, Low: 9, Close: 9.5, Volume: 0),
            new Bar(Open: 10.0, High: 11, Low: 9.5, Close: 10.5, Volume: 0),
        };

    Assert.Equal(0.3333, Round(Bop.Latest(bars), 4));
    Assert.Equal(new double?[] { 0.0, 0.3333333333333333 }, Bop.BopSeries(bars));
    Assert.Equal(Bop.Latest(bars), Bop.BopSeries(bars)[^1]);
  }

  [Fact]
  public void Zero_range_bars_have_an_undefined_balance_of_power()
  {
    Assert.Equal(new double?[] { null }, Bop.BopSeries(new[] { new Bar(Open: 5, High: 5, Low: 5, Close: 5, Volume: 0) }));
    Assert.Null(Bop.Latest(new[] { new Bar(Open: 5, High: 5, Low: 5, Close: 5, Volume: 0) }));
  }

  [Fact]
  public void Empty_input_yields_null_and_an_empty_series()
  {
    Assert.Null(Bop.Latest(Array.Empty<Bar>()));
    Assert.Empty(Bop.BopSeries(Array.Empty<Bar>()));
  }

  private static double? Round(double? value, int digits) =>
      value is null ? null : Math.Round(value.Value, digits, MidpointRounding.AwayFromZero);
}

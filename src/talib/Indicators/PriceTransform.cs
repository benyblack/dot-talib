namespace Talib.Indicators;

/// <summary>
/// Price Transform functions (TA-Lib group).
///
/// Per-bar price averages. Transforms are defined for every bar: the
/// series functions return one value per bar, aligned with the input.
/// Only the fields each transform needs are read (the Open only by
/// <see cref="AvgPrice"/>); the rest may be left zero.
///
/// Bars are always given in chronological order, oldest first.
/// </summary>
public static class PriceTransform
{
  /// <summary>Average Price: <c>(open + high + low + close) / 4</c>.</summary>
  /// <param name="bar">The bar; Open, High, Low and Close are read.</param>
  /// <returns>The average price of the bar.</returns>
  public static double AvgPrice(Bar bar)
  {
    return (bar.Open + bar.High + bar.Low + bar.Close) / 4;
  }

  /// <summary>Median Price: <c>(high + low) / 2</c>.</summary>
  /// <param name="bar">The bar; High and Low are read.</param>
  /// <returns>The median price of the bar.</returns>
  public static double MedPrice(Bar bar)
  {
    return (bar.High + bar.Low) / 2;
  }

  /// <summary>Typical Price: <c>(high + low + close) / 3</c>.</summary>
  /// <param name="bar">The bar; High, Low and Close are read.</param>
  /// <returns>The typical price of the bar.</returns>
  public static double TypPrice(Bar bar)
  {
    return (bar.High + bar.Low + bar.Close) / 3;
  }

  /// <summary>Weighted Close Price: <c>(high + low + 2 * close) / 4</c>.</summary>
  /// <param name="bar">The bar; High, Low and Close are read.</param>
  /// <returns>The weighted close price of the bar.</returns>
  public static double WclPrice(Bar bar)
  {
    return (bar.High + bar.Low + 2 * bar.Close) / 4;
  }

  /// <summary>The Average Price of every bar.</summary>
  /// <param name="bars">Bars, oldest first; Open, High, Low and Close are read.</param>
  /// <returns>The average price of each bar, aligned with the input.</returns>
  public static double[] AvgPriceSeries(Bar[] bars)
  {
    var result = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      result[i] = AvgPrice(bars[i]);
    }

    return result;
  }

  /// <summary>The Median Price of every bar.</summary>
  /// <param name="bars">Bars, oldest first; High and Low are read.</param>
  /// <returns>The median price of each bar, aligned with the input.</returns>
  public static double[] MedPriceSeries(Bar[] bars)
  {
    var result = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      result[i] = MedPrice(bars[i]);
    }

    return result;
  }

  /// <summary>The Typical Price of every bar.</summary>
  /// <param name="bars">Bars, oldest first; High, Low and Close are read.</param>
  /// <returns>The typical price of each bar, aligned with the input.</returns>
  public static double[] TypPriceSeries(Bar[] bars)
  {
    var result = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      result[i] = TypPrice(bars[i]);
    }

    return result;
  }

  /// <summary>The Weighted Close Price of every bar.</summary>
  /// <param name="bars">Bars, oldest first; High, Low and Close are read.</param>
  /// <returns>The weighted close price of each bar, aligned with the input.</returns>
  public static double[] WclPriceSeries(Bar[] bars)
  {
    var result = new double[bars.Length];
    for (int i = 0; i < bars.Length; i++)
    {
      result[i] = WclPrice(bars[i]);
    }

    return result;
  }
}

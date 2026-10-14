namespace Talib.Indicators;

/// <summary>
/// Statistic functions (TA-Lib group).
///
/// <para>
///   Linear regression fits <c>y = intercept + slope * x</c> over each
///   lookback window with <c>x = 0..period - 1</c>. <see cref="LinearReg"/>
///   is the fitted value at the window's last point, <see cref="Tsf"/>
///   projects one bar ahead, and <see cref="Angle"/> is the slope in
///   degrees. <see cref="Beta"/> and <see cref="Correl"/> relate two
///   series over the window.
/// </para>
///
/// Prices are always given in chronological order, oldest first (the
/// reference module is scalar-only, so there are no series forms). All
/// scalar functions return <c>null</c> when there is not enough data.
/// </summary>
public static class Stats
{
  /// <summary>Calculates the population variance of the <c>period</c> newest prices.</summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The variance at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Variance(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    double sum = 0;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      sum += prices[i];
    }

    double mean = sum / period;

    double total = 0;
    for (int i = prices.Length - period; i < prices.Length; i++)
    {
      total = total + (prices[i] - mean) * (prices[i] - mean);
    }

    return total / period;
  }

  /// <summary>
  /// Calculates the beta of <paramref name="prices"/> against
  /// <paramref name="market"/> over the <c>period</c> newest points:
  /// the covariance of the two windows divided by the variance of the
  /// <paramref name="prices"/> window (the reference divides by the
  /// first series' variance — its doctest pins it, e.g.
  /// <c>beta([1,2,3,4], [2,4,6,8], 3) == 2.0</c>).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="market">Market prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The beta at the latest point, or <c>null</c> when there is not enough data or the prices' variance is zero.</returns>
  public static double? Beta(double[] prices, double[] market, int period = 14)
  {
    Guard.Period(period);
    (double Cov, double VarX, double VarY)? moments = Moments(prices, market, period);
    if (moments is null)
    {
      return null;
    }

    var (cov, varX, _) = moments.Value;
    return varX == 0 ? null : cov / varX;
  }

  /// <summary>
  /// Calculates the Pearson correlation of the two series over the
  /// <c>period</c> newest points.
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="market">Market prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The correlation at the latest point, or <c>null</c> when there is not enough data or either variance is zero.</returns>
  public static double? Correl(double[] prices, double[] market, int period = 14)
  {
    Guard.Period(period);
    (double Cov, double VarX, double VarY)? moments = Moments(prices, market, period);
    if (moments is null)
    {
      return null;
    }

    var (cov, varX, varY) = moments.Value;
    double denominator = Math.Sqrt(varX * varY);
    return denominator == 0 ? null : cov / denominator;
  }

  /// <summary>
  /// Calculates the fitted value of the linear regression at the window's
  /// last point (TA-Lib LINEARREG).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The linear regression value at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? LinearReg(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    var (slope, intercept) = Fit(prices, period);
    return intercept + slope * (period - 1);
  }

  /// <summary>
  /// Calculates the slope of the linear regression over the
  /// <c>period</c> newest prices (TA-Lib LINEARREG_SLOPE).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The slope at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Slope(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    return Fit(prices, period).Slope;
  }

  /// <summary>
  /// Calculates the intercept of the linear regression over the
  /// <c>period</c> newest prices (TA-Lib LINEARREG_INTERCEPT).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The intercept at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Intercept(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    return Fit(prices, period).Intercept;
  }

  /// <summary>
  /// Calculates the regression slope in degrees
  /// (TA-Lib LINEARREG_ANGLE).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The angle at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Angle(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    return Math.Atan(Fit(prices, period).Slope) * (180 / Math.PI);
  }

  /// <summary>
  /// Calculates the Time Series Forecast: the regression projected one
  /// bar past the window's last point (TA-Lib TSF).
  /// </summary>
  /// <param name="prices">Prices, oldest first.</param>
  /// <param name="period">Lookback window, a positive integer.</param>
  /// <returns>The forecast at the latest point, or <c>null</c> when there is not enough data.</returns>
  public static double? Tsf(double[] prices, int period = 14)
  {
    Guard.Period(period);
    if (prices.Length < period)
    {
      return null;
    }

    var (slope, intercept) = Fit(prices, period);
    return intercept + slope * period;
  }

  // Least squares over the newest `period` prices, with x = 0..n-1.
  private static (double Slope, double Intercept) Fit(double[] prices, int period)
  {
    int n = period;
    long sumX = n * (n - 1L) / 2;
    long sumX2 = (n - 1L) * n * (2 * n - 1) / 6;
    int start = prices.Length - period;

    double sumY = 0;
    for (int i = start; i < prices.Length; i++)
    {
      sumY += prices[i];
    }

    double sumXy = 0;
    for (int x = 0; x < n; x++)
    {
      sumXy = sumXy + x * prices[start + x];
    }

    double denominator = n * sumX2 - sumX * sumX;
    double slope = (n * sumXy - sumX * sumY) / denominator;
    double intercept = (sumY - slope * sumX) / n;
    return (slope, intercept);
  }

  // Covariance and variances of the two newest-`period` windows
  // (population moments); null when either series is too short.
  private static (double Cov, double VarX, double VarY)? Moments(double[] prices, double[] market, int period)
  {
    if (prices.Length < period || market.Length < period)
    {
      return null;
    }

    int startX = prices.Length - period;
    int startY = market.Length - period;
    double sumX = 0;
    for (int i = startX; i < prices.Length; i++)
    {
      sumX += prices[i];
    }

    double sumY = 0;
    for (int i = startY; i < market.Length; i++)
    {
      sumY += market[i];
    }

    double meanX = sumX / period;
    double meanY = sumY / period;

    double cov = 0;
    double varX = 0;
    double varY = 0;
    for (int i = 0; i < period; i++)
    {
      double a = prices[startX + i];
      double b = market[startY + i];
      cov = cov + (a - meanX) * (b - meanY);
      varX = varX + (a - meanX) * (a - meanX);
      varY = varY + (b - meanY) * (b - meanY);
    }

    return (cov / period, varX / period, varY / period);
  }
}

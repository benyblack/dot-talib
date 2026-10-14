namespace Talib;

/// <summary>
/// One OHLCV bar. Bar-based indicators (ATR, OBV, ...) read only the
/// fields they need; the rest may be left zero.
/// </summary>
/// <param name="Open">The bar's opening price.</param>
/// <param name="High">The bar's highest price.</param>
/// <param name="Low">The bar's lowest price.</param>
/// <param name="Close">The bar's closing price.</param>
/// <param name="Volume">The bar's traded volume.</param>
public readonly record struct Bar(double Open, double High, double Low, double Close, double Volume);

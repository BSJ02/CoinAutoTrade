using CoinAutoTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinAutoTradingApp.UpbitAPI.Models;
using System.Runtime.Intrinsics.X86;

namespace CoinAutoTradingApp.Utilities
{
    public class Calculate
    {
        // RSI
        public static double RSI(List<CandleMinute> candles, int period = 14, int count = 14)
        {
            double totalGain = 0;
            double totalLoss = 0;

            for (int i = period + count; i > period - 2; i--)
            {
                double prevClosePrice = candles[i + 1].TradePrice;
                double currClosePrice = candles[i].TradePrice;

                totalGain += Math.Max(0, currClosePrice - prevClosePrice);
                totalLoss += Math.Max(0, prevClosePrice - currClosePrice);
            }

            double avgGain = totalGain / count;
            double avgLoss = totalLoss / count;

            for (int i = period - 2; i >= 0; i--)
            {
                double prevClosePrice = candles[i + 1].TradePrice;
                double currClosePrice = candles[i].TradePrice;

                double gain = Math.Max(0, currClosePrice - prevClosePrice);
                double loss = Math.Max(0, prevClosePrice - currClosePrice);

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;
            }

            double rs = avgGain / avgLoss;
            double rsi = 100 - (100 / (1 + rs));

            return rsi;
        }
    

        // ATR
        public static double ATR(List<CandleMinute> candles, int period = 14, int count = 14)
        {
            if (candles.Count < period)
                return 0;

            double trSum = 0;
            for (int i = period + count; i > period - 2; i--)
            {
                double high = candles[i].HighPrice;
                double low = candles[i].LowPrice;
                double prevClose = candles[i + 1].TradePrice;

                double tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
                trSum += tr;
            }

            double atr = trSum / period;

            for (int i = period - 2; i >= 0; i--)
            {
                double high = candles[i].HighPrice;
                double low = candles[i].LowPrice;
                double prevClose = candles[i + 1].TradePrice;

                double tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));

                atr = (atr * (period - 1) + tr) / period;
            }

            return atr;
        }


        // DMI
        public static (double pdi, double mdi, double adx) DMI(List<CandleMinute> candles, int period = 14, int count = 14)
        {
            List<double> trList = new();
            List<double> plusDMList = new();
            List<double> minusDMList = new();
            List<double> dxList = new();
            (double pdi, double mdi, double adx) results = (0, 0, 0);

            // Step 1: Calculate TR, +DM, -DM
            for (int i = 0; i < period + count; i++)
            {
                var curr = candles[i];
                var prev = candles[i + 1];

                double highDiff = curr.HighPrice - prev.HighPrice;
                double lowDiff = prev.LowPrice - curr.LowPrice;

                double plusDM = (highDiff > lowDiff && highDiff > 0) ? highDiff : 0;
                double minusDM = (lowDiff > highDiff && lowDiff > 0) ? lowDiff : 0;

                double tr = Math.Max(curr.HighPrice - curr.LowPrice,
                            Math.Max(Math.Abs(curr.HighPrice - prev.TradePrice),
                                     Math.Abs(curr.LowPrice - prev.TradePrice)));

                plusDMList.Add(plusDM);
                minusDMList.Add(minusDM);
                trList.Add(tr);
            }

            // Step 2: 초기 Sum 계산
            double sumTR = trList.Take(count).Sum();
            double sumPlusDM = plusDMList.Take(count).Sum();
            double sumMinusDM = minusDMList.Take(count).Sum();

            double plusDI = 100 * (sumPlusDM / sumTR);
            double minusDI = 100 * (sumMinusDM / sumTR);
            double dx = 100 * (Math.Abs(plusDI - minusDI) / (plusDI + minusDI));
            dxList.Add(dx);

            double finalPDI = 0, finalMDI = 0;

            // Step 2.5: 초기화
            sumTR = trList[period - 1];
            sumPlusDM = plusDMList[period - 1];
            sumMinusDM = minusDMList[period - 1];

            // Step 3: 이후 구간
            for (int i = period - 2; i >= 0; i--)
            {
                sumTR = sumTR - (sumTR / period) + trList[i];
                sumPlusDM = sumPlusDM - (sumPlusDM / period) + plusDMList[i];
                sumMinusDM = sumMinusDM - (sumMinusDM / period) + minusDMList[i];

                plusDI = (sumTR != 0) ? 100 * (sumPlusDM / sumTR) : 0;
                minusDI = (sumTR != 0) ? 100 * (sumMinusDM / sumTR) : 0;

                double diSum = plusDI + minusDI;

                if (diSum != 0)
                {
                    dx = 100 * (Math.Abs(plusDI - minusDI) / (plusDI + minusDI));
                    dxList.Add(dx);

                    double adx = dxList.Average();

                    results.pdi = Math.Round(plusDI, 2);
                    results.mdi = Math.Round(minusDI, 2);
                    results.adx = Math.Round(adx, 2);
                }
                else
                {
                    dxList.Add(0);

                    results.pdi = 0;
                    results.mdi = 0;
                    results.adx = 0;
                }
            }

            return results;
        }


        // EMA
        public static List<double> EMAHistory(List<CandleMinute> candles, int period)
        {
            List<double> emaValues = new List<double>();

            decimal multiplier = 2m / (period + 1);

            double sma = candles.Take(period).Average(c => c.TradePrice);
            emaValues.Add(sma);

            for (int i = period - 1; i >= 0; i--)
            {
                double ema = ((candles[i].TradePrice - emaValues.Last()) * (double)multiplier) + emaValues.Last();
                emaValues.Add(ema);
            }

            return emaValues;
        }


        // BollingerBand
        public static (double upperBand, double lowerBand, double movingAverage) BollingerBands(List<CandleMinute> candles, int period = 20, int multiplier = 2)
        {
            var recentCandles = candles.Take(period).ToList();

            double movingAverage = recentCandles.Average(c => c.TradePrice);

            double sumOfSquares = recentCandles.Sum(c => Math.Pow(c.TradePrice - movingAverage, 2));
            double standardDeviation = Math.Sqrt(sumOfSquares / (period - 1));

            double upperBand = movingAverage + multiplier * standardDeviation;
            double lowerBand = movingAverage - multiplier * standardDeviation;

            return (upperBand, lowerBand, movingAverage);
        }


        // CCI
        public static double CCI(List<CandleMinute> candles, int period = 14)
        {
            var recentCandles = candles.Take(period).ToList();

            var latestCandle = recentCandles[0];
            double typicalPrice = (latestCandle.HighPrice + latestCandle.LowPrice + latestCandle.TradePrice) / 3;

            double sma = recentCandles.Average(c => (c.HighPrice + c.LowPrice + c.TradePrice) / 3);

            double meanDeviation = recentCandles.Average(c => Math.Abs(((c.HighPrice + c.LowPrice + c.TradePrice) / 3) - sma));

            return (typicalPrice - sma) / (0.015 * meanDeviation);
        }


        // Keltner Channel
        public static (double upper, double middle, double lower) KeltnerChannel(List<CandleMinute> candles, int period = 20, double atrMultiplier = 1)
        {
            if (candles.Count < period) return (0, 0, 0); // 데이터 부족 시 0 반환

            double ema = EMAHistory(candles, period).Last();  // 중앙선 (Middle Line)
            double atr = ATR(candles, period);

            double keltnerUpper = ema + atrMultiplier * atr;  // 상단 밴드
            double keltnerLower = ema - atrMultiplier * atr;  // 하단 밴드

            return (keltnerUpper, ema, keltnerLower);
        }
    }
}

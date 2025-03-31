using CoinAutoTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinAutoTradingApp.UpbitAPI.Models;

namespace CoinAutoTradingApp.Utilities
{
    public class Calculate
    {
        // RSI
        public static double RSI(List<CandleMinute> candles, int period = 14)
        {
            if (candles.Count < period + 1) return 0; // 데이터가 부족하면 0 반환

            // 첫 period만큼의 gain, loss 계산
            List<double> gains = new List<double>();
            List<double> losses = new List<double>();

            for (int i = 1; i < period + 1; i++)
            {
                double change = candles[i].TradePrice - candles[i - 1].TradePrice;
                gains.Add(Math.Max(0, change));
                losses.Add(Math.Max(0, -change));
            }

            // 첫 번째 평균 Gain과 Loss 계산
            double avgGain = gains.Average();
            double avgLoss = losses.Average();

            // RS와 RSI 계산
            double rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            double rsi = 100 - (100 / (1 + rs));

            return rsi;
        }


        // ATR
        public static double ATR(List<CandleMinute> candles, int period = 14)
        {
            if (candles.Count < period + 1)
                return 0;

            List<double> trValues = new List<double>();

            for (int i = 1; i <= period; i++)
            {
                double high = candles[i].HighPrice;
                double low = candles[i].LowPrice;
                double prevClose = candles[i - 1].TradePrice;

                double tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
                trValues.Add(tr);
            }

            double atr = trValues.Average();

            return atr;
        }


        // EMA
        public static List<double> EMAHistory(List<CandleMinute> candles, int period)
        {
            List<double> emaValues = new List<double>();

            decimal multiplier = 2m / (period + 1);

            double sma = candles.Take(period).Average(c => c.TradePrice);
            emaValues.Add(sma);

            for (int i = period; i >= 0; i--)
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
            double standardDeviation = Math.Sqrt(sumOfSquares / period);

            double upperBand = movingAverage + multiplier * standardDeviation;
            double lowerBand = movingAverage - multiplier * standardDeviation;

            return (upperBand, lowerBand, movingAverage);
        }


        // CCI
        public static double CCI(List<CandleMinute> candles, int period = 20)
        {
            if (candles.Count < period) return 0;

            var recentCandles = candles.Take(period).ToList();

            var typicalPrices = recentCandles.Select(c => (c.HighPrice + c.LowPrice + c.TradePrice) / 3).ToList();

            double sma = typicalPrices.Average();

            double meanDeviation = typicalPrices.Average(tp => Math.Abs(tp - sma));

            return (typicalPrices.Last() - sma) / (0.015 * meanDeviation);
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

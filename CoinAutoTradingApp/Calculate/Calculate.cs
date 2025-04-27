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
            decimal totalGain = 0;
            decimal totalLoss = 0;

            for (int i = period + count; i > period - 2; i--)
            {
                decimal prevClosePrice = candles[i + 1].TradePrice;
                decimal currClosePrice = candles[i].TradePrice;

                totalGain += Math.Max(0, currClosePrice - prevClosePrice);
                totalLoss += Math.Max(0, prevClosePrice - currClosePrice);
            }

            decimal avgGain = totalGain / count;
            decimal avgLoss = totalLoss / count;

            for (int i = period - 2; i >= 0; i--)
            {
                decimal prevClosePrice = candles[i + 1].TradePrice;
                decimal currClosePrice = candles[i].TradePrice;

                decimal gain = Math.Max(0, currClosePrice - prevClosePrice);
                decimal loss = Math.Max(0, prevClosePrice - currClosePrice);

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;
            }

            double rs = (double)(avgGain / avgLoss);
            double rsi = 100 - (100 / (1 + rs));

            return rsi;
        }
    

        // ATR
        public static decimal ATR(List<CandleMinute> candles, int period = 14, int count = 14)
        {
            if (candles.Count < period)
                return 0;

            decimal trSum = 0;
            for (int i = period + count; i > period - 2; i--)
            {
                decimal high = candles[i].HighPrice;
                decimal low = candles[i].LowPrice;
                decimal prevClose = candles[i + 1].TradePrice;

                decimal tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
                trSum += tr;
            }

            decimal atr = (trSum / period);

            for (int i = period - 2; i >= 0; i--)
            {
                decimal high = candles[i].HighPrice;
                decimal low = candles[i].LowPrice;
                decimal prevClose = candles[i + 1].TradePrice;

                decimal tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));

                atr = (atr * (period - 1) + tr) / period;
            }

            return atr;
        }


        // EMA
        public static List<decimal> EMAHistory(List<CandleMinute> candles, int period)
        {
            List<decimal> emaValues = new List<decimal>(new decimal[candles.Count]);
            decimal multiplier = 2m / (period + 1);

            int startIndex = candles.Count - period;
            if (startIndex < 0) return emaValues;

            // SMA 계산 (가장 오래된 기간 기준)
            decimal sma = candles.Skip(startIndex).Take(candles.Count - startIndex).Average(c => c.TradePrice);
            emaValues[startIndex] = sma;
            decimal previousEma = sma;

            // EMA 계산: 오래된 → 최근 방향
            for (int i = startIndex - 1; i >= 0; i--)
            {
                decimal close = candles[i].TradePrice;
                decimal ema = ((close - previousEma) * multiplier) + previousEma;
                emaValues[i] = ema;
                previousEma = ema;
            }

            return emaValues;
        }

        // VWMA
        public static List<decimal> VWMA(List<CandleMinute> candles, int period)
        {
            List<decimal> vwmaValues = new List<decimal>(new decimal[candles.Count]);

            // 오래된 것부터 최신으로 (인덱스 증가 방향)
            for (int i = candles.Count - period; i >= 0; i--)
            {
                decimal volumeSum = 0;
                decimal weightedPriceSum = 0;

                for (int j = i; j < i + period; j++)
                {
                    decimal price = candles[j].TradePrice;
                    decimal volume = candles[j].CandleAccTradeVolume;

                    weightedPriceSum += price * volume;
                    volumeSum += volume;
                }

                if (volumeSum != 0)
                    vwmaValues[i] = weightedPriceSum / volumeSum;
            }

            return vwmaValues;
        }

        public static decimal POC(List<CandleMinute> candles, int period)
        {
            decimal currentPrice = candles[0].TradePrice;

            // ATR을 계산하여 변동성을 측정
            decimal atr = Calculate.ATR(candles, period); // ATR 계산 함수 호출

            // 변동성을 바탕으로 priceRange 설정 (ATR에 비례)
            decimal priceRange = atr * 0.1m;  // ATR의 10% 정도를 priceRange로 설정

            // priceRange가 너무 작으면 최소값 설정
            priceRange = Math.Max(priceRange, currentPrice * 0.001m);  // 최소 0.1%로 설정

            Dictionary<decimal, decimal> volumeAtPriceRange = new Dictionary<decimal, decimal>();

            // 캔들의 가격 범위를 priceRange 단위로 나누어 거래량 집계
            foreach (var candle in candles.Take(period).ToList())
            {
                decimal price = candle.TradePrice;
                decimal volume = candle.CandleAccTradeVolume;

                // 각 가격 구간에 맞춰서 거래량 집계
                decimal lowerBound = Math.Floor(price / priceRange) * priceRange;
                decimal upperBound = lowerBound + priceRange;

                if (price >= lowerBound && price < upperBound)
                {
                    if (volumeAtPriceRange.ContainsKey(lowerBound))
                    {
                        volumeAtPriceRange[lowerBound] += volume;
                    }
                    else
                    {
                        volumeAtPriceRange[lowerBound] = volume;
                    }
                }
            }

            // 거래량이 가장 큰 가격대 찾기 (POC)
            decimal poc = 0;
            decimal maxVolume = 0;

            foreach (var entry in volumeAtPriceRange)
            {
                if (entry.Value > maxVolume)
                {
                    maxVolume = entry.Value;
                    poc = entry.Key;
                }
            }

            return poc;
        }


        // CCI
        public static double CCI(List<CandleMinute> candles, int period = 14)
        {
            var recentCandles = candles.Take(period).ToList();
            var latestCandle = recentCandles[0];

            decimal typicalPrice = (latestCandle.HighPrice + latestCandle.LowPrice + latestCandle.TradePrice) / 3;

            decimal sma = recentCandles.Average(c => (c.HighPrice + c.LowPrice + c.TradePrice) / 3);

            decimal meanDeviation = recentCandles.Average(c => Math.Abs(((c.HighPrice + c.LowPrice + c.TradePrice) / 3) - sma));

            if (meanDeviation == 0)
                return 0;

            return (double)(typicalPrice - sma) / (0.015 * (double)meanDeviation);
        }


        // Keltner Channel
        public static (decimal upper, decimal middle, decimal lower) KeltnerChannel(List<CandleMinute> candles, int period = 20, decimal atrMultiplier = 1)
        {
            if (candles.Count < period) return (0, 0, 0); // 데이터 부족 시 0 반환

            decimal ema = EMAHistory(candles, period).Last();  // 중앙선 (Middle Line)
            decimal atr = ATR(candles, period);

            decimal keltnerUpper = ema + atrMultiplier * atr;  // 상단 밴드
            decimal keltnerLower = ema - atrMultiplier * atr;  // 하단 밴드

            return (keltnerUpper, ema, keltnerLower);
        }


        public static double Slope((int index, double price) recentCandle, (int index, double price) prevCandle)
        {
            if (recentCandle.index == prevCandle.index) return double.NaN;  // 분모 0 방지

            double slope = (recentCandle.price - prevCandle.price) / (prevCandle.index - recentCandle.index); // 0이 최신 캔들이므로 분모 변경
            return slope;
        }

        public static double Intercept(double slope, (int index, double price) candle)
        {
            double intercept = candle.price - slope * candle.index;
            return intercept;
        }

        public static decimal Volume(CandleMinute candle)
        {
            return candle.CandleAccTradeVolume;
        }
    }
}

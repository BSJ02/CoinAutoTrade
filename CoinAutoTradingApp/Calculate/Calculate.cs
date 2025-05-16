using CoinAutoTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinAutoTradingApp.UpbitAPI.Models;
using System.Runtime.Intrinsics.X86;
using System.Reflection.PortableExecutable;

namespace CoinAutoTradingApp.Utilities
{
    public class Calculate
    {
        // RSI
        public static List<decimal> RSI(List<CandleMinute> candles, int period = 14)
        {
            var rsiList = new List<decimal>();

            if (candles == null || candles.Count < period + 1)
                return rsiList;

            var reversed = candles.ToList();
            reversed.Reverse(); // 과거 → 현재

            decimal gain = 0;
            decimal loss = 0;

            // 1. 초기 평균 계산 (period 구간)
            for (int i = 1; i <= period; i++)
            {
                var diff = reversed[i].TradePrice - reversed[i - 1].TradePrice;
                if (diff > 0) gain += diff;
                else loss -= diff;
            }

            decimal avgGain = gain / period;
            decimal avgLoss = loss / period;

            // 2. 첫 RSI 기록
            decimal rs = avgLoss == 0 ? 0 : avgGain / avgLoss;
            rsiList.Add(avgLoss == 0 ? 100 : Math.Round(100 - (100 / (1 + rs)), 2));

            // 3. 이후 RSI 히스토리 기록
            for (int i = period + 1; i < reversed.Count; i++)
            {
                var diff = reversed[i].TradePrice - reversed[i - 1].TradePrice;
                decimal currentGain = diff > 0 ? diff : 0;
                decimal currentLoss = diff < 0 ? -diff : 0;

                avgGain = (avgGain * (period - 1) + currentGain) / period;
                avgLoss = (avgLoss * (period - 1) + currentLoss) / period;

                if (avgLoss == 0)
                    rsiList.Add(100);
                else if (avgGain == 0)
                    rsiList.Add(0);
                else
                {
                    rs = avgGain / avgLoss;
                    rsiList.Add(Math.Round(100 - (100 / (1 + rs)), 2));
                }
            }

            rsiList.Reverse();
            return rsiList;
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

        private static List<decimal> EmaList(List<decimal> values, int period)
        {
            List<decimal> emaValues = new List<decimal>(new decimal[values.Count]);
            decimal multiplier = 2m / (period + 1);

            int startIndex = values.Count - period;
            if (startIndex < 0) return emaValues;

            decimal sma = values.Skip(startIndex).Take(values.Count - startIndex).Average();
            emaValues[startIndex] = sma;
            decimal previousEma = sma;

            for (int i = startIndex - 1; i >= 0; i--)
            {
                decimal value = values[i];
                decimal ema = ((value - previousEma) * multiplier) + previousEma;
                emaValues[i] = ema;
                previousEma = ema;
            }

            return emaValues;
        }

        // WT Cross
        public static List<decimal> WtCross(List<CandleMinute> candles, int channelLength = 9, int averageLength = 12)
        {
            if (candles == null || candles.Count < channelLength + averageLength)
                throw new ArgumentException("캔들 데이터가 부족합니다.");

            List<decimal> typicalPrices = new List<decimal>();
            for (int i = 0; i < candles.Count; i++)
            {
                decimal tp = (candles[i].HighPrice + candles[i].LowPrice + candles[i].TradePrice) / 3;
                typicalPrices.Add(tp);
            }

            // TypicalPrice 기준 EMA
            List<decimal> esa = EmaList(typicalPrices, channelLength);

            List<decimal> dList = new List<decimal>();
            for (int i = 0; i < typicalPrices.Count; i++)
            {
                dList.Add(Math.Abs(typicalPrices[i] - esa[i]));
            }

            List<decimal> de = EmaList(dList, channelLength);

            List<decimal> ci = new List<decimal>();
            for (int i = 0; i < typicalPrices.Count; i++)
            {
                if (de[i] == 0)
                    ci.Add(0);
                else
                    ci.Add((typicalPrices[i] - esa[i]) / (0.015m * de[i]));
            }

            List<decimal> wt = EmaList(ci, averageLength);

            return wt;
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

        // POC
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
        public static double CCI(List<CandleMinute> candles, int period = 20)
        {
            var recentCandles = candles.Take(period).ToList();
            var latestCandle = recentCandles[0];

            decimal typicalPrice = (latestCandle.HighPrice + latestCandle.LowPrice + latestCandle.TradePrice) / 3;

            decimal sma = recentCandles.Average(c => (c.HighPrice + c.LowPrice + c.TradePrice) / 3);

            decimal meanDeviation = recentCandles.Average(c => Math.Abs(((c.HighPrice + c.LowPrice + c.TradePrice) / 3) - sma));

            if (meanDeviation == 0)
                return 0;

            return (double)((typicalPrice - sma) / (0.015m * meanDeviation));
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


        // MACD
        public static MACD MACD(List<CandleMinute> candles, int shortPeriod = 12, int longPeriod = 26, int signalPeriod = 9)
        {
            if (candles == null || candles.Count < longPeriod + signalPeriod)
                throw new ArgumentException("캔들 데이터가 부족합니다.");

            // 1. Short EMA (12) & Long EMA (26)
            List<decimal> shortEma = EMAHistory(candles, shortPeriod);
            List<decimal> longEma = EMAHistory(candles, longPeriod);

            List<decimal> macdLine = new List<decimal>(new decimal[candles.Count]);
            for (int i = 0; i < candles.Count; i++)
            {
                macdLine[i] = shortEma[i] - longEma[i];
            }

            // 2. Signal Line (MACD의 EMA 9)
            List<decimal> signalLine = EmaList(macdLine, signalPeriod);

            // 3. Histogram (MACD - Signal)
            List<decimal> histogram = new List<decimal>(new decimal[candles.Count]);
            for (int i = 0; i < candles.Count; i++)
            {
                histogram[i] = macdLine[i] - signalLine[i];
            }

            return new MACD() { MacdLine = macdLine, SignalLine = signalLine, Histogram = histogram };
        }


        // Stochastic Oscillator
        public static Stochastic Stochastic(List<CandleMinute> candles, int kPeriod = 14, int dPeriod = 3)
        {
            if (candles == null || candles.Count < kPeriod + dPeriod)
                throw new ArgumentException("캔들 데이터가 부족합니다.");

            List<decimal> kValues = new List<decimal>(new decimal[candles.Count]);
            List<decimal> dValues = new List<decimal>(new decimal[candles.Count]);

            // %K 계산
            for (int i = candles.Count - kPeriod; i >= 0; i--)
            {
                decimal highestHigh = candles.Skip(i).Take(kPeriod).Max(c => c.HighPrice);
                decimal lowestLow = candles.Skip(i).Take(kPeriod).Min(c => c.LowPrice);
                decimal currentClose = candles[i].TradePrice;

                if (highestHigh == lowestLow)
                {
                    kValues[i] = 50; // 변동 없으면 50 고정
                }
                else
                {
                    kValues[i] = (currentClose - lowestLow) / (highestHigh - lowestLow) * 100;
                }
            }

            // %D 계산 (K값들의 이동평균)
            for (int i = candles.Count - kPeriod; i >= 0; i--)
            {
                if (i + dPeriod <= candles.Count)
                {
                    decimal sumK = 0;
                    for (int j = i; j < i + dPeriod; j++)
                    {
                        sumK += kValues[j];
                    }
                    dValues[i] = sumK / dPeriod;
                }
            }


            return new Stochastic() { K = kValues, D = dValues };
        }


        // BollingerBand
        public static BollingerBand BollingerBand(List<CandleMinute> candles, int period = 20, decimal k = 2m)
        {
            if (candles == null || candles.Count < period)
                throw new ArgumentException($"캔들 데이터가 부족합니다. 최소 {period}개의 캔들이 필요합니다.");

            // 최신 N개 캔들에서 종가만 추출
            var recentCloses = candles.Take(period).Select(c => c.TradePrice).ToList();

            // 평균 계산 (Basis)
            decimal mean = recentCloses.Average();

            // 표준편차 계산
            double variance = recentCloses
                .Select(p => Math.Pow((double)(p - mean), 2))
                .Average();
            decimal stdDev = (decimal)Math.Sqrt(variance);

            // 밴드 계산
            decimal upper = mean + k * stdDev;
            decimal lower = mean - k * stdDev;

            return new BollingerBand
            {
                Basis = mean,
                UpperBand = upper,
                LowerBand = lower
            };
        }


        public static bool GetEMAConvergenceStates(List<decimal[]> emaArrays, int lookback = 6, decimal threshold = 0.005m)
        {
            for (int i = 1; i <= lookback; i++)
            {
                decimal minEMA = decimal.MaxValue;
                decimal maxEMA = decimal.MinValue;

                foreach (decimal[] emaArray in emaArrays)
                {
                    minEMA = Math.Min(minEMA, emaArray[i]);
                    maxEMA = Math.Max(maxEMA, emaArray[i]);
                }

                if ((maxEMA - minEMA) / maxEMA > threshold)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsEmaTrendingUp(decimal[] ema, int lookback = 5)
        {
            if (ema.Length < lookback + 1)
                return false;

            for (int i = 0; i < lookback; i++)
            {
                if (ema[i] <= ema[i + 1])  // candles[0]이 최신일 때
                    return false;
            }

            return true;
        }

        public static bool IsBullishEngulfingCandle(List<CandleMinute> candles)
        {
            return candles[2].LowPrice < candles.Skip(2).Take(10).Min(c => c.LowPrice) &&
                   candles[2].OpeningPrice > candles[2].TradePrice &&
                   candles[2].LowPrice <= candles[1].LowPrice &&
                   candles[2].TradePrice == candles[1].OpeningPrice &&
                   candles[2].HighPrice < candles[1].HighPrice &&
                   candles[2].CandleAccTradeVolume < candles[1].CandleAccTradeVolume;
        }

        public static bool IsDojiCandle(CandleMinute candle)
        {
            decimal candleLowHighGap = candle.HighPrice - candle.LowPrice;
            return candle.LowPrice + candleLowHighGap * 0.45m <= candle.OpeningPrice &&
                   candle.LowPrice + candleLowHighGap * 0.45m <= candle.TradePrice &&
                   candle.LowPrice + candleLowHighGap * 0.55m >= candle.OpeningPrice &&
                   candle.LowPrice + candleLowHighGap * 0.55m >= candle.TradePrice;
        }


        public static SupportResistanceLevels GetSupportResistanceLevels(
            List<CandleMinute> candles, int range = 60, decimal priceRangePercent = 0.001m, int minTouches = 3)
        {
            if (candles == null || candles.Count < range)
                throw new ArgumentException("Not enough candle data.");

            var recentCandles = candles.Take(range).ToList();

            // 1. 종가 기준 지지선 및 저항선 계산
            var tradePrices = recentCandles.Select(c => c.TradePrice).ToList();
            var supportCandidates = GetTouchPriceLevels(tradePrices, priceRangePercent, minTouches, true);
            var resistanceCandidates = GetTouchPriceLevels(tradePrices, priceRangePercent, minTouches, false);

            decimal supportLine = supportCandidates.Any() ? supportCandidates.Min() : 0;
            decimal resistanceLine = resistanceCandidates.Any() ? resistanceCandidates.Max() : 0;

            // 2. 손절라인 계산: 지지선에서 0.5% 낮은 값으로 설정
            decimal stopLossLine = supportLine * (1 - 0.005m); // 0.5% 만큼 낮추기

            return new SupportResistanceLevels
            {
                SupportLine = supportLine,
                ResistanceLine = resistanceLine,
                StopLossLine = stopLossLine
            };
        }

        private static List<decimal> GetTouchPriceLevels(
            List<decimal> prices, decimal rangePercent, int minTouches, bool isSupport)
        {
            var levels = new List<decimal>();
            foreach (var price in prices)
            {
                bool found = false;
                foreach (var level in levels)
                {
                    if (Math.Abs(price - level) / level <= rangePercent)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    levels.Add(price);
                }
            }

            var levelCounts = new Dictionary<decimal, int>();

            foreach (var level in levels)
            {
                int count = prices.Count(p => Math.Abs(p - level) / level <= rangePercent);
                if (count >= minTouches)
                {
                    levelCounts[level] = count;
                }
            }

            // 지지선은 낮은 값 우선, 저항선은 높은 값 우선
            return isSupport
                ? levelCounts.OrderBy(k => k.Key).Select(k => k.Key).ToList()
                : levelCounts.OrderByDescending(k => k.Key).Select(k => k.Key).ToList();
        }


        public static decimal Slope((decimal x1, decimal y1) point1, (decimal x2, decimal y2) point2)
        {
            if (point2.x2 == point1.x1)
                throw new DivideByZeroException("x1과 x2가 같습니다. 기울기를 계산할 수 없습니다.");

            decimal slope = (point2.y2 - point1.y1) / (point2.x2 - point1.x1);
            return slope;
        }
    }
}

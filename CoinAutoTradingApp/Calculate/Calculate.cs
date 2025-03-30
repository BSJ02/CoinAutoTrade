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
        public static List<double> RSIHistory(List<CandleMinute> candles, int period = 14)
        {
            List<double> rsiValues = new List<double>();

            if (candles.Count < period) return rsiValues; // 데이터가 부족하면 빈 리스트 반환

            List<double> gains = new List<double>();
            List<double> losses = new List<double>();

            for (int i = 1; i < period; i++)
            {
                double change = candles[i].TradePrice - candles[i - 1].TradePrice;
                gains.Add(Math.Max(0, change));
                losses.Add(Math.Max(0, -change));
            }

            double avgGain = gains.Average();
            double avgLoss = losses.Average();

            double rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            rsiValues.Add(100 - (100 / (1 + rs)));

            for (int i = period; i < candles.Count; i++)
            {
                double change = candles[i].TradePrice - candles[i - 1].TradePrice;
                double gain = Math.Max(0, change);
                double loss = Math.Max(0, -change);

                gains.Add(gain);
                losses.Add(loss);

                avgGain = ((avgGain * (period - 1)) + gain) / period;
                avgLoss = ((avgLoss * (period - 1)) + loss) / period;

                rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                rsiValues.Add(100 - (100 / (1 + rs)));
            }

            return rsiValues;
        }


        // ATR
        public static List<double> ATRHistory(List<CandleMinute> candles, int period = 14)
        {
            List<double> atrHistory = new List<double>();

            List<double> trValues = new List<double>();

            for (int i = 1; i < candles.Count; i++)
            {
                double high = candles[i].HighPrice;
                double low = candles[i].LowPrice;
                double prevClose = candles[i - 1].TradePrice;

                double tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
                trValues.Add(tr);
            }

            for (int i = period - 1; i < trValues.Count; i++)
            {
                atrHistory.Add(trValues.Skip(i - period + 1).Take(period).Average());
            }

            return atrHistory;
        }


        // MACD
        public static (double macd, double signal, double histogram) MACD(List<CandleMinute> candles, int shortPeriod = 12, int longPeriod = 26, int signalPeriod = 9)
        {
            if (candles.Count < longPeriod) return (0, 0, 0); // 충분한 데이터가 없으면 0 반환

            List<double> shortEMAHistory = EMAHistory(candles, shortPeriod);
            List<double> longEMAHistory = EMAHistory(candles, longPeriod);

            double macd = shortEMAHistory.Last() - longEMAHistory.Last();

            List<double> macdList = shortEMAHistory.Zip(longEMAHistory, (shortEma, longEma) => shortEma - longEma).ToList();

            List<double> signalHistory = EMAHistory(macdList.Select(m => new CandleMinute { TradePrice = m }).ToList(), signalPeriod);
            double signal = signalHistory.Last();

            double histogram = macd - signal;

            return (macd, signal, histogram);
        }


        // EMA
        public static List<double> EMAHistory(List<CandleMinute> candles, int period)
        {
            List<double> emaValues = new List<double>();

            if (candles.Count < period)
                return emaValues; // 데이터 부족 시 빈 리스트 반환

            decimal multiplier = 2m / (period + 1);

            double sma = candles.Take(period).Average(c => c.TradePrice);
            emaValues.Add(sma);

            for (int i = period; i < candles.Count; i++)
            {
                sma = ((candles[i].TradePrice - sma) * (double)multiplier) + sma;
                emaValues.Add(sma);
            }

            return emaValues;
        }


        // BollingerBand
        public static (double upperBand, double lowerBand, double movingAverage) BollingerBands(List<CandleMinute> candles, int period = 20, int multiplier = 2)
        {
            double movingAverage = candles.Take(period).Average(c => c.TradePrice);

            double sumOfSquares = candles.Take(period).Sum(c => Math.Pow((double)(c.TradePrice - movingAverage), 2));
            double standardDeviation = Math.Sqrt((double)(sumOfSquares / period));

            double upperBand = movingAverage + multiplier * standardDeviation;
            double lowerBand = movingAverage - multiplier * standardDeviation;

            return (upperBand, lowerBand, movingAverage);
        }


        // ADX
        public static double ADX(List<CandleMinute> candles, int period = 14)
        {
            if (candles.Count < period * 2) return 0;

            List<double> dxValues = new List<double>();

            for (int i = period; i < candles.Count; i++)
            {
                var (diPlus, diMinus) = DI(candles.GetRange(i - period, period + 1), period);
                double dx = Math.Abs(diPlus - diMinus) / (diPlus + diMinus) * 100;
                dxValues.Add(dx);
            }

            double adx = dxValues.Take(period).Average();

            for (int i = period; i < dxValues.Count; i++)
            {
                adx = ((adx * (period - 1)) + dxValues[i]) / period;
            }

            return adx;
        }

        // DI
        public static (double diPlus, double diMinus) DI(List<CandleMinute> candles, int period = 14)
        {
            if (candles.Count < period) return (0, 0);

            double smoothedPlusDM = 0, smoothedMinusDM = 0, smoothedTR = 0;

            for (int i = 1; i < candles.Count; i++)
            {
                double highDiff = candles[i].HighPrice - candles[i - 1].HighPrice;
                double lowDiff = candles[i - 1].LowPrice - candles[i].LowPrice;

                double currentTR = Math.Max(candles[i].HighPrice - candles[i].LowPrice,
                                    Math.Max(Math.Abs(candles[i].HighPrice - candles[i - 1].TradePrice),
                                             Math.Abs(candles[i].LowPrice - candles[i - 1].TradePrice)));

                double plusDM = (highDiff > lowDiff && highDiff > 0) ? highDiff : 0;
                double minusDM = (lowDiff > highDiff && lowDiff > 0) ? lowDiff : 0;

                if (i < period)
                {
                    smoothedPlusDM += plusDM;
                    smoothedMinusDM += minusDM;
                    smoothedTR += currentTR;
                }
                else
                {
                    smoothedPlusDM = (smoothedPlusDM - smoothedPlusDM / period) + plusDM;
                    smoothedMinusDM = (smoothedMinusDM - smoothedMinusDM / period) + minusDM;
                    smoothedTR = (smoothedTR - smoothedTR / period) + currentTR;
                }
            }

            if (smoothedTR == 0) return (0, 0); // 0으로 나누는 오류 방지

            double diPlus = (smoothedPlusDM / smoothedTR) * 100;
            double diMinus = (smoothedMinusDM / smoothedTR) * 100;

            return (diPlus, diMinus);
        }



        // Parabolic SAR
        public static double ParabolicSAR(List<CandleMinute> candles, double af = 0.02, double maxAf = 0.2)
        {
            if (candles.Count < 2) return 0;

            double sar = candles[0].LowPrice;
            double ep = candles[0].HighPrice;
            double acceleration = af;
            bool isUptrend = true;

            for (int i = 1; i < candles.Count; i++)
            {
                sar = sar + acceleration * (ep - sar);

                if (isUptrend)
                {
                    if (candles[i].TradePrice < sar)
                    {
                        isUptrend = false;
                        sar = ep;
                        acceleration = af;
                    }
                    else if (candles[i].HighPrice > ep)
                    {
                        ep = candles[i].HighPrice;
                        acceleration = Math.Min(acceleration + af, maxAf);
                    }
                }
                else
                {
                    if (candles[i].TradePrice > sar)
                    {
                        isUptrend = true;
                        sar = ep;
                        acceleration = af;
                    }
                    else if (candles[i].LowPrice < ep)
                    {
                        ep = candles[i].LowPrice;
                        acceleration = Math.Min(acceleration + af, maxAf);
                    }
                }
            }

            return sar;
        }


        // Stochastic
        public static (double k, double d) StochasticOscillator(List<CandleMinute> candles, int period = 14, int smooth = 3)
        {
            if (candles.Count < period) return (0, 0);

            var lastCandles = candles.Take(period).ToList();  // 최신 캔들부터 period만큼 가져오기

            double highestHigh = lastCandles.Max(c => c.HighPrice);
            double lowestLow = lastCandles.Min(c => c.LowPrice);

            double currentClose = candles[0].TradePrice;  // candles[0]이 최신 캔들이므로 가장 최근 종가를 사용
            double k = ((currentClose - lowestLow) / (highestHigh - lowestLow)) * 100;

            List<double> kValues = new List<double>();
            for (int i = smooth - 1; i < period; i++)
            {
                var smoothCandles = candles.Skip(i).Take(smooth).ToList();  // sliding window 방식으로 smooth기간만큼 데이터 가져오기
                double tempHigh = smoothCandles.Max(c => c.HighPrice);
                double tempLow = smoothCandles.Min(c => c.LowPrice);
                double tempK = ((smoothCandles.Last().TradePrice - tempLow) / (tempHigh - tempLow)) * 100;
                kValues.Add(tempK);
            }

            double d = kValues.Average();

            return (k, d);
        }



        // CCI
        public static double CCI(List<CandleMinute> candles, int period = 20)
        {
            if (candles.Count < period) return 0;

            var lastCandles = candles.Take(period).ToList();  // 최신 캔들부터 period만큼 가져옴
            double typicalPrice = lastCandles[0].HighPrice + lastCandles[0].LowPrice + lastCandles[0].TradePrice;
            typicalPrice /= 3;

            double sma = lastCandles.Average(c => (c.HighPrice + c.LowPrice + c.TradePrice) / 3);

            double meanDeviation = lastCandles.Average(c => Math.Abs(((c.HighPrice + c.LowPrice + c.TradePrice) / 3) - sma));

            return (typicalPrice - sma) / (0.015 * meanDeviation);
        }

        public static List<double> CCIHistory(List<CandleMinute> candles, int period = 20)
        {
            List<double> cciHistory = new List<double>();

            if (candles.Count < period) return cciHistory; // 데이터가 부족하면 빈 리스트 반환

            var firstSubset = candles.Take(period).ToList();
            double sma = firstSubset.Average(c => (c.HighPrice + c.LowPrice + c.TradePrice) / 3);
            double meanDeviation = firstSubset.Average(c => Math.Abs(((c.HighPrice + c.LowPrice + c.TradePrice) / 3) - sma));
            cciHistory.Add((firstSubset.Last().HighPrice + firstSubset.Last().LowPrice + firstSubset.Last().TradePrice) / 3 - sma / (0.015 * meanDeviation));

            for (int i = period; i < candles.Count; i++)
            {
                var prevCandle = candles[i - period];
                var currentCandle = candles[i];

                double typicalPrice = (currentCandle.HighPrice + currentCandle.LowPrice + currentCandle.TradePrice) / 3;
                sma = sma + (typicalPrice - ((prevCandle.HighPrice + prevCandle.LowPrice + prevCandle.TradePrice) / 3)) / period;
                meanDeviation = meanDeviation + (Math.Abs(typicalPrice - sma) - Math.Abs(((prevCandle.HighPrice + prevCandle.LowPrice + prevCandle.TradePrice) / 3) - sma)) / period;

                double cci = (typicalPrice - sma) / (0.015 * meanDeviation);
                cciHistory.Add(cci);
            }

            return cciHistory;
        }


        // Ichimoku
        public static (double tenkan, double kijun, double senkouA, double senkouB, double chikou) IchimokuCloud(List<CandleMinute> candles)
        {
            int tenkanPeriod = 9, kijunPeriod = 26, senkouBPeriod = 52;

            if (candles.Count < Math.Max(tenkanPeriod, Math.Max(kijunPeriod, senkouBPeriod)))
                return (0, 0, 0, 0, 0); // 데이터 부족 시 0 반환

            var tenkanCandles = candles.Take(tenkanPeriod);
            var kijunCandles = candles.Take(kijunPeriod);
            var senkouBCandles = candles.Take(senkouBPeriod);

            double tenkan = (tenkanCandles.Max(c => c.HighPrice) + tenkanCandles.Min(c => c.LowPrice)) / 2;
            double kijun = (kijunCandles.Max(c => c.HighPrice) + kijunCandles.Min(c => c.LowPrice)) / 2;
            double senkouA = (tenkan + kijun) / 2;
            double senkouB = (senkouBCandles.Max(c => c.HighPrice) + senkouBCandles.Min(c => c.LowPrice)) / 2;

            double chikou = candles[Math.Min(26, candles.Count - 1)].TradePrice;

            return (tenkan, kijun, senkouA, senkouB, chikou);
        }


        // VWAP
        public static double VWAP(List<CandleMinute> candles)
        {
            if (candles == null || candles.Count == 0)
                return 0; // 캔들 리스트가 비어 있을 경우 처리

            double totalPriceVolume = 0, totalVolume = 0;

            foreach (var c in candles)
            {
                totalPriceVolume += c.TradePrice * c.CandleAccTradeVolume;
                totalVolume += c.CandleAccTradeVolume;
            }

            return totalPriceVolume / Math.Max(totalVolume, 1e-10);  // 0 나누기 방지
        }


        // Keltner Channel
        public static (double upper, double middle, double lower) KeltnerChannel(List<CandleMinute> candles, int period = 20, double atrMultiplier = 1)
        {
            if (candles.Count < period) return (0, 0, 0); // 데이터 부족 시 0 반환

            double ema = EMAHistory(candles, period).Last();  // 중앙선 (Middle Line)
            double atr = ATRHistory(candles, period).Last();

            double keltnerUpper = ema + atrMultiplier * atr;  // 상단 밴드
            double keltnerLower = ema - atrMultiplier * atr;  // 하단 밴드

            return (keltnerUpper, ema, keltnerLower);
        }



        // On-Balance Volume (OBV)
        public static double OBV(List<CandleMinute> candles)
        {
            if (candles.Count < 2) return 0; // 최소 2개의 캔들 필요

            double obv = 0; // OBV를 0으로 시작

            for (int i = 1; i < candles.Count; i++)
            {
                if (candles[i].TradePrice > candles[i - 1].TradePrice)
                {
                    obv += candles[i].CandleAccTradeVolume; // 가격 상승 시 거래량 추가
                }
                else if (candles[i].TradePrice < candles[i - 1].TradePrice)
                {
                    obv -= candles[i].CandleAccTradeVolume; // 가격 하락 시 거래량 차감
                }
            }

            return obv;
        }
        public static List<double> OBVHistory(List<CandleMinute> candles)
        {
            List<double> obvHistory = new List<double>();

            if (candles.Count == 0) return obvHistory; // 빈 리스트 예외 처리

            double obv = candles[0].CandleAccTradeVolume; // 첫 번째 거래량을 초기 OBV 값으로 설정
            obvHistory.Add(obv); // OBV 초기값 리스트에 추가

            for (int i = 1; i < candles.Count; i++)
            {
                if (candles[i].TradePrice > candles[i - 1].TradePrice)
                {
                    obv += candles[i].CandleAccTradeVolume; // 가격 상승 시 거래량 더함
                }
                else if (candles[i].TradePrice < candles[i - 1].TradePrice)
                {
                    obv -= candles[i].CandleAccTradeVolume; // 가격 하락 시 거래량 차감
                }
                else
                {
                    obv = obv;
                }

                obvHistory.Add(obv); // 계산된 OBV 값을 리스트에 추가
            }

            return obvHistory; // 최종 OBV 값 리스트 반환
        }


        // VolumeHistory
        public static List<double> VolumeHistory(List<CandleMinute> candles, int period = 14)
        {
            List<double> volumeHistory = new List<double>();

            if (candles.Count < period) return volumeHistory; // 충분한 데이터가 없으면 빈 리스트 반환

            double sumVolume = candles.Take(period).Sum(c => c.CandleAccTradeVolume); // 초기 period 개수만큼의 합
            volumeHistory.Add(sumVolume / period); // 첫 번째 평균을 리스트에 추가

            for (int i = period; i < candles.Count; i++)
            {
                sumVolume -= candles[i - period].CandleAccTradeVolume;
                sumVolume += candles[i].CandleAccTradeVolume;

                volumeHistory.Add(sumVolume / period); // 새로운 평균을 리스트에 추가
            }

            return volumeHistory; // 최종 거래량 이동 평균 리스트 반환
        }
    }
}

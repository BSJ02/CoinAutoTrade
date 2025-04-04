﻿using CoinAutoTradingApp.Models;
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
        public static (double[] pdi, double[] mdi, double[] adx) DMI(List<CandleMinute> candles, int period = 14, int count = 14)
        {
            List<double> pdi = new List<double>();
            List<double> mdi = new List<double>();
            List<double> adx = new List<double>();

            double pDmSum = 0;
            double mDmSum = 0;
            double trSum = 0;

            for (int i = period + count; i > period - 2; i--)
            {
                double currHigh = candles[i].HighPrice;
                double currLow = candles[i].LowPrice;

                double prevHigh = candles[i + 1].HighPrice;
                double prevLow = candles[i + 1].LowPrice;
                double prevClose = candles[i + 1].TradePrice;

                double pDm = Math.Max(currHigh - prevHigh, 0);
                double mDm = Math.Max(prevLow - currLow, 0);

                double tr = Math.Max(Math.Abs(currHigh - currLow),
                              Math.Max(Math.Abs(currHigh - prevClose), Math.Abs(currLow - prevClose)));

                pDmSum += pDm;
                mDmSum += mDm;
                trSum += tr;
            }

            double initPDI = 100 * (pDmSum / trSum);
            double initMDI = 100 * (mDmSum / trSum);
            pdi.Add(initPDI);
            mdi.Add(initMDI);

            double initADX = 100 * Math.Abs(initPDI - initMDI) / (initPDI + initMDI);
            adx.Add(initADX);

            for (int i = period - 2; i >= 0; i--)
            {
                double pDm = Math.Max(candles[i].HighPrice - candles[i + 1].HighPrice, 0);
                double mDm = Math.Max(candles[i + 1].LowPrice - candles[i].LowPrice, 0);

                double tr = Math.Max(candles[i].HighPrice - candles[i].LowPrice,
                             Math.Max(Math.Abs(candles[i].HighPrice - candles[i + 1].TradePrice),
                                      Math.Abs(candles[i].LowPrice - candles[i + 1].TradePrice)));

                pDmSum = pDmSum - (pDmSum / period) + pDm;
                mDmSum = mDmSum - (mDmSum / period) + mDm;
                trSum = trSum - (trSum / period) + tr;

                double currPDI = 100 * (pDmSum / trSum);
                double currMDI = 100 * (mDmSum / trSum);
                pdi.Add(currPDI);
                mdi.Add(currMDI);

                double currDX = 100 * Math.Abs(currPDI - currMDI) / (currPDI + currMDI);
                double currADX = ((adx.Last() * (period - 1)) + currDX) / period;
                adx.Add(currADX);
            }

            return (pdi.ToArray(), mdi.ToArray(), adx.ToArray());
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
            double standardDeviation = Math.Sqrt(sumOfSquares / period - 1);

            double upperBand = movingAverage + multiplier * standardDeviation;
            double lowerBand = movingAverage - multiplier * standardDeviation;

            return (upperBand, lowerBand, movingAverage);
        }


        // CCI
        public static double CCI(List<CandleMinute> candles, int period = 20)
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

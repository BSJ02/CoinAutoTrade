﻿using CoinAutoTradingApp.Enum;
using CoinAutoTradingApp.UpbitAPI.Models;
using CoinAutoTradingApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;


namespace CoinAutoTradingApp;
public partial class TradePage : ContentPage
{
    // 매수 조건
    public bool IsBuyConditionONE((double upper, double middle, double lower) keltner,
                                         (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                          double cci9, double rsi, double atr, List<CandleMinute> minCandles)
    {
        if (isHaveMarket)
            return false;


        // ✅ 필수 조건

        // 1: CCI 일정 값
        bool isCciCondition = cci9 > -120 && cci9 < 20;

        // 2: RSI 일정 값
        bool isRsiCondition = rsi > 30 && rsi < 60;

        // 3: 아래꼬리가 긴 캔들인지 확인
        int longLowWickCount = 0;
        bool isLongLowerWick = false;
        for (int i = 3; i >= 2; i--)
        {
            var prevCandles = minCandles.Skip(i).ToList();
            var prevBands = Calculate.BollingerBands(prevCandles);
            var prevKeltner = Calculate.KeltnerChannel(prevCandles);

            double prevHighLowGap = Math.Abs(prevCandles[0].HighPrice - prevCandles[0].LowPrice);
            double prevLowTradeGap = Math.Abs(prevCandles[0].TradePrice - Math.Min(prevCandles[0].OpeningPrice, prevCandles[0].TradePrice));

            isLongLowerWick = prevCandles[0].TradePrice < prevCandles[0].OpeningPrice &&
                              prevHighLowGap * 0.5 < prevLowTradeGap &&
                              prevCandles[0].HighPrice < Math.Min(prevBands.movingAverage, prevKeltner.middle);

            if (!isLongLowerWick)
                break;

            longLowWickCount++;
        }

        // 4: 이전 캔들 양봉 확인
        bool isBullishCandle = minCandles[1].TradePrice > minCandles[1].OpeningPrice;

        // 5: 조정 줄 때 매수
        bool isTradPriceCondition = minCandles[0].TradePrice <= Math.Min(minCandles[0].OpeningPrice, minCandles[1].LowPrice + ((minCandles[1].HighPrice - minCandles[1].LowPrice) / 2)) &&
                                    minCandles[0].TradePrice < Math.Min(bollingerBands.movingAverage, keltner.middle);


        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;      // 필수 조건 카운트

        // 🔢 필수 조건 카운트
        if (longLowWickCount == 2) { debugMessage += "| LW "; count++; }
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isBullishCandle) { debugMessage += "| BC "; count++; }
        if (isTradPriceCondition)
        {
            debugMessage += "| Price ";
            count++;
        }

        if (!string.IsNullOrEmpty(debugMessage) && count >= 4)
        {
            debugMessage = $"{count}/6 {debugMessage}";
            AddDebugMessage(debugMessage);

            AddDebugMessage(minCandles[0].Market);
        }

        return count >= 5;
    }

    public bool IsBuyConditionTWO((double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 double cci9, double rsi, List<CandleMinute> minCandles)
    {
        if (isHaveMarket)
            return false;


        // ✅ 필수 조건

        // 1: CCI 일정 값
        bool isCciCondition = cci9 > -120 && cci9 < 20;

        // 2: RSI 일정 값
        bool isRsiCondition = rsi > 30 && rsi < 60;

        // 3: 음봉 이후 아래꼬리가 긴 캔들 나오는지 확인
        double prevHighLowGap = Math.Abs(minCandles[3].HighPrice - minCandles[3].LowPrice);
        double prevLowTradeGap = Math.Abs(minCandles[3].TradePrice - Math.Min(minCandles[3].OpeningPrice, minCandles[3].TradePrice));

        bool isLongLowerWick = minCandles[4].TradePrice < minCandles[4].OpeningPrice &&
                               minCandles[3].TradePrice < minCandles[3].OpeningPrice &&
                               prevHighLowGap * 0.5 < prevLowTradeGap;

        // 4: 도지 캔들 화인
        bool isDojiCandle = Math.Abs(minCandles[2].OpeningPrice - minCandles[2].TradePrice) <= (minCandles[2].HighPrice - minCandles[2].LowPrice) * 0.05;

        // 5: 이전 캔들 양봉 확인
        bool isBullishCandle = minCandles[1].TradePrice > minCandles[1].OpeningPrice;

        // 6: 오픈가로 매수
        bool isTradPriceCondition = minCandles[0].TradePrice <= minCandles[0].OpeningPrice &&
                                    minCandles[0].TradePrice < Math.Min(bollingerBands.movingAverage, keltner.middle);

        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;      // 필수 조건 카운트

        // 🔢 필수 조건 카운트
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isLongLowerWick) { debugMessage += "| LW "; count++; }
        if (isBullishCandle) { debugMessage += "| BC "; count++; }
        if (isDojiCandle) { debugMessage += "| DO "; count++; }
        if (isTradPriceCondition)
        {
            debugMessage += "| Price ";
            count++;
        }

        if (!string.IsNullOrEmpty(debugMessage) && count >= 4)
        {
            debugMessage = $"{count}/6 {debugMessage}";
            AddDebugMessage(debugMessage);

            AddDebugMessage(minCandles[0].Market);
        }

        return count >= 6;
    }

    public bool IsBuyConditionTHREE((double upper, double middle, double lower) keltner,
                                    (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                     double cci9, double rsi, List<CandleMinute> minCandles)
    {
        if (isHaveMarket)
            return false;

        bool isTouchedBandLow = false;

        for (int i = 0; i <= 1; i++)
        {
            var prevCandles = minCandles.Skip(i + 1).ToList();
            var prevBollinger = Calculate.BollingerBands(prevCandles);
            var prevKeltner = Calculate.KeltnerChannel(prevCandles);

            isTouchedBandLow = prevCandles[i].LowPrice <= Math.Min(prevBollinger.lowerBand, prevKeltner.lower);

            if (!isTouchedBandLow)
                break;
        }

        double prevHighLowGap = minCandles[1].HighPrice - minCandles[1].LowPrice;

        bool isPriceUpByRatioOfPrevGap = minCandles[0].TradePrice >= minCandles[1].LowPrice + (prevHighLowGap * 0.15) &&
                                         minCandles[0].TradePrice <= minCandles[1].LowPrice + (prevHighLowGap * 0.3) &&
                                         minCandles[0].TradePrice >= minCandles[0].OpeningPrice;

        return isTouchedBandLow;
    }

    // 추가 매수
    public bool ExecuteAdditionalBuy(double currPrice, double avgPrice, double cci9,
                                    (double upper, double middle, double lower) keltner,
                                    (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                     List<CandleMinute> minCandles,
                                     double executeAddPercent = 0.013)
    {
        if (!isHaveMarket)
            return false;

        string market = minCandles[0].Market;

        marketTouchedBandHigh[market] = false;
        marketTouchedBandMiddle[market] = false;

        bool isCciCondition = cci9 > -150;
        bool isTradePriceCondition = avgPrice * (1 - executeAddPercent) > currPrice;

        return isCciCondition && isTradePriceCondition;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci9, double cci14, double atr, double rsi,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;
        
        string market = minCandles[0].Market;

        if (!marketTouchedBandHigh[market])
        {
            marketTouchedBandHigh[market] = minCandles[0].TradePrice >= Math.Min(bollingerBands.upperBand, keltner.upper);
        }
        if (!marketTouchedBandMiddle[market])
        {
            marketTouchedBandMiddle[market] = minCandles[0].TradePrice >= Math.Min(bollingerBands.movingAverage, keltner.middle);
        }

        bool isThouchedBandHigh = marketTouchedBandHigh[market] && cci9 < 130;
        bool isThouchedBandMiddle = marketTouchedBandMiddle[market] && (cci9 < 65 || cci14 < 65);
        bool isAboveEntryPlusAtr = currPrice > avgPrice + atr && cci14 < 0;

        bool isAboveBreakevenPrice = currPrice > (avgPrice * (1 + FeeRate * (marketBuyCount[market] + 1)) + atr * 0.15);

        return isAboveBreakevenPrice && 
               (
                   isThouchedBandHigh ||
                   isAboveEntryPlusAtr ||
                   isThouchedBandMiddle ||
                   avgPrice > (bollingerBands.movingAverage + keltner.middle) / 2
               );
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr,
                               List<CandleMinute> minCandles, double stopLossPercent = 0.015)
    {
        string market = minCandles[0].Market;

        if (!isHaveMarket)
            return false;

        if (marketBuyCount[market] <= 4)
            return false;

        bool isAtrStopLoss = currPrice <= avgPrice * (1 - stopLossPercent) ;

        return isAtrStopLoss;
    }

    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, 
                                             double[] ema9, double[] ema20, double[] ema50, double[] ema100,
                                             double cci9, double cci14, double atr, double rsi,
                                            (double pdi, double mdi, double adx) dmi,
                                            (double upper, double middle, double lower) keltner, 
                                            (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionONE = IsBuyConditionONE(keltner, bollingerBands, cci9, rsi, atr, minCandles) && isKRWHeld;
        bool isBuyConditionTWO = IsBuyConditionTWO(keltner, bollingerBands, cci9, rsi, minCandles);
        bool isBuyConditionTHREE = IsBuyConditionTHREE(keltner, bollingerBands, cci9, rsi, minCandles);
        // 추가 매수
        bool isExecuteAdditionalBuy = ExecuteAdditionalBuy(currPrice, avgPrice, cci9, keltner, bollingerBands, minCandles);

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci9, cci14, atr, rsi, keltner, bollingerBands, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, minCandles);
        

        string market = minCandles[0].Market;

        // 매도
        if (isStopLoss)
        {
            return ExecuteSellOrder("Stop Loss"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            return ExecuteSellOrder("Take Profit"); // 익절 매도
        }

        // 매수
        if (waitBuyTime.ContainsKey(market))
        {
            if ((DateTime.Now - waitBuyTime[market]).TotalSeconds > 60)
            {
                waitBuyTime.Remove(market);
            }
        }
        else
        {
            if (isExecuteAdditionalBuy)
            {
                return ExecuteBuyOrder("추가 매수"); // 매수
            }
            else if (isBuyConditionONE)
            {
                return ExecuteBuyOrder("꼬리 긴 캔들 이후 양봉"); // 매수
            }
            else if (isBuyConditionTWO)
            {
                return ExecuteBuyOrder("도지 캔들 이후 양봉"); // 매수
            }
            else if (isBuyConditionTHREE)
            {
                return ExecuteBuyOrder("BB 하단 연속 터치 후 양봉"); // 매수
            }
        }

        return TradeType.None;
    }


    // 매수 명령을 실행하는 함수
    public TradeType ExecuteBuyOrder(string strategyType)
    {
        AddChatMessage($"매수 주문 실행: {strategyType} 전략");
        return TradeType.Buy;
    }

    // 매도 명령을 실행하는 함수
    public TradeType ExecuteSellOrder(string strategyType)
    {
        AddChatMessage($"매도 주문 실행: {strategyType} 전략");
        return TradeType.Sell;
    }

}
using CoinAutoTradingApp.Enum;
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
    public bool CheckShortTermBuyCondition((double[] pdi, double[] mdi, double[] adx) dmi,
                                           (double upper, double middle, double lower) keltner,
                                           (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                            double cci, double rsi, double atr, List<CandleMinute> minCandles)
    {
        bool isEmaCondition = minCandles.Skip(1).Take(3).Min(c => c.LowPrice) <= minCandles[0].LowPrice;

        bool isCciCondition = cci > -85 && cci <= 20;

        bool isRsiCondition = rsi > 33 && rsi < 50;

        int skipIndex = 8;
        int takeIndex = dmi.pdi.Length - skipIndex - 1;
        bool isDMICondition = dmi.mdi.Last() > dmi.pdi.Last() &&
                              dmi.mdi.Last() <= dmi.pdi.Last() + (dmi.adx.Last() * 0.6) && 
                              dmi.pdi.Skip(skipIndex).Take(takeIndex).Min() < dmi.pdi.Last() &&
                              dmi.mdi.Skip(skipIndex).Take(takeIndex).Max() > dmi.mdi.Last();

        int period = 3;
        bool isBollingerBandsCoindition = false;

        double dynamicATR = atr * 1.5 + minCandles[0].TradePrice > Math.Min(bollingerBands.movingAverage, keltner.middle) ?
                            atr * 0.8 : atr;

        for (int i = period; i >= 0; i--)
        {
            if (i == 0)
            {
                isBollingerBandsCoindition &= minCandles[i].HighPrice <= Math.Min(bollingerBands.movingAverage, keltner.middle) &&
                                              minCandles[i].TradePrice < Math.Min(Math.Min(bollingerBands.movingAverage, keltner.middle),
                                                                                  Math.Min(bollingerBands.lowerBand, keltner.lower) + dynamicATR) &&
                                              minCandles[i].LowPrice >= Math.Min(bollingerBands.lowerBand, keltner.lower);
            }
            else
            {
                var prevKeltner = Calculate.KeltnerChannel(minCandles.Skip(i).ToList());
                var prevBollingerBands = Calculate.BollingerBands(minCandles.Skip(i).ToList());

                isBollingerBandsCoindition = minCandles[i].TradePrice >= Math.Min(prevBollingerBands.lowerBand, prevKeltner.lower) &&
                                             minCandles[i].HighPrice < Math.Min(prevBollingerBands.movingAverage, prevKeltner.middle);
            }

            if (!isBollingerBandsCoindition)
                break;
        }

        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;

        if (isEmaCondition) { debugMessage += "| EMA "; count++; }
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isBollingerBandsCoindition) { debugMessage += "| BAND "; count++; }
        if (isDMICondition) { debugMessage += "| DMI "; count++; }

        if (!string.IsNullOrEmpty(debugMessage) && count >= 3)
        {
            debugMessage = $"{count}/5 {debugMessage}";
            AddDebugMessage(debugMessage);
            AddDebugMessage($"{minCandles[0].Market}");
        }

        return isEmaCondition && isCciCondition && isRsiCondition && isBollingerBandsCoindition && isDMICondition;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci, double atr, double rsi,
                                (double[] pdi, double[] mdi, double[] adx) dmi,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 List<CandleMinute> minCandles)
    {
        if (!avgBuyPrice.ContainsKey(minCandles[0].Market))
            return false;

        int period = 3;
        bool isUpperBandsCoindition = false;
        bool isMiddleBandsCoindition = false;

        for (int i = 1; i <= period; i++)
        {
            var prevKeltner = Calculate.KeltnerChannel(minCandles.Skip(i).ToList());
            var prevBollingerBands = Calculate.BollingerBands(minCandles.Skip(i).ToList());

            if (!isUpperBandsCoindition)
            {
                isUpperBandsCoindition = minCandles[i].HighPrice >= Math.Min(prevBollingerBands.upperBand, prevKeltner.upper);
            }

            if (!isMiddleBandsCoindition)
            {
                isMiddleBandsCoindition = minCandles[i].HighPrice >= Math.Max(prevBollingerBands.movingAverage, prevKeltner.middle);
            }
        }

        double dynamicATR = atr * 1.5 + minCandles[0].TradePrice > Math.Min(bollingerBands.movingAverage, keltner.middle) ?
                            atr * 0.8 : atr;

        // 디버그 메시지 추가
        string debugMessage = "";
        int count = 0;

        if (cci <= 110 && isUpperBandsCoindition) { debugMessage += "| UpperBand "; count++; }
        if (cci <= 10 && isMiddleBandsCoindition) { debugMessage += "| MiddleBand "; count++; }
        if (cci <= 0 && currPrice > avgPrice + dynamicATR) { debugMessage += "| AvgPrice + ATR "; count++; }
        if (cci >= 100 && rsi > 50 && dmi.adx.Last() >= 35) { debugMessage += "| CCI,RSI,ADX "; count++; }

        if (!string.IsNullOrEmpty(debugMessage))
        {
            debugMessage = $"{count}/4 {debugMessage}";
            AddChatMessage(debugMessage);
            AddChatMessage($"{minCandles[0].Market} 익절");
        }

        return (cci <= 110 && isUpperBandsCoindition) ||
               (cci <= 10 && isMiddleBandsCoindition) ||
               (cci <= 0 && currPrice > avgPrice + dynamicATR) ||
               (cci >= 100 && rsi >= 50 && dmi.adx.Last() >= 35);
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr, double cci, double rsi,
                              (double[] pdi, double[] mdi, double[] adx) dmi,
                               List<CandleMinute> minCandles, double atrMultiplier = 1.5, double stopLossPercentage = 0.025)
    {
        if (!avgBuyPrice.ContainsKey(minCandles[0].Market))
            return false;

        int skipIndex = 8;
        int takeIndex = dmi.pdi.Length - skipIndex - 1;

        bool isAtrStopLoss = currPrice <= avgPrice - (atr * atrMultiplier);
        bool isPercentageStopLoss = currPrice <= avgPrice * (1 - stopLossPercentage);

        // 디버그 메시지 생성
        string debugMessage = "";
        int count = 0;

        if (isAtrStopLoss) { debugMessage += "| ATR Stop "; count++; }
        if (isPercentageStopLoss) { debugMessage += "| % Stop "; count++; }

        if (!string.IsNullOrEmpty(debugMessage))
        {
            debugMessage = $"{count}/2 {debugMessage}";
            AddChatMessage(debugMessage);
            AddChatMessage($"{minCandles[0].Market} 손절");
        }

        return isAtrStopLoss || isPercentageStopLoss;
    }

    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, 
                                             double[] ema9, double[] ema20, double[] ema50, double[] ema100,
                                             double cci, double atr, double rsi,
                                             (double[] pdi, double[] mdi, double[] adx) dmi,
                                            (double upper, double middle, double lower) keltner, 
                                            (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isShortTermBuy = CheckShortTermBuyCondition(dmi, keltner, bollingerBands, cci, rsi, atr, minCandles) && isKRWHeld;
        
        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci, atr, rsi, dmi, keltner, bollingerBands, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, cci, rsi, dmi, minCandles);
        
        // 매수 및 매도 로직
        if (isStopLoss)
        {
            return ExecuteSellOrder("Stop Loss"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            return ExecuteSellOrder("Take Profit"); // ATR 기반 손절 매도
        }

        // 매수 조건 처리
        else if (isShortTermBuy)
        {
            return ExecuteBuyOrder("Short Term Buy"); // 단기 트레이딩 매수
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
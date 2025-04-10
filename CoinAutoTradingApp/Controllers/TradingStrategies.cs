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
    public bool IsTechnicalPullbackEntry((double pdi, double mdi, double adx) dmi,
                                         (double upper, double middle, double lower) keltner,
                                         (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                          double cci9, double cci14, double rsi, double atr, List<CandleMinute> minCandles)
    {
        // 🔶 보조 조건 1: 눌림목 (최근 저점보다 높음)
        bool isEmaCondition = minCandles.Skip(1).Take(5).Min(c => (c.LowPrice + atr * 0.01)) <= minCandles[0].LowPrice;

        // 🔶 보조 조건 2
        int period = 5;

        bool isDMICondition = false;

        double minValue = double.MaxValue;
        double minLowPrice = double.MaxValue;
        double minCCI = double.MaxValue;
        double minADX = double.MaxValue;

        (double[] pdi, double[] mdi) dmiArray = (new double[period], new double[period]);

        for (int i = period; i > 0; i--)
        {
            if (i <= 3)
            {
                var prevKeltner = Calculate.KeltnerChannel(minCandles.Skip(i).ToList());
                var prevBollingerBands = Calculate.BollingerBands(minCandles.Skip(i).ToList());

                minCCI = Math.Min(minCCI, Calculate.CCI(minCandles.Skip(i).ToList()));

                minValue = Math.Min(minValue, Math.Min(prevBollingerBands.lowerBand, prevKeltner.lower));
            }

            // 🔶 보조 조건 2: DMI 약세 → 반전 가능성
            var prevDMI = Calculate.DMI(minCandles.Skip(i).ToList());

            minADX = Math.Min(minADX, prevDMI.adx);

            dmiArray.pdi[i - 1] = prevDMI.pdi;
            dmiArray.mdi[i - 1] = prevDMI.mdi;
        }

        minLowPrice = Math.Min(minLowPrice, minCandles[0].LowPrice);
        minValue = Math.Min(minValue, Math.Min(bollingerBands.lowerBand, keltner.lower));
        isDMICondition = dmiArray.mdi.Max() > dmi.mdi + 1 &&
                         dmiArray.pdi.Max() > dmi.pdi + 1;

        double prevCCI9 = Calculate.CCI(minCandles.Skip(1).ToList(), 9);
        double prevCCI14 = Calculate.CCI(minCandles.Skip(1).ToList());

        // ✅ 필수 조건
        bool isCciCondition = cci14 > -120 && cci14 < -40 &&
                              cci14 > prevCCI14 + 5 &&
                              cci9 < prevCCI9 &&
                              cci14 > minCCI + 10 &&
                              cci9 > cci14 && cci9 - 10 < cci14;
        bool isRsiCondition = rsi >= 33 && rsi < 55;

        double averageCenter = (bollingerBands.movingAverage + keltner.middle) / 2;
        double adjustedLow = minLowPrice + atr * 0.15;

        bool isTradPriceCondition = minCandles[0].TradePrice <= Math.Min(averageCenter, adjustedLow) &&
                                    minCandles[0].LowPrice >= Math.Min(minValue, minLowPrice) &&
                                    minCandles[0].HighPrice <= averageCenter;

        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;      // 필수 조건 카운트
        int softCount = 0;  // 보조 조건 카운트

        const int softConditionRequired = 2;

        // 🔢 필수 조건 카운트
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isTradPriceCondition)
        {
            debugMessage += "| Price ";
            count++;
        }

        if (!string.IsNullOrEmpty(debugMessage) && count >= softConditionRequired)
        {
            if (count < 3)
            {
                debugMessage = $"필수 {count}/3 {debugMessage}";
                AddDebugMessage(debugMessage);
            }
            else
            {
                debugMessage = "";

                // 🔢 보조 조건 카운트
                if (isEmaCondition) { debugMessage += "| EMA "; softCount++; }
                if (isDMICondition) { debugMessage += "| DMI "; softCount++; }

                debugMessage = $"보조 {softCount}/2 {debugMessage}";
                AddDebugMessage(debugMessage);
            }
            
            AddDebugMessage(minCandles[0].Market);
        }

        return count >= 3 && softCount >= softConditionRequired;
    }

    public bool IsReasonableEntryDip(double cci9,double cci14, double atr, List<CandleMinute> minCandles)
    {
        double prevCCI14 = Calculate.CCI(minCandles.Skip(1).ToList());

        bool isCciCondition = prevCCI14 + 10 < cci14 && cci14 < -120 && cci14 > cci9; // cci14 -120 미만일 때 cci14가 cci9 역전하면 매수
        bool isTradPriceCondition = minCandles[0].TradePrice <= minCandles[1].TradePrice + atr * 0.1;

        return isCciCondition && isTradPriceCondition;
    }


    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci, double atr, double rsi,
                                (double pdi, double mdi, double adx) dmi,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;

        bool isUpperBandsCoindition = minCandles[0].TradePrice >= Math.Min(bollingerBands.upperBand, keltner.upper) - atr * 0.05;
        bool isMiddleBandsCoindition = minCandles[0].TradePrice >= Math.Min(bollingerBands.movingAverage, keltner.middle) - atr * 0.05;
        bool isUpperMiddleAverageCondition = minCandles[0].TradePrice >= ((Math.Min(bollingerBands.upperBand, keltner.upper) + Math.Min(bollingerBands.movingAverage, keltner.middle)) / 2) - atr * 0.05;

        return avgPrice * (1 + FeeRate) < currPrice &&
              (
                  (cci <= 150 && isUpperBandsCoindition) ||
                  (cci <= 50 && isUpperMiddleAverageCondition) ||
                  (cci <= 0 && isMiddleBandsCoindition)
              );
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr, double cci, double rsi,
                              (double pdi, double mdi, double adx) dmi,
                               List<CandleMinute> minCandles, double atrMultiplier = 1.5, double stopLossPercent = 0.015)
    {
        if (!isHaveMarket)
            return false;

        bool isAtrStopLoss = currPrice <= avgPrice - atr ||
                             currPrice <= avgPrice * (1 - stopLossPercent) ;

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
        bool isTechnicalPullbackEntry = IsTechnicalPullbackEntry(dmi, keltner, bollingerBands, cci9, cci14, rsi, atr, minCandles) && isKRWHeld;
        bool isReasonableEntryDip = IsReasonableEntryDip(cci9, cci14, atr, minCandles);

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci14, atr, rsi, dmi, keltner, bollingerBands, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, cci14, rsi, dmi, minCandles);
        
        // 매수 및 매도 로직
        if (isStopLoss)
        {
            return ExecuteSellOrder("Stop Loss"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            return ExecuteSellOrder("Take Profit"); // 익절 매도
        }

        // 매수 조건 처리
        else if (isReasonableEntryDip)
        {
            return ExecuteBuyOrder("Reasonable Buy Condition"); // 매수
        }
        else if (isTechnicalPullbackEntry)
        {
            return ExecuteBuyOrder("Technical Buy Condition"); // 매수
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
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
    public bool CheckBuyCondition((double pdi, double mdi, double adx) dmi,
                                  (double upper, double middle, double lower) keltner,
                                  (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                   double cci, double rsi, double atr, List<CandleMinute> minCandles)
    {
        // 🔶 보조 조건 1: 눌림목 (최근 저점보다 높음)
        bool isEmaCondition = minCandles.Skip(1).Take(3).Min(c => c.LowPrice) <= minCandles[0].LowPrice;

        // 🔶 보조 조건 2, 3
        int period = 5;

        bool isBollingerBandsCondition = true;
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
                // 🔶 보조 조건 2: 볼밴 하단 돌파 후 반등 시도
                var prevKeltner = Calculate.KeltnerChannel(minCandles.Skip(i).ToList());
                var prevBollingerBands = Calculate.BollingerBands(minCandles.Skip(i).ToList());

                minCCI = Math.Min(minCCI, Calculate.CCI(minCandles.Skip(i).ToList()));
                minValue = Math.Min(Math.Min(minValue, minCandles[i].LowPrice), Math.Min(prevBollingerBands.lowerBand, prevKeltner.lower));

                if (isBollingerBandsCondition)
                    isBollingerBandsCondition = minCandles[i].TradePrice >= Math.Min(prevBollingerBands.lowerBand, prevKeltner.lower) &&
                                                minCandles[i].HighPrice < (prevBollingerBands.movingAverage + prevKeltner.middle) / 2;
            }

            // 🔶 보조 조건 3: DMI 약세 → 반전 가능성
            var prevDMI = Calculate.DMI(minCandles.Skip(i).ToList());

            minADX = Math.Min(minADX, prevDMI.adx);
            minLowPrice = Math.Min(minLowPrice, minCandles[i].LowPrice);

            dmiArray.pdi[i - 1] = prevDMI.pdi;
            dmiArray.mdi[i - 1] = prevDMI.mdi;
        }

        isDMICondition = dmi.mdi >= dmi.pdi &&
                         dmi.adx > minADX &&
                         dmiArray.mdi.Max() > dmi.mdi;

        // ✅ 필수 조건
        bool isCciCondition = cci > -100 && cci < 50 &&
                              minCCI <= cci;
        bool isRsiCondition = rsi >= 30 && rsi < 46;
        bool isTradPrice = minCandles[0].TradePrice <= Math.Min((bollingerBands.movingAverage + keltner.middle) / 2,
                                                                minValue + atr < minCandles[0].TradePrice ?
                                                                Math.Min(minLowPrice, minCandles[0].LowPrice) + atr * 0.29 :
                                                                minValue + atr * 0.49) &&
                           minCandles[0].LowPrice >= minValue;

        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;      // 필수 조건 카운트
        int softCount = 0;  // 보조 조건 카운트

        // 🔢 필수 조건 카운트
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isTradPrice)
        {
            debugMessage += "| Price ";
            count++;
        }

        if (!string.IsNullOrEmpty(debugMessage) && count >= 2)
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
                if (isBollingerBandsCondition) { debugMessage += "| BAND "; softCount++; }
                if (isDMICondition) { debugMessage += "| DMI "; softCount++; }

                debugMessage = $"보조 {softCount}/3 {debugMessage}";
                AddDebugMessage(debugMessage);
            }
            
            AddDebugMessage(minCandles[0].Market);
        }

        return count >= 3 && softCount >= 2;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci, double atr, double rsi,
                                (double pdi, double mdi, double adx) dmi,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;

        bool isUpperBandsCoindition = minCandles[0].HighPrice >= Math.Min(bollingerBands.upperBand, keltner.upper);
        bool isMiddleBandsCoindition = minCandles[0].HighPrice >= (bollingerBands.movingAverage + keltner.middle) / 2;
        bool isUpperMiddleAverageCondition = minCandles[0].HighPrice >= (Math.Min(bollingerBands.upperBand, keltner.upper) + ((bollingerBands.movingAverage + keltner.middle) / 2)) / 2;

        return avgPrice * FeeRate < minCandles[0].TradePrice &&
             ((cci <= 150 && isUpperBandsCoindition) ||
              (cci <= 50 && isUpperMiddleAverageCondition) ||
              (cci <= 0 && isMiddleBandsCoindition) ||
              (cci <= 0 && currPrice >= avgPrice + atr));
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr, double cci, double rsi,
                              (double pdi, double mdi, double adx) dmi,
                               List<CandleMinute> minCandles, double atrMultiplier = 1.5)
    {
        if (!isHaveMarket)
            return false;

        int period = 14;
        bool isAtrLoss = false;

        for (int i = period; i >= 0; i--)
        {
            double prevATR = Calculate.ATR(minCandles.Skip(i).ToList());

            isAtrLoss = minCandles[i].TradePrice <= avgPrice - prevATR;

            if (isAtrLoss)
                break;
        }

        if (isAtrLoss)
            isAtrLoss = minCandles[0].TradePrice >= avgPrice - atr * 0.3;

        bool isAtrMultiplierStopLoss = currPrice <= avgPrice - (atr * atrMultiplier);

        return isAtrLoss || isAtrMultiplierStopLoss;
    }

    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, 
                                             double[] ema9, double[] ema20, double[] ema50, double[] ema100,
                                             double cci, double atr, double rsi,
                                             (double pdi, double mdi, double adx) dmi,
                                            (double upper, double middle, double lower) keltner, 
                                            (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isShortTermBuy = CheckBuyCondition(dmi, keltner, bollingerBands, cci, rsi, atr, minCandles) && isKRWHeld;
        
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
            return ExecuteSellOrder("Take Profit"); // 익절 매도
        }

        // 매수 조건 처리
        else if (isShortTermBuy)
        {
            return ExecuteBuyOrder("Check Buy Condition"); // 매수
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
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
    public bool CheckShortTermBuyCondition((double[] pdi, double[] mdi) dmi,
                                           (double upper, double middle, double lower) keltner,
                                           (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                            double cci, double rsi, double atr, List<CandleMinute> minCandles)
    {
        bool isEmaCondition = minCandles.Skip(1).Take(3).Min(c => c.LowPrice) <= minCandles[0].LowPrice;

        bool isCciCondition = cci > -88 && cci < 0;

        bool isRsiCondition = rsi > 33 && rsi < 50;

        int period = 3;
        bool isBollingerBandsCoindition = false;

        for (int i = 1; i <= period; i++)
        {
            var prevKeltner = Calculate.KeltnerChannel(minCandles.Skip(i).ToList());
            var prevBollingerBands = Calculate.BollingerBands(minCandles.Skip(i).ToList());

            isBollingerBandsCoindition = minCandles[i].TradePrice >= Math.Min(prevBollingerBands.lowerBand, prevKeltner.lower) &&
                                         minCandles[i].HighPrice < Math.Min(prevBollingerBands.movingAverage, prevKeltner.middle);

            if (!isBollingerBandsCoindition)
                return false;
        }

        double dynamicATR = atr * 1.5 + Math.Min(bollingerBands.lowerBand, keltner.lower) <= Math.Min(bollingerBands.movingAverage, keltner.middle) ?
                            atr : atr * 0.75;

        isBollingerBandsCoindition &= minCandles[0].HighPrice <= Math.Min(bollingerBands.movingAverage, keltner.middle) &&
                                      minCandles[0].TradePrice <= Math.Min(bollingerBands.movingAverage, Math.Min(keltner.middle, bollingerBands.lowerBand + dynamicATR)) &&
                                      minCandles[0].LowPrice >= bollingerBands.lowerBand;

        bool isDMICondition = dmi.pdi.Last() < dmi.mdi.Last() &&
                              dmi.pdi.Skip(7).Max() < dmi.pdi.Last() &&
                              dmi.mdi.Skip(7).Min() > dmi.mdi.Last(); ;


        return isEmaCondition && isCciCondition && isRsiCondition && isBollingerBandsCoindition && isDMICondition;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci, double atr,
                                (double[] pdi, double[] mdi) dmi,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 List<CandleMinute> minCandles)
    {
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

        double dynamicATR = atr * 1.6 + bollingerBands.lowerBand < Math.Min(bollingerBands.movingAverage, keltner.middle) ?
                            atr : atr * 0.8;

        return (cci <= 110 && isUpperBandsCoindition) ||
               (cci <= 10 && isMiddleBandsCoindition) ||
               (cci <= 10 && currPrice >= avgPrice + dynamicATR);
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr, double cci, double rsi,
                              (double[] pdi, double[] mdi) dmi,
                               List<CandleMinute> minCandles, double atrMultiplier = 1.5, double stopLossPercentage = 0.025)
    {
        bool isDMICondition = dmi.pdi.Last() < dmi.mdi.Last()&
                              dmi.pdi.Skip(7).Min() > dmi.pdi.Last() &&
                              dmi.mdi.Skip(7).Max() < dmi.mdi.Last();

        return currPrice <= avgPrice - (atr * atrMultiplier) ||
               currPrice <= avgPrice * (1 - stopLossPercentage) ||
               (rsi <= 35 && cci <= -140 && isDMICondition);
    }

    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, 
                                             double[] ema9, double[] ema20, double[] ema50, double[] ema100,
                                             double cci, double atr, double rsi,
                                             (double[] pdi, double[] mdi) dmi,
                                            (double upper, double middle, double lower) keltner, 
                                            (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                             List<CandleMinute> minCandles, bool isCoinHeld, bool isKRWHeld)
    {
        // (매수)
        bool isShortTermBuy = CheckShortTermBuyCondition(dmi, keltner, bollingerBands, cci, rsi, atr, minCandles) && isKRWHeld;
        
        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci, atr, dmi, keltner, bollingerBands, minCandles) && isCoinHeld;
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, cci, rsi, dmi, minCandles) && isCoinHeld;
        
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
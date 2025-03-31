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
    // 단기 트레이딩 매수 조건
    public bool CheckShortTermBuyCondition((double upper, double middle, double lower) keltner,
                                           (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                            double cci, double rsi, double atr, List<CandleMinute> minCandles)
    {
        bool isEmaCondition = minCandles.Skip(1).Take(3).Min(c => c.LowPrice) <= minCandles[0].LowPrice;

        bool isCciCondition = cci >= -93 && cci < 40;

        bool isRsiCondition = rsi >= 30;

        int period = 3;
        bool isBollingerBandsCoindition = false;

        for (int i = 1; i <= period; i++)
        {
            var prevKeltner = Calculate.KeltnerChannel(minCandles.Skip(i).ToList());
            var prevBollingerBands = Calculate.BollingerBands(minCandles.Skip(i).ToList());

            isBollingerBandsCoindition = minCandles[i].LowPrice >= prevBollingerBands.lowerBand &&
                                         minCandles[i].HighPrice < Math.Min(prevBollingerBands.movingAverage, prevKeltner.middle);

            if (!isBollingerBandsCoindition)
                return false;
        }
        
        isBollingerBandsCoindition &= minCandles[0].TradePrice <= bollingerBands.lowerBand + atr &&
                                      minCandles[0].LowPrice >= bollingerBands.lowerBand;


        return isEmaCondition && isCciCondition && isRsiCondition && isBollingerBandsCoindition;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands)
    {
        if (avgPrice <= 5000)
            return false;

        return (cci < 100 && currPrice >= Math.Min(bollingerBands.upperBand, keltner.upper)) ||
               (cci < 5 && currPrice > Math.Max(bollingerBands.movingAverage, keltner.middle));
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr,
                               (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                               List<CandleMinute> candles, double atrMultiplier = 1.5, double stopLossPercentage = 0.025)
    {
        if (avgPrice <= 5000)
            return false;

        return currPrice <= avgPrice - (atr * atrMultiplier) ||
               currPrice <= avgPrice * (1 - stopLossPercentage) ||
               candles[0].HighPrice < bollingerBands.lowerBand;
    }

    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, 
                                             double[] ema9, double[] ema20, double[] ema50, double[] ema100,
                                             double cci, double atr, double rsi,
                                            (double upper, double middle, double lower) keltner, 
                                            (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                             List<CandleMinute> minCandles, bool isCoinHeld, bool isKRWHeld)
    {
        // (매수)
        bool isShortTermBuy = CheckShortTermBuyCondition(keltner, bollingerBands, cci, rsi, atr, minCandles) && isKRWHeld;
        
        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci, keltner, bollingerBands) && isCoinHeld;
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, bollingerBands, minCandles) && isCoinHeld;
        
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
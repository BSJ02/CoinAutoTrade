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
        var slicedCandles = minCandles.Skip(1).ToList();

        // ✅ 필수 조건
        // 1: 최근 저점보다 높음
        bool isLowPriceCondition = slicedCandles.Take(5).Min(c => (c.LowPrice)) <= minCandles[0].LowPrice;

        // 2: CCI 상승
        double prevCCI9 = Calculate.CCI(slicedCandles, 9);

        double[] prevCCI14 = new double[2];

        for (int i = 0; i < 2; i++)
        {
            prevCCI14[i] = Calculate.CCI(slicedCandles, 14);
        }

        bool isCciCondition = prevCCI14[1] + 40 < cci14 && prevCCI14[0] + 5 < cci14 &&
                              cci14 < cci9 && Math.Abs(Math.Abs(prevCCI9) - Math.Abs(prevCCI14[0])) > Math.Abs(cci9) - Math.Abs(cci14) && 
                              cci14 > -105 && cci14 < -60;

        // 3: RSI
        double prevRSI = Calculate.RSI(slicedCandles);
        bool isRsiCondition = prevRSI + 1 < rsi &&
                              rsi > 32 && rsi < 55;

        // 4: 현재가가 저점일시 매수
        double minLowPrice = Math.Min(minCandles[0].LowPrice, minCandles[1].TradePrice);

        bool isTradPriceCondition = minCandles[0].TradePrice < minLowPrice + atr * 0.15 &&
                                    minCandles[0].HighPrice < minCandles[0].TradePrice + atr * 0.5;

        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;      // 필수 조건 카운트

        // 🔢 필수 조건 카운트
        if (isLowPriceCondition) { debugMessage += "| EMA "; count++; }
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isTradPriceCondition)
        {
            debugMessage += "| Price ";
            count++;
        }

        if (!string.IsNullOrEmpty(debugMessage) && count >= 3)
        {
            debugMessage = $"{count}/4 {debugMessage}";
            AddDebugMessage(debugMessage);

            AddDebugMessage(minCandles[0].Market);
        }

        return count >= 4;
    }

    public bool IsReasonableEntryDip((double upper, double middle, double lower) keltner,
                                     (double upperBand, double lowerBand, double movingAverage) bollingerBands, 
                                      double cci9,double cci14, double rsi, double atr, List<CandleMinute> minCandles)
    {
        int period = 2;

        // 0 : 바로 이전 캔들
        double[] prevCCI14 = new double[period];
        double[] prevCCI9 = new double[period];

        for (int i = 0; i < period; i++)
        {
            var slicedCandles = minCandles.Skip(i + 1).ToList();

            prevCCI9[i] = Calculate.CCI(slicedCandles, 9);
            prevCCI14[i] = Calculate.CCI(slicedCandles, 14);
        }

        bool isCciCondition = prevCCI14[1] + 50 < prevCCI14[0] &&
                              prevCCI14[0] + 10 < cci14 && cci14 < -100 &&
                              cci14 < cci9 &&
                              Math.Abs(prevCCI9[0] - prevCCI14[0]) > Math.Abs(cci9 - cci14);

        bool isRsiCondition = rsi >= 30 && rsi < 50;
        bool isTradPriceCondition = minCandles[0].TradePrice <= (minCandles[1].TradePrice < Math.Min(bollingerBands.lowerBand, keltner.lower) 
                                                                 ? minCandles[1].TradePrice 
                                                                 : Math.Min(bollingerBands.lowerBand, keltner.lower));

        if (isCciCondition && isTradPriceCondition)
        {
            string debugMessage = "| REasonable | ";
            AddDebugMessage(debugMessage);
            AddDebugMessage(minCandles[0].Market);
        }

        return isCciCondition && isRsiCondition && isTradPriceCondition;
    }


    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci, double atr, double rsi,
                                (double pdi, double mdi, double adx) dmi,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;

        bool isUpperCoindition = minCandles[0].TradePrice >= Math.Min(bollingerBands.upperBand, keltner.upper) - atr * 0.05;
        bool isMiddleCoindition = minCandles[0].TradePrice >= Math.Min(bollingerBands.movingAverage, keltner.middle) - atr * 0.05;
        bool isUpperMiddleAverageCondition = minCandles[0].TradePrice >= ((Math.Min(bollingerBands.upperBand, keltner.upper) + Math.Min(bollingerBands.movingAverage, keltner.middle)) / 2) - atr * 0.05;

        avgPrice = avgPrice * (1 + FeeRate);

        return avgPrice < currPrice &&
              (
                  // condition 매도
                  (cci < 150 && isUpperCoindition) ||
                  (cci < 50 && isUpperMiddleAverageCondition) ||
                  (cci < 0 && isMiddleCoindition) ||
                  // atr 매도
                  (cci < -70 && currPrice >= avgPrice + atr * 0.29) ||
                  (cci < -50 && currPrice >= avgPrice + atr * 0.49) ||
                  (cci < -30 && currPrice >= avgPrice + atr * 0.79) ||
                  (cci < 0 && currPrice >= avgPrice + atr)
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
        bool isReasonableEntryDip = IsReasonableEntryDip(keltner, bollingerBands, cci9, cci14, rsi, atr, minCandles);

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci14, atr, rsi, dmi, keltner, bollingerBands, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, cci14, rsi, dmi, minCandles);
        

        string market = minCandles[0].Market;

        // 매수
        if (waitBuyCondition.ContainsKey(market))
        {
            if ((waitBuyCondition[market] - DateTime.Now).TotalSeconds > 120)
            {
                waitBuyCondition.Remove(market);
            }
        }
        else
        {
            if (isReasonableEntryDip)
            {
                return ExecuteBuyOrder("Reasonable Buy Condition"); // 매수
            }
            else if (isTechnicalPullbackEntry)
            {
                return ExecuteBuyOrder("Technical Buy Condition"); // 매수
            }
        }

        // 매도
        if (isStopLoss)
        {
            return ExecuteSellOrder("Stop Loss"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            return ExecuteSellOrder("Take Profit"); // 익절 매도
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
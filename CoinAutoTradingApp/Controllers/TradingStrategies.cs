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

        // 1: RSI
        double prevRSI = Calculate.RSI(slicedCandles);

        bool isRsiCondition = prevRSI * (1.08 - (prevRSI / 1000)) < rsi &&
                              rsi > 30 && rsi < 50;

        // 2: CCI 상승
        int period = 2;

        double[] prevCCI9 = new double[period];
        double[] prevCCI14 = new double[period];

        for (int i = 0; i < period; i++)
        {
            slicedCandles = minCandles.Skip(i + 1).ToList();

            prevCCI9[i] = Calculate.CCI(slicedCandles, 9);
            prevCCI14[i] = Calculate.CCI(slicedCandles, 14);
        }

        // CCI 반등
        bool isCciReboundCondition = prevCCI14[1] - 40 > prevCCI14[0] &&
                                     prevCCI9[1] - 40 > prevCCI9[0] &&
                                     prevCCI14[0] + Math.Max(10, Math.Abs(prevCCI14[0] * 0.1)) < cci14 &&
                                     prevCCI9[0] + Math.Max(12, Math.Abs(prevCCI9[0] * 0.12)) < cci9 &&
                                     cci14 < 100 && cci9 < 100 && cci9 > -140;

        // CCI 연속 상승
        bool isCciRisingCondition = prevCCI14[1] + Math.Max(26, Math.Abs(prevCCI14[0] * 0.26)) < prevCCI14[0] &&
                                    prevCCI14[0] + Math.Max(8, Math.Abs(prevCCI14[0] * 0.8)) < cci14 &&
                                    prevCCI9[1] + Math.Max(39, Math.Abs(prevCCI9[0] * 0.39)) < prevCCI9[0] &&
                                    prevCCI9[0] + Math.Max(10, Math.Abs(prevCCI9[0] * 0.1)) < cci9 &&
                                    cci14 > -100 && cci14 < 0 &&
                                    cci9 > -100 && cci9 < 0;

        bool isCciCondition = isCciReboundCondition || isCciRisingCondition;


        // 3: 현재가가 저점일시 매수
        bool isTradPriceCondition = false;

        if (isCciReboundCondition)
        {
            isTradPriceCondition = minCandles[0].LowPrice >= minCandles[1].LowPrice &&
                                   minCandles[0].TradePrice < Math.Min(minCandles[1].TradePrice, minCandles[0].LowPrice) + atr * 0.1 &&
                                   minCandles[0].HighPrice < minCandles[0].TradePrice + atr * 0.5;
        }
        else if (isCciRisingCondition)
        {
            isTradPriceCondition = minCandles[0].LowPrice >= minCandles[1].LowPrice &&
                                   minCandles[0].TradePrice < Math.Min(minCandles[1].TradePrice, minCandles[0].LowPrice) + atr * 0.15 &&
                                   minCandles[0].HighPrice < minCandles[0].TradePrice + atr * 0.5;
        }

        // 디버그 메세지 추가
        string debugMessage = "";
        int count = 0;      // 필수 조건 카운트

        // 🔢 필수 조건 카운트
        if (isCciCondition) { debugMessage += "| CCI "; count++; }
        if (isRsiCondition) { debugMessage += "| RSI "; count++; }
        if (isTradPriceCondition)
        {
            debugMessage += "| Price ";
            count++;
        }

        if (!string.IsNullOrEmpty(debugMessage) && count >= 2)
        {
            debugMessage = $"T {count}/3 {debugMessage}";
            AddDebugMessage(debugMessage);

            AddDebugMessage(minCandles[0].Market);
        }

        return count >= 3;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci9, double atr, double rsi,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;
        
        string market = minCandles[0].Market;
        var slicedCandles = minCandles.Skip(1).ToList();

        avgPrice = avgPrice * (1 + FeeRate);
        return avgPrice < currPrice &&
              (
                  // atr 손절
                  (cci9 < entryCciByMarket[market] + 25 && currPrice >= avgPrice + atr * 0.25) ||
                  (cci9 < entryCciByMarket[market] + 55 && currPrice >= avgPrice + atr * 0.5) ||
                  (cci9 < entryCciByMarket[market] + 75 && currPrice >= avgPrice + atr * 0.75) ||
                  (cci9 < entryCciByMarket[market] + 100 && currPrice >= avgPrice + atr) ||
                  (cci9 < entryCciByMarket[market] + 125 && currPrice >= avgPrice + atr * 1.25) ||
                  (cci9 < entryCciByMarket[market] + 150 && currPrice >= avgPrice + atr * 1.5) ||
                  // cci 손절
                  cci9 < entryCciByMarket[market] - Math.Max(10, Math.Abs(entryCciByMarket[market] * 0.1))
              );
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr,
                               List<CandleMinute> minCandles, double atrMultiplier = 1.1, double stopLossPercent = 0.01)
    {
        if (!isHaveMarket)
            return false;

        bool isAtrStopLoss = currPrice <= avgPrice - atr * atrMultiplier ||
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

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci9, atr, rsi, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atr, minCandles);
        

        string market = minCandles[0].Market;

        // 매수
        if (waitBuyCondition.ContainsKey(market))
        {
            if ((waitBuyCondition[market] - DateTime.Now).TotalSeconds > 60)
            {
                waitBuyCondition.Remove(market);
            }
        }
        else
        {
            if (isTechnicalPullbackEntry)
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
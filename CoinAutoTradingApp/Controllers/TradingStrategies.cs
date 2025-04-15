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
                              rsi > 30 && rsi < 55;

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


        double cci14ReboundGap = Math.Abs(prevCCI14[1]) - Math.Abs(prevCCI14[0]);
        double cci9ReboundGap = Math.Abs(prevCCI9[1]) - Math.Abs(prevCCI9[0]);

        // CCI 반등
        bool isCciReboundCondition = cci14ReboundGap < -32 && cci9ReboundGap < -40 &&   // true면 급락 했음을 의미
                                     prevCCI14[0] + Math.Max(8, Math.Abs(cci14ReboundGap * 0.08)) < cci14 &&
                                     prevCCI9[0] + Math.Max(12, Math.Abs(cci9ReboundGap * 0.12)) < cci9 &&
                                     cci14 > -130 && cci14 < 0 && 
                                     cci9 > -130 && cci9 < 0;

        // CCI 연속 상승
        bool isCciRisingCondition = prevCCI14[1] + Math.Max(24, Math.Abs(prevCCI14[0] * 0.24)) < prevCCI14[0] &&
                                    prevCCI14[0] + Math.Max(8, Math.Abs(prevCCI14[0] * 0.08)) < cci14 &&
                                    prevCCI9[1] + Math.Max(32, Math.Abs(prevCCI9[0] * 0.32)) < prevCCI9[0] &&
                                    prevCCI9[0] + Math.Max(12, Math.Abs(prevCCI9[0] * 0.12)) < cci9 &&
                                    cci14 > -100 && cci14 < -40 &&
                                    cci9 > -100 && cci9 < -40;

        bool isCciCondition = isCciReboundCondition || isCciRisingCondition;


        // 3: 현재가가 저점일시 매수
        bool isTradPriceCondition = false;

        if (isCciReboundCondition)
        {
            isTradPriceCondition = minCandles[0].LowPrice > minCandles[1].LowPrice &&
                                   minCandles[0].TradePrice < Math.Min(minCandles[1].TradePrice, minCandles[0].LowPrice) + atr * 0.1 &&
                                   minCandles[0].HighPrice < minCandles[0].TradePrice + atr * 0.3;
        }
        else if (isCciRisingCondition)
        {
            isTradPriceCondition = minCandles[0].LowPrice > minCandles[1].LowPrice &&
                                   minCandles[0].TradePrice < Math.Min(minCandles[1].TradePrice, minCandles[0].LowPrice) + atr * 0.12 &&
                                   minCandles[0].HighPrice < minCandles[0].TradePrice + atr * 0.36;
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

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci14, double atr, double rsi,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;
        
        string market = minCandles[0].Market;
        var slicedCandles = minCandles.Skip(1).ToList();

        entryCciRsiByMarket[market] = (Math.Max(entryCciRsiByMarket[market].cci, cci14), Math.Max(entryCciRsiByMarket[market].rsi, rsi));

        double maxCCI = entryCciRsiByMarket[market].cci;
        double maxRsi = entryCciRsiByMarket[market].rsi;

        double prevRSI = Calculate.RSI(slicedCandles);
        double prevCCI14 = Calculate.CCI(slicedCandles, 14);

        bool isCciCondition = cci14 < maxCCI - Math.Max(5, Math.Abs(maxCCI) * 0.05);
        bool isRsiCondition = maxRsi * (1 - (maxRsi / 1500)) > rsi;

        avgPrice = avgPrice * (1 + FeeRate * 2);

        return avgPrice + atr * 0.2 < currPrice &&
               isCciCondition &&
               isRsiCondition;
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr,
                               List<CandleMinute> minCandles, double atrMultiplier = 1.1, double stopLossPercent = 0.005)
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
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cci14, atr, rsi, minCandles);
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
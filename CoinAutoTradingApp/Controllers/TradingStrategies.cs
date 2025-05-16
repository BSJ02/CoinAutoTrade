using CoinAutoTradingApp.Enum;
using CoinAutoTradingApp.Models;
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
    string buyCondition = "";

    // 매수 조건
    public bool IsBollingerBandBuySignal(decimal currPrice,
                                         decimal bbBasis, decimal bbDeviation,
                                         List<decimal> rsi, List<decimal> ema14,
                                         List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market) && isHaveMarket)
            return false;

        // 0: 유동성 확인
        var upperBand = bbBasis + bbDeviation * 2;
        var bandGapPercent = (upperBand - bbBasis) / upperBand;
        if (bandGapPercent < 0.005m)
            return false;

        var currentEMA14 = ema14[0];
        var currentRSI = rsi[0];

        if (currentEMA14 < bbBasis)
            return false;

        if (currentRSI < 55)
            return false;

        if (currPrice == minCandles[0].LowPrice || currPrice <= bbBasis)
            return false;

        return true;
    }

    public bool IsRSIBuySignal(decimal currPrice,
                                    decimal bbBasis, decimal bbDeviation,
                                    List<decimal> rsi,
                                    List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market) && isHaveMarket)
            return false;

        var candleInterval = 1;
        long currentMinute = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 60000;
        int startIndex = -1;

        (decimal time, decimal rsi) startRSI = (-1, 0);
        
        for (int i = 1; i < rsi.Count - 1; i++)
        {
            var prevRSI = rsi[i + 1];
            var currRSI = rsi[i];
            var nextRSI = rsi[i - 1];
            if (currRSI >= 65 && currRSI > prevRSI + 2 && currRSI > nextRSI + 2)
            {
                long xCoordinate = currentMinute - (i * candleInterval);
                startIndex = i;

                startRSI = (xCoordinate, currRSI);
                break;
            }
        }

        (decimal time, decimal rsi) trendPoint = (-1, 0);
        for (int i = startIndex; i > 0; i--)
        {
            var currRSI = rsi[i];
            if (startRSI.rsi - 30 < currRSI)
                continue;

            var prevRSI = rsi[i + 1];
            var nextRSI = rsi[i - 1];
            if (currRSI > prevRSI + 2 && currRSI > nextRSI + 2)
            {
                if (currRSI > trendPoint.rsi)
                {
                    long xCoordinate = currentMinute - (i * candleInterval);
                    trendPoint = (xCoordinate, currRSI);
                }
            }
        }

        if (startRSI.time < 0 || trendPoint.time < 0)
            return false;

        decimal slope = Calculate.Slope(startRSI, trendPoint);
        decimal intercept = trendPoint.rsi - slope * trendPoint.time;

        decimal predictedRSI = slope * currentMinute + intercept;
        decimal currentRSI = rsi[0];
        decimal previousRSI = rsi[1];
        if (currentRSI > predictedRSI && predictedRSI > previousRSI)
        {
            entryCondition[market] = EntryCondition.Scalping;
            buyCondition = "RSI 추세선 돌파";

            return true;
        }

        return false;
    }

    public bool ShouldTakeProfit(List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        return takeProfitCondition.ContainsKey(market) ? takeProfitCondition[market] : false;
    }

    public bool ShouldStopLoss(List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        return stopLossCondition.ContainsKey(market) ? stopLossCondition[market] : false;
    }

    public TradeType EvaluateTradeConditions(decimal currPrice, decimal avgPrice,
                                             BollingerBand bollingerBand, decimal bbDeviation,
                                             List<decimal> ema14,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isScalpingBuySignal = IsBollingerBandBuySignal(currPrice, bollingerBand.Basis, bbDeviation, Calculate.RSI(minCandles), ema14, minCandles) && isKRWHeld;

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(minCandles);
        
        string market = minCandles[0].Market;

        // 매도
        if (isStopLoss)
        {
            buyCondition = "";
            return ExecuteSellOrder("손절"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            buyCondition = "";
            return ExecuteSellOrder("익절"); // 익절 매도
        }

        // 매수
        if (isScalpingBuySignal)
        {
            return ExecuteBuyOrder($"{buyCondition}"); // 매수
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
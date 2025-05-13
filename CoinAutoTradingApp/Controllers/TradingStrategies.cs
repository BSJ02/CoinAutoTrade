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
    public bool IsBuyConditionOne(decimal currPrice,
                                  decimal bbBasis, decimal bbDeviation,
                                  List<decimal> ema7, List<decimal> ema28, List<decimal> ema56, List<decimal> ema112,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market) && isHaveMarket)
            return false;

        // 0: 유동성 확인
        var upperBand = bbBasis + bbDeviation;
        var lowerBand = bbBasis - bbDeviation;
        var bandGapPercent = (upperBand - lowerBand) / upperBand;
        if (bandGapPercent < 0.01m)
            return false;

        var currEMA7 = ema7[0];
        var currEMA28 = ema28[0];
        var currEMA56 = ema56[0];
        var currEMA112 = ema112[0];

        var prevEMA7 = ema7[1];
        var prevEMA28 = ema28[1];
        var prevEMA56 = ema56[1];
        var prevEMA112 = ema112[1];

        bool isEMAOrdered = prevEMA7 > prevEMA28 && prevEMA28 > prevEMA56 && prevEMA56 > prevEMA112;    // EMA 정배열
        bool isEMAReversed = prevEMA112 > prevEMA56 && (prevEMA56 > prevEMA28 || Math.Abs(prevEMA56 - prevEMA28) / prevEMA56 <= 0.0005m);   // EMA 역배열

        if (isEMAOrdered)
        {
            bool isEntryPrice = currPrice < currEMA56 * 1.0005m &&
                                minCandles[0].LowPrice > currEMA56 - bbDeviation;

            if (isEntryPrice)
            {
                entryCondition[market] = EntryCondition.EMAOrdered;
                buyCondition = "정배열";

                return true;
            }
        }
        else if (isEMAReversed)
        {
            bool isGoldenCross = currEMA7 > currEMA112;
            bool isEntryPrice = currPrice < currEMA112 * 1.0005m && minCandles[0].LowPrice != minCandles[0].TradePrice;

            if (isGoldenCross && isEntryPrice)
            {
                entryCondition[market] = EntryCondition.EMAReversed;
                buyCondition = "역배열";

                return true;
            }
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
                                             List<decimal> ema7, List<decimal> ema28, List<decimal> ema56, List<decimal> ema112,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionOne = IsBuyConditionOne(currPrice, bollingerBand.Basis, bbDeviation, ema7, ema28, ema56, ema112, minCandles) && isKRWHeld;

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
        if (isBuyConditionOne)
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
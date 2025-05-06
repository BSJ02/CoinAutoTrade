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
    // 매수 조건
    public bool IsBuyConditionOne(decimal currPrice,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market) && isHaveMarket)
            return false;

        var currCandle = minCandles[0];
        var prevCandle = minCandles[1];

        // 0: 매매가 설정
        bool isTradePrice = currCandle.TradePrice <= currCandle.OpeningPrice &&
                            currCandle.TradePrice >= prevCandle.TradePrice;

        // 1: 하락 추세 반전
        decimal candleLowHighPriceGap = prevCandle.HighPrice - prevCandle.LowPrice;
        bool isDowntrendReversing = prevCandle.LowPrice <= minCandles.Skip(2).Take(5).Min(c => c.LowPrice) &&
                                    prevCandle.TradePrice >= prevCandle.LowPrice + candleLowHighPriceGap * 0.8m &&
                                    prevCandle.OpeningPrice >= prevCandle.LowPrice + candleLowHighPriceGap * 0.8m;

        // 2: BullishEngulfingCandle
        bool isBullishEngulfingCandle = Calculate.IsBullishEngulfingCandle(minCandles);

        // 3: 모닝스타 캔들 패턴
        bool isMorningStarCandle = minCandles[3].OpeningPrice > minCandles[3].TradePrice &&
                                   Calculate.IsDojiCandle(minCandles[2]) &&
                                   minCandles[1].TradePrice > minCandles[3].TradePrice;

        return isTradePrice &&
               (
                   isDowntrendReversing ||
                   isBullishEngulfingCandle ||
                   isMorningStarCandle
               );
    }

    public bool ShouldTakeProfit(decimal currPrice, decimal avgPrice,
                                 List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        trailingStopPrice[market] = trailingStopPrice.ContainsKey(market) ? Math.Max(currPrice, trailingStopPrice[market]) : currPrice;

        if (currPrice < avgPrice * (1 + FeeRate * 6))
            return false;

        return currPrice >= profitPrice ||
               currPrice <= trailingStopPrice[market] * 0.998m;
    }

    public bool ShouldStopLoss(decimal currPrice, decimal avgPrice,
                               List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        return currPrice < stopLossPrice;
    }

    public TradeType EvaluateTradeConditions(decimal currPrice, decimal avgPrice,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionOne = IsBuyConditionOne(currPrice, minCandles) && isKRWHeld;

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, minCandles);
        
        string market = minCandles[0].Market;

        // 매도
        if (isStopLoss)
        {
            return ExecuteSellOrder("손절"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            return ExecuteSellOrder("익절"); // 익절 매도
        }

        // 매수
        if (waitBuyTime.ContainsKey(market))
        {
            if ((DateTime.Now - waitBuyTime[market]).TotalSeconds > 120)
            {
                waitBuyTime.Remove(market);
            }
        }
        else
        {
            if (isBuyConditionOne)
            {
                return ExecuteBuyOrder("매수"); // 매수
            }
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
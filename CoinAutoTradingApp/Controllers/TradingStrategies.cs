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
    public bool IsBuyConditionOne(decimal currPrice, decimal ema50, decimal ema200,
                                  decimal vwma, decimal poc,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (!isHaveMarket || pendingBuyOrders.ContainsKey(market))
            return false;


        // 1: EMA 역배열
        bool isEMACondition = ema50 < poc * 1.001m &&
                              ema50 < vwma &&
                              poc < vwma &&
                              vwma < ema200;

        // 2: 매수가 설정
        bool isTradPriceCondition = minCandles[0].HighPrice > poc &&
                                    minCandles[0].TradePrice > minCandles[0].OpeningPrice &&
                                    minCandles[0].TradePrice <= poc * 1.0005m;

        if (isEMACondition && isTradPriceCondition)
            entryCondition[market] = EntryCondition.Ema200AboveEma50;

        return isEMACondition && isTradPriceCondition;
    }

    public bool IsBuyConditionTwo(decimal currPrice, decimal ema50, decimal ema200,
                                  decimal vwma, decimal poc,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (!isHaveMarket || pendingBuyOrders.ContainsKey(market))
            return false;


        // 1: EMA 정배열
        bool isEMACondition = poc < ema50 &&
                              vwma < ema50 &&
                              minCandles[1].TradePrice < Math.Min(ema50, Math.Min(poc, vwma));

        // 2: 매수가 설정
        bool isTradPriceCondition = minCandles[0].OpeningPrice == minCandles[1].TradePrice &&
                                    minCandles[0].OpeningPrice == minCandles[0].LowPrice &&
                                    minCandles[0].TradePrice == minCandles[0].OpeningPrice;

        // 3: 거래량 확인
        bool isVolumeCondition = minCandles[1].TradePrice > minCandles[1].OpeningPrice &&
                                 minCandles[1].CandleAccTradeVolume > minCandles.Skip(1).Take(6).Average(c => c.CandleAccTradeVolume) * 7;

        if (isEMACondition && isTradPriceCondition && isVolumeCondition)
            entryCondition[market] = EntryCondition.Ema50AboveEma200;

        return isEMACondition && isTradPriceCondition && isVolumeCondition;
    }

    public bool ShouldTakeProfit(decimal currPrice, decimal avgPrice,
                                 decimal ema50, decimal ema200, decimal vwma, decimal poc,
                                 List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket || !entryCondition.ContainsKey(market))
            return false;

        if (currPrice <= avgPrice * (1 + FeeRate * 2))
            return false;

        if (entryCondition[market] == EntryCondition.Ema200AboveEma50)
        {
            return currPrice >= vwma * 0.9999m;
        }
        else if (entryCondition[market] == EntryCondition.Ema50AboveEma200)
        {
            return ema50 < minCandles[0].OpeningPrice;
        }

        return false;
    }

    public bool ShouldStopLoss(decimal currPrice, decimal avgPrice,
                               List<CandleMinute> minCandles,
                               decimal stopLoss = 0.01m)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket || !entryCondition.ContainsKey(market))
            return false;

        return currPrice <= avgPrice * (1 - stopLoss);
    }

    public TradeType EvaluateTradeConditions(decimal currPrice, decimal avgPrice,
                                             decimal ema50, decimal ema200, decimal vwma, decimal poc,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionOne = IsBuyConditionOne(currPrice, ema50, ema200, vwma, poc, minCandles) && isKRWHeld;
        bool isBuyConditionTwo = IsBuyConditionTwo(currPrice, ema50, ema200, vwma, poc, minCandles) && isKRWHeld;

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, ema50, ema200, vwma, poc, minCandles);
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
                return ExecuteBuyOrder("ema 역배열"); // 매수
            }
            else if (isBuyConditionTwo)
            {
                return ExecuteBuyOrder("ema 정배열"); // 매수
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
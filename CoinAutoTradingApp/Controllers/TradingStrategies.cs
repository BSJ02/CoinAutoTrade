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
    public bool IsBuyConditionOne(double currPrice, double ema50, double ema200,
                                  double vwma, double poc,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (!isHaveMarket || !pendingBuyOrders.ContainsKey(market))
            return false;


        // 1: EMA 역배열
        bool isEMACondition = poc < ema50 &&
                              ema50 < vwma &&
                              vwma < ema200;

        // 2: 매수가 설정
        bool isTradPriceCondition = minCandles[0].HighPrice >= poc &&
                                    minCandles[0].TradePrice <= poc &&
                                    minCandles[0].OpeningPrice < poc;

        if (isEMACondition && isTradPriceCondition)
            entryCondition[market] = EntryCondition.Ema200AboveEma50;

        return isEMACondition && isTradPriceCondition;
    }

    public bool IsBuyConditionTwo(double currPrice, double ema50, double ema200,
                                  double vwma, double poc,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (!isHaveMarket || !pendingBuyOrders.ContainsKey(market))
            return false;


        // 1: EMA 정배열
        bool isEMACondition = ema200 < ema50 &&
                              ema200 < poc &&
                              ema200 < vwma &&
                              poc < ema50 &&
                              vwma < ema50;

        // 2: 매수가 설정
        bool isTradPriceCondition = minCandles[0].HighPrice >= poc &&
                                    minCandles[0].OpeningPrice < poc &&
                                    minCandles[0].TradePrice <= Math.Max(poc, vwma)  &&
                                    minCandles[0].HighPrice >= vwma &&
                                    minCandles[0].OpeningPrice < vwma;

        if (isEMACondition && isTradPriceCondition)
            entryCondition[market] = EntryCondition.Ema50AboveEma200;

        return isEMACondition && isTradPriceCondition;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice,
                                 double ema50, double ema200, double vwma, double poc,
                                 List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        if (entryCondition[market] == EntryCondition.Ema200AboveEma50)
        {
            if (poc > ema50)
            {
                return currPrice >= ema200 * 0.9995;
            }
            else
            {
                return currPrice >= vwma * 0.9995;
            }
        }
        else if (entryCondition[market] == EntryCondition.Ema50AboveEma200)
        {
            return ema50 < minCandles[0].OpeningPrice;
        }

        return false;
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice,
                               List<CandleMinute> minCandles,
                               double stopLoss = 0.01)
    {
        if (!isHaveMarket)
            return false;

        return currPrice <= avgPrice * (1 - stopLoss);
    }

    public TradeType EvaluateTradeConditions(double currPrice, double avgPrice, 
                                             double ema50, double ema200, double vwma, double poc,
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
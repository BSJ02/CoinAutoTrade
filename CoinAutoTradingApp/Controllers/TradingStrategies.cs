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
        if (isHaveMarket)
            return false;

        string market = minCandles[0].Market;

        bool isEMACondition = ema50 < poc * 1.001 &&
                              ema50 < vwma &&
                              poc < vwma &&
                              vwma < ema200;

        bool isTradPriceCondition = minCandles[0].HighPrice > poc &&
                                    minCandles[0].TradePrice <= poc;

        if (isEMACondition && isTradPriceCondition)
            entryCondition[market] = EntryCondition.Ema200AboveEma50;

        return isEMACondition && isTradPriceCondition;
    }

    public bool IsBuyConditionTwo(double currPrice, double ema50, double ema200,
                                  double vwma, double poc,
                                  List<CandleMinute> minCandles)
    {
        if (isHaveMarket)
            return false;

        string market = minCandles[0].Market;

        // 1: 거래량 증가
        bool isEMACondition = ema200 < vwma &&
                              poc < vwma &&
                              vwma < ema50;

        // 2: 매수가 설정
        bool isTradPriceCondition = false;

        if (ema200 < poc)
        {
            isTradPriceCondition = minCandles[0].TradePrice <= ema200;
        }
        else
        {
            isTradPriceCondition = minCandles[0].TradePrice <= poc;
        }

        if (isEMACondition && isTradPriceCondition)
            entryCondition[market] = EntryCondition.Ema50AboveEma200;

        return isEMACondition && isTradPriceCondition;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice,
                                 double ema50, double ema200, double vwma,
                                 List<CandleMinute> minCandles)
    {
        if (!isHaveMarket)
            return false;

        string market = minCandles[0].Market;

        if (entryCondition[market] == EntryCondition.Ema200AboveEma50)
        {
            return currPrice >= vwma;
        }
        else if (entryCondition[market] == EntryCondition.Ema50AboveEma200)
        {
            return ema50 <= ema200;
        }

        return false;
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice,
                               List<CandleMinute> minCandles,
                               double stopLoss = 0.015)
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
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, ema50, ema200, vwma, minCandles);
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
                return ExecuteBuyOrder("지지층 횡보"); // 매수
            }
            else if (isBuyConditionTwo)
            {
                return ExecuteBuyOrder("지지층 돌파"); // 매수
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
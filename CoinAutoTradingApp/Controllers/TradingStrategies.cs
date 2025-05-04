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
                                  double rsi, MACD macd, Stochastic stochastic,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market))
            return false;

        // 1: MACD 확인
        bool isMACDCondition = macd.MacdLine[0] > macd.SignalLine[0];

        // 2: RSI 확인
        bool isRSICondition = rsi > 45 && rsi < 70;

        // 3: Stochastic 확인
        bool isStochasticCondition = stochastic.K[0] <= 55 && stochastic.K[0] > 25 &&
                                     stochastic.K[0] > stochastic.D[0] && stochastic.K[0] > stochastic.K[1] + 1.5m;

        return isMACDCondition && isRSICondition && isStochasticCondition;
    }

    public bool ShouldTakeProfit(decimal currPrice, decimal avgPrice,
                                 Stochastic stochastic,
                                 List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        trailingStopPrice[market] = trailingStopPrice.ContainsKey(market) ? Math.Max(currPrice, trailingStopPrice[market]) : currPrice;

        if (currPrice < avgPrice * (1 + FeeRate * 6))
            return false;

        return (stochastic.K[1] >= 80 && stochastic.K[0] < 80) ||
               (stochastic.K[0] < stochastic.D[0] && stochastic.D[0] < stochastic.D[1] - 1.5m);
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
                                             double rsi, MACD macd, Stochastic stochastic,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionOne = IsBuyConditionOne(currPrice, rsi, macd, stochastic, minCandles) && isKRWHeld;

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, stochastic, minCandles);
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
                return ExecuteBuyOrder("BB 매수"); // 매수
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
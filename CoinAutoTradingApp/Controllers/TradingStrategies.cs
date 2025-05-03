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
                                  decimal[] ema5, decimal[] ema20, decimal[] ema60, decimal[] ema120,
                                  BollingerBand bollingerBand,
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market))
            return false;


        // 1: EMA 우상향 확인
        /*bool isEMACondition = Calculate.IsEmaTrendingUp(ema60) && Calculate.IsEmaTrendingUp(ema120) &&
                              ema60[0] > ema120[0];*/


        // 2: 유동성 확인
        decimal bandLowerUpperGapPercent = (bollingerBand.UpperBand - bollingerBand.LowerBand) / bollingerBand.UpperBand;
        bool isBandGapCondition = bandLowerUpperGapPercent <= 0.035m &&
                                  bandLowerUpperGapPercent >= 0.01m;


        // 3: 로우 밴드 안 뚫었는지 확인
        bool isBandCondition = true;

        for (int i = 0; i < 3; i++)
        {
            var candles = minCandles.Skip(i).ToList();
            var band = Calculate.BollingerBand(candles);

            isBandCondition = minCandles[i].HighPrice < band.Basis &&
                              minCandles[i].LowPrice > band.LowerBand;

            if (!isBandCondition)
            {
                break;
            }
        }


        // 4: 매수가 설정
        decimal bandLowMiddleGap = bollingerBand.Basis - bollingerBand.LowerBand;

        bool isTradePriceCondition = minCandles[0].TradePrice < bollingerBand.LowerBand + bandLowMiddleGap / 3 &&
                                     minCandles[0].LowPrice > bollingerBand.LowerBand;


        return isBandGapCondition && isBandCondition && isTradePriceCondition;
    }

    public bool ShouldTakeProfit(decimal currPrice, decimal avgPrice,
                                 BollingerBand bollingerBand,
                                 List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        trailingStopPrice[market] = trailingStopPrice.ContainsKey(market) ? Math.Max(currPrice, trailingStopPrice[market]) : currPrice;

        if (currPrice <= avgPrice * (1 + FeeRate * 2))
            return false;

        decimal priceLowHighGap = minCandles[0].HighPrice - minCandles[0].LowPrice;
        if (minCandles[0].OpeningPrice < minCandles[0].TradePrice && 
            currPrice >= minCandles[0].LowPrice + priceLowHighGap * 0.8m)
            return false;

        return currPrice <= trailingStopPrice[market] * 0.9995m;
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
                                             decimal[] ema5, decimal[] ema20, decimal[] ema60, decimal[] ema120, decimal[] vwma,
                                             BollingerBand bollingerBand,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionOne = IsBuyConditionOne(currPrice, ema5, ema20, ema60, ema120, bollingerBand, minCandles) && isKRWHeld;

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, bollingerBand, minCandles);
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
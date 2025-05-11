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
                                  List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;

        if (pendingBuyOrders.ContainsKey(market) && isHaveMarket)
            return false;

        // 0: 유동성 확인
        var upperBand = bbBasis + bbDeviation * 2;
        var lowerBand = bbBasis - bbDeviation * 2;
        var bandGapPercent = (upperBand - lowerBand) / upperBand;
        if (bandGapPercent < 0.013m)
            return false;

        var ema10 = Calculate.EMAHistory(minCandles, 10);
        var ema34 = Calculate.EMAHistory(minCandles, 34);

        if (ema10[0] < ema10[1] || ema10[0] > ema34[0])
            return false;

        if (Math.Abs(ema34[0] - ema10[0]) / ema34[0] > 0.001m)
            return false;

        if (minCandles[0].OpeningPrice > ema34[0] * 1.0005m)
            return false;

        return currPrice > ema34[0] &&
               currPrice <= ema34[0] * 1.0005m;

        #region BollingerBand 매매
        //bbCount[market] = 3;

        //decimal bbLowerBand = 0;
        //while (true)
        //{
        //    bbLowerBand = bbBasis - bbDeviation * bbCount[market];

        //    if (minCandles[0].LowPrice <= bbLowerBand)
        //    {
        //        bbCount[market]++;
        //    }
        //    else
        //    {
        //        break;
        //    }
        //}

        //if (bbCount[market] == 3)
        //{
        //    return false;
        //}

        //buyCondition = $"BB{bbCount[market]} 하단 터치";

        //return currPrice <= bbLowerBand + bbDeviation &&
        //       currPrice > bbLowerBand &&
        //       minCandles[0].LowPrice != minCandles[0].TradePrice;
        #endregion
    }

    public bool ShouldTakeProfit(decimal currPrice, decimal avgPrice,
                                 List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        return currPrice >= profitPrice[market] && minCandles[0].HighPrice != minCandles[0].TradePrice;
    }

    public bool ShouldStopLoss(decimal currPrice, decimal avgPrice,
                               List<CandleMinute> minCandles)
    {
        string market = minCandles[0].Market;
        if (!isHaveMarket)
            return false;

        return currPrice < stopLossPrice[market];
    }

    public TradeType EvaluateTradeConditions(decimal currPrice, decimal avgPrice, 
                                             BollingerBand bollingerBand, decimal bbDeviation,
                                             List<CandleMinute> minCandles, bool isKRWHeld)
    {
        // (매수)
        bool isBuyConditionOne = IsBuyConditionOne(currPrice, bollingerBand.Basis, bbDeviation, minCandles) && isKRWHeld;

        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, minCandles);
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, minCandles);
        
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
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
    // 단기 트레이딩 매수 조건
    public bool CheckShortTermBuyCondition((double upper, double middle, double lower) keltner,
                                           (double upperBand, double lowerBand, double movingAverage) bollingerBands,
                                            double[] cciHistory, double[] volumeHistory, double[] atrHistory, List<CandleMinute> candles)
    {
        bool isEmaCondition = candles.Skip(1).Take(3).Max(c => c.LowPrice) <= candles[0].LowPrice;

        bool isCciCondition = cciHistory.Last() > -80 &&
                              cciHistory.Last() < 80 &&
                              cciHistory.Last() >= cciHistory.Skip(1).TakeLast(3).Max();

        int period = 3;
        bool isKeltnerCondition = false;
        bool isBollingerBandsCoindition = false;

        for (int i = 1; i <= period; i++)
        {
            var prevKeltner = Calculate.KeltnerChannel(candles.Skip(i).ToList());
            isKeltnerCondition = candles[i].TradePrice >= prevKeltner.lower &&
                                 candles[i].TradePrice < prevKeltner.upper;

            var prevBollingerBands = Calculate.BollingerBands(candles.Skip(i).ToList());
            isBollingerBandsCoindition = candles[i].LowPrice >= bollingerBands.lowerBand &&
                                         candles[i].HighPrice <= bollingerBands.movingAverage;
        }
        
        isKeltnerCondition &= candles[0].TradePrice < keltner.upper;
        isBollingerBandsCoindition &= candles[0].TradePrice <= bollingerBands.movingAverage;

        return isEmaCondition && isCciCondition && (isKeltnerCondition || isBollingerBandsCoindition);
    }



    // 중기 스윙 트레이딩 매도 조건
    public bool CheckSwingSellCondition(double price, double rsi, (double upperBand, double lowerBand, double movingAverage) bands,
                                    double adx, (double diPlus, double diMinus) di, List<CandleMinute> candles)
    {
        bool isRsiCondition = rsi >= 70;

        bool isBollingerCondition = price >= bands.upperBand && candles.Take(2).All(c => c.TradePrice >= bands.upperBand);

        double prevADX = Calculate.ADX(candles.Skip(1).ToList());
        bool isAdxCondition = adx > 25 && adx > prevADX;

        bool isPriceFalling = price < candles[1].TradePrice;

        bool isDiCondition = di.diMinus > di.diPlus;

        bool isExtendedFall = candles.Take(2).All(c => c.TradePrice > price);

        return isRsiCondition && isBollingerCondition && isAdxCondition && isPriceFalling && isDiCondition && isExtendedFall;
    }

    // 변동성 돌파 전략 매도 조건
    public bool CheckVolatilityBreakoutSellCondition(double price, (double tenkan, double kijun, double senkouA, double senkouB, double chikou) ichimoku,
                                                    double vwap, double adx, (double diPlus, double diMinus) di, List<CandleMinute> candles)
    {
        bool isIchimokuResistanceBroken = price < Math.Min(ichimoku.senkouA, ichimoku.senkouB);

        bool isTenkanKijunCross = ichimoku.tenkan < ichimoku.kijun;

        bool isChikouBelowCloud = ichimoku.chikou < Math.Min(ichimoku.senkouA, ichimoku.senkouB);

        bool isVwapCondition = price < vwap;

        double prevADX = Calculate.ADX(candles.Skip(1).ToList());
        bool isAdxCondition = adx > 25 && adx > prevADX;

        bool isDiBearish = di.diMinus > di.diPlus;

        return isIchimokuResistanceBroken && isTenkanKijunCross && isVwapCondition && isChikouBelowCloud && isAdxCondition && isDiBearish;
    }

    public bool ShouldTakeProfit(double currPrice, double avgPrice, double cci,
                                (double upper, double middle, double lower) keltner,
                                (double upperBand, double lowerBand, double movingAverage) bollingerBands)
    {
        if (avgPrice <= 5000)
            return false;

        return (cci < 100) &&
               (currPrice >= bollingerBands.upperBand || currPrice >= keltner.upper);
    }

    public bool ShouldStopLoss(double currPrice, double avgPrice, double atr,
                               double atrMultiplier = 1.5, double stopLossPercentage = 0.026)
    {
        if (avgPrice <= 5000)
            return false;

        return currPrice <= avgPrice - (atr * atrMultiplier) ||
               currPrice <= avgPrice * (1 - stopLossPercentage);
    }


    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, double[] ema9, double[] ema20, double[] ema50, double[] ema100,
        double[] cciHistory, (double macd, double signal, double histogram) macd,
        (double upper, double middle, double lower) keltner, double[] obvHistory, double[] volumeHistory,  double rsi,
        (double upperBand, double lowerBand, double movingAverage) bollingerBands,
        double adx, (double diPlus, double diMinus) di,
        (double tenkan, double kijun, double senkouA, double senkouB, double chikou) ichimoku,
        (double k, double d) stochastic, double[] atrHistory, double vwap,
        List<CandleMinute> candles, bool isCoinHeld, bool isKRWHeld)
    {
        // (매수)
        bool isShortTermBuy = CheckShortTermBuyCondition(keltner, bollingerBands, cciHistory, volumeHistory, atrHistory, candles) && isKRWHeld;
       
        // 중기 스윙 트레이딩 전략 평가 (매도)
        bool isSwingSell = CheckSwingSellCondition(currPrice, rsi, bollingerBands, adx, di, candles) && isCoinHeld;
        // 변동성 돌파 전략 평가 (매도)
        bool isVolatilityBreakoutSell = CheckVolatilityBreakoutSellCondition(currPrice, ichimoku, vwap, adx, di, candles) && isCoinHeld;
        // 익절 (매도)
        bool isTakeProfit = ShouldTakeProfit(currPrice, avgPrice, cciHistory.Last(), keltner, bollingerBands) && isCoinHeld;
        // 손절 (매도)
        bool isStopLoss = ShouldStopLoss(currPrice, avgPrice, atrHistory[atrHistory.Length - 1]) && isCoinHeld;
        
        // 매수 및 매도 로직
        if (isStopLoss)
        {
            return ExecuteSellOrder("Stop Loss"); // 손절 매도
        }
        else if (isTakeProfit)
        {
            return ExecuteSellOrder("Take Profit"); // ATR 기반 손절 매도
        }
        else if (isVolatilityBreakoutSell)
        {
            return ExecuteSellOrder("Volatility"); // 변동성 돌파 매도
        }
        else if (isSwingSell)
        {
            return ExecuteSellOrder("Swing"); // 중기 스윙 트레이딩 매도
        }

        // 매수 조건 처리
        else if (isShortTermBuy)
        {
            return ExecuteBuyOrder("Short Term Buy"); // 단기 트레이딩 매수
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
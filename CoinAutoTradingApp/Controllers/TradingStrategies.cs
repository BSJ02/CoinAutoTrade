using CoinAutoTradingApp.Enum;
using CoinAutoTradingApp.UpbitAPI.Models;
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
    public bool CheckShortTermBuyCondition(List<CandleMinute> candles, double[] ema20, double[] ema50, double[] cciHistory, double keltnerUpper, double[] obvHistory, double[] volumeHistory, double atr)
    {
        bool isEmaCondition = ema20.Last() > ema50.Last() && ema20.Last() > ema20[ema20.Length - 2];

        bool isCciCondition = cciHistory.Length >= 6 &&
                              cciHistory.Last() > -100 &&  // CCI가 -100 이상으로 회복
                              cciHistory.Last() > cciHistory[cciHistory.Length - 2] && // 최근 값이 직전 값보다 상승
                              cciHistory[cciHistory.Length - 2] < cciHistory[cciHistory.Length - 3]; // 직전 값이 하락했던 상태

        bool isKeltnerCondition = candles.Take(3).All(c => c.TradePrice > keltnerUpper && (c.HighPrice - c.LowPrice) / c.TradePrice < (atr * 2));

        bool isObvCondition = obvHistory.Length >= 3 && obvHistory.TakeLast(3).Zip(obvHistory.Skip(obvHistory.Length - 2), (current, previous) => current > previous).Count(isIncreased => isIncreased) >= 2;

        double avgVolume = volumeHistory.Length >= 10 ? volumeHistory.Skip(volumeHistory.Length - 10).Average() : volumeHistory.Average();
        bool isVolumeCondition = volumeHistory.TakeLast(5).Count(v => v > avgVolume * 1.2) >= 3;

        return isEmaCondition && isCciCondition && isKeltnerCondition && (isObvCondition || isVolumeCondition);
    }

    // 중기 스윙 트레이딩 매수 조건
    public bool CheckSwingBuyCondition(double price, double[] ema50, double[] ema200, double rsi, (double upperBand, double lowerBand, double movingAverage) bands, double adx, (double diPlus, double diMinus) di, List<CandleMinute> candles)
    {
        bool isEmaCondition = ema50.Last() > ema200.Last() && ema50.Last() > ema50[ema50.Length - 2];
        bool isRsiCondition = rsi <= 35;

        double previousCandleClose = candles[1].TradePrice;
        double previousCandleLow = candles[1].LowPrice;
        bool isBollingerCondition = (previousCandleLow < bands.lowerBand) && (candles[0].TradePrice > previousCandleClose);

        bool isAdxCondition = adx > 25 && (di.diPlus > di.diMinus);

        return isEmaCondition && isRsiCondition && isBollingerCondition && isAdxCondition;
    }

    // 변동성 돌파 전략 매수 조건
    public bool CheckVolatilityBreakoutBuyCondition(double price, (double tenkan, double kijun, double senkouA, double senkouB, double chikou) ichimoku, double stochasticK, double stochasticD, double[] atrHistory, double vwap, List<CandleMinute> candles)
    {
        // ATR 상승 조건
        double atrAverage = atrHistory.TakeLast(5).Average();
        bool isAtrCondition = atrHistory.Last() > atrAverage * 1.15;

        // 가격이 Ichimoku 구름 위에서 3개 이상 캔들 동안 유지
        bool isIchimokuCondition = candles.Take(3).All(c => c.TradePrice > Math.Max(ichimoku.senkouA, ichimoku.senkouB)) &&
                                   price > candles.Take(5).Max(c => c.TradePrice) - atrAverage * 0.5;
        bool isIchimokuLongTerm = ichimoku.chikou > Math.Max(ichimoku.senkouA, ichimoku.senkouB);

        // VWAP 상단 유지 및 가격이 기준선(kijun) 이상일 때
        bool isVwapCondition = price > vwap && price > ichimoku.kijun;

        // Stochastic %K가 %D를 상향 돌파 & 50 이상
        bool isStochasticCondition = stochasticK > stochasticD && stochasticK > 50 &&
                                     (stochasticK < 80 || (stochasticK >= 80 && stochasticD < 80));

        return isIchimokuCondition && isIchimokuLongTerm && isAtrCondition && isVwapCondition && isStochasticCondition;
    }


    // 중기 스윙 트레이딩 매도 조건
    public bool CheckSwingSellCondition(double price, double rsi, (double upperBand, double lowerBand, double movingAverage) bands,
                                    double adx, (double diPlus, double diMinus) di, List<CandleMinute> candles)
    {
        bool isRsiCondition = rsi >= 70;
        bool isBollingerCondition = price >= bands.upperBand;
        bool isAdxCondition = adx > 25;

        double previousCandleClose = candles[1].TradePrice; // 이전 캔들의 종가
        bool isPriceFalling = price < previousCandleClose;

        bool isDiCondition = di.diMinus > di.diPlus;

        bool isExtendedFall = price < previousCandleClose && price < candles[2].TradePrice;

        return isRsiCondition && isBollingerCondition && isAdxCondition && isPriceFalling && isDiCondition && isExtendedFall;
    }

    // 변동성 돌파 전략 매도 조건
    public bool CheckVolatilityBreakoutSellCondition(
    double price,
    (double tenkan, double kijun, double senkouA, double senkouB, double chikou) ichimoku,
    double vwap,
    double adx,
    (double diPlus, double diMinus) di)
    {
        bool isIchimokuResistanceBroken = price < Math.Min(ichimoku.senkouA, ichimoku.senkouB);
        bool isChikouBelowCloud = ichimoku.chikou < Math.Min(ichimoku.senkouA, ichimoku.senkouB);

        bool isVwapCondition = price < vwap;
        bool isAdxStrong = adx >= 25;
        bool isDiBearish = di.diMinus > di.diPlus;

        return isIchimokuResistanceBroken && isVwapCondition && isChikouBelowCloud && isAdxStrong && isDiBearish;
    }

    public bool IsGradualPriceDecline(double rsi, (double macd, double signal, double histogram) macd,
        (double k, double d) stochastic, bool isAvgPriceLow,
        List<CandleMinute> candles, double declineThreshold = 0.02)
    {
        double initialPrice = candles[10].TradePrice;
        double finalPrice = candles[1].TradePrice;

        double priceChange = (initialPrice - finalPrice) / initialPrice;

        bool isGradualDecline = priceChange >= declineThreshold;

        bool isRsiLow = rsi <= 30; // RSI가 과매도 상태인 경우
        bool isMacdNegative = macd.macd < 0; // MACD가 여전히 음수일 경우
        bool isStochasticLow = stochastic.d <= 20; // Stochastic Oscillator가 과매도 상태인 경우

        return isGradualDecline && isRsiLow && isMacdNegative && isStochasticLow;
    }

    public bool IsBearishSignal(
    double[] ema9, double[] ema50, // 이동 평균선 (9일, 50일)
    double rsi,
    double adx, (double diPlus, double diMinus) di, // ADX & DI
    (double macd, double signal, double histogram) macd, // MACD
    double[] volumeHistory) // 거래량
    {
        bool isDeadCross = ema9.Last() < ema50.Last() && ema9.Last() < ema9[ema9.Length - 2];
        bool isRsiBearish = rsi < 50;

        bool isDiBearish = di.diMinus > di.diPlus;
        bool isAdxStrong = adx >= 20;

        bool isMacdBearish = macd.macd < 0;

        double averageVolume = volumeHistory.TakeLast(5).Average();
        bool isVolumeSpike = volumeHistory.Last() > averageVolume * 1.2;

        return isDeadCross && isRsiBearish && isDiBearish && isAdxStrong && isMacdBearish && isVolumeSpike;
    }

    public bool CheckStopLossCondition(double prevPrice, double currPrice, double avgPrice, double stopLossPercentage = 0.03)
    {
        if (avgPrice <= 0)
            return false;

        bool isStopLossCondition = (currPrice <= avgPrice * (1 - stopLossPercentage)) ||
                                   ((avgPrice * 1.015 <= currPrice) && prevPrice > currPrice);
        return isStopLossCondition;
    }

    public bool CheckAtrStopLoss(double price, double avgPrice, double atr, double atrMultiplier = 1.5)
    {
        if (avgPrice <= 0 || atr <= 0)
            return false;

        bool isStopLossCondition = (price <= avgPrice - (atr * atrMultiplier)) || (price >= avgPrice + (atr * atrMultiplier));
        return isStopLossCondition;
    }


    public TradeType EvaluateTradeConditions(double prevPrice, double currPrice, double avgPrice, double[] ema9, double[] ema20, double[] ema50, double[] ema200,
        double[] cciHistory, (double macd, double signal, double histogram) macd,
        double keltnerUpper, double keltnerLower, double[] obvHistory, double[] volumeHistory,  double rsi,
        (double upperBand, double lowerBand, double movingAverage) bands,
        double adx, (double diPlus, double diMinus) di,
        (double tenkan, double kijun, double senkouA, double senkouB, double chikou) ichimoku,
        (double k, double d) stochastic, double[] atrHistory, double vwap,
        List<CandleMinute> candles, bool isCoinHeld, bool isKRWHeld)
    {
        // 단기 트레이딩 전략 평가 (매수)
        bool isShortTermBuy = CheckShortTermBuyCondition(candles, ema20, ema50, cciHistory, keltnerUpper, obvHistory, volumeHistory, atrHistory.Last()) && isKRWHeld;
        // 중기 스윙 트레이딩 전략 평가 (매수)
        bool isSwingBuy = CheckSwingBuyCondition(currPrice, ema50, ema200, rsi, bands, adx, di, candles) && isKRWHeld;
        // 변동성 돌파 전략 평가 (매수)
        bool isVolatilityBreakoutBuy = CheckVolatilityBreakoutBuyCondition(currPrice, ichimoku, stochastic.k, stochastic.d, atrHistory, vwap, candles) && isKRWHeld;

        // 중기 스윙 트레이딩 전략 평가 (매도)
        bool isSwingSell = CheckSwingSellCondition(currPrice, rsi, bands, adx, di, candles) && isCoinHeld;
        // 변동성 돌파 전략 평가 (매도)
        bool isVolatilityBreakoutSell = CheckVolatilityBreakoutSellCondition(currPrice, ichimoku, vwap, adx, di) && isCoinHeld;
        // 손절 (매도)
        bool isStopLossCondition = CheckStopLossCondition(prevPrice, currPrice, avgPrice) && isCoinHeld;
        // (매도)
        bool isAtrStopLossCondition = CheckAtrStopLoss(currPrice, avgPrice, atrHistory[atrHistory.Length - 1]) && isCoinHeld;
        // (매도)
        bool isBearishSignal = IsBearishSignal(ema9, ema50, rsi, adx, di, macd, volumeHistory) && isCoinHeld;
        bool isGradualPriceDecline = IsGradualPriceDecline(rsi, macd, stochastic, avgPrice * 0.9 > currPrice, candles) && isCoinHeld;

        // 매수 및 매도 로직
        if (isStopLossCondition)
        {
            return ExecuteSellOrder("Stop Loss"); // 손절 매도
        }
        else if (isAtrStopLossCondition)
        {
            return ExecuteSellOrder("ATR Stop Loss"); // ATR 기반 손절 매도
        }
        else if (isVolatilityBreakoutSell)
        {
            return ExecuteSellOrder("Volatility"); // 변동성 돌파 매도
        }
        else if (isBearishSignal)
        {
            return ExecuteSellOrder("Bearish Signal"); // 하락 신호 매도
        }
        else if (isSwingSell)
        {
            return ExecuteSellOrder("Swing"); // 중기 스윙 트레이딩 매도
        }
        else if (isGradualPriceDecline)
        {
            return ExecuteSellOrder("Gradual Price Decline"); // 서서히 하락하는 가격 매도
        }

        // 매수 조건 처리
        else if (isShortTermBuy)
        {
            return ExecuteBuyOrder("Short Term Buy"); // 단기 트레이딩 매수
        }
        else if (isSwingBuy)
        {
            return ExecuteBuyOrder("Swing Buy"); // 중기 스윙 트레이딩 매수
        }
        else if (isVolatilityBreakoutBuy)
        {
            return ExecuteBuyOrder("Volatility Breakout Buy"); // 변동성 돌파 매수
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
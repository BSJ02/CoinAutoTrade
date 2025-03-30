using CoinAutoTradingApp.Models;
using CoinAutoTradingApp.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinAutoTradingApp.UpbitAPI;
using CoinAutoTradingApp.UpbitAPI.Models;
using static CoinAutoTradingApp.UpbitAPI.APIClass;
using CoinAutoTradingApp.Enum;

namespace CoinAutoTradingApp;
public partial class TradePage : ContentPage
{
    private APIClass API;
    private List<string> selectedMarkets = new List<string>(); // ✅ 선택한 코인 리스트
    private Dictionary<string, double> avgBuyPrice; // 평단가 저장
    private CancellationTokenSource tradeLoopTokenSource;

    private Dictionary<string, (double price, DateTime time)> pendingBuyOrders;  // 미체결 주문 추적
    private Dictionary<string, (double price, DateTime time)> pendingSellOrders;

    private const double FeeRate = 0.0005;  // 수수료
    private const double PendingOrderTimeLimit = 30; // 미체결 주문 취소 기간
    private const double InvestmentRatio = 0.2; // 한번 매수시 구매 비율 (총 KRW / InvestmentRatio)
    private const double MaxTradeKRW = 1000000;   // 매매 시 최대 금액

    // ✅ 프로그램 시작 후, 기존 보유 코인의 avgBuyPrice 세팅
    public void InitializeAvgBuyPrices()
    {
        var accounts = API.GetAccount();
        if (accounts != null)
        {
            foreach (var account in accounts)
            {
                if (account.Currency == "KRW") continue; // 원화 제외

                string market = $"KRW-{account.Currency}";
                double balance = account.Balance;
                double avgPrice = account.AvgBuyPrice;

                if (balance > 0)
                {
                    avgBuyPrice[market] = avgPrice; // ✅ 이미 보유한 코인의 평균 매수가 초기화
                    AddDebugMessage($"🔄 기존 보유 코인 추가: {market} | 평단가: {avgPrice:N2} ₩");
                }
            }
        }
    }

    // 📌 자동 매매 로직
    public void Trade()
    {
        foreach (var market in selectedMarkets)
        {
            var candles = API.GetCandleMinutes(market, (MinuteUnit)5, DateTime.UtcNow, 200)?.Cast<CandleMinute>().ToList();
            if (candles == null || candles.Count < 200)
            {
                AddDebugMessage($"⚠️ {market} 캔들 데이터 부족");
                continue;
            }

            double availableKRW = API.GetKRW().availableKRW;
            double tradeKRW = availableKRW > MaxTradeKRW ? MaxTradeKRW : availableKRW;

            double prevPrice = candles[1].TradePrice;
            double currPrice = candles[0].TradePrice;
            double avgPrice = avgBuyPrice.TryGetValue(market, out double price) ? price : 0;

            double[] ema9 = Calculate.EMAHistory(candles, 9).ToArray();
            double[] ema20 = Calculate.EMAHistory(candles, 20).ToArray();
            double[] ema50 = Calculate.EMAHistory(candles, 50).ToArray();
            double[] ema100 = Calculate.EMAHistory(candles, 100).ToArray();

            var macd = Calculate.MACD(candles);
            double[] cciHistory = Calculate.CCIHistory(candles).ToArray();

            var keltner = Calculate.KeltnerChannel(candles, 20);

            double[] obvHistory = Calculate.OBVHistory(candles).ToArray();
            double[] volumeHistory = Calculate.VolumeHistory(candles).ToArray();
            double rsi = Calculate.RSIHistory(candles).Last();

            var bollingerBands = Calculate.BollingerBands(candles, 20);
            double adx = Calculate.ADX(candles);
            var di = Calculate.DI(candles);
            var ichimoku = Calculate.IchimokuCloud(candles);
            var stochastic = Calculate.StochasticOscillator(candles);

            double[] atrHistory = Calculate.ATRHistory(candles).ToArray();
            double vwap = Calculate.VWAP(candles);


            // 미체결 주문 자동 취소
            if (pendingBuyOrders.ContainsKey(market))
            {
                var (orderPrice, orderTime) = pendingBuyOrders[market];
                if ((DateTime.Now - orderTime).TotalSeconds > PendingOrderTimeLimit || Math.Abs(currPrice - orderPrice) / orderPrice > 0.02)
                {
                    API.CancelOrder(market);
                    AddChatMessage($"🚫 미체결 매수 취소: {market} | 가격: {orderPrice:N2}");
                    pendingBuyOrders.Remove(market);
                }
            }

            if (pendingSellOrders.ContainsKey(market))
            {
                var (sellPrice, sellTime) = pendingSellOrders[market];
                if ((DateTime.Now - sellTime).TotalSeconds > PendingOrderTimeLimit)
                {
                    API.CancelOrder(market);
                    AddChatMessage($"🚫 미체결 매도 취소: {market} | 가격: {sellPrice:N2}");
                    pendingSellOrders.Remove(market);
                }
            }

            bool isBuyCondition = (pendingBuyOrders.ContainsKey(market) ? pendingBuyOrders[market].price * 0.99 >= currPrice : true) &&
                (avgBuyPrice.ContainsKey(market) ? avgBuyPrice[market] * 0.99 >= currPrice : true);

            // 매매
            var tradeType = EvaluateTradeConditions(
                prevPrice, currPrice, avgPrice,
                ema9, ema20, ema50, ema100,
                cciHistory, macd,
                keltner,
                obvHistory, volumeHistory,
                rsi, bollingerBands, adx, di,
                ichimoku, stochastic,
                atrHistory, vwap, candles,
                avgBuyPrice.ContainsKey(market),
                availableKRW > 5000 && isBuyCondition
            );

            /* ------------------------------- 매 수 -------------------------------*/
            if (TradeType.Buy.Equals(tradeType))
            {
                double buyPrice = currPrice;
                double buyQuantity = (tradeKRW * (1 - FeeRate)) / buyPrice;

                if (buyPrice * buyQuantity > 5000 && isBuyCondition)
                {
                    MakeOrderLimitBuy buyOrder = API.MakeOrderLimitBuy(market, buyPrice, buyQuantity);
                    if (buyOrder != null)
                    {
                        if (avgBuyPrice.ContainsKey(market))
                        {
                            double currentHolding = API.GetBalance(market);
                            double newTotalQuantity = currentHolding + buyQuantity;
                            avgBuyPrice[market] = ((avgBuyPrice[market] * currentHolding) + (buyPrice * buyQuantity)) / newTotalQuantity;
                        }
                        else
                        {
                            avgBuyPrice[market] = buyPrice;
                        }
                        pendingBuyOrders[market] = (buyPrice, DateTime.Now);


                        AddChatMessage($"🟡 매수: {market} | {buyPrice:C2} | {buyQuantity} = {buyPrice * buyQuantity:C2}");
                    }
                    else
                    {
                        AddChatMessage($"🚫 [Error] Buy order failed. Market: {market}, Price: {buyPrice:C2}, Quantity: {buyQuantity}");
                    }
                }
            }
            /* ---------------------------------------------------------------------*/


            /* ------------------------------- 매 도 -------------------------------*/
            if (TradeType.Sell.Equals(tradeType))
            {
                double sellVolume = API.GetBalance(market);
                if (sellVolume * currPrice > 5000)
                {
                    MakeOrderMarketSell sellOrder = API.MakeOrderMarketSell(market, sellVolume);
                    if (sellOrder != null)
                    {
                        avgBuyPrice.Remove(market);
                        pendingSellOrders[market] = (currPrice, DateTime.Now);

                        AddChatMessage($"🔴 매도: {market} | {currPrice:C2} | {sellVolume} = {currPrice * sellVolume:C2}");
                    }
                    else
                    {
                        AddChatMessage($"🚫 [Error] Sell order failed. Market: {market}");
                    }
                }
            }
            /* ---------------------------------------------------------------------*/
        }

    }
}
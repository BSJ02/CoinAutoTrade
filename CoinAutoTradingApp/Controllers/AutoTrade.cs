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

    // 미체결 주문 취소 (bid: 매수, ask: 매도)
    private Dictionary<string, (double price, DateTime time, string side)> pendingBuyOrders;
    private Dictionary<string, (double price, DateTime time, string side)> pendingSellOrders;

    private const double FeeRate = 0.0005;  // 수수료
    private const double PendingOrderTimeLimit = 60; // 미체결 주문 취소 기간
    private const double MaxTradeKRW = 500000;   // 매매 시 최대 금액

    private bool isHaveMarket = false;

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
            var minCandles = API.GetCandleMinutes(market, (CandleUnit)5, DateTime.UtcNow, 110)?.Cast<CandleMinute>().ToList();
            if (minCandles == null || minCandles.Count < 110)
            {
                AddDebugMessage($"⚠️ {market} 캔들 데이터 부족");
                continue;
            }

            isHaveMarket = API.IsHaveMarket(market);

            double availableKRW = API.GetKRW().availableKRW;
            double tradeKRW = availableKRW > MaxTradeKRW ? MaxTradeKRW : availableKRW;

            double prevPrice = minCandles[1].TradePrice;
            double currPrice = minCandles[0].TradePrice;
            double avgPrice = avgBuyPrice.TryGetValue(market, out double price) ? price : 0;

            double[] ema9 = Calculate.EMAHistory(minCandles, 9).ToArray();
            double[] ema20 = Calculate.EMAHistory(minCandles, 20).ToArray();
            double[] ema50 = Calculate.EMAHistory(minCandles, 50).ToArray();
            double[] ema100 = Calculate.EMAHistory(minCandles, 100).ToArray();

            double cci9 = Calculate.CCI(minCandles, 9);
            double cci14 = Calculate.CCI(minCandles, 14);

            var dmi = Calculate.DMI(minCandles);
            var bollingerBands = Calculate.BollingerBands(minCandles, 20);
            var keltner = Calculate.KeltnerChannel(minCandles, 20);

            double rsi = Calculate.RSI(minCandles);
            double atr = Calculate.ATR(minCandles);

            // 미체결 주문 취소
            CancelPendingOrder(pendingBuyOrders, market, OrderSide.bid.ToString());
            CancelPendingOrder(pendingSellOrders, market, OrderSide.ask.ToString());


            bool isBuyCondition = !pendingBuyOrders.ContainsKey(market) && !avgBuyPrice.ContainsKey(market);

            // 매매
            var tradeType = EvaluateTradeConditions(
                prevPrice, currPrice, avgPrice,
                ema9, ema20, ema50, ema100,
                cci9, cci14, atr, rsi, dmi, keltner, bollingerBands, minCandles,
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
                        avgBuyPrice[market] = buyPrice;
                        pendingBuyOrders[market] = (buyPrice, DateTime.Now, "bid");

                        AddChatMessage($"🟢 매수: {market.Split('-')[1]}");

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
                        pendingSellOrders[market] = (currPrice, DateTime.Now, "ask");

                        AddChatMessage($"🔴 매도: {market.Split('-')[1]} | {((currPrice - avgPrice) * sellVolume) - (currPrice * sellVolume * FeeRate + avgPrice * sellVolume * FeeRate):C2}");

                        totalProfit += ((currPrice - avgPrice) * sellVolume) - (currPrice * sellVolume * FeeRate + avgPrice * sellVolume * FeeRate);
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

    private void CancelPendingOrder(Dictionary<string, (double price, DateTime time, string side)> pendingOrders, string market, string orderSide)
    {
        if (!pendingOrders.ContainsKey(market)) return;

        if ((DateTime.Now - pendingOrders[market].time).TotalSeconds > PendingOrderTimeLimit)
        {
            var openOrders = API.GetOpenOrders(market);
            if (openOrders == null || openOrders.Count == 0)
            {
                pendingOrders.Remove(market);
                return;
            }

            foreach (var order in openOrders)
            {
                if (order.Market != market || order.Side != orderSide)
                    continue;

                if (API.CancelOrder(order.Uuid) != null)
                {
                    if (!isHaveMarket)
                    {
                        avgBuyPrice.Remove(market);
                    }

                    AddChatMessage($"🚫 미체결 {(orderSide == OrderSide.bid.ToString() ? "매수" : "매도")} 취소: {market} | 가격: {order.Price:N2}");
                    pendingOrders.Remove(market);
                    break;
                }
            }
        }

    }
}
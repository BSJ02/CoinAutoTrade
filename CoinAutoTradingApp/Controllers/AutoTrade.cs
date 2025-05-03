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
    private CancellationTokenSource tradeLoopTokenSource;

    // 미체결 주문 취소 (bid: 매수, ask: 매도)
    private Dictionary<string, (decimal price, DateTime time, string side)> pendingBuyOrders;
    private Dictionary<string, (decimal price, DateTime time, string side)> pendingSellOrders;

    private Dictionary<string, EntryCondition> entryCondition;
    private Dictionary<string, decimal> trailingStopPrice;

    private Dictionary<string, DateTime> waitBuyTime;

    private int totalBuyTrades = 0;
    private int totalSellTrades = 0;

    private decimal stopLossPrice = 0;

    private const decimal FeeRate = 0.005m;  // 수수료
    private const double PendingOrderTimeLimit = 60; // 미체결 주문 취소 기간
    private const double MaxTradeKRW = 1000000;   // 매매 시 최대 금액

    private string targetMarket = "";
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
                decimal avgPrice = account.AvgBuyPrice;

                if (balance > 0)
                {
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
            if (targetMarket != "" &&   // 주문할 마켓 탐색
                targetMarket != market) // 해당 마켓만 매매
                continue;

            var minCandles = API.GetCandles(market, (CandleUnit)5, DateTime.UtcNow, 200)?.Cast<CandleMinute>().ToList();
            if (minCandles == null || minCandles.Count < 200)
            {
                AddDebugMessage($"⚠️ {market} 캔들 데이터 부족");
                continue;
            }

            isHaveMarket = API.IsHaveMarket(market);

            double availableKRW = API.GetKRW().availableKRW;

            var accounts = API.GetAccount();

            decimal currPrice = minCandles[0].TradePrice;
            decimal avgPrice = 0;
            if (API.GetAccount(market) != null)
            {
                avgPrice = API.GetAccount(market).AvgBuyPrice;
            }
            decimal[] ema5 = Calculate.EMAHistory(minCandles, 5).ToArray();
            decimal[] ema20 = Calculate.EMAHistory(minCandles, 20).ToArray();
            decimal[] ema60 = Calculate.EMAHistory(minCandles, 60).ToArray();
            decimal[] ema120 = Calculate.EMAHistory(minCandles, 120).ToArray();
            decimal[] vwma = Calculate.VWMA(minCandles, 100).ToArray();
            BollingerBand bollingerBand = Calculate.BollingerBand(minCandles);

            // 미체결 주문 취소
            CancelPendingOrder(pendingBuyOrders, market, OrderSide.bid.ToString());
            CancelPendingOrder(pendingSellOrders, market, OrderSide.ask.ToString());

            bool isBuyCondition = !pendingBuyOrders.ContainsKey(market) && !isHaveMarket;

            // 매매
            var tradeType = EvaluateTradeConditions(
                currPrice, avgPrice,
                ema5, ema20,
                ema60, ema120,
                vwma, bollingerBand,
                minCandles,
                availableKRW > 5000 && isBuyCondition
            );

            /* ------------------------------- 매 수 -------------------------------*/
            if (TradeType.Buy.Equals(tradeType))
            {
                double tradeKRW = availableKRW > MaxTradeKRW ? MaxTradeKRW : availableKRW;
                decimal buyQuantity = ((decimal)tradeKRW * (1 - FeeRate)) / currPrice;

                if (currPrice * buyQuantity > 5000 && isBuyCondition)
                {
                    double haveBalance = API.GetBalance(market);

                    MakeOrderLimitBuy buyOrder = API.MakeOrderLimitBuy(market, currPrice, buyQuantity);
                    if (buyOrder != null)
                    {
                        stopLossPrice = bollingerBand.LowerBand * 0.997m;

                        targetMarket = market;

                        totalBuyTrades++;

                        waitBuyTime[market] = DateTime.Now; // 급락 때 연속 매수 방지

                        pendingBuyOrders[market] = (currPrice, DateTime.Now, "bid");

                        AddChatMessage($"🟢 매수: {market.Split('-')[1]} | 입장가: {currPrice:C2}");
                    }
                    else
                    {
                        AddChatMessage($"🚫 [Error] Buy order failed. Market: {market}, Price: {currPrice:C2}, Quantity: {buyQuantity}");
                    }
                }
            }
            /* ---------------------------------------------------------------------*/


            /* ------------------------------- 매 도 -------------------------------*/
            if (TradeType.Sell.Equals(tradeType))
            {
                double sellVolume = API.GetBalance(market);
                if ((decimal)sellVolume * currPrice > 5000)
                {
                    MakeOrderMarketSell sellOrder = API.MakeOrderMarketSell(market, sellVolume);
                    if (sellOrder != null)
                    {
                        stopLossPrice = 0;

                        targetMarket = "";

                        totalSellTrades++;

                        AddChatMessage($"🔴 매도: {market.Split('-')[1]} | {(currPrice - avgPrice * (1 + FeeRate * 2m)) / avgPrice * 100:N3}%");

                        entryCondition.Remove(market);

                        pendingSellOrders[market] = (currPrice, DateTime.Now, "ask");
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

    private void CancelPendingOrder(Dictionary<string, (decimal price, DateTime time, string side)> pendingOrders, string market, string orderSide)
    {
        if (!pendingOrders.ContainsKey(market)) return;

        if ((DateTime.Now - pendingOrders[market].time).TotalSeconds > PendingOrderTimeLimit)
        {
            if (!isHaveMarket)
            {
                targetMarket = "";
            }

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
                    entryCondition.Remove(market);

                    if (orderSide == OrderSide.bid.ToString())
                    {
                        totalBuyTrades--;
                    }
                    else
                    {
                        totalSellTrades--;
                    }

                    AddChatMessage($"🚫 미체결 {(orderSide == OrderSide.bid.ToString() ? "매수" : "매도")} 취소: {market} | 가격: {order.Price:N2}");

                    pendingOrders.Remove(market);
                    break;
                }
            }
        }

    }
}
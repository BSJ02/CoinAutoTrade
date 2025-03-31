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
    private const double PendingOrderTimeLimit = 60; // 미체결 주문 취소 기간
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
            var minCandles = API.GetCandleMinutes(market, (CandleUnit)5, DateTime.UtcNow, 200)?.Cast<CandleMinute>().ToList();
            if (minCandles == null || minCandles.Count < 200)
            {
                AddDebugMessage($"⚠️ {market} 캔들 데이터 부족");
                continue;
            }

            double availableKRW = API.GetKRW().availableKRW;
            double tradeKRW = availableKRW > MaxTradeKRW ? MaxTradeKRW : availableKRW;

            double prevPrice = minCandles[1].TradePrice;
            double currPrice = minCandles[0].TradePrice;
            double avgPrice = avgBuyPrice.TryGetValue(market, out double price) ? price : 0;

            double[] ema9 = Calculate.EMAHistory(minCandles, 9).ToArray();
            double[] ema20 = Calculate.EMAHistory(minCandles, 20).ToArray();
            double[] ema50 = Calculate.EMAHistory(minCandles, 50).ToArray();
            double[] ema100 = Calculate.EMAHistory(minCandles, 100).ToArray();

            double cci = Calculate.CCI(minCandles);

            var bollingerBands = Calculate.BollingerBands(minCandles, 20);
            var keltner = Calculate.KeltnerChannel(minCandles, 20);

            double rsi = Calculate.RSI(minCandles);
            double atr = Calculate.ATR(minCandles);


            // 미체결 주문 자동 취소
            if (pendingBuyOrders.ContainsKey(market))
            {
                var (orderPrice, orderTime) = pendingBuyOrders[market];
                if ((DateTime.Now - orderTime).TotalSeconds > PendingOrderTimeLimit || Math.Abs(currPrice - orderPrice) / orderPrice > 0.02)
                {
                    if (API.CancelOrder(market) != null)
                    {
                        AddChatMessage($"🚫 미체결 매수 취소: {market} | 가격: {orderPrice:N2}");
                        pendingBuyOrders.Remove(market);
                    }
                    /*else
                    {
                        AddChatMessage($"🚨 미체결 주문 취소 실패");
                    }*/
                }
            }

            if (pendingSellOrders.ContainsKey(market))
            {
                var (sellPrice, sellTime) = pendingSellOrders[market];
                if ((DateTime.Now - sellTime).TotalSeconds > PendingOrderTimeLimit)
                {
                    if (API.CancelOrder(market) != null)
                    {
                        AddChatMessage($"🚫 미체결 매도 취소: {market} | 가격: {sellPrice:N2}");
                        pendingSellOrders.Remove(market);
                    }
                    /*else
                    {
                        AddChatMessage($"🚨 미체결 주문 취소 실패");
                    }*/
                }
            }

            bool isBuyCondition = !pendingBuyOrders.ContainsKey(market);
            if (avgBuyPrice.ContainsKey(market))
            {
                if (avgBuyPrice[market] * API.GetBalance(market) > 5000)
                {
                    isBuyCondition &= (avgBuyPrice.ContainsKey(market) ? avgBuyPrice[market] * 0.99 >= currPrice : true ||
                                       avgBuyPrice.ContainsKey(market) ? avgBuyPrice[market] - atr >= currPrice : true);
                }
            }

            // 매매
            var tradeType = EvaluateTradeConditions(
                prevPrice, currPrice, avgPrice,
                ema9, ema20, ema50, ema100,
                cci, atr, rsi, keltner, bollingerBands, minCandles,
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
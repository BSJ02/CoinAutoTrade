using CoinAutoTradingApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CoinAutoTradingApp.UpbitAPI;
using CoinAutoTradingApp.Enum;

namespace CoinAutoTradingApp;

public partial class TradePage : ContentPage
{
    private decimal startKRW;

    private DateTime tradStartTime;
    private DateTime tradEndTime;

    private DateTime debugMessageResetTime;
    private int resetTimeLimit;

    private DateTime lastMarketUpdateTime;

    private readonly object marketLock = new();

    public TradePage(APIClass api)
    {
        InitializeComponent();

        this.API = api;

        this.BindingContext = this;

        ChatMessages = new ObservableCollection<ChatMessage>();
        DebugMessages = new ObservableCollection<ChatMessage>();

        pendingBuyOrders = new Dictionary<string, (decimal, DateTime, string)>();
        pendingSellOrders = new Dictionary<string, (decimal, DateTime, string)>();

        bbCount = new Dictionary<string, int>();

        profitPrice = new Dictionary<string, decimal>();
        stopLossPrice = new Dictionary<string, decimal>();

        debugMessageResetTime = DateTime.Now;
        resetTimeLimit = 120;

        lastMarketUpdateTime = DateTime.Now;
    }

    // 자동 매매 시작 함수
    private void StartTrading(object sender, EventArgs e)
    {
        SetTopMarketsByVolume();
        if (selectedMarkets == null || selectedMarkets.Count == 0)
        {
            AddDebugMessage("⚠️ 선택된 코인이 없음. 자동 매매 실행 불가.");
            return;
        }

        InitializeAvgBuyPrices(); // ✅ 기존 보유 코인 avgBuyPrice 설정
        tradeLoopTokenSource = new CancellationTokenSource();
        CancellationToken token = tradeLoopTokenSource.Token;

        startKRW = (decimal)API.GetKRW().totalKRW;
        tradStartTime = DateTime.Now;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if ((DateTime.Now - lastMarketUpdateTime).TotalHours >= 1)
                    {
                        List<string> marketsSnapshot;

                        lock (marketLock)
                        {
                            marketsSnapshot = new List<string>(selectedMarkets);
                        }

                        int haveCount = 0;
                        foreach (var market in marketsSnapshot)
                        {
                            if (API.IsHaveMarket(market))
                            {
                                haveCount++;
                            }
                        }

                        if (haveCount == 0)
                        {
                            SetTopMarketsByVolume();
                            lastMarketUpdateTime = DateTime.Now;
                            AddDebugMessage("🔄 거래량 상위 코인 갱신");
                        }
                    }

                    Trade();
                    AddDebugMessage($"⏳ 자동 매매 실행 중...");
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"❌ 자동 매매 오류 발생: {ex.Message}");
                }

                await Task.Delay(500);

                if ((DateTime.Now - debugMessageResetTime).TotalSeconds > resetTimeLimit)
                {
                    DebugMessages.Clear();
                    debugMessageResetTime = DateTime.Now;
                }
            }
        }, token);
    }

    // 자동 매매 중지 함수
    private void StopTrading(object sender, EventArgs e)
    {
        if (tradeLoopTokenSource != null)
        {
            tradEndTime = DateTime.Now;
            tradeLoopTokenSource.Cancel();
            tradeLoopTokenSource.Dispose();
            tradeLoopTokenSource = null;
            AddChatMessage("🛑 자동 매매 중지됨.");
            AddChatMessage($"매수: {totalBuyTrades}회");

            var endKRW = (decimal)API.GetKRW().totalKRW;
            AddChatMessage($"시간: {(int)(tradEndTime - tradStartTime).TotalMinutes}분 : {(endKRW - startKRW) / endKRW * 100:N2}% ({endKRW - startKRW:C2})");
        }
    }
}
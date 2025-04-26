using CoinAutoTradingApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CoinAutoTradingApp.UpbitAPI;
using CoinAutoTradingApp.Enum;

namespace CoinAutoTradingApp;

public partial class TradePage : ContentPage
{
    private double totalProfit;

    private DateTime tradStartTime;
    private DateTime tradEndTime;

    private DateTime debugMessageResetTime;
    private int resetTimeLimit;

    public TradePage(APIClass api)
    {
        InitializeComponent();

        this.API = api;

        this.BindingContext = this;

        ChatMessages = new ObservableCollection<ChatMessage>();
        DebugMessages = new ObservableCollection<ChatMessage>();

        avgBuyPrice = new Dictionary<string, double>();
        prevAvgBuyPrice = new Dictionary<string, double>();

        pendingBuyOrders = new Dictionary<string, (double, DateTime, string)>();
        pendingSellOrders = new Dictionary<string, (double, DateTime, string)>();

        entryCondition = new Dictionary<string, EntryCondition>();
        trailingStopPrice = new Dictionary<string, double>();

        waitBuyTime = new Dictionary<string, DateTime>();

        debugMessageResetTime = DateTime.Now;
        resetTimeLimit = 120;
    }

    // 자동 매매 시작 함수
    private void StartTrading(object sender, EventArgs e)
    {
        SetTop10MarketsByVolume();
        if (selectedMarkets == null || selectedMarkets.Count == 0)
        {
            AddDebugMessage("⚠️ 선택된 코인이 없음. 자동 매매 실행 불가.");
            return;
        }

        InitializeAvgBuyPrices(); // ✅ 기존 보유 코인 avgBuyPrice 설정
        tradeLoopTokenSource = new CancellationTokenSource();
        CancellationToken token = tradeLoopTokenSource.Token;

        totalProfit = API.GetKRW().totalKRW;
        tradStartTime = DateTime.Now;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Trade();
                    AddDebugMessage($"⏳ 자동 매매 실행 중... {DateTime.Now:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"❌ 자동 매매 중 오류 발생: {ex.Message}");
                }

                await Task.Delay(1000);


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
            AddChatMessage($"시간: {(int)(tradEndTime - tradStartTime).TotalMinutes}분 : {API.GetKRW().totalKRW - totalProfit:C2}");
        }
    }
}
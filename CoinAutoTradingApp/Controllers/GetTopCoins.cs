using CoinAutoTradingApp.UpbitAPI.Models;
using CoinAutoTradingApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CoinAutoTradingApp.UpbitAPI.APIClass;

namespace CoinAutoTradingApp;

public partial class TradePage : ContentPage
{
    private void SetSelectedMarkets(string coinList)
    {
        if (string.IsNullOrWhiteSpace(coinList))
        {
            AddDebugMessage("⚠️ 선택된 코인 목록이 비어 있음.");
            selectedMarkets.Clear();
            return;
        }

        selectedMarkets = coinList.Split(',')
                          .Select(m => m.Trim())
                          .Where(m => !string.IsNullOrEmpty(m))
                          .ToList();

        if (selectedMarkets.Count == 0)
        {
            AddDebugMessage("⚠️ 선택된 코인이 없음. 자동 매매 실행 불가.");
            return;
        }

        AddChatMessage($"✅ 자동 매매 대상 코인 설정: {string.Join(", ", selectedMarkets)}");
    }

    public void SetTop10MarketsByVolume()
    {
        var markets = API.GetMarketAll();

        var marketCodes = string.Join(",", markets.Select(m => m.Market));
        var tickers = API.GetTicker(marketCodes);

        var marketVolumes = tickers
            .Where(ticker => ticker.AccTradeVolume > 0) // 거래량이 0인 마켓은 제외
            .Select(ticker => new
            {
                Market = ticker.Market,
                Volume = ticker.AccTradePrice
            })
            .OrderByDescending(market => market.Volume) // 거래대금 내림차순 정렬
            .Take(15) // 상위 마켓 선택
            .ToList();

        var validMarkets = new List<string>();

        foreach (var marketData in marketVolumes)
        {
            var candles = API.GetCandleMinutes(marketData.Market, (CandleUnit)1, DateTime.UtcNow, 110)
                             ?.Cast<CandleMinute>()
                             .ToList();

            if (candles != null && candles.Count >= 110 && !marketData.Market.Contains("BTC") && !marketData.Market.Contains("XRP"))
            {
                validMarkets.Add(marketData.Market);
            }

            if (validMarkets.Count == 8)
                break;
        }

        var selectedMarketsString = string.Join(", ", validMarkets);
        SetSelectedMarkets(selectedMarketsString); // SetSelectedMarkets 호출

        AddDebugMessage($"✅ 거래대금 상위 8개 마켓이 자동 매매 대상에 설정되었습니다: {selectedMarketsString}");
    }
}
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

        lock (marketLock)
        {
            selectedMarkets = coinList.Split(',')
                  .Select(m => m.Trim())
                  .Where(m => !string.IsNullOrEmpty(m))
                  .ToList();
        }

        if (selectedMarkets.Count == 0)
        {
            AddDebugMessage("⚠️ 선택된 코인이 없음. 자동 매매 실행 불가.");
            return;
        }

        AddDebugMessage($"✅ 자동 매매 대상 코인 설정: {string.Join(", ", selectedMarkets)}");
    }

    public void SetTopMarketsByVolume()
    {
        var markets = API.GetMarketAll();

        var marketCodes = string.Join(",", markets.Select(m => m.Market));
        var tickers = API.GetTicker(marketCodes);

        var marketVolumes = tickers
            .Where(ticker => ticker.AccTradePrice24h > 100_000_000_000)
            .Select(ticker => new
            {
                Market = ticker.Market,
                Volume = ticker.AccTradePrice24h
            })
            .OrderByDescending(market => market.Volume) // 거래대금 내림차순 정렬
            .ToList();

        var validMarkets = new List<string>();

        foreach (var marketData in marketVolumes)
        {
            var candles = API.GetCandles(marketData.Market, (CandleUnit)5, DateTime.UtcNow, 200)
                             ?.Cast<CandleMinute>()
                             .ToList();

            if (candles != null && candles.Count >= 200 && !FilterOutCoins(marketData.Market))
            {
                validMarkets.Add(marketData.Market);
            }
        }

        var selectedMarketsString = string.Join(", ", validMarkets);
        SetSelectedMarkets(selectedMarketsString); // SetSelectedMarkets 호출

        AddDebugMessage($"✅ 거래대금 상위 {validMarkets.Count}개 마켓이 자동 매매 대상에 설정되었습니다: {selectedMarketsString}");
    }

    public bool FilterOutCoins(string market)
    {
        return market.Contains("USDT") ||
               market.Contains("XRP") ||
               market.Contains("ETH") ||
               market.Contains("BTC");
    }
}
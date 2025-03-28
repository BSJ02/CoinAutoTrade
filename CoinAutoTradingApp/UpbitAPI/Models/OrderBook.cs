using Newtonsoft.Json;
using System.Collections.Generic;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 14. OrderBook (호가 정보) 모델
    public class OrderBook
    {
        [JsonProperty("market")]
        public string Market { get; set; }           // 마켓 코드

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }          // 호가 생성 시간

        [JsonProperty("total_bid_size")]
        public double TotalBidSize { get; set; }      // 매수 누적량

        [JsonProperty("total_ask_size")]
        public double TotalAskSize { get; set; }      // 매도 누적량

        [JsonProperty("orderbook_units")]
        public List<OrderBookUnit> OrderBookUnits { get; set; } // 호가
    }

    // 14.1. OrderBook 내부 OrderBookUnit 모델
    public class OrderBookUnit
    {
        [JsonProperty("ask_price")]
        public double AskPrice { get; set; }        // 매도 호가

        [JsonProperty("bid_price")]
        public double BidPrice { get; set; }        // 매수 호가

        [JsonProperty("ask_size")]
        public double AskSize { get; set; }         // 매도량

        [JsonProperty("bid_size")]
        public double BidSize { get; set; }         // 매수량
    }
}

using Newtonsoft.Json;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 13. Ticker (현재가 정보) 모델
    public class Ticker : IDisposable
    {
        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("trade_date")]
        public string TradeDate { get; set; }

        [JsonProperty("trade_time")]
        public string TradeTime { get; set; }

        [JsonProperty("trade_date_kst")]
        public string TradeDateKst { get; set; }

        [JsonProperty("trade_time_kst")]
        public string TradeTimeKst { get; set; }

        [JsonProperty("trade_timestamp")]
        public long? TradeTimestamp { get; set; }

        [JsonProperty("opening_price")]
        public decimal? OpeningPrice { get; set; }

        [JsonProperty("high_price")]
        public decimal? HighPrice { get; set; }

        [JsonProperty("low_price")]
        public decimal? LowPrice { get; set; }

        [JsonProperty("trade_price")]
        public decimal? TradePrice { get; set; }

        [JsonProperty("prev_closing_price")]
        public decimal? PrevClosingPrice { get; set; }

        [JsonProperty("change")]
        public string Change { get; set; }

        [JsonProperty("change_price")]
        public decimal? ChangePrice { get; set; }

        [JsonProperty("change_rate")]
        public decimal? ChangeRate { get; set; }

        [JsonProperty("signed_change_price")]
        public decimal? SignedChangePrice { get; set; }

        [JsonProperty("signed_change_rate")]
        public decimal? SignedChangeRate { get; set; }

        [JsonProperty("trade_volume")]
        public decimal? TradeVolume { get; set; }

        [JsonProperty("acc_trade_price")]
        public decimal? AccTradePrice { get; set; }

        [JsonProperty("acc_trade_price_24h")]
        public decimal? AccTradePrice24h { get; set; }

        [JsonProperty("acc_trade_volume")]
        public decimal? AccTradeVolume { get; set; }

        [JsonProperty("highest_52_week_price")]
        public decimal? Highest52WeekPrice { get; set; }

        [JsonProperty("highest_52_week_date")]
        public string Highest52WeekDate { get; set; }

        [JsonProperty("lowest_52_week_price")]
        public decimal? Lowest52WeekPrice { get; set; }

        [JsonProperty("lowest_52_week_date")]
        public string Lowest52WeekDate { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        public void Dispose() { }
    }
}

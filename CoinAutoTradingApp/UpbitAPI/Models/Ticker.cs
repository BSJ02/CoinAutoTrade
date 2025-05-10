using Newtonsoft.Json;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 13. Ticker (현재가 정보) 모델
    public class Ticker : IDisposable
    {
        [JsonProperty("market")]
        public string Market { get; set; }         // 마켓 코드

        [JsonProperty("trade_date")]
        public string TradeDate { get; set; }      // 최근 거래 일자 (UTC)

        [JsonProperty("trade_time")]
        public string TradeTime { get; set; }      // 최근 거래 시각 (UTC)

        [JsonProperty("trade_date_kst")]
        public string TradeDateKst { get; set; }   // 최근 거래 일자 (KST)

        [JsonProperty("trade_time_kst")]
        public string TradeTimeKst { get; set; }   // 최근 거래 시각 (KST)

        [JsonProperty("trade_timestamp")]
        public long? TradeTimestamp { get; set; }   // 체결 타임스탬프 (milliseconds)

        [JsonProperty("opening_price")]
        public double OpeningPrice { get; set; }    // 시가

        [JsonProperty("high_price")]
        public double HighPrice { get; set; }       // 고가

        [JsonProperty("low_price")]
        public double LowPrice { get; set; }        // 저가

        [JsonProperty("trade_price")]
        public double TradePrice { get; set; }     // 종가

        [JsonProperty("prev_closing_price")]
        public double PrevClosingPrice { get; set; } // 전일 종가

        [JsonProperty("change")]
        public string Change { get; set; }          // EVEN : 보합, RISE : 상승, FALL : 하락

        [JsonProperty("change_price")]
        public double? ChangePrice { get; set; }     // 변화액

        [JsonProperty("change_rate")]
        public double ChangeRate { get; set; }      // 변화율

        [JsonProperty("signed_change_price")]
        public double SignedChangePrice { get; set; } // 부호가 있는 변화액

        [JsonProperty("signed_change_rate")]
        public double SignedChangeRate { get; set; }  // 부호가 있는 변화율

        [JsonProperty("trade_volume")]
        public double TradeVolume { get; set; }     // 가장 최근 거래량

        [JsonProperty("acc_trade_price")]
        public double AccTradePrice { get; set; }    // 누적 거래 금액

        [JsonProperty("acc_trade_price_24h")]
        public double AccTradePrice24h { get; set; }    // 누적 거래 금액 24h

        [JsonProperty("acc_trade_volume")]
        public double AccTradeVolume { get; set; }   // 누적 거래량

        [JsonProperty("highest_52_week_price")]
        public double Highest52WeekPrice { get; set; } // 52주 최고가

        [JsonProperty("highest_52_week_date")]
        public string Highest52WeekDate { get; set; } // 52주 최고가 달성일

        [JsonProperty("lowest_52_week_price")]
        public double Lowest52WeekPrice { get; set; }  // 52주 최저가

        [JsonProperty("lowest_52_week_date")]
        public string Lowest52WeekDate { get; set; }  // 52주 최저가 달성일

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }        // 타임스탬프 (milliseconds)


        public void Dispose() { }
    }
}

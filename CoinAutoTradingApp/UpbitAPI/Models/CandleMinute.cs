using Newtonsoft.Json;
using System;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 9. 분 캔들 (CandleMinute) 모델
    public class CandleMinute
    {
        [JsonProperty("market")]
        public string Market { get; set; }         // 마켓 코드

        [JsonProperty("candle_date_time_utc")]
        public DateTime CandleDateTimeUtc { get; set; } // 캔들 기준 시각 (UTC 기준)

        [JsonProperty("candle_date_time_kst")]
        public DateTime CandleDateTimeKst { get; set; } // 캔들 기준 시각 (KST 기준)

        [JsonProperty("opening_price")]
        public decimal OpeningPrice { get; set; }    // 시가

        [JsonProperty("high_price")]
        public decimal HighPrice { get; set; }       // 고가

        [JsonProperty("low_price")]
        public decimal LowPrice { get; set; }        // 저가

        [JsonProperty("trade_price")]
        public decimal TradePrice { get; set; }     // 종가

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }        // 해당 캔들에서 마지막 틱이 저장된 시각

        [JsonProperty("candle_acc_trade_price")]
        public decimal CandleAccTradePrice { get; set; } // 누적 거래 금액

        [JsonProperty("candle_acc_trade_volume")]
        public decimal CandleAccTradeVolume { get; set; } // 누적 거래량

        [JsonProperty("unit")]
        public int Unit { get; set; }           // 분 단위 (1, 3, 5, 15, 30, 60, 240)
    }
}

using Newtonsoft.Json;
using System;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 11. 주 캔들 (CandleWeek) 모델
    public class CandleWeek
    {
        [JsonProperty("market")]
        public string Market { get; set; }         // 마켓 코드

        [JsonProperty("candle_date_time_utc")]
        public DateTime CandleDateTimeUtc { get; set; } // 캔들 기준 시각 (UTC 기준)

        [JsonProperty("candle_date_time_kst")]
        public DateTime CandleDateTimeKst { get; set; } // 캔들 기준 시각 (KST 기준)

        [JsonProperty("opening_price")]
        public double OpeningPrice { get; set; }    // 시가

        [JsonProperty("high_price")]
        public double HighPrice { get; set; }       // 고가

        [JsonProperty("low_price")]
        public double LowPrice { get; set; }        // 저가

        [JsonProperty("trade_price")]
        public double TradePrice { get; set; }     // 종가

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }        // 해당 캔들에서 마지막 틱이 저장된 시각

        [JsonProperty("candle_acc_trade_price")]
        public double CandleAccTradePrice { get; set; } // 누적 거래 금액

        [JsonProperty("candle_acc_trade_volume")]
        public double CandleAccTradeVolume { get; set; } // 누적 거래량

        [JsonProperty("first_day_of_period")]
        public DateTime FirstDayOfPeriod { get; set; } // 해당 주차의 시작 날짜
    }
}

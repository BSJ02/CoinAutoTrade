using Newtonsoft.Json;
using System;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // ✅ 지정가 매도 주문 (MakeOrderLimitSell)
    public class MakeOrderLimitSell
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }    // 주문의 고유 ID

        [JsonProperty("side")]
        public string Side { get; set; }    // 주문 종류 (ask = 매도)

        [JsonProperty("ord_type")]
        public string OrdType { get; set; } // 주문 방식 (limit = 지정가)

        [JsonProperty("volume")]
        public string Volume { get; set; }  // 주문량

        [JsonProperty("price")]
        public string Price { get; set; }   // 지정한 매도가격

        [JsonProperty("state")]
        public string State { get; set; }   // 주문 상태 (wait, done, cancel)

        [JsonProperty("market")]
        public string Market { get; set; }  // 마켓 ID

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } // 주문 생성 시간
    }
}

using Newtonsoft.Json;
using System;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // ✅ 7. 지정가 매수 주문 (MakeOrderLimitBuy) 모델
    public class MakeOrderLimitBuy
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }    // 주문의 고유 ID

        [JsonProperty("side")]
        public string Side { get; set; } = "bid"; // ✅ 매수 주문 (bid)

        [JsonProperty("ord_type")]
        public string OrdType { get; set; } = "limit"; // ✅ 지정가 주문 (limit)

        [JsonProperty("volume")]
        public string Volume { get; set; }  // 주문량 (개수)

        [JsonProperty("price")]
        public string Price { get; set; }   // 지정한 매수 가격

        [JsonProperty("state")]
        public string State { get; set; }   // 주문 상태 (wait, done, cancel)

        [JsonProperty("market")]
        public string Market { get; set; }  // 마켓 ID (KRW-ETH, KRW-XRP 등)

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } // 주문 생성 시간
    }
}

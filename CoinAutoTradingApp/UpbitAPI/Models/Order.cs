using Newtonsoft.Json;
using System;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 3. 주문 (Order) 모델
    public class Order : IDisposable
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }           // 주문의 고유 ID

        [JsonProperty("side")]
        public string Side { get; set; }           // 주문 종류 (bid, ask)

        [JsonProperty("ord_type")]
        public string OrdType { get; set; }        // 주문 방식 (limit, price, market)

        [JsonProperty("price")]
        public double Price { get; set; }          // 주문 당시 화폐 가격

        [JsonProperty("avg_price")]
        public double AvgPrice { get; set; }       // 체결 가격의 평균가

        [JsonProperty("state")]
        public string State { get; set; }          // 주문 상태 (wait, done, cancel)

        [JsonProperty("market")]
        public string Market { get; set; }         // 마켓 ID

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }     // 주문 생성 시간

        [JsonProperty("volume")]
        public double Volume { get; set; }         // 주문량

        [JsonProperty("remaining_volume")]
        public double RemainingVolume { get; set; }  // 남은 주문량

        [JsonProperty("reserved_fee")]
        public string ReservedFee { get; set; }     // 수수료

        [JsonProperty("remaining_fee")]
        public string RemainingFee { get; set; }   // 남은 수수료

        [JsonProperty("paid_fee")]
        public string PaidFee { get; set; }        // 지불된 수수료

        [JsonProperty("locked")]
        public string Locked { get; set; }         // 묶여있는 금액

        [JsonProperty("executed_volume")]
        public string ExecutedVolume { get; set; }  // 체결된 양

        [JsonProperty("trades_count")]
        public int TradesCount { get; set; }      // 해당 주문에 대한 체결 횟수

        public ErrorClass Error { get; set; }

        public void Dispose() { }

    }
}

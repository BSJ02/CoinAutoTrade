using Newtonsoft.Json;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    public class Account : IDisposable
    {
        [JsonProperty("currency")]
        public string Currency { get; set; } // ✅ 화폐 단위 (KRW, BTC, ETH 등)

        [JsonProperty("balance")]
        public double Balance { get; set; } // ✅ 현재 보유량

        [JsonProperty("locked")]
        public double Locked { get; set; } // ✅ 주문 중 묶인 금액

        [JsonProperty("avg_buy_price")]
        public decimal AvgBuyPrice { get; set; } // ✅ 평균 매수 단가

        [JsonProperty("avg_buy_price_modified")]
        public bool AvgBuyPriceModified { get; set; } // ✅ 매수 평균가 수정 여부

        // ✅ 가용 원화 계산 (KRW 계좌일 때만)
        public double AvailableKRW => (Currency == "KRW") ? Balance - Locked : 0;

        public void Dispose() { }
    }
}

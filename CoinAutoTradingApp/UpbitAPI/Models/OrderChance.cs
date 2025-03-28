using Newtonsoft.Json;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 2. 주문 가능 정보 (OrderChance) 모델
    public class OrderChance
    {
        [JsonProperty("bid_fee")]
        public string BidFee { get; set; }          // 매수 수수료 비율

        [JsonProperty("ask_fee")]
        public string AskFee { get; set; }          // 매도 수수료 비율

        [JsonProperty("market")]
        public Market Market { get; set; }      // 마켓에 대한 정보

        [JsonProperty("bid_account")]
        public BidAccount BidAccount { get; set; }  // 매수 시 사용하는 화폐 계좌 정보

        [JsonProperty("ask_account")]
        public AskAccount AskAccount { get; set; }  // 매도 시 사용하는 화폐 계좌 정보
    }

    // 2.1. OrderChance 내부 Market 모델
    public class Market
    {
        [JsonProperty("id")]
        public string Id { get; set; }              // 마켓 ID (ex: KRW-BTC)

        [JsonProperty("name")]
        public string Name { get; set; }            // 마켓 이름

        [JsonProperty("order_types")]
        public List<string> OrderTypes { get; set; }  // 지원하는 주문 방식 (limit, price, market)

        [JsonProperty("order_sides")]
        public List<string> OrderSides { get; set; }  // 지원하는 주문 종류 (bid, ask)

        [JsonProperty("bid")]
        public Bid Bid { get; set; }              // 매수 관련 정보

        [JsonProperty("ask")]
        public Ask Ask { get; set; }              // 매도 관련 정보

        [JsonProperty("max_total")]
        public string MaxTotal { get; set; }        // 최대 매도/매수 금액
        [JsonProperty("state")]
        public string State { get; set; }          // 마켓 상태
    }

    // 2.2. OrderChance 내부 Bid/Ask 모델
    public class Bid
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }       // 화폐를 의미하는 영문 대문자 코드

        [JsonProperty("price_unit")]
        public string PriceUnit { get; set; }      // 주문 가격 단위

        [JsonProperty("min_total")]
        public string MinTotal { get; set; }        // 최소 매도/매수 금액
    }

    public class Ask
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }       // 화폐를 의미하는 영문 대문자 코드

        [JsonProperty("price_unit")]
        public string PriceUnit { get; set; }      // 주문 가격 단위

        [JsonProperty("min_total")]
        public string MinTotal { get; set; }        // 최소 매도/매수 금액
    }

    // 2.3. OrderChance 내부 BidAccount/AskAccount 모델
    public class BidAccount
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }       // 화폐를 의미하는 영문 대문자 코드

        [JsonProperty("balance")]
        public string Balance { get; set; }        // 주문가능 금액/수량

        [JsonProperty("locked")]
        public string Locked { get; set; }         // 묶여있는 주문 금액/수량

        [JsonProperty("avg_buy_price")]
        public string AvgBuyPrice { get; set; }   // 매수평균가

        [JsonProperty("avg_buy_price_modified")]
        public bool AvgBuyPriceModified { get; set; }  // 매수평균가 수정 여부

        [JsonProperty("unit_currency")]
        public string UnitCurrency { get; set; }  // 화폐를 의미하는 영문 대문자 코드 (KRW, BTC, USDT)
    }

    public class AskAccount
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }       // 화폐를 의미하는 영문 대문자 코드

        [JsonProperty("balance")]
        public string Balance { get; set; }        // 주문가능 금액/수량

        [JsonProperty("locked")]
        public string Locked { get; set; }         // 묶여있는 주문 금액/수량

        [JsonProperty("avg_buy_price")]
        public string AvgBuyPrice { get; set; }   // 매수평균가

        [JsonProperty("avg_buy_price_modified")]
        public bool AvgBuyPriceModified { get; set; }  // 매수평균가 수정 여부

        [JsonProperty("unit_currency")]
        public string UnitCurrency { get; set; }  // 화폐를 의미하는 영문 대문자 코드 (KRW, BTC, USDT)
    }
}

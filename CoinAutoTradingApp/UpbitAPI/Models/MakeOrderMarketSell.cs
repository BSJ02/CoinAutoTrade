﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 7. 시장가 매도 주문 (MakeOrderMarketSell) 모델
    public class MakeOrderMarketSell
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }    // 주문의 고유 ID

        [JsonProperty("side")]
        public string Side { get; set; }    // 주문 종류 (bid, ask)

        [JsonProperty("ord_type")]
        public string OrdType { get; set; } // 주문 방식 (limit, price, market)

        [JsonProperty("volume")]
        public string Volume { get; set; }  // 주문량

        [JsonProperty("price")]
        public string Price { get; set; }   // 주문 당시 화폐 가격

        [JsonProperty("state")]
        public string State { get; set; }   // 주문 상태 (wait, done, cancel)

        [JsonProperty("market")]
        public string Market { get; set; }  // 마켓 ID

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } // 주문 생성 시간
    }
}

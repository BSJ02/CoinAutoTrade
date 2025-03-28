using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.UpbitAPI.Models
{
    // 8. 마켓 코드 조회 (MarketAll) 모델
    public class MarketAll
    {
        [JsonProperty("market")]
        public string Market { get; set; }   // 마켓 코드

        [JsonProperty("korean_name")]
        public string KoreanName { get; set; }  // 한글명

        [JsonProperty("english_name")]
        public string EnglishName { get; set; } // 영문명

        [JsonProperty("market_warning")]
        public string MarketWarning { get; set; } // 유의 종목 여부 (NONE, CAUTION)
    }
}

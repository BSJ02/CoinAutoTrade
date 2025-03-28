using CoinAutoTradingApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinAutoTradingApp.UpbitAPI.Models;

namespace CoinAutoTradingApp.Models
{
    public class Candle
    {
        public DateTime Timestamp { get; set; }  // 캔들 시간
        public decimal Open { get; set; }        // 시작가
        public decimal High { get; set; }        // 최고가
        public decimal Low { get; set; }         // 최저가
        public decimal Close { get; set; }       // 종가
        public decimal Volume { get; set; }      // 거래량
        public decimal QuoteVolume { get; set; } // 거래 금액
        public int TradeCount { get; set; }      // 거래 횟수


        // 추가적인 기술 지표 (옵션)
        public double RSI { get; set; }
        public double ATR { get; set; }
        public double MACD { get; set; }
        public double MACDSignal { get; set; }
        public double MACDHistogram { get; set; }
    }
}

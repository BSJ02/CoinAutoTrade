using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Enum
{
    public enum TradeType
    {
        Buy,
        Sell,
        None
    }

    public enum EntryCondition
    {
        Ema50AboveEma200,  // EMA50 > EMA200: 단기 추세가 장기 추세를 돌파
        Ema200AboveEma50,  // EMA200 > EMA50: 단기 추세가 장기 추세를 돌파
        None
    }
}

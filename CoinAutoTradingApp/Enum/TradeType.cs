using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Enum
{
    public enum TradeType
    {
        None,
        Buy,
        Sell
    }

    public enum EntryCondition
    {
        None,
        Ema50AboveVWMA,  // EMA50 > VWMA: 단기 추세가 장기 추세를 돌파
        VWMAAboveEma50  // VWMA > EMA50: 단기 추세가 장기 추세를 돌파
    }
}

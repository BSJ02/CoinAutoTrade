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
        EMAOrdered,  // EMA 정배열
        EMAReversed  // EMA 역배열
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Models
{
    public class MACD
    {
        public List<decimal> MacdLine { get; set; }
        public List<decimal> SignalLine { get; set; }
        public List<decimal> Histogram { get; set; }
    }
}

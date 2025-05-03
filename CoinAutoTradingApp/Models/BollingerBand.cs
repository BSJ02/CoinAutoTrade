using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Models
{
    public class BollingerBand
    {
        public decimal Basis { get; set; }         // 중심선 (SMA)
        public decimal UpperBand { get; set; }     // 상단 밴드
        public decimal LowerBand { get; set; }     // 하단 밴드
    }
}

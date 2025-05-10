using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Models
{
    public class SupportResistanceLevels
    {
        public decimal SupportLine { get; set; }
        public decimal ResistanceLine { get; set; }
        public decimal StopLossLine { get; set; }
    }
}

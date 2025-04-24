using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Models
{
    public class BuyInformation
    {
        public string Market { get; set; }
        public double Slope { get; set; }
        public (double low, double high) Intercept { get; set; }

        public BuyInformation(string market, double slope, (double low, double high) intercept)
        {
            Market = market;
            Slope = slope;
            Intercept = intercept;
        }
    }
}

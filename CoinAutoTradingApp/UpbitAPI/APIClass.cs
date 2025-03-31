using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using CoinAutoTradingApp.UpbitAPI.Models;

namespace CoinAutoTradingApp.UpbitAPI
{
    public class APIClass
    {
        private Param param;
        private NoParam noparam;

        public APIClass(string upbitAccessKey, string upbitSecretKey)
        {
            param = new Param(upbitAccessKey, upbitSecretKey);
            noparam = new NoParam(upbitAccessKey, upbitSecretKey);
        }
        /*--------------------- EXCHANGE API ---------------------*/
        public List<Account> GetAccount()
        {
            // 자산 - 전체 계좌 조회
            var data = noparam.Get("/v1/accounts", RestSharp.Method.Get);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<Account>>(data);
            }
            else
            {
                return null;
            }
        }

        // AvailableKRW = 사용 가능 KRW, totalKRW = 주문 들어간 KRW 포함 모든 KRW
        public (double availableKRW, double totalKRW) GetKRW()
        {
            var accounts = GetAccount(); // 기존 계좌 조회 메서드 호출
            if (accounts == null || accounts.Count == 0)
            {
                Debug.WriteLine("❌ 계좌 정보를 가져올 수 없음.");
                return (0, 0);
            }

            // ✅ KRW 계좌 정보만 필터링
            var krwAccount = accounts.FirstOrDefault(a => a.Currency == "KRW");
            if (krwAccount == null)
            {
                Debug.WriteLine("❌ KRW 계좌 정보 없음.");
                return (0, 0);
            }

            double availableKRW = krwAccount.Balance - krwAccount.Locked;
            double totalKRW = krwAccount.Balance + krwAccount.Locked;
            return (availableKRW, totalKRW);
        }

        public double GetBalance(string market)
        {
            var accounts = GetAccount(); // ✅ 계좌 정보 가져오기
            if (accounts == null || accounts.Count == 0)
            {
                Debug.WriteLine("❌ 계좌 정보를 가져올 수 없음.");
                return 0;
            }

            string currency = market.Replace("KRW-", ""); // ✅ 'KRW-BTC' -> 'BTC' 변환
            var account = accounts.FirstOrDefault(a => a.Currency == currency);
            if (account == null)
            {
                Debug.WriteLine($"❌ {market} 보유량 없음.");
                return 0;
            }

            Debug.WriteLine($"✅ {market} 보유량: {account.Balance}");
            return account.Balance;
        }

        public double GetCurrentPrice(string market)
        {
            var ticker = GetTicker(market); // ✅ 업비트 API에서 현재 가격 가져오기
            if (ticker != null && ticker.Count > 0)
            {
                return ticker[0].TradePrice;
            }
            return 0;
        }



        public OrderChance GetOrderChance(string market)
        {
            // 주문 - 주문 가능 정보
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            var data = param.Get("/v1/orders/chance", parameters, RestSharp.Method.Get);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<OrderChance>(data);
            }
            else
            {
                return null;
            }
        }
        public Order GetOrder(string uuid)
        {
            // 주문 - 개별 주문 조회
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("uuid", uuid);
            var data = param.Get("/v1/order", parameters, RestSharp.Method.Get);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<Order>(data);
            }
            else
            {
                return null;
            }
        }

        public CancelOrder CancelOrder(string uuid, string identifier = null)
        {
            // 주문 - 주문 취소 접수
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "uuid", uuid }
            };

            var data = param.Delete("/v1/order", parameters);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<CancelOrder>(data);
            }
            else
            {
                return null;
            }
        }
        public MakeOrder MakeOrderLimit(string market, OrderSide orderSide, double volume, double price)
        {
            // 주문 - 주문하기 - 지정가 매수&매도
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("side", orderSide.ToString());
            parameters.Add("volume", volume.ToString());
            parameters.Add("price", price.ToString());
            parameters.Add("ord_type", "limit");

            var data = param.Post("/v1/orders", parameters, RestSharp.Method.Post);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<MakeOrder>(data);
            }
            else
            {
                return null;
            }
        }

        public MakeOrderLimitBuy MakeOrderLimitBuy(string market, double buyPrice, double buyQuantity)
        {
            // ✅ 지정가 매수 주문
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("side", "bid"); // ✅ 매수 주문
            parameters.Add("volume", buyQuantity.ToString());
            parameters.Add("price", buyPrice.ToString());
            parameters.Add("ord_type", "limit"); // ✅ 지정가 주문

            var data = param.Post("/v1/orders", parameters, RestSharp.Method.Post);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<MakeOrderLimitBuy>(data);
            }
            else
            {
                return null;
            }
        }

        public MakeOrderMarketBuy MakeOrderMarketBuy(string market, double price)
        {
            // 주문 - 주문하기 - 시장가매수

            /* 주문 가격. (지정가, 시장가 매수 시 필수)
            ex) KRW-BTC 마켓에서 1BTC당 1,000 KRW로 거래할 경우, 값은 1000 이 된다.
            ex) KRW-BTC 마켓에서 1BTC당 매도 1호가가 500 KRW 인 경우,
            시장가 매수 시 값을 1000으로 세팅하면 2BTC가 매수된다.
            (수수료가 존재하거나 매도 1호가의 수량에 따라 상이할 수 있음)  
            --> 결론 : price는 원화가치인듯 */

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("side", OrderSide.bid.ToString());
            parameters.Add("price", price.ToString());
            parameters.Add("ord_type", "price");
            var data = param.Post("/v1/orders", parameters, RestSharp.Method.Post);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<MakeOrderMarketBuy>(data);
            }
            else
            {
                return null;
            }

        }

        public MakeOrderLimitSell MakeOrderLimitSell(string market, double sellPrice, double sellQuantity)
        {
            // ✅ 지정가 매도 주문 요청
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("side", "ask"); // ✅ 매도 주문
            parameters.Add("volume", sellQuantity.ToString());
            parameters.Add("price", sellPrice.ToString());
            parameters.Add("ord_type", "limit"); // ✅ 지정가 주문

            var data = param.Post("/v1/orders", parameters, RestSharp.Method.Post);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<MakeOrderLimitSell>(data);
            }
            else
            {
                return null;
            }
        }


        public MakeOrderMarketSell MakeOrderMarketSell(string market, double volume)
        {
            // 주문 - 주문하기 - 시장가매도
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("side", OrderSide.ask.ToString());
            parameters.Add("volume", volume.ToString());
            parameters.Add("ord_type", "market");
            var data = param.Post("/v1/orders", parameters, RestSharp.Method.Post);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<MakeOrderMarketSell>(data);
            }
            else
            {
                return null;
            }
        }


        /*--------------------- QUOTATION API ---------------------*/

        public List<MarketAll> GetMarketAll()
        {
            // 시세 종목 조회 - 마켓 코드 조회
            var data = noparam.Get("/v1/market/all", RestSharp.Method.Get);
            return JsonConvert.DeserializeObject<List<MarketAll>>(data);

        }
        public List<CandleMinute> GetCandleMinutes(string market, CandleUnit unit, DateTime? to = null, int count = 1)
        {
            // ✅ 'to'가 null이면 현재 시간 기준으로 설정
            DateTime requestTime = to ?? DateTime.UtcNow;

            // 시세 캔들 조회 - 분(Minute) 캔들
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "market", market },
                { "to", requestTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") }, // ✅ 업비트 API에 맞는 UTC 포맷 사용
                { "count", count.ToString() }
            };

            var data = param.Get($"/v1/candles/minutes/{(int)unit}", parameters, RestSharp.Method.Get);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<CandleMinute>>(data);
            }
            else
            {
                return null;
            }
        }


        public List<CandleDay> GetCandleDays(string market, int count = 1, DateTime? to = null)
        {
            DateTime requestTime = to ?? DateTime.UtcNow;

            // 시세 캔들 조회 - 일(Day) 캔들
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "market", market },
                { "to", requestTime.ToString("yyyy-MM-dd HH:mm:ss") }, // ✅ 업비트 API에 맞는 UTC 포맷 사용
                { "count", count.ToString()}
            };

            var data = param.Get($"/v1/candles/days", parameters, RestSharp.Method.Get);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<CandleDay>>(data);
            }
            else
            {
                return null;
            }
        }
        public List<CandleWeek> GetCandleWeeks(string market, DateTime to = default(DateTime), int count = 1)
        {
            // 시세 캔들 조회 - 주(Week) 캔들
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            parameters.Add("count", count.ToString());
            var data = param.Get("/v1/candles/weeks", parameters, RestSharp.Method.Get);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<CandleWeek>>(data);
            }
            else
            {
                return null;
            }

        }
        public List<CandleMonth> GetCandleMonths(string market, DateTime to = default(DateTime), int count = 1)
        {
            // 시세 캔들 조회 - 월(Month) 캔들
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("market", market);
            parameters.Add("to", to.ToString("yyyy-MM-dd HH:mm:ss"));
            parameters.Add("count", count.ToString());
            var data = param.Get("/v1/candles/months", parameters, RestSharp.Method.Get);
            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<CandleMonth>>(data);
            }
            else
            {
                return null;
            }
        }

        public List<Ticker> GetTicker(string markets)
        {
            // 시세 Ticker조회 - 현재가정보
            // market을 콤마로 구분하여 입력한다. 
            // ex) "KRW-BTC, KRW-ETH, ....."
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("markets", markets);
            var data = param.Get("/v1/ticker", parameters, RestSharp.Method.Get);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<Ticker>>(data);
            }
            else
            {
                return null;
            }


        }
        public List<OrderBook> GetOrderBook(string markets)
        {
            // 시세 호가 정보 조회 - 호가 정보 조회
            // market을 콤마로 구분하여 입력한다. 
            // ex) "KRW-BTC, KRW-ETH, ....."
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("markets", markets);
            var data = param.Get("/v1/orderbook", parameters, RestSharp.Method.Get);

            if (data != null)
            {
                return JsonConvert.DeserializeObject<List<OrderBook>>(data);
            }
            else
            {
                return null;
            }

        }

        


        public enum OrderSide
        {
            bid,    // 매수
            ask     // 매도
        }
        public enum CandleUnit
        {
            _1 = 1,
            _3 = 3,
            _5 = 5,
            _10 = 10,
            _15 = 15,
            _30 = 30,
            _60 = 60,
            _240 = 240
        }


    }



}
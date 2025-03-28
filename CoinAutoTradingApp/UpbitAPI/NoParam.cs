using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.UpbitAPI
{
    public class NoParam
    {

        private string upbitAccessKey;
        private string upbitSecretKey;

        private DateTime dt_1970_01_01;   //timestamp를 계산하기 위한 변수
        private const string baseUrl = "https://api.upbit.com";


        public NoParam(string upbitAccessKey, string upbitSecretKey)
        {
            //APIClass에서 받은 키를 입력
            this.upbitAccessKey = upbitAccessKey;
            this.upbitSecretKey = upbitSecretKey;

            this.dt_1970_01_01 = new DateTime(1970, 01, 01);
        }

        public string Get(string path, Method method)
        {
            try
            {
                var token = JWT_NoParam();

                var client = new RestClient(baseUrl);       // RestSharp 클라이언트 생성
                var request = new RestRequest(path, method);

                //request.AddOrUpdateHeader("Content-Type", "application/json");
                request.AddOrUpdateHeader("Authorization", $"Bearer {token}"); // "Bearer " 포함

                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    return response.Content;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"예외 발생: {ex.Message}");
                return null;
            }
        }


        public string JWT_NoParam()
        {
            TimeSpan diff = DateTime.UtcNow - dt_1970_01_01;
            var nonce = Convert.ToInt64(diff.TotalMilliseconds);

            var payload = new JwtPayload
            {
                { "access_key", this.upbitAccessKey },
                { "nonce", nonce }
            };

            byte[] keyBytes = Encoding.Default.GetBytes(this.upbitSecretKey);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256");

            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);

            return new JwtSecurityTokenHandler().WriteToken(secToken);
        }
    }
}

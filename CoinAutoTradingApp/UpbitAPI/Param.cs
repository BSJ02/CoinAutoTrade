using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.UpbitAPI
{
    public class Param
    {
        private string upbitAccessKey;
        private string upbitSecretKey;

        private DateTime dt_1970_01_01;   //timestamp를 계산하기 위한 변수
        private const string baseUrl = "https://api.upbit.com";


        public Param(string upbitAccessKey, string upbitSecretKey)
        {
            //APIClass에서 받은 키를 입력
            this.upbitAccessKey = upbitAccessKey;
            this.upbitSecretKey = upbitSecretKey;

            this.dt_1970_01_01 = new DateTime(1970, 1, 1);
        }

        public string Post(string path, Dictionary<string, string> parameters, Method method)
        {
            // POST, DELETE, UPDATE 방식을 포함하는듯?

            StringBuilder queryStringSb = GetQueryString(parameters);
            var tokenSb = JWT_param(queryStringSb.ToString()); // 입력받은 변수를 JWT토큰으로 변환
            var token = tokenSb.ToString();

            var client = new RestClient(baseUrl);
            var request = new RestRequest(path, method);

            // JsonConvert.SerializeObject(parameters);  // dictionary to Json
            request.AddJsonBody(JsonConvert.SerializeObject(parameters));  // add Json to body
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", token);

            var response = client.Execute(request);

            try
            {
                if (response.IsSuccessful)
                {
                    return response.Content;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

        }

        public string SendRequest(string path, Dictionary<string, string> parameters, Method method)
        {
            StringBuilder queryStringSb = GetQueryString(parameters);
            var token = JWT_param(queryStringSb.ToString()).ToString();

            var client = new RestClient(baseUrl);
            var request = new RestRequest($"{path}?{queryStringSb.ToString()}", method); // Query String 방식 적용

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", token);

            var response = client.Execute(request);

            return response.IsSuccessful ? response.Content : null;
        }

        public string Get(string path, Dictionary<string, string> parameters, Method method)
        {
            StringBuilder queryStringSb = GetQueryString(parameters);

            var token = JWT_param(queryStringSb.ToString()).ToString();

            string fullPath = path + "?" + queryStringSb.ToString();

            var client = new RestClient(baseUrl);
            var request = new RestRequest(fullPath, method);
            request.AddHeader("Authorization", token);

            var response = client.Execute(request);

            return response.IsSuccessful ? response.Content : null;
        }

        public StringBuilder GetQueryString(Dictionary<string, string> parameters)
        {
            // Dictionary 형태로 받은 key = value 형태를 
            // ?key1=value1&key2=value2 ... 형태로 만들어줌
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                builder.Append(pair.Key).Append("=").Append(pair.Value).Append("&");
            }

            if (builder.Length > 0)
            {
                builder.Length = builder.Length - 1; // 마지막 &를 제거하기 위함.
            }
            return builder;
        }
        public StringBuilder JWT_param(string queryString)
        {

            SHA512 sha512 = SHA512.Create();
            byte[] queryHashByteArray = sha512.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            string queryHash = BitConverter.ToString(queryHashByteArray).Replace("-", "").ToLower();

            TimeSpan diff = DateTime.Now - dt_1970_01_01;
            var nonce = Convert.ToInt64(diff.TotalMilliseconds);

            var payload = new JwtPayload
                    {
                        { "access_key", this.upbitAccessKey },
                        { "nonce", nonce  },
                        { "query_hash", queryHash },
                        { "query_hash_alg", "SHA512" }
                    };

            byte[] keyBytes = Encoding.Default.GetBytes(this.upbitSecretKey);
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, "HS256");
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(secToken);

            StringBuilder returnStr = new StringBuilder();
            returnStr.Append("Bearer "); // 띄어쓰기 한칸 있어야함 주의!
            returnStr.Append(jwtToken);

            return returnStr;
        }

    }
}
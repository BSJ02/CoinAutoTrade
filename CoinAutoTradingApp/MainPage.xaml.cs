using System.Net.Http;
using Newtonsoft.Json.Linq;
using CoinAutoTradingApp.UpbitAPI;
using System.Net;

namespace CoinAutoTradingApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnLoginClicked(object sender, EventArgs e)
        {
            var accessKey = AccessKey.Text;
            var secretKey = SecretKey.Text;

            APIClass api = new APIClass(accessKey, secretKey);
            var account = api.GetAccount();

            if (account != null)
            {
                var tradePage = new TradePage(api);
                Navigation.PushAsync(tradePage); // 화면 전환
            }
            else
            {
                ResultLabel.Text = "api not found";
            }
        }
    }
}

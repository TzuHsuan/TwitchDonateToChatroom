using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Threading.Tasks;
using TwitchDonateToChatroom.Models;

namespace TwitchDonateToChatroom.Service
{
    class ECpayCheckService : PaymentCheckService 
    {
        private readonly string apiEndpoint = "https://payment.ecpay.com.tw/Broadcaster/CheckDonate/";

        private readonly string paymentServiceName = "綠界";

        public ECpayCheckService(string paymentid, string channelName, string messageTemplate, TwitchIRCService sendMsg) : base(paymentid, channelName, messageTemplate, sendMsg)
        {
        }

        public override async Task Timer_ElapsedAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync(apiEndpoint + _id, null);

                if (response.IsSuccessStatusCode)
                {
                    var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    //string json = "[{'donateid':'10311031','name':'XXX','amount':100,'msg':'這是一筆贊助測試～'}]".Replace("'", "\"");
                    //var list = JsonDocument.Parse(json);

                    var donateList = list.RootElement;

                    if (donateList.GetArrayLength() > 0)
                    {

                        List<Member> donates = JsonSerializer.Deserialize<List<Member>>(donateList.ToString());

                        MessageBox.Show(donates[0].msg);

                        DonateProcess(donates);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}

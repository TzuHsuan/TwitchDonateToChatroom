using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using TwitchDonateToChatroom.Models;
using TwitchDonateToChatroom.Service.Interface;

namespace TwitchDonateToChatroom.Service
{
    public class PaymentCheckService : IOpayCheckService
    {


        #region Fields

        private readonly string apiEndpoint;

        private readonly string paymentServiceName;

        protected readonly HttpClient _httpClient = new HttpClient();

        private TwitchIRCService _irc;

        protected readonly string _id;

        private int _donatesFlag = 0;

        private readonly string _channelName;

        private readonly string _messageTemplate;

        #endregion

        #region Constructor

        public PaymentCheckService(string paymentid, string channelName, string messageTemplate, TwitchIRCService irc)
        {
            this._id = paymentid;

            this._channelName = channelName;

            this._messageTemplate = messageTemplate;

            _irc = irc;
        }

        #endregion

        #region Events

        public virtual async Task Timer_ElapsedAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync(apiEndpoint + _id, null);

                if (response.IsSuccessStatusCode)
                {
                    var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                    var donateList = list.RootElement.GetProperty("lstDonate");

                    if (donateList.EnumerateArray().Count() > 1)
                    {
                        List<Member> donates = JsonSerializer.Deserialize<List<Member>>(donateList.ToString());

                        DonateProcess(donates);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        #endregion

        #region Methods

        protected void DonateProcess(List<Member> lists)
        {
            foreach (var item in lists)
            {
                if (_donatesFlag < int.Parse(item.donateid))
                {
                    _irc.Send(_channelName, _messageTemplate.Replace("{name}", item.name).Replace("{amount}", item.amount.ToString()).Replace("{msg}", item.msg).Replace("{paysource}", paymentServiceName));
                    //Log(item);
                    _donatesFlag = int.Parse(item.donateid);

                    Thread.Sleep(500);
                }
            }
        }

        [Obsolete]
        private void Log(Member item)
        {
            StreamWriter sw = new StreamWriter(@"log.txt", true);
            string text = $"{DateTime.Now} {item.donateid} {item.name} {item.amount} {item.msg}";
            sw.WriteLine(text);
            sw.Flush();
            sw.Close();
        }

        #endregion
    }

}

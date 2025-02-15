﻿using System;
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
    public class OpayCheckService : PaymentCheckService
    {

        #region Fields

        private readonly string apiEndpoint = "https://payment.opay.tw/Broadcaster/CheckDonate/";

        private readonly string paymentServiceName = "歐付寶";

        #endregion

        #region Constructor

        public OpayCheckService(string opayid, string channelName, string messageTemplate, TwitchIRCService irc) : base(opayid, channelName, messageTemplate, irc)
        {
        }

        #endregion

        #region Events

        public override async Task Timer_ElapsedAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync(apiEndpoint + _id, null);

                if (response.IsSuccessStatusCode)
                {
                    var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                    var donateList = list.RootElement.GetProperty("lstDonate");

                    if (donateList.GetArrayLength() > 0)
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
    }
}

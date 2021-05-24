using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchDonateToChatroom.Models;
using TwitchDonateToChatroom.Service;
using TwitchDonateToChatroom.Service.Interface;
using TwitchDonateToChatroom.ViewModels;

namespace TwitchDonateToChatroom.Views
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataConfig data = DeserializeBinary();
            opayid.Text = data.OpayId;
            ecpayid.Text = data.ECpayId;
            channelname.Text = data.ChannelName;
            username.Text = data.UserName;
            twitchoauth.Text = data.TwitchOauth;
            messagetemplate.Text = data.MessageTemplate;
        }

        //public static string 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(opayid.Text) && string.IsNullOrWhiteSpace(ecpayid.Text))
            {
                MessageBox.Show("兩種付款服務ID請至少填一個");
                return;
            }
            else if (channelname.Text.Length < 1)
            {
                MessageBox.Show("頻道 ID 沒填!!");
                return;
            }
            else if (username.Text.Length < 1)
            {
                MessageBox.Show("Twitch ID 沒填!!");
                return;
            }
            else if (twitchoauth.Text.Length < 1)
            {
                MessageBox.Show("OAuth沒填!!");
                return;
            }

            try
            {
                TwitchIRCService irc = new TwitchIRCService(username.Text, twitchoauth.Text, channelname.Text);

                List<IOpayCheckService> paymentProviders = new List<IOpayCheckService>();

                if (!string.IsNullOrWhiteSpace(ecpayid.Text))
                {
                     IOpayCheckService ecpayCheckService = new ECpayCheckService(ecpayid.Text, channelname.Text, messagetemplate.Text, irc);

                    paymentProviders.Add(ecpayCheckService);
                }

                if (!string.IsNullOrWhiteSpace(opayid.Text))
                {
                    IOpayCheckService opayCheckService = new OpayCheckService(opayid.Text, channelname.Text, messagetemplate.Text, irc);

                    paymentProviders.Add(opayCheckService);
                }


                Task.Run(async () =>
                {
                    MessageBox.Show("start!");
                    while (true)
                    {

                        paymentProviders.ForEach(async provider =>
                        {
                            await provider.Timer_ElapsedAsync();
                        });

                        await Task.Delay(5000);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink source = sender as Hyperlink;
            System.Diagnostics.Process.Start("explorer", source.NavigateUri.AbsoluteUri);
            e.Handled = true;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                DataConfig data = new DataConfig()
                {
                    ChannelName = channelname.Text,
                    ECpayId = ecpayid.Text,
                    OpayId = opayid.Text,
                    UserName = username.Text,
                    TwitchOauth = twitchoauth.Text,
                    MessageTemplate = messagetemplate.Text
                };
                FileStream fs = new FileStream("Config.json", FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(fs, data);
                await fs.DisposeAsync();
                MessageBox.Show("存檔成功!");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }   
        }

        private DataConfig DeserializeBinary()
        {
            if (File.Exists("Config.json"))
            {
                string config = File.ReadAllText("Config.json");
                DataConfig data = JsonSerializer.Deserialize<DataConfig>(config);
                return data;
            }
            else
                return new DataConfig(); 
        }

        protected override void OnClosed(EventArgs e)
        {
            MainWindowVM vm = DataContext as MainWindowVM;

            vm.CTS.Cancel();

            base.OnClosed(e);
        }
    }
}

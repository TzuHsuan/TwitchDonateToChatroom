using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TwitchDonateToChatroom.Models;
using TwitchDonateToChatroom.Service;
using TwitchDonateToChatroom.Service.Interface;

namespace TwitchDonateToChatroom.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        #region Fields

        private IOpayCheckService _opayCheckService;

        private IOpayCheckService _ecpayCheckService;

        private IConfigService _configService;

        public TwitchIRCService irc;

        private string _opayId = null;

        private string _ecpayId = null;

        private string _channelName = null;

        private string _userName = null;

        private string _oauth = null;

        private string _messageTamplate = null;

        private string _status = null;

        private bool isStart = false;

        private ICommand _startCommand = null;

        private ICommand _saveCommand = null;

        #endregion

        #region Constructor

        public MainWindowVM()
        {
            _configService = ActivatorUtilities.CreateInstance<ConfigService>(App.ServiceProvider);
        }

        #endregion

        #region Properties

        public string OpayId
        {
            get => _opayId;
            set
            {
                    SetProperty(ref _opayId, value);
            }
        }

        public string ECpayId
        {
            get => _ecpayId;
            set
            {
                SetProperty(ref _ecpayId, value);
            }
        }

        public string ChannelName
        {
            get => _channelName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ShowMessageBox("頻道 ID 沒填!!");
                else
                    SetProperty(ref _channelName, value);
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ShowMessageBox("發言 ID 沒填!!");
                else
                    SetProperty(ref _userName, value);
            }
        }

        public string Oauth
        {
            get => _oauth;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ShowMessageBox("發言帳號 OAuth 沒填!!");
                else
                    SetProperty(ref _oauth, value);
            }
        }

        public string MessageTamplate
        {
            get => _messageTamplate;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ShowMessageBox("發言範本 沒填!!");
                else
                    SetProperty(ref _messageTamplate, value);
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                SetProperty(ref _status, value);
            }
        }

        public ICommand StartCommand
        {
            get
            {
                if (_startCommand == null)
                    _startCommand = new DelegateCommand(StartCapture, CanStartCapture);

                return _startCommand;
            }
        }



        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                    _saveCommand = new DelegateCommand(async (o) => await SaveAsync(), () => true);

                return _saveCommand;
            }
        }


        public CancellationTokenSource CTS { get; } = new CancellationTokenSource();

        #endregion

        private void ShowMessageBox(string text)
        {
            MessageBox.Show(text);
        }

        private void StartCapture(object sender)
        {
            if (string.IsNullOrWhiteSpace(_opayId) && string.IsNullOrWhiteSpace(ECpayId)){
                ShowMessageBox("兩種付款服務ID請至少填一個");
                return;
            }
            isStart = true;

            irc = new TwitchIRCService(_userName, _oauth, _channelName);

            List<IOpayCheckService> paymentProviders = new List<IOpayCheckService>();

            if (!string.IsNullOrWhiteSpace(_ecpayId))
            {
                _ecpayCheckService = ActivatorUtilities.CreateInstance<ECpayCheckService>(App.ServiceProvider,
                    _ecpayId,
                    _channelName,
                    _messageTamplate,
                    irc);

                paymentProviders.Add(_ecpayCheckService);
            }

            if (!string.IsNullOrWhiteSpace(_opayId))
            {
                _opayCheckService = ActivatorUtilities.CreateInstance<OpayCheckService>(App.ServiceProvider,
                    _opayId,
                    _channelName,
                    _messageTamplate,
                    irc);

                paymentProviders.Add(_opayCheckService);
            }


            Task.Run(async () =>
            {
                while (true)
                {
                    CTS.Token.ThrowIfCancellationRequested();

                    paymentProviders.ForEach(async provider =>
                    {
                        await provider.Timer_ElapsedAsync();
                    });

                    CTS.Token.ThrowIfCancellationRequested();

                    await Task.Delay(5000);
                }
            }, CTS.Token);

            Button button = sender as Button;

            button.IsEnabled = false;
        }

        private bool CanStartCapture()
        {
            return !isStart;
        }

        private async Task SaveAsync()
        {
            await _configService.SaveAsync(new DataConfig
            {
                ChannelName = _channelName,
                OpayId = _opayId,
                UserName = _userName,
                TwitchOauth = _oauth,
                MessageTemplate = _messageTamplate
            });
        }
    }
}

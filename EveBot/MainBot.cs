using System;
using System.ComponentModel;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace EveBot
{
    class MainBot
    {
        private BackgroundWorker BW;
        private ConfigJson config;        
        private TelegramBotClient Bot;

        public MainBot()
        {
            config = new ConfigJson();
            BW = new BackgroundWorker();
        }

        public void StartBot()
        {
            string key = config.TOKEN;
            Bot = new TelegramBotClient(key);
            BW.DoWork += BWBot;

            if (!String.IsNullOrEmpty(key) && !BW.IsBusy)
            {
                BW.RunWorkerAsync();

                Logger.Info("start bot " + config.NameBot);
            }           
        }

        public void EndBot()
        {
            Bot.StopReceiving();
        }

        async void BWBot(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;            

            await Bot.SetWebhookAsync("");

            Bot.OnUpdate += GettingUpdates;
            Bot.OnCallbackQuery += ProcessCallback;

            Bot.StartReceiving();
        }

        async void GettingUpdates(object su, UpdateEventArgs evu)
        {
            if (evu.Update.CallbackQuery != null || evu.Update.InlineQuery != null)
                return;

            var message = evu.Update.Message;

            await Bot.SendTextMessageAsync(message.Chat.Id, "aga");
        }

        async void ProcessCallback(object sc, CallbackQueryEventArgs ev)
        {
            var message = ev.CallbackQuery.Message;
            long chatId = message.Chat.Id;
            string callbackQueryData = ev.CallbackQuery.Data;

            try
            {
                try
                {
                    await Bot.EditMessageTextAsync(chatId, message.MessageId, message.Text,
                        parseMode: ParseMode.Html, replyMarkup: null);
                }
                catch (ApiRequestException ewq)
                {
                    Logger.Warn(ewq.Message);
                    await Bot.EditMessageTextAsync(chatId, message.MessageId, message.Text,
                        parseMode: ParseMode.Default, replyMarkup: null);
                }
            }
            catch (ApiRequestException ert)
            {
                Logger.Warn("Ошибка при редактировании кнопок:" + ert.Message);
            }
        }
    }
}
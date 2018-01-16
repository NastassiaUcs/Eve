using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EveBot
{
    class MainBot
    {
        private BackgroundWorker BW;
        private ConfigJson config;        
        private TelegramBotClient Bot;
        private DataBase dataBase;

        public const int TELEGRAM_ERROR_CODE_BAD_REQUEST = 400;
        public const int TELEGRAM_ERROR_CODE_BLOCKED = 403;

        public MainBot()
        {
            config = new ConfigJson();
            dataBase = new DataBase(config);
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

            Message message = evu.Update.Message;

            if (message != null)
            {
                dataBase.SaveINMessage(message);
            }

            string textMessage = message.Text;

            if (textMessage == "/start")
            {
                await FirstStart(message);
                return;
            }
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

        private async Task FirstStart(Message message)
        {   
            await SendMessage(message.Chat.Id, Texts.MSG_START);
        }        

        public async Task<Message> SendMessage(long chatID, string textMsg,
            ReplyMarkup replyMarkup = null, InlineKeyboardMarkup inlineKeyboard = null)
        {
            Message mmsg = null;

            try
            {
                if (inlineKeyboard == null)
                    mmsg = await Bot.SendTextMessageAsync(chatID, textMsg, parseMode: ParseMode.Html, replyMarkup: replyMarkup);
                else
                    mmsg = await Bot.SendTextMessageAsync(chatID, textMsg, parseMode: ParseMode.Html, replyMarkup: inlineKeyboard);
            }
            catch (ApiRequestException e)
            {
                if (e.ErrorCode == TELEGRAM_ERROR_CODE_BAD_REQUEST)
                {
                    textMsg = textMsg.Replace("<b>", "");
                    textMsg = textMsg.Replace("</b>", "");
                    if (inlineKeyboard == null)
                        mmsg = await Bot.SendTextMessageAsync(chatID, textMsg, parseMode: ParseMode.Default,
                            replyMarkup: replyMarkup);
                    else
                        mmsg = await Bot.SendTextMessageAsync(chatID, textMsg, parseMode: ParseMode.Default,
                            replyMarkup: inlineKeyboard);
                }
                else if (e.ErrorCode == TELEGRAM_ERROR_CODE_BLOCKED)
                {
                    Logger.Warn("чат " + chatID.ToString() + ": " + e.Message);                                      
                    //todo: update status in database
                }
            }

            if (mmsg != null)
            {
                dataBase.SaveOutMessage(mmsg);
            }

            return mmsg;
        }
    }
}
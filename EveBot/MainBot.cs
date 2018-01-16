using System;
using System.Collections;
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

        private Hashtable EveAction;

        public const int TELEGRAM_ERROR_CODE_BAD_REQUEST = 400;
        public const int TELEGRAM_ERROR_CODE_BLOCKED = 403;

        public MainBot()
        {
            EveAction = new Hashtable();
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
                await ChoiceActivity(message);
            }            
        }

        async void ProcessCallback(object sc, CallbackQueryEventArgs ev)
        {
            var message = ev.CallbackQuery.Message;
            message.From = ev.CallbackQuery.From;
            message.Text = ev.CallbackQuery.Data;

            long chatId = message.Chat.Id;            

            try
            {
                await EditMessage(chatId, message.MessageId, message.Text);             
            }
            catch (ApiRequestException ert)
            {
                Logger.Warn("Ошибка при редактировании кнопок:" + ert.Message);
            }

            if (message != null)
            {
                dataBase.SaveINMessage(message);
                await ChoiceActivity(message);
            }
        }

        private async Task ChoiceActivity(Message message)
        {
            string textMessage = message.Text;

            if (textMessage == "/start")
            {
                await FirstStart(message);
                return;
            }
            else if (textMessage == "что-то другое, вынесем в константы")
            {
                //что-то делаем
            }
        }

        private async Task FirstStart(Message message)
        {
            var chatId = message.Chat.Id;

            await SendMessage(chatId, Texts.MSG_START);
            if (!EveAction.Contains(chatId))
            {
                EveAction.Add(chatId, new ChatActivity());
            }
        }        

        public async Task EditMessage(long chatId, int messageId, string text)
        {
            try
            {
                await Bot.EditMessageTextAsync(chatId, messageId, text,
                    parseMode: ParseMode.Html, replyMarkup: null);
            }
            catch (ApiRequestException ewq)
            {
                Logger.Warn(ewq.Message);
                text = DelTags(text);
                await Bot.EditMessageTextAsync(chatId, messageId, text,
                    parseMode: ParseMode.Default, replyMarkup: null);
            }          
        }

        private string DelTags(string text)
        {
            text = text.Replace("<b>", "");
            text = text.Replace("</b>", "");
            return text;
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
                    textMsg = DelTags(textMsg);

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
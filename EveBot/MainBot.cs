using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

namespace EveBot
{
    public class MainBot
    {
        private BackgroundWorker BW;
        private ConfigJson config;        
        private TelegramBotClient Bot;
        private DataBase dataBase;

        private Dictionary<long, ChatActivity> EveAction;

        public const int TELEGRAM_ERROR_CODE_BAD_REQUEST = 400;
        public const int TELEGRAM_ERROR_CODE_BLOCKED = 403;

        public MainBot()
        {
            EveAction = new Dictionary<long, ChatActivity>();
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
            Bot.OnInlineQuery += ProcessInline;

            Bot.StartReceiving();
        }

        async void ProcessInline(object si, InlineQueryEventArgs ei)
        {
            var query = ei.InlineQuery.Query;

            InlineQueryResult[] results = {
                new InlineQueryResultArticle
                {
                    Id = "1",
                    Title = "Eve:",
                    Description = "Сейчас ответ на любой запрос - ага",
                    InputMessageContent = new InputTextMessageContent
                    {
                        DisableWebPagePreview = true,
                        MessageText = "ага",
                        ParseMode = ParseMode.Default,
                    }
                },
                new InlineQueryResultPhoto
                {
                    Id = "2",
                    Url = "https://guitarcity.by/image/data/logo_bf.png",
                    ThumbUrl = "https://guitarcity.by/image/data/logo_bf.png",
                    Caption = "Текст под фоткой",
                    Description = "Описание",
                }
            };

            await AnswerInline(ei.InlineQuery.Id, results);
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
                Logger.Warn("Ошибка при редактировании кнопок: " + ert.Message);
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

            if (textMessage == Texts.COMMAND_START)
            {
                await FirstStart(message);
                return;
            }
            else if (textMessage == Texts.COMMAND_CANCEL)
            {
                // почистить не пустые объекты типа, удалить активность
            }
            else 
            {
                ChatActivity chatActivity = EveAction[message.Chat.Id];
                if (chatActivity != null)
                    if (chatActivity.command == null)
                    {
                        chatActivity.command = BaseCommand.Test(textMessage);
                    }
                    else
                    {
                        string text = await chatActivity.command.TestMsg(message);
                        await SendMessage(message.Chat.Id, text);
                    }
            }
        }

        private async Task FirstStart(Message message)
        {
            var chatId = message.Chat.Id;

            await SendMessage(chatId, Texts.MSG_START);
            if (!EveAction.ContainsKey(chatId))
            {
                EveAction.Add(chatId, new ChatActivity());
                // если юзер есть в базе, то селект, иначе вставить юзера
            }
            else
            {
                // селект последнюю активность из базы тогда
            }
        }        

        public async Task AnswerInline(string InlineQueryId, InlineQueryResult[] results)
        {
            try
            {
                await Bot.AnswerInlineQueryAsync(InlineQueryId, results);
            }
            catch (ApiRequestException e)
            {
                Logger.Error("ошибка при отправке ответа на инлайн-запрос: " + e.Message);
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
                text = Texts.DelTags(text);
                await Bot.EditMessageTextAsync(chatId, messageId, text,
                    parseMode: ParseMode.Default, replyMarkup: null);
            }          
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
                    textMsg = Texts.DelTags(textMsg);

                    if (inlineKeyboard == null)
                        mmsg = await Bot.SendTextMessageAsync(chatID, textMsg, parseMode: ParseMode.Default,
                            replyMarkup: replyMarkup);
                    else
                        mmsg = await Bot.SendTextMessageAsync(chatID, textMsg, parseMode: ParseMode.Default,
                            replyMarkup: inlineKeyboard);
                }
                else if (e.ErrorCode == TELEGRAM_ERROR_CODE_BLOCKED)
                {
                    Logger.Warn("чат " + chatID + ": " + e.Message);                                      
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
﻿using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace EveBot
{
    public class InsertCommand : BaseCommand
    {
        public async Task<string> TestMsg(Message message)
        {
            return "ага в инсерт";
        }
    }
}
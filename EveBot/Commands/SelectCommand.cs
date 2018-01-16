using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace EveBot
{
    public class SelectCommand: BaseCommand
    {
        public override async Task<string> TestMsg(Message message)
        {
            return "ага в select";
        }
    }
}
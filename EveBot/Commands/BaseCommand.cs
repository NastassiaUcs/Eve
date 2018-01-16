using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace EveBot
{
    public class BaseCommand
    {
        public static BaseCommand Test(string test)
        {
            BaseCommand command = null;
            if (test == Texts.COMMAND_INSERT)
            {
                command = new InsertCommand();
            }
            else if (test == Texts.COMMAND_SELECT)
            {
                command = new SelectCommand();
            }
            return command;
        }

        public virtual async Task<string> TestMsg(Message message)
        {
            return "ага в base";
        }
    }
}
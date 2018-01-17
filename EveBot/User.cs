using System;
using Telegram.Bot.Types;

namespace EveBot
{
    public class User
    {
        public int id;
        public int telegramId;
        public string firstName;
        public string lastName;
        public string userName;
        public string passwordWeb;
        public string nameForBot;

        public string Name
        {
            get
            {
                string name = System.String.IsNullOrEmpty(lastName) ? firstName : firstName + " " + lastName;
                return name;
            }
        }

        public string NameWithUserName
        {
            get
            {
                return Name + (userName != null && userName.Length > 0 ? " @" + userName : "");
            }
        }

        public string NameWithTelegramId
        {
            get
            {
                return NameWithUserName + " " + telegramId;
            }
        }

        //public static string GetUserName(Message message)
        //{
        //    string userName = String.IsNullOrEmpty(message.From.LastName) ? message.From.FirstName + " " : 
        //        message.From.FirstName + " " + message.From.LastName;
        //    return userName;
        //}

        public static User CreateNewUserFrom(Telegram.Bot.Types.User userTg)
        {
            User u = new User
            {                
                firstName = userTg.FirstName,
                lastName = userTg.LastName,
                userName = userTg.Username,
                telegramId = userTg.Id
            };

            return u;
        }
    }
}
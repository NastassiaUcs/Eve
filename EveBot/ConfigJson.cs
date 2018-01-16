using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace EveBot
{
    public class ConfigJson
    {
        private const string PATH_LOG = "path_log";
        private const string NAME_DELETION_DELAY = "deletion_delay";
        private const string NAME_TOKEN = "bot_token";
        private const string NAME_ID_BOT = "bot_id";
        private const string NAME_ADMIN = "admins";
        private const string NAME_BOT = "bot_username";
        private const string TEST = "test";
        private const string BASE_NAME = "db_name";
        private const string BASE_USER = "db_user";
        private const string BASE_PASSWORD = "db_pass";
        private const string DATA_SOURCE = "db_host";        

        private Hashtable settings;

        #region getset
       
        public string PathLog
        {
            get
            {
                return (string)(settings[PATH_LOG] ?? @".\log");
            }
            private set
            {
                settings[PATH_LOG] = value;
            }
        }       

        public string DbName
        {
            get => (string)settings[BASE_NAME];
            private set => settings[BASE_NAME] = value;
        }

        public string DbUser
        {
            get => (string)settings[BASE_USER];
            private set => settings[BASE_USER] = value;
        }

        public string DbPass
        {
            get => (string)settings[BASE_PASSWORD];
            private set => settings[BASE_PASSWORD] = value;
        }

        public string DataSource
        {
            get => (string)settings[DATA_SOURCE];
            private set => settings[DATA_SOURCE] = value;
        }

        public bool Test
        {
            get => (bool)settings[TEST];
            private set => settings[TEST] = value;
        }

        public string NameBot
        {
            get
            {
                return settings[NAME_BOT].ToString();
            }
            private set
            {
                settings[NAME_BOT] = value;
            }
        }

        public int DeletionDelay
        {
            get
            {
                return Convert.ToInt32(settings[NAME_DELETION_DELAY]);
            }
            private set
            {
                settings[NAME_DELETION_DELAY] = value;
            }
        }

        public string TOKEN
        {
            get
            {
                return settings[NAME_TOKEN].ToString();
            }
            private set
            {
                settings[NAME_TOKEN] = value;
            }
        }

        public int IDBOT
        {
            get
            {
                return Convert.ToInt32(settings[NAME_ID_BOT]);
            }
            private set
            {
                settings[NAME_ID_BOT] = value;
            }
        }

        public List<string> Admins
        {
            get
            {
                List<string> tolist = settings[NAME_ADMIN].ToString().Split(';').ToList<string>();
                return tolist;
            }
            private set
            {
                settings[NAME_ADMIN] = value;
            }
        }
        #endregion getset

        public ConfigJson()
        {
            var filename = @"./settings.json";
            Logger.Info("get settings from " + filename);
            using (StreamReader r = new StreamReader(filename))
            {
                string json = r.ReadToEnd();
                settings = JsonConvert.DeserializeObject<Hashtable>(json);
            }

            if (!Directory.Exists(PathLog))
            {
                try
                {
                    Directory.CreateDirectory(PathLog);
                    Logger.Info(@"create new folder: " + PathLog);
                }
                catch (Exception exp)
                {
                    Logger.Error(exp.Message);
                }
            }

            FieldInfo[] fieldsInfo = typeof(ConfigJson).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var fieldInfo in fieldsInfo)
            {
                if (fieldInfo.IsLiteral)
                {
                    var name = fieldInfo.GetValue(null).ToString();
                    if (!settings.ContainsKey(name))
                    {
                        Logger.Warn("Не найден настроечный параметр " + name);
                    }
                    else
                    {
                        Logger.Debug(name + "=" + settings[name]);
                    }
                }
            }
        }
    }
}
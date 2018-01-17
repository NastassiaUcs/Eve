using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using System.Threading;
using Telegram.Bot.Types;

namespace EveBot
{
    class DataBase
    {
        private SemaphoreSlim _semaphoreSlim;

        private MySqlConnection connection;
        private ConfigJson config;

        public DataBase(ConfigJson config)
        {
            this.config = config;
            connection = new MySqlConnection(
                string.Format(
                    "Database={0};Data Source={1};User Id={2};Password={3}",
                    config.DbName, config.DataSource, config.DbUser, config.DbPass
                )
            );            
            _semaphoreSlim = new SemaphoreSlim(1);
        }

        private long Query(string sql, Hashtable parameters, Action<MySqlDataReader> processor)
        {
            _semaphoreSlim.Wait();
            long result = 0;
            Logger.Info("query db\n" + sql);
            try
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(sql, connection);

                if (parameters != null)
                {
                    var parametersName = parameters.Keys;
                    foreach (var name in parametersName)
                    {
                        Type t = typeof(int);
                        if (parameters[name] != null)
                            t = parameters[name].GetType();

                        if (t.Equals(typeof(string)))
                        {
                            byte[] UTF8bytes = UTF8Encoding.UTF8.GetBytes(parameters[name].ToString());
                            cmd.Parameters.AddWithValue(name.ToString(), UTF8bytes);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue(name.ToString(), parameters[name]);
                        }
                    }
                }

                if (processor == null)
                {
                    cmd.ExecuteNonQuery();
                    result = cmd.LastInsertedId;
                }
                else
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        processor(reader);
                        //warning: not tested, be careful to use this value
                        result = reader.RecordsAffected;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                result = -1;
            }
            finally
            {
                connection.Close();
            }
            _semaphoreSlim.Release();
            return result;
        }

        public void SaveOutMessage(Message message)
        {
            Logger.Info("insert OUT message in base");           
            
            Query("insert into out_messages (message) select @message;", 
                new Hashtable
                {
                    { "@message", GetJsonMessage(message) }
                }, 
                null);
        }

        public void SaveINMessage(Message message)
        {
            Logger.Info("insert IN message in base");

            Query("insert into in_messages (message) select @message;",
                new Hashtable
                {
                    { "@message", GetJsonMessage(message) }
                },
                null);
        }

        public static string GetJsonMessage(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }
    }
}
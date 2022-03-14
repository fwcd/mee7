﻿using Discord;
using Discord.WebSocket;
using MEE7.Backend.HelperFunctions;
using Newtonsoft.Json;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MEE7.Configuration
{
    public static class Config
    {
        static readonly object lockject = new object();
        static readonly string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar;
        static readonly string configPath = exePath + "config.json";
        static readonly string configBackupPath = exePath + "config_backup.json";
        static readonly ulong DiscordConfigChannelID = Program.logChannel;
        static readonly string DiscordConfigMessage = "autosave";
        public static bool UnsavedChanges = false;
        public static ConfigData Data
        {
            get
            {
                lock (lockject)
                {
                    UnsavedChanges = true;
                    return data;
                }
            }
            set
            {
                UnsavedChanges = true;
                data = value;
            }
        }
        private static ConfigData data = new ConfigData();

        static Config()
        {
            if (Config.Exists())
                Config.Load();
            else
                Config.Data = new ConfigData();
        }

        public static string GetConfigPath()
        {
            return configPath;
        }
        public static bool Exists()
        {
            return File.Exists(configPath);
        }
        public static void Save()
        {
            lock (lockject)
            {
                if (File.Exists(configPath))
                    File.Copy(configPath, configBackupPath, true);
                File.WriteAllText(configPath, JsonConvert.SerializeObject(Data, Formatting.Indented));

                if (Program.ClientReady && File.Exists(configPath) && data.ServerList.Count > 0)
                    DiscordNETWrapper.SendFile(configPath, (IMessageChannel)Program.GetChannelFromID(DiscordConfigChannelID), DiscordConfigMessage).Wait();

                UnsavedChanges = false;
            }
        }
        public static void Load()
        {
            lock (lockject)
            {
                string url = "", discordConfig = "";
                try
                {
                    var tmp_channel = Program.GetChannelFromID(DiscordConfigChannelID);
                    if (tmp_channel == null)
                        return;

                    url = ((IMessageChannel)tmp_channel).GetMessagesAsync().FlattenAsync().Result.
                        First(x => x.Content.StartsWith(DiscordConfigMessage) && x.Attachments.Count > 0 && x.Attachments.First().Filename == "config.json").Attachments.First().Url;
                    using (var wc = new System.Net.WebClient())
                        discordConfig = wc.DownloadString(url);
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(discordConfig))
                    Data = JsonConvert.DeserializeObject<ConfigData>(discordConfig);
                else if (Exists())
                    Data = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configPath));
                else
                    Data = new ConfigData();
            }
        }
        public static void LoadFrom(string JSON)
        {
            lock (lockject)
            {
                Data = JsonConvert.DeserializeObject<ConfigData>(JSON);
            }
        }
        public static new string ToString()
        {
            string output = "";

            FieldInfo[] Infos = typeof(ConfigData).GetFields();
            foreach (FieldInfo info in Infos)
            {
                output += "\n" + info.Name + ": ";

                if (info.FieldType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(info.FieldType))
                {
                    output += "\n";
                    IEnumerable a = (IEnumerable)info.GetValue(Data);
                    IEnumerator e = a.GetEnumerator();
                    e.Reset();
                    while (e.MoveNext())
                    {
                        output += e.Current;
                        if (e.Current.GetType() == typeof(ulong))
                        {
                            try
                            {
                                ISocketMessageChannel Channel = (ISocketMessageChannel)Program.GetChannelFromID((ulong)e.Current);
                                output += " - Name: " + Channel.Name + " - Server: " + ((SocketGuildChannel)Channel).Guild.Name + "\n";
                            }
                            catch { output += "\n"; }
                        }
                        else
                            output += "\n";
                    }
                }
                else
                {
                    output += info.GetValue(Data) + "\n";
                }
            }

            return output;
        }
    }
}

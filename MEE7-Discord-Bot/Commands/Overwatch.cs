﻿using Discord.WebSocket;
using MEE7.Backend;
using MEE7.Backend.HelperFunctions;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MEE7.Commands
{
    public class Overwatch : Command
    {
        public Overwatch() : base("overwatch", "Prints todays overwatch arcade game modes", isExperimental: false, isHidden: false)
        {

        }

        public override void Execute(SocketMessage message)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var gameMode = GetGamemodeJson("https://overwatcharcade.today/api/overwatch/arcademodes".GetHTMLfromURL());
            var today = GetTodayJson("https://overwatcharcade.today/api/overwatch/today".GetHTMLfromURL());
            DiscordNETWrapper.SendText($"Updated at {today.CreatedAt.UtcDateTime.ToLongDateString()}, " +
                $"{today.CreatedAt.UtcDateTime.ToShortTimeString()} by {today.User.Battletag}", message.Channel).Wait();
            foreach (Tile t in today.Modes.Tiles())
                DiscordNETWrapper.SendEmbed(DiscordNETWrapper.CreateEmbedBuilder(t.Name, t.Label == null ? t.Players : $"{t.Players} - {t.Label}",
                    "", null, gameMode.FirstOrDefault(x => x.Id == t.Id)?.Image), message.Channel).Wait();
        }

        static TodayJsonRoot GetTodayJson(string json) => JsonConvert.DeserializeObject<TodayJsonRoot>(json);
        static GamemodeJsonRoot[] GetGamemodeJson(string json) => JsonConvert.DeserializeObject<GamemodeJsonRoot[]>(json);

        // Today API JSON Structure - generated by https://app.quicktype.io/#l=cs
        public partial class TodayJsonRoot
        {
            [JsonProperty("created_at")]
            public DateTimeOffset CreatedAt { get; set; }

            [JsonProperty("is_today")]
            public bool IsToday { get; set; }

            [JsonProperty("user")]
            public User User { get; set; }

            [JsonProperty("modes")]
            public Modes Modes { get; set; }
        }
        public partial class Modes
        {
            [JsonProperty("tile_1")]
            public Tile Tile1 { get; set; }

            [JsonProperty("tile_2")]
            public Tile Tile2 { get; set; }

            [JsonProperty("tile_3")]
            public Tile Tile3 { get; set; }

            [JsonProperty("tile_4")]
            public Tile Tile4 { get; set; }

            [JsonProperty("tile_5")]
            public Tile Tile5 { get; set; }

            [JsonProperty("tile_6")]
            public Tile Tile6 { get; set; }

            [JsonProperty("tile_7")]
            public Tile Tile7 { get; set; }

            public Tile[] Tiles() => new Tile[] { Tile1, Tile2, Tile3, Tile4, Tile5, Tile6, Tile7 };
        }
        public partial class Tile
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("image")]
            public Uri Image { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("players")]
            public string Players { get; set; }

            [JsonProperty("label")]
            public string Label { get; set; }
        }
        public partial class User
        {
            [JsonProperty("battletag")]
            public string Battletag { get; set; }

            [JsonProperty("avatar")]
            public Uri Avatar { get; set; }
        }

        // Gamemode API JSON Structure - generated by https://app.quicktype.io/#l=cs
        class GamemodeJsonRoot
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("players")]
            public string Players { get; set; }

            [JsonProperty("image")]
            public string Image { get; set; }
        }
    }
}

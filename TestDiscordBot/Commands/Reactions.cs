﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiscordBot.Commands
{
    public class Reactions : Command
    {
        Reaction[] reactions;

        public Reactions() : base("reaction", "Post reactions", false)
        {
            reactions = new Reaction[]
            {
                // TODO: Add new reactions here
                new Reaction("You sure", "Send this if some people are too sure about themselves.", async (message) => {
                    await Global.SendText("https://cdn.discordapp.com/attachments/473991188974927884/510947142127452172/sure2smol.gif", message.Channel); }),
                new Reaction("wat", "Send this if wat.", async (message) => {
                    await Global.SendText("https://cdn.discordapp.com/attachments/473991188974927884/510954862616379403/Screenshot_9223371422766423856_opera.png", message.Channel); }),
                new Reaction("You cant scare me", "Send this if you cant be scared.", async (message) => {
                    await Global.SendText("https://youtu.be/ug2aoVZYgaU?t=157", message.Channel); }),
                new Reaction("Who are you", "Send this if someone wants to know who you are.", async (message) => {
                    await Global.SendText("https://youtu.be/ug2aoVZYgaU?t=246", message.Channel); }),
                new Reaction("I dont believe it", "Send this if you dont believe something.", async (message) => {
                    await Global.SendText("https://youtu.be/ug2aoVZYgaU?t=460", message.Channel); }),
                new Reaction("I didnt say it would be easy", "Send this if something isnt easy.", async (message) => {
                    await Global.SendText("https://youtu.be/ug2aoVZYgaU?t=491", message.Channel); }),
                new Reaction("we got em R2", "Send this if you got em.", async (message) => {
                    await Global.SendText("https://youtu.be/GQhKBn42XcU?t=24", message.Channel); }),
                new Reaction("I came in like a wrecking ball", "Send this if you came in like a wrecking ball.", async (message) => {
                    await Global.SendText("https://youtu.be/My2FRPA3Gf8?t=93", message.Channel); })
            };
        }

        public override async Task execute(SocketMessage message) 
        {
            string[] split = message.Content.Split(' '); 
            if (split.Length == 1)
            {
                EmbedBuilder Embed = new EmbedBuilder(); 
                Embed.WithColor(0, 128, 255);
                foreach (Reaction react in reactions)
                {
                    Embed.AddField(prefix + command + " " + react.name, react.desc);
                }
                Embed.WithDescription("Reactions:");
                await Global.SendEmbed(Embed, message.Channel);
            }
            else
            {
                string command = split.Skip(1).Aggregate((x, y) => x + " " + y).ToLower();
                int[] scores = new int[reactions.Length];
                for (int i = 0; i < reactions.Length; i++)
                {
                    scores[i] = Global.LevenshteinDistance(command, reactions[i].name.ToLower());
                }
                int index = Array.IndexOf(scores, scores.Min());
                reactions[index].execute(message);
            }
        }

        class Reaction
        {
            public delegate void Send(SocketMessage message);
            public string name, desc;
            public Send execute;

            public Reaction(string name, string desc, Send execute)
            {
                this.name = name;
                this.desc = desc;
                this.execute = execute;
            }
        }
    }
}
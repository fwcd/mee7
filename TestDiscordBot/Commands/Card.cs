﻿using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiscordBot.Commands
{
    public class Card : Command
    {
        public Card() : base("card", "Posts a random yugioh card.", false)
        {

        }

        public override async Task execute(SocketMessage commandmessage)
        {
            string[] Files = Directory.GetFiles(@"D:\Eigene Dateien\Medien\Bilder\Reactions\Yugioh card memes");
            List<string> SendableFiles = new List<string>();
            foreach (string s in Files)
            {
                if (Path.GetExtension(s) == ".jpg" || Path.GetExtension(s) == ".png" || Path.GetExtension(s) == ".jpeg" ||
                    Path.GetExtension(s) == ".gif" || Path.GetExtension(s) == ".mp4")
                    SendableFiles.Add(s);
            }
            string filepath = SendableFiles[Global.RDM.Next(SendableFiles.Count)];
            await Global.SendFile(filepath, commandmessage.Channel);
        }
    }
}

﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using XnaGeometry;
using System.IO;
using Color = System.Drawing.Color;

namespace MEE7.Commands
{
    public partial class Edit : Command
    {
        public Edit() : base("edit", "Edit stuff using various functions")
        {
            Commands = InputCommands.Union(TextCommands.Union(PictureCommands));

            HelpMenu = new EmbedBuilder();
            HelpMenu.WithDescription("Operators:\n" +
                "> Concatinates functions\n" +
                "() Let you add additional arguments for the command (optional)\n" +
               $"\neg. {PrefixAndCommand} thisT(omegaLUL) > swedish > Aestheticify\n" +
                "\nEdit Commands:");
            AddToHelpmenu("Input Commands", InputCommands);
            AddToHelpmenu("Text Commands", TextCommands);
            AddToHelpmenu("Picture Commands", PictureCommands);
        }
        void AddToHelpmenu(string Name, EditCommand[] editCommands)
        {
            string CommandToCommandTypeString(EditCommand c) => $"**{c.Command}**: " +
                //  $"`{(c.ExpectedInputType == null ? "_" : c.ExpectedInputType.ToReadableString())}` -> " +
                //  $"`{c.Function(default, "", c.ExpectedInputType.GetDefault()).GetType().ToReadableString()}`" +
                $"";
            int maxlength = editCommands.
                Select(CommandToCommandTypeString).
                Select(x => x.Length).
                Max();
            HelpMenu.AddFieldDirectly(Name, "" + editCommands.
                Select(c => CommandToCommandTypeString(c) +
                $"{new string(Enumerable.Repeat(' ', maxlength - c.Command.Length - 1).ToArray())}{c.Desc}\n").
                Combine() + "");
        }

        public override void Execute(SocketMessage message)
        {
            if (message.Content.Length <= PrefixAndCommand.Length + 1)
                Program.SendEmbed(HelpMenu, message.Channel).Wait();
            else
                PrintResult(RunCommands(message), message);
        }
        object RunCommands(SocketMessage message)
        {
            string input = message.Content.Remove(0, PrefixAndCommand.Length + 1);
            IEnumerable<string> commands = input.Split('|').First().Split('>').Select(x => x.Trim(' '));
            object currentData = null;

            if (commands.Count() > 50)
            {
                Program.SendText($"That's too many commands for one message.", message.Channel).Wait();
                return null;
            }

            foreach (string c in commands)
            {
                string cwoargs = new string(c.TakeWhile(x => x != '(').ToArray());
                string args = c.GetEverythingBetween("(", ")");

                EditCommand command = Commands.FirstOrDefault(x => x.Command.ToLower() == cwoargs.ToLower());
                if (command == null)
                {
                    Program.SendText($"I don't know a command called {cwoargs}", message.Channel).Wait();
                    return null;
                }

                if (command.InputType != null && (currentData == null || currentData.GetType() != command.InputType))
                {
                    Program.SendText($"Wrong Data Type Error in {c}\nExpected: {command.InputType}\nGot: {currentData.GetType()}", message.Channel).Wait();
                    return null;
                }

                try
                {
                    currentData = command.Function(message, args, currentData);
                }
                catch (Exception e)
                {
                    Program.SendText($"[{c}] {e.Message}",
                        message.Channel).Wait();
                    return null;
                }
            }

            return currentData;
        }
        void PrintResult(object currentData, SocketMessage message)
        {
            if (currentData is EmbedBuilder)
                Program.SendEmbed(currentData as EmbedBuilder, message.Channel).Wait();
            else if (currentData is Tuple<string, EmbedBuilder>)
            {
                var t = currentData as Tuple<string, EmbedBuilder>;
                Program.SendEmbed(t.Item2, message.Channel).Wait();
                Program.SendText(t.Item1, message.Channel).Wait();
            }
            else if (currentData is Bitmap)
                Program.SendBitmap(currentData as Bitmap, message.Channel).Wait();
            else if (currentData == null)
#pragma warning disable CS0642 // Its supposed to be like this
                ;
#pragma warning restore CS0642
            else
                Program.SendText(currentData.ToString(), message.Channel).Wait();
        }

        class EditCommand
        {
            public string Command, Desc;
            public Type InputType, OutputType;
            public Func<SocketMessage, string, object, object> Function;

            public EditCommand(string Command, string Desc, Func<SocketMessage, string, object, object> Function, Type InputType = null, Type OutputType = null)
            {
                if (Command.ContainsOneOf(new string[] { "|", ">", "<", "." }))
                    throw new IllegalCommandException("Illegal Symbol!");

                this.Command = Command;
                this.Desc = Desc;
                this.Function = Function;
                this.InputType = InputType;
                this.OutputType = OutputType;
            }
        }
        readonly IEnumerable<EditCommand> Commands;
    }
}
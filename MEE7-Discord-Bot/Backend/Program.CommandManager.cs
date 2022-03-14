﻿using Discord;
using Discord.WebSocket;
using MEE7.Backend;
using MEE7.Backend.HelperFunctions;
using MEE7.Commands;
using MEE7.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MEE7
{
    public static partial class Program
    {
#if DEBUG
        public const string Prefix = "°";
#else
        public const string Prefix = "$";
#endif

        static Command[] commands;
        static readonly EmbedBuilder helpMenu = new EmbedBuilder();
        static readonly Type[] commandTypes = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                               from assemblyType in domainAssembly.GetTypes()
                                               where assemblyType.IsSubclassOf(typeof(Command))
                                               select assemblyType).ToArray();

        static int concurrentCommandExecutions = 0;
        static readonly string commandExecutionLock = "";
        static readonly ulong[] experimentalChannels = new ulong[] { 473991188974927884 };
        static readonly List<DiscordUser> usersWithRunningCommands = new List<DiscordUser>();

        public delegate void NonCommandMessageRecievedHandler(IMessage message);
        public static event NonCommandMessageRecievedHandler OnNonCommandMessageRecieved;
        public delegate void EmojiReactionAddedHandler(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3);
        public static event EmojiReactionAddedHandler OnEmojiReactionAdded;
        public delegate void EmojiReactionRemovedHandler(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3);
        public static event EmojiReactionRemovedHandler OnEmojiReactionRemoved;
        public delegate void EmojiReactionUpdatedHandler(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3);
        public static event EmojiReactionUpdatedHandler OnEmojiReactionUpdated;
        public delegate void UserJoinedHandler(SocketGuildUser arg);
        public static event UserJoinedHandler OnUserJoined;
        public delegate void ChannelCreatedHandler(SocketChannel arg);
        public static event ChannelCreatedHandler OnChannelCreated;
        public delegate void ChannelDestroyedHandler(SocketChannel arg);
        public static event ChannelDestroyedHandler OnChannelDestroyed;
        public delegate void ChannelUpdatedHandler(SocketChannel arg1, SocketChannel arg2);
        public static event ChannelUpdatedHandler OnChannelUpdated;
        public delegate void CurrentUserUpdatedHandler(SocketSelfUser arg1, SocketSelfUser arg2);
        public static event CurrentUserUpdatedHandler OnCurrentUserUpdated;
        public delegate void GuildAvailableHandler(SocketGuild arg);
        public static event GuildAvailableHandler OnGuildAvailable;
        public delegate void GuildMembersDownloadedHandler(SocketGuild arg);
        public static event GuildMembersDownloadedHandler OnGuildMembersDownloaded;
        public delegate void GuildMemberUpdatedHandler(SocketGuildUser arg1, SocketGuildUser arg2);
        public static event GuildMemberUpdatedHandler OnGuildMemberUpdated;
        public delegate void GuildUnavailableHandler(SocketGuild arg);
        public static event GuildUnavailableHandler OnGuildUnavailable;
        public delegate void GuildUpdatedHandler(SocketGuild arg1, SocketGuild arg2);
        public static event GuildUpdatedHandler OnGuildUpdated;
        public delegate void LatencyUpdatedHandler(int arg1, int arg2);
        public static event LatencyUpdatedHandler OnLatencyUpdated;
        public delegate void LeftGuildHandler(SocketGuild arg);
        public static event LeftGuildHandler OnLeftGuild;
        public delegate void LoggedInHandler();
        public static event LoggedInHandler OnLoggedIn;
        public delegate void LoggedOutHandler();
        public static event LoggedOutHandler OnLoggedOut;
        public delegate void MessageDeletedHandler(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2);
        public static event MessageDeletedHandler OnMessageDeleted;
        public delegate void MessagesBulkDeletedHandler(IReadOnlyCollection<Cacheable<IMessage, ulong>> arg1, Cacheable<IMessageChannel, ulong> arg2);
        public static event MessagesBulkDeletedHandler OnMessagesBulkDeleted;
        public delegate void MessageUpdatedHandler(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, Cacheable<IMessageChannel, ulong> arg3);
        public static event MessageUpdatedHandler OnMessageUpdated;
        public delegate void ReactionsClearedHandler(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2);
        public static event ReactionsClearedHandler OnReactionsCleared;
        public delegate void RecipientAddedHandler(SocketGroupUser arg1);
        public static event RecipientAddedHandler OnRecipientAdded;
        public delegate void RecipientRemovedHandler(SocketGroupUser arg1);
        public static event RecipientRemovedHandler OnRecipientRemoved;
        public delegate void RoleCreatedHandler(SocketRole arg1);
        public static event RoleCreatedHandler OnRoleCreated;
        public delegate void RoleDeletedHandler(SocketRole arg1);
        public static event RoleDeletedHandler OnRoleDeleted;
        public delegate void RoleUpdatedHandler(SocketRole arg1, SocketRole arg2);
        public static event RoleUpdatedHandler OnRoleUpdated;
        public delegate void UserBannedHandler(SocketUser arg1, SocketGuild arg2);
        public static event UserBannedHandler OnUserBanned;
        public delegate void UserIsTypingHandler(SocketUser arg1, ISocketMessageChannel arg2);
        public static event UserIsTypingHandler OnUserIsTyping;
        public delegate void UserLeftHandler(SocketGuildUser arg);
        public static event UserLeftHandler OnUserLeft;
        public delegate void UserUnbannedHandler(SocketUser arg, SocketGuild arg2);
        public static event UserUnbannedHandler OnUserUnbanned;
        public delegate void UserUpdatedHandler(SocketUser arg1, SocketUser arg2);
        public static event UserUpdatedHandler OnUserUpdated;
        public delegate void UserVoiceStateUpdatedHandler(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3);
        public static event UserVoiceStateUpdatedHandler OnUserVoiceStateUpdated;
        public delegate void VoiceServerUpdatedHandler(SocketVoiceServer arg1);
        public static event VoiceServerUpdatedHandler OnVoiceServerUpdated;

        static readonly List<Tuple<IUserMessage, Exception>> cachedErrorMessages = new List<Tuple<IUserMessage, Exception>>();
        static readonly string errorMessage = "Uwu We made a fucky wucky!! A wittle fucko boingo! " +
            "The code monkeys at our headquarters are working VEWY HAWD to fix this!";
        static readonly Emoji errorEmoji = new Emoji("🤔");

        static void UpdateWorkState()
        {
            if (concurrentCommandExecutions > 1)
                Program.SetStatus(UserStatus.DoNotDisturb);
            else if (concurrentCommandExecutions > 0)
                Program.SetStatus(UserStatus.AFK);
            else
                Program.SetStatus(UserStatus.Online);
        }
        public static void DisposeErrorMessages()
        {
            foreach (Tuple<IUserMessage, Exception> err in cachedErrorMessages)
            {
                err.Item1.RemoveAllReactionsAsync().Wait();
                err.Item1.ModifyAsync(m => m.Content = errorMessage).Wait();
            }
        }

        public static Command GetCommandInstance(string CommandName)
        {
            return commands.FirstOrDefault(x => x.CommandLine.ToLower() == CommandName.ToLower());
        }

        // Events
        private static async Task Client_JoinedGuild(SocketGuild arg)
        {
            try
            {
                bool hasWrite = false, hasRead = false, hasReadHistory = false, hasFiles = false;
                SocketGuild g = Program.GetGuildFromID(479950092938248193);
                IUser u = g.Users.FirstOrDefault(x => x.Id == Program.GetSelf().Id);
                if (u != null)
                {
                    IEnumerable<IRole> roles = (u as IGuildUser).RoleIds.Select(x => (u as IGuildUser).Guild.GetRole(x));
                    foreach (IRole r in roles)
                    {
                        if (r.Permissions.SendMessages)
                            hasWrite = true;
                        if (r.Permissions.ViewChannel)
                            hasRead = true;
                        if (r.Permissions.ReadMessageHistory)
                            hasReadHistory = true;
                        if (r.Permissions.AttachFiles)
                            hasFiles = true;
                    }
                }

                if (!hasWrite)
                {
                    IDMChannel c = g.Owner.CreateDMChannelAsync().Result;
                    await c.SendMessageAsync("How can one be on your server and not have the right to write messages!? This is outrageous, its unfair!");
                    return;
                }

                if (!hasRead || !hasReadHistory || !hasFiles)
                {
                    await g.TextChannels.ElementAt(0).SendMessageAsync("Whoever added me didn't give me all the usual permissions :c");
                    return;
                }
            }
            catch (Exception e) { ConsoleWrapper.WriteLine(e.ToString(), ConsoleColor.Red); }
        }
        private static Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            Task.Run(async () =>
            {
                Tuple<IUserMessage, Exception> error = cachedErrorMessages.FirstOrDefault(x => x.Item1.Id == arg1.Id);
                if (error != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    if (arg3.User.GetValueOrDefault().Id == Master.Id)
                        await error.Item1.ModifyAsync(m => m.Content = errorMessage + "\n\n```" + error.Item2 + "```");
                }
            });
            if (arg3.UserId != OwnID)
                Task.Run(() =>
                {
                    try
                    {
                        OnEmojiReactionAdded?.InvokeParallel(arg1, arg2, arg3);
                        OnEmojiReactionUpdated?.InvokeParallel(arg1, arg2, arg3);
                    }
                    catch (Exception e) { ConsoleWrapper.WriteLine(e.ToString(), ConsoleColor.Red); }
                });

            return Task.FromResult(default(object));
        }
        private static Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            Task.Run(() =>
            {
                Tuple<IUserMessage, Exception> error = cachedErrorMessages.FirstOrDefault(x => x.Item1.Id == arg1.Id);
                if (error != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    if (arg3.User.GetValueOrDefault().Id == Master.Id)
                        error.Item1.ModifyAsync(m => m.Content = errorMessage).Wait();
                }
            });
            if (arg3.UserId != OwnID)
                Task.Run(() =>
                {
                    try
                    {
                        OnEmojiReactionRemoved?.InvokeParallel(arg1, arg2, arg3);
                        OnEmojiReactionUpdated?.InvokeParallel(arg1, arg2, arg3);
                    }
                    catch (Exception e) { ConsoleWrapper.WriteLine(e.ToString(), ConsoleColor.Red); }
                });

            return Task.FromResult(default(object));
        }
        private static Task MessageReceived(IMessage message)
        {
            if (message.Channel.Id == logChannel)
                Task.Run(() =>
                {
                    if (message.Content.StartsWith(Program.logStartupMessagePräfix) &&
                        message.Content != Program.logStartupMessage)
                        Program.Exit(0);
                });
            if (!message.Author.IsBot && message.Content.StartsWith(Prefix))
                Task.Run(() => ParallelMessageReceived(message));
            if (message.Content.Length > 0 && (char.IsLetter(message.Content[0]) || message.Content[0] == '<' || message.Content[0] == ':'))
                Task.Run(() =>
                {
                    try { OnNonCommandMessageRecieved.InvokeParallel(message); }
                    catch (Exception e) { ConsoleWrapper.WriteLine(e.ToString(), ConsoleColor.Red); }
                });
            return Task.FromResult(default(object));
        }
        private static void ParallelMessageReceived(IMessage message)
        {
            if (message.Channel is SocketGuildChannel)
                Saver.SaveServer(message.GetServerID());
            DiscordUser user = Saver.SaveUser(message.Author.Id);
            user.TotalCommandsUsed++;

            if (message.Content.StartsWith(Prefix + "help"))
            {
                string[] split = message.Content.Split(' ');
                if (split.Length < 2)
                    DiscordNETWrapper.SendEmbed(helpMenu, message.Channel).Wait();
                else
                {
                    foreach (Command c in commands)
                        if (c.CommandLine == split[1])
                        {
                            DiscordNETWrapper.SendEmbed(c.HelpMenu, message.Channel).Wait();
                            return;
                        }
                    DiscordNETWrapper.SendText("That command doesn't implement a HelpMenu", message.Channel).Wait();
                }
            }
            else
            {
                if (Program.RunningOnCI || user.UserID == Program.Master.Id || !usersWithRunningCommands.Contains(user))
                {
                    usersWithRunningCommands.Add(user);

                    // Find command
                    string[] split = message.Content.Split(new char[] { ' ', '\n' });
                    Command called = commands.FirstOrDefault(x => (x.Prefix + x.CommandLine).ToLower() == split[0].ToLower());
                    if (called != null)
                    {
                        ExecuteCommand(called, message);
                    }
                    else
                    {
                        // No command found
                        float[] distances = new float[commands.Length];
                        for (int i = 0; i < commands.Length; i++)
                            if (commands[i].CommandLine != "" && !commands[i].IsHidden)
                                distances[i] = StringExtensions.ModifiedLevenshteinDistance((commands[i].Prefix + commands[i].CommandLine).ToLower(), split[0].ToLower());
                            else
                                distances[i] = int.MaxValue;
                        int minIndex = 0;
                        float min = float.MaxValue;
                        for (int i = 0; i < commands.Length; i++)
                            if (distances[i] < min)
                            {
                                minIndex = i;
                                min = distances[i];
                            }
                        if (min < Math.Min(4, split[0].Length - 1))
                        {
                            if (commands[minIndex].CommandLine != "9ball")
                                DiscordNETWrapper.SendText("I don't know that command, but " + commands[minIndex].Prefix + commands[minIndex].CommandLine + 
                                    " is pretty close:", message.Channel).Wait();
                            ExecuteCommand(commands[minIndex], message);
                        }
                    }

                    usersWithRunningCommands.RemoveAll(x => x.UserID == user.UserID);
                }
                else
                    DiscordNETWrapper.SendText("You are already executing a command, wait for the current one to finish.", message.Channel).Wait();
            }
        }
        private static void ExecuteCommand(Command command, IMessage message)
        {
            if (command.GetType() == typeof(Template) && !experimentalChannels.Contains(message.Channel.Id))
                return;
            if (command.IsExperimental && !experimentalChannels.Contains(message.Channel.Id))
            {
                DiscordNETWrapper.SendText("Experimental commands cant be used here!", message.Channel).Wait();
                return;
            }

            IDisposable typingState = null;
            try
            {
                typingState = message.Channel.EnterTypingState();
                lock (commandExecutionLock)
                {
                    concurrentCommandExecutions++;
                    UpdateWorkState();
                }

                Saver.SaveUser(message.Author.Id);
                command.Execute(message);

                if (message.Channel is SocketGuildChannel)
                    ConsoleWrapper.WriteLineAndDiscordLog($"{DateTime.Now.ToLongTimeString()} Send {command.GetType().Name}\tin " +
                        $"{((SocketGuildChannel)message.Channel).Guild.Name} \tin {message.Channel.Name} \tfor {message.Author.Username}", ConsoleColor.Green);
                else
                    ConsoleWrapper.WriteLineAndDiscordLog($"{DateTime.Now.ToLongTimeString()} Send {command.GetType().Name}\tin " +
                       $"DMs \tfor {message.Author.Username}", ConsoleColor.Green);
            }
            catch (Exception e)
            {
                try // Try in case I dont have the permissions to write at all
                {
                    IUserMessage m = message.Channel.SendMessageAsync(errorMessage).Result;

                    m.AddReactionAsync(errorEmoji).Wait();
                    cachedErrorMessages.Add(new Tuple<IUserMessage, Exception>(m, e));
                }
                catch { }

                ConsoleWrapper.WriteLineAndDiscordLog($"{DateTime.Now.ToLongTimeString()} [{command.GetType().Name}] {e.Message}\n  " +
                    $"{e.StackTrace.Split('\n').FirstOrDefault(x => x.Contains(":line "))?.Split(Path.DirectorySeparatorChar).Last().Replace(":", ", ")}", ConsoleColor.Red);
                Saver.SaveToLog(e.ToString());
            }
            finally
            {
                typingState.Dispose();
                lock (commandExecutionLock)
                {
                    concurrentCommandExecutions--;
                    UpdateWorkState();
                }
            }
        }
    }
}

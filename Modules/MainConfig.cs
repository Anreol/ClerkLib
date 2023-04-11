using ClerkLib.FileReader;
using Discord.Net;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Diagnostics;
using System;

namespace ClerkLib.Modules
{
    public class MainConfig : IBotModule, IDisposable
    {
        public MainBotConfig BotConfig => fileReader.Data;
        private IJsonFileReader<MainBotConfig> fileReader;
        public DiscordSocketClient discordSocketClient => _discordClient;
        private DiscordSocketClient _discordClient;

        internal static Dictionary<SocketApplicationCommand, Func<SocketSlashCommand, Task>> socketApplicationCommands = new Dictionary<SocketApplicationCommand, Func<SocketSlashCommand, Task>>();
        private bool disposed;

        public MainConfig(JsonFileReader<MainBotConfig> jsonFileReader, DiscordSocketClient socketClient)
        {
            if (jsonFileReader == null || jsonFileReader.Data.Equals(default))
            {
                throw new ArgumentException("The JSON data could not be loaded or failed to load.");
            }
            fileReader = jsonFileReader;
            _discordClient = socketClient;
        }

        public void Subscribe()
        {
            _discordClient.Ready += Ready;
            _discordClient.SlashCommandExecuted += SlashCommandExecuted;
        }

        private async Task Ready()
        {
            await CreateGlobalCommands(_discordClient);
            foreach (var id in fileReader.Data.GuildIds)
            {
                await CreateCommandInGuild(_discordClient, id);
            }
        }

        public void Unsubscribe()
        {
            _discordClient.SlashCommandExecuted -= SlashCommandExecuted;
        }
        public static async Task CreateGlobalCommands(DiscordSocketClient client)
        {
            // Global commands available everywhere
            SlashCommandBuilder pingCommand = new SlashCommandBuilder()
            {
                Name = "hello",
                Description = "Returns ping latency"
            };

            try
            {
                //Create global commands
                socketApplicationCommands.Add(await client.CreateGlobalApplicationCommandAsync(pingCommand.Build()), async (command) =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    await command.RespondAsync("Pong!");
                    stopwatch.Stop();
                    await command.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = $"Ping time: {stopwatch.ElapsedMilliseconds}ms";
                    });
                });
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
        public static async Task CreateCommandInGuild(DiscordSocketClient client, ulong guildId)
        {
            SocketGuild guild = client.GetGuild(guildId);

            // Guild-Specific Command
            SlashCommandBuilder sendMessageAsBotCommand = new SlashCommandBuilder()
            {
                Name = "send-message",
                Description = "Sends a message to a channel through the bot",
                
            };

            try
            {
                //Create the guild commands
                if (guild != null)
                {
                    socketApplicationCommands.Add(await guild.CreateApplicationCommandAsync(sendMessageAsBotCommand.Build()), async (command) =>
                    {
                        await command.RespondAsync("guild command1!!11111111!");
                    }); ;
                }
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private static async Task SlashCommandExecuted(SocketSlashCommand command)
        {
            var action = socketApplicationCommands.FirstOrDefault(socCommand => socCommand.Key.Id == command.CommandId).Value;
            if (action != null)
            {
                await action.Invoke(command);
            }
        }
        void IDisposable.Dispose()
        {
            if (!disposed)
            {
                Unsubscribe();
                socketApplicationCommands = null;
                _discordClient = null;
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public class MainBotConfig
        {
            public string ApiToken { get; set; }
            public List<ulong> GuildIds { get; set; }
        }
    }
}
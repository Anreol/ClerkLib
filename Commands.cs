using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ClerkLib
{
    internal class Commands
    {
        internal static Dictionary<SocketApplicationCommand, Func<SocketSlashCommand, Task>> socketApplicationCommands = new Dictionary<SocketApplicationCommand, Func<SocketSlashCommand, Task>>();

        public static async Task CreateCommandInGuild(DiscordSocketClient client, ulong guildId)
        {
            SocketGuild guild = client.GetGuild(guildId);

            // Guild-Specific Command
            SlashCommandBuilder guildCommand = new SlashCommandBuilder()
            {
                Name = "first-command",
                Description = "Test",
            };

            // Global commands available everywhere
            SlashCommandBuilder pingCommand = new SlashCommandBuilder()
            {
                Name = "hello",
                Description = "Returns ping latency"
            };

            try
            {
                //Create the guild commands
                if (guild != null)
                {
                    await guild.CreateApplicationCommandAsync(guildCommand.Build());
                }

                //Create global commands
                socketApplicationCommands.Add(await client.CreateGlobalApplicationCommandAsync(pingCommand.Build()), async (SocketSlashCommand command) =>
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

            //Subscribe to process commands
            client.SlashCommandExecuted += SlashCommandExecuted;
        }

        private static async Task SlashCommandExecuted(SocketSlashCommand command)
        {
            var action = socketApplicationCommands.FirstOrDefault(socCommand => socCommand.Key.Id == command.CommandId).Value;
            if (action != null)
            {
                await action.Invoke(command);
            }
        }
    }
}
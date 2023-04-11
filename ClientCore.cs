using ClerkLib.FileReader;
using ClerkLib.Modules;
using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using static ClerkLib.Modules.MainConfig;
using static ClerkLib.Modules.MessageCommand;
using static ClerkLib.Modules.ReactionWatcher;

namespace ClerkLib
{
    public class ClientCore
    {
        public DiscordSocketClient Client { get => _client; }

        private DiscordSocketClient _client;

        public MainConfig MainConfig
        {
            get
            {
                return (MainConfig)runningModules.FirstOrDefault(module => module is MainConfig);
            }
        }
        private List<IBotModule> runningModules = new List<IBotModule>();
        public ClientCore()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                // enable the message intent
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
                MessageCacheSize = 5
            }
            );

            string mainBotConfig = Path.Combine(Environment.CurrentDirectory, "MainBotConfig.json");
            string reactionWatcherPath = Path.Combine(Environment.CurrentDirectory, "ReactionWatcherConfig.json");
            string messageCommandConfig = Path.Combine(Environment.CurrentDirectory, "TextCommandConfig.json");
            if (File.Exists(mainBotConfig))
            {
                runningModules.Add(new MainConfig(new JsonFileReader<MainBotConfig>(mainBotConfig), _client));
            }
            if (File.Exists(reactionWatcherPath))
            {
                runningModules.Add(new ReactionWatcher(new JsonFileReader<ReactionWatch>(reactionWatcherPath), _client));
            }
            if (File.Exists(messageCommandConfig))
            {
                runningModules.Add(new MessageCommand(new JsonFileReader<List<TextCommand>>(messageCommandConfig), _client));
            }

            foreach (IBotModule botModule in runningModules)
            {
                botModule.Subscribe();
            }

            _client.Log += LogAsync;

            _client.Ready += ReadyAsync;

        }

        public async Task RunAsync(string token)
        {
            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine("Bot is ready!");

            return Task.CompletedTask;
        }
    }
}
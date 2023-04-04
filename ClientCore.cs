using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using static ClerkLib.Configuration;

namespace ClerkLib
{
    public class ClientCore
    {
        public Configuration Configuration { get => _config; }
        public DiscordSocketClient Client { get => _client; }

        private DiscordSocketClient _client;
        private Configuration _config;
        private TextCommands _textCommandHandler;
        public ClientCore()
        {
            _config = new ClerkLib.Configuration();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                // enable the message intent
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
                MessageCacheSize = 5
            }
            );

            _client.Log += LogAsync;

            _client.Ready += ReadyAsync;

            _client.MessageReceived += MessageReceivedAsync;

            _client.ReactionAdded += ReactionAdded;

            if (_config.textCommandConfig != null)
            {
                _textCommandHandler = new TextCommands(this);
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            // Get the guild object from the channel object
            IGuild guild = (await channel.GetOrDownloadAsync() as IGuildChannel).Guild;
            if (guild == null)
            {
                // The channel is not a guild channel (e.g. it's a DM), so we can't get the guild object
                return;
            }

            foreach (var observableChannel in _config.botConfig.ObservableChannels)
            {
                if (channel.Id == observableChannel.ChannelId)
                {
                    foreach (var emojiWatch in observableChannel.EmojiWatches)
                    {
                        foreach (var emojiName in emojiWatch.EmojiNames)
                        {
                            if (reaction.Emote.Name == emojiName)
                            {
                                // Get the message that was reacted to
                                var message = await cachedMessage.GetOrDownloadAsync();

                                // Get the count of reactions with this emoji
                                var reactionCount = message.Reactions[reaction.Emote].ReactionCount;

                                //Get author
                                IGuildUser user = await guild.GetUserAsync(message.Author.Id);

                                await message.Channel.SendMessageAsync($"Message {message.Id} has {reactionCount} reactions with {reaction.Emote.Name} emoji");
                                if (user != null && reactionCount >= emojiWatch.DefaultTriggerTreshold)
                                {
                                    string textToSend = emojiWatch.DoRoleRemovalFirst ? "Reached trigger threshold of {0}, will now attempt to REMOVE roles and then add roles." : "Reached trigger threshold of {0}, will now attempt to ADD roles and then remove roles.";
                                    await message.Channel.SendMessageAsync(text: string.Format(textToSend, emojiWatch.DefaultTriggerTreshold.ToString()));

                                    if (emojiWatch.DoRoleRemovalFirst && emojiWatch.RolesToRemove != null && emojiWatch.RolesToRemove.Count > 0)
                                    {
                                        await user.RemoveRolesAsync(emojiWatch.RolesToRemove);
                                    }
                                    if (emojiWatch.RolesToAdd != null && emojiWatch.RolesToAdd.Count > 0)
                                    {
                                        await user.AddRolesAsync(emojiWatch.RolesToAdd);
                                    }
                                    if (!emojiWatch.DoRoleRemovalFirst && emojiWatch.RolesToRemove != null && emojiWatch.RolesToRemove.Count > 0)
                                    {
                                        await user.RemoveRolesAsync(emojiWatch.RolesToRemove);
                                    }
                                }
                            }
                        }
                    }
                }
            }
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
            Commands.CreateCommandInGuild(_client, _config.botAuth.GuildId);
            Console.WriteLine("Bot is ready!");

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // Check if the message was sent by a user (not a bot)
            if (!message.Author.IsBot)
            {

            }

            //Ping function.
            if (message.MentionedUsers.Any(x => x.Id == _client.CurrentUser.Id))
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var reply = await message.Channel.SendMessageAsync("Pong!");
                stopwatch.Stop();
                await reply.ModifyAsync(x =>
                {
                    x.Content = $"Ping time: {stopwatch.ElapsedMilliseconds}ms";
                });
            }
        }
    }
}
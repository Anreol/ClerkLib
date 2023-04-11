using ClerkLib.FileReader;
using Discord;
using Discord.WebSocket;
using System.Text.Json.Serialization;

namespace ClerkLib.Modules
{
    internal class ReactionWatcher : IBotModule, IDisposable
    {
        private IJsonFileReader<ReactionWatch> fileReader;
        public DiscordSocketClient discordSocketClient => _discordClient;
        private DiscordSocketClient _discordClient;

        private bool disposed;

        public ReactionWatcher(JsonFileReader<ReactionWatch> jsonFileReader, DiscordSocketClient socketClient)
        {
            if (jsonFileReader == null || jsonFileReader.Data.Equals(default))
            {
                throw new ArgumentException("The JSON data could not be loaded or failed to load.");
            }
            fileReader = jsonFileReader;
            _discordClient = socketClient;

            // Go through every observable channel
            foreach (var channel in fileReader.Data.ObservableChannels)
            {
                // Create a new list to hold the matching emoji watches
                var matchingEmojiWatches = new List<EmojiWatch>();

                // Go through every emoji watch identifier in the observable channel
                foreach (var identifier in channel.EmojiWatchIdentifiers)
                {
                    // Find the matching emoji watch in the configuration's emoji watches list
                    matchingEmojiWatches.Add(fileReader.Data.EmojiWatches.FirstOrDefault(x => x.NameIdentifier == identifier));
                }

                // Set the list of matching emoji watches to the observable channel's emoji watches list
                channel.EmojiWatches = matchingEmojiWatches;
            }
        }

        public void Subscribe()
        {
            _discordClient.ReactionAdded += ReactionAdded;
        }

        public void Unsubscribe()
        {
            _discordClient.ReactionAdded -= ReactionAdded;
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

            foreach (var observableChannel in fileReader.Data.ObservableChannels)
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

        void IDisposable.Dispose()
        {
            if (!disposed)
            {
                Unsubscribe();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public class ReactionWatch
        {
            public List<ObservableChannel> ObservableChannels { get; set; }
            public List<EmojiWatch> EmojiWatches { get; set; }
        }

        public struct EmojiWatch
        {
            public string NameIdentifier { get; set; }
            public int DefaultTriggerTreshold { get; set; }
            public bool DoRoleRemovalFirst { get; set; }
            public List<string> EmojiNames { get; set; }
            public List<ulong> RolesToAdd { get; set; }
            public List<ulong> RolesToRemove { get; set; }
        }

        public class ObservableChannel
        {
            public ulong ChannelId { get; set; }
            public List<string> EmojiWatchIdentifiers { get; set; }

            [JsonIgnore]
            public List<EmojiWatch> EmojiWatches { get; internal set; }
        }
    }
}
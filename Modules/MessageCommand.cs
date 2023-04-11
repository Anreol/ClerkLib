using ClerkLib.FileReader;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace ClerkLib.Modules
{
    internal class MessageCommand : IBotModule, IDisposable
    {
        private IJsonFileReader<List<TextCommand>> fileReader;
        private bool disposed;

        public DiscordSocketClient discordSocketClient => _discordClient;
        private DiscordSocketClient _discordClient;

        public MessageCommand(JsonFileReader<List<TextCommand>> jsonFileReader, DiscordSocketClient socketClient)
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
            _discordClient.MessageReceived += OnMessageReceived;
        }

        public void Unsubscribe()
        {
            _discordClient.MessageReceived -= OnMessageReceived;
        }

        private async Task OnMessageReceived(SocketMessage message)
        {
            // Get the guild object from the channel object
            IGuild guild = (message.Channel as SocketTextChannel).Guild;
            if (guild == null)
            {
                // The channel is not a guild channel (e.g. it's a DM), so we can't get the guild object
                return;
            }

            foreach (var textCommand in fileReader.Data)
            {
                // Check if the message is a reply to another message
                if (message.Content.StartsWith(textCommand.ExecutingName) && message.Reference != null && message.Reference.MessageId.IsSpecified)
                {
                    // Retrieve the original message that was replied to
                    var originalMessage = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);

                    //Make sure both author and original message author exist
                    IGuildUser guildUserExecuting = await guild.GetUserAsync(message.Author.Id);
                    IGuildUser guildUserRepliedTo = await guild.GetUserAsync(originalMessage.Author.Id);
                    if (guildUserExecuting == null || guildUserRepliedTo == null)
                    {
                        return;
                    }

                    //Make sure that the user executing the command has the role needed for it
                    if (!guildUserExecuting.RoleIds.Any(role => textCommand.ExecutingRoles.Contains(role)))
                    {
                        return;
                    }

                    //First remove roles if applies
                    if (textCommand.RemovingRoles != null && textCommand.RemovingRoles.Count > 0)
                    {
                        await guildUserRepliedTo.RemoveRolesAsync(textCommand.RemovingRoles);
                    }
                    //Then add roles if applies
                    if (textCommand.ApplyingRoles != null && textCommand.ApplyingRoles.Count > 0)
                    {
                        await guildUserRepliedTo.AddRolesAsync(textCommand.ApplyingRoles);
                    }

                    IGuildChannel targetChannel = await guild.GetChannelAsync(textCommand.NotificationChannelId);

                    //Get message description 
                    string notificationMessageDescriptionBase = textCommand.NotificationDescriptions != null ? textCommand.NotificationDescriptions[Random.Shared.Next(0, textCommand.NotificationDescriptions.Count + 1)] : "Description Text";
                    string notificationMessageDescriptionFormatted = string.Format(notificationMessageDescriptionBase, originalMessage.Author.Id, targetChannel.Name);

                    string notificationMessageTitleBase = textCommand.NotificationTitles != null ? textCommand.NotificationTitles[Random.Shared.Next(0, textCommand.NotificationTitles.Count + 1)] : "Title Text";
                    string notificationMessageTitleFormatted = string.Format(notificationMessageDescriptionBase, originalMessage.Author.Id, targetChannel.Name);

                    await message.Channel.SendMessageAsync(notificationMessageDescriptionFormatted);

                    EmbedBuilder embedBuilder = new EmbedBuilder()
                    {
                        Title = notificationMessageTitleBase,
                        Description = notificationMessageDescriptionFormatted,
                        Color = Color.Red,
                        Timestamp = DateTime.UtcNow
                    };
                    if (textCommand.AppendOriginalMessageIfReply)
                    {
                        embedBuilder.AddField("Original message: ", originalMessage.Content);
                    }
                    await ((SocketTextChannel)targetChannel).SendMessageAsync(embed: embedBuilder.Build());
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
        public struct TextCommandWrapper
        {

        }
        public struct TextCommand

        {
            [JsonProperty("executingName")]
            public string ExecutingName { get; set; }

            [JsonProperty("appendOriginalMessageIfReply")]
            public bool AppendOriginalMessageIfReply { get; set; }

            [JsonProperty("executingRoles")]
            public List<ulong> ExecutingRoles { get; set; }

            [JsonProperty("applyingRoles")]
            public List<ulong> ApplyingRoles { get; set; }

            [JsonProperty("removingRoles")]
            public List<ulong> RemovingRoles { get; set; }

            [JsonProperty("notificationChannelId")]
            public ulong NotificationChannelId { get; set; }

            [JsonProperty("notificationTitles")]
            public List<string> NotificationTitles { get; set; }

            [JsonProperty("notificationDescriptions")]
            public List<string> NotificationDescriptions { get; set; }
        }
    }
}
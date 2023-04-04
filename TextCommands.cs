using Discord;
using Discord.WebSocket;

namespace ClerkLib
{
    internal class TextCommands
    {
        private ClientCore _clientcore;

        public TextCommands(ClientCore client)
        {
            _clientcore = client;
            _clientcore.Client.MessageReceived += OnMessageReceived;
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

            foreach (var textCommand in _clientcore.Configuration.textCommandConfig.TextCommands)
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
    }
}
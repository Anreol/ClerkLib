using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClerkLib
{
    internal class TextCommands
    {
        public static void DoStuff(DiscordSocketClient client)
        {
            client.MessageReceived += OnMessageReceived;
        }

        private static async Task OnMessageReceived(SocketMessage message)
        {
            // Check if the message is a reply to another message
            if (message.Reference != null && message.Reference.MessageId.IsSpecified)
            {
                // Retrieve the original message that was replied to
                var originalMessage = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);

                // Handle the original message and the received message
                Console.WriteLine($"User replied to message \"{originalMessage.Content}\" with message \"{message.Content}\"");
            }
            else
            {
                // Handle the received message as normal
                if (message.Content.Equals("hello"))
                {
                    await message.Channel.SendMessageAsync("Hello!");
                }
            }
        }
    }
}

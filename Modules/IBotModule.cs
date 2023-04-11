using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClerkLib.Modules
{
    public interface IBotModule
    {
        DiscordSocketClient discordSocketClient { get; }
        void Subscribe();
        void Unsubscribe();
    }
}

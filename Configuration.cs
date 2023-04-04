using Newtonsoft.Json;

namespace ClerkLib
{
    public class Configuration
    {
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

        public struct BotConfig
        {
            public List<ObservableChannel> ObservableChannels { get; set; }
            public List<EmojiWatch> EmojiWatches { get; set; }
        }

        public struct BotAuth
        {
            public string ApiToken { get; set; }
            public ulong GuildId { get; set; }
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

        public class TextCommandConfig
        {
            [JsonProperty("textCommands")]
            public List<TextCommand> TextCommands { get; set; }
        }

        public BotAuth botAuth { get; set; }
        public TextCommandConfig textCommandConfig { get; set; }
        public BotConfig botConfig { get; set; }

        public Configuration()
        {
            //Read files...
            string botAuthPath = Path.Combine(Environment.CurrentDirectory, "BotAuth.json");
            string botConfigPath = Path.Combine(Environment.CurrentDirectory, "BotConfig.json");
            string textCommandConfig = Path.Combine(Environment.CurrentDirectory, "TextCommandConfig.json");

            //Make sure the basic exists
            if (!File.Exists(botAuthPath))
            {
                throw new FileNotFoundException("Couldn't find file location of auth file: {0}", botAuthPath);
            }
            Console.WriteLine("Reading Auth info from {0} with configuration file from: {1} along with text command module: {2}", botAuthPath, botConfigPath, textCommandConfig);
            string botAuthAsString = File.ReadAllText(botAuthPath);
            botAuth = JsonConvert.DeserializeObject<BotAuth>(botAuthAsString);

            //Additional stuff
            if (File.Exists(botConfigPath))
            {
                string botConfigAsString = File.ReadAllText(botConfigPath);
                botConfig = JsonConvert.DeserializeObject<BotConfig>(botConfigAsString);
            }
            if (File.Exists(textCommandConfig))
            {
                string banishAsConfig = File.ReadAllText(textCommandConfig);
                this.textCommandConfig = JsonConvert.DeserializeObject<TextCommandConfig>(banishAsConfig);
            }

            // Go through every observable channel
            foreach (var channel in botConfig.ObservableChannels)
            {
                // Create a new list to hold the matching emoji watches
                var matchingEmojiWatches = new List<EmojiWatch>();

                // Go through every emoji watch identifier in the observable channel
                foreach (var identifier in channel.EmojiWatchIdentifiers)
                {
                    // Find the matching emoji watch in the configuration's emoji watches list
                    var matchingEmojiWatch = botConfig.EmojiWatches.FirstOrDefault(x => x.NameIdentifier == identifier);

                    // If a matching emoji watch is found, add it to the list of matching emoji watches
                    //if (matchingEmojiWatch != null)
                    {
                        matchingEmojiWatches.Add(matchingEmojiWatch);
                    }
                }

                // Set the list of matching emoji watches to the observable channel's emoji watches list
                channel.EmojiWatches = matchingEmojiWatches;
            }
        }
    }
}
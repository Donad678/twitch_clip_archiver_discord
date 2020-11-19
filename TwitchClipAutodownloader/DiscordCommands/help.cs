using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipAutodownloader.DiscordCommands
{
    public class help : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;

        public help(CommandService service)
        {
            _service = service;
        }

        [Command("help")]
        [Summary("Shows a list of all available commands per module.")]
        public async Task HelpAsync()
        {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "You can use these commands"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"***{cmd.Aliases.First()}***\t{cmd.Summary}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }
            await dmChannel.SendMessageAsync("", false, builder.Build());
        }

        [Command("help")]
        [Summary("Shows what a specific command does and what parameters it takes.")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    //x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                    x.Value = $"Description: {cmd.Summary}";
                    x.IsInline = false;
                });
            }
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
    }
}

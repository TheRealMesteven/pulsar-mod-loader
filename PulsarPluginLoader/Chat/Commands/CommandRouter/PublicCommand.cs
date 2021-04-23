﻿
namespace PulsarPluginLoader.Chat.Commands.CommandRouter
{
    public abstract class PublicCommand
    {
        /// <summary>
        /// Command aliases will fail silently if the alias is not unique
        /// </summary>
        /// <returns>An array containing names for the command that can be used by the player</returns>
        public abstract string[] CommandAliases();
        /// <summary>
        /// 
        /// </summary>
        /// <returns>A short description of what the command does</returns>
        public abstract string Description();
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Examples of how to use the command including what arguments are valid</returns>
        public virtual string[] UsageExamples()
        {
            return new string[] { $"!{CommandAliases()[0]}" };
        }
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="arguments">A string containing all of the text entered after the command</param>
        /// <param name="SenderID">The PlayerID of the player who sent the message</param>
        public abstract void Execute(string arguments, int SenderID);
    }
}
﻿using Blasphemous.CheatConsole;
using Gameplay.UI.Widgets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blasphemous.Randomizer.Multiworld.Services
{
    public class MultiworldCommand : ModCommand
    {
        protected override string CommandName => "ap";

        protected override bool AllowUppercase => true;

        protected override Dictionary<string, Action<string[]>> AddSubCommands()
        {
            return new Dictionary<string, Action<string[]>>()
            {
                { "help", Help },
                { "status", Status },
                //{ "deathlink", Deathlink },
                { "say", Say },
                { "hint", Hint },
            };
        }

        public void HackWriteToConsole(string message)
        {
            ConsoleWidget.Write(message);
        }

        private void Help(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            Write("Available AP commands:");
            Write("ap status: Display connection status");
            //Write("multiworld deathlink: Toggles deathlink on/off");
            Write("ap say COMMAND: Sends a text message or command to the server");
            Write("ap hint ITEM: Hints for your specified item in the multiworld");
        }

        private void Status(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            string server = Main.Multiworld.APManager.ServerAddress;
            if (server == string.Empty)
                Write("Not connected to any server");
            else
                Write($"Connected to {server}");
        }

        //private void Deathlink(string[] parameters)
        //{
        //    if (!ValidateParameterList(parameters, 0)) return;

        //    if (Main.Multiworld.APManager.Connected)
        //    {
        //        bool enabled = Main.Multiworld.DeathLinkManager.ToggleDeathLink();
        //        Write("Deathlink has been " + (enabled ? "enabled" : "disabled"));
        //    }
        //    else
        //        Write("Not connected to any server!");
        //}

        private void Say(string[] parameters)
        {
            // Too few parameters
            if (parameters.Length < 1)
            {
                Write("This command requires at least one parameter.  You passed " + parameters.Length);
                return;
            }

            // Not connected to multiworld
            if (!Main.Multiworld.APManager.Connected)
            {
                Write("Not connected to any server!");
                return;
            }

            StringBuilder output = new StringBuilder(parameters[0]);
            for (int i = 1; i < parameters.Length; i++)
            {
                output.AppendFormat(" {0}", parameters[i]);
            }
            Main.Multiworld.APManager.SendMessage(output.ToString());
        }

        private void Hint(string[] parameters)
        {
            // Too few parameters
            if (parameters.Length < 1)
            {
                Write("This command requires at least one parameter.  You passed " + parameters.Length);
                return;
            }

            // Not connected to multiworld
            if (!Main.Multiworld.APManager.Connected)
            {
                Write("Not connected to any server!");
                return;
            }

            string item = string.Join(" ", parameters);
            Main.Multiworld.APManager.SendHint(item);
        }

        private ConsoleWidget x_console;
        private ConsoleWidget ConsoleWidget
        {
            get
            {
                if (x_console == null)
                    x_console = UnityEngine.Object.FindObjectOfType<ConsoleWidget>();
                return x_console;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using ModdingAPI;

namespace BlasphemousMultiworld
{
    public class MultiworldCommand : ModCommand
    {
        protected override string CommandName => "multiworld";

        protected override bool AllowUppercase => true;

        protected override Dictionary<string, Action<string[]>> AddSubCommands()
        {
            return new Dictionary<string, Action<string[]>>()
            {
                { "help", Help },
                { "status", Status },
                { "connect", Connect },
                { "disconnect", Disconnect },
                { "deathlink", Deathlink },
                { "players", Players }
            };
        }

        private void Help(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            Write("Available MULTIWORLD commands:");
            Write("multiworld status: Display connection status");
            Write("multiworld connect SERVER NAME [PASSWORD]: Connect to SERVER with player name as NAME with optional PASSWORD");
            Write("multiworld disconnect: Disconnect from current server");
            Write("multiworld deathlink: Toggles deathlink on/off");
            Write("multiworld players : List all players in this multiworld");
        }

        private void Status(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            string server = Main.Multiworld.connection.getServer();
            if (server == "")
                Write("Not connected to any server");
            else
                Write($"Connected to {server}");
        }

        private void Connect(string[] parameters)
        {
            string password;
            if (parameters.Length == 2) { password = ""; }
            else if (parameters.Length == 3) { password = parameters[2]; }
            else
            {
                Write("The command 'connect' requires either 2 or 3 parameters.  You passed " + parameters.Length);
                return;
            }

            Write($"Attempting to connect to {parameters[0]} as {parameters[1]}...");
            string result = Main.Multiworld.tryConnect(parameters[0], parameters[1], password);
            Write(result);
        }

        private void Disconnect(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            if (Main.Multiworld.connection.connected)
            {
                Main.Multiworld.connection.Disconnect();
                Write("Disconnected from server");
            }
            else
                Write("Not connected to any server!");
        }

        private void Deathlink(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            if (Main.Multiworld.connection.connected)
            {
                bool enabled = Main.Multiworld.toggleDeathLink();
                Write("Deathlink has been " + (enabled ? "enabled" : "disabled"));
            }
            else
                Write("Not connected to any server!");
        }

        private void Players(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            string[] players = Main.Multiworld.connection.getPlayers();
            if (players.Length == 0)
            {
                Write("Not connected to any server!");
                return;
            }

            string output = "Multiworld players: ";
            foreach (string player in players)
                output += player + ", ";
            Write(output.Substring(0, output.Length - 2));
        }
    }
}

using System;
using System.Collections.Generic;
using ModdingAPI.Commands;

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
                //{ "players", Players }
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
            //Write("multiworld players : List all players in this multiworld");
        }

        private void Status(string[] parameters)
        {
            if (!ValidateParameterList(parameters, 0)) return;

            string server = Main.Multiworld.connection.ServerAddress;
            if (server == string.Empty)
                Write("Not connected to any server");
            else
                Write($"Connected to {server}");
        }

        private void Connect(string[] parameters)
        {
            // Already connected
            if (Main.Multiworld.connection.connected)
            {
                Write("Already connected to a server!");
                return;
            }

            // Too few parameters
            if (parameters.Length < 2)
            {
                Write("This command requires either 2 or 3 parameters.  You passed " + parameters.Length);
                return;
            }

            string name = "";
            string password = null;
            int passIdx = -1;

            // Name has a space and spans multiple parameters
            if (parameters[1].StartsWith("\""))
            {
                // Find the ending index of the name
                for (int i = parameters.Length - 1; i >= 1; i--)
                {
                    if (parameters[i].EndsWith("\""))
                    {
                        passIdx = i + 1;
                        break;
                    }
                }

                // Verify the ending quote exists
                if (passIdx == -1)
                {
                    Write("Invalid syntax!");
                    return;
                }

                // Build up the name
                for (int i = 1; i < passIdx; i++)
                {
                    name += parameters[i] + " ";
                }
                name = name.Substring(1, name.Length - 3);
            }
            else
            {
                // Name is only one word
                name = parameters[1];
                passIdx = 2;
            }

            // Too many parameters
            if (parameters.Length > passIdx + 1)
            {
                Write("This command requires either 2 or 3 parameters.  You passed " + parameters.Length);
                return;
            }

            // If password is there set it
            if (parameters.Length > passIdx)
            {
                password = parameters[passIdx];
            }

            Write($"Attempting to connect to {parameters[0]} as {name}...");
            string result = Main.Multiworld.tryConnect(parameters[0], name, password);
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
                bool enabled = Main.Multiworld.DeathLinkManager.ToggleDeathLink();
                Write("Deathlink has been " + (enabled ? "enabled" : "disabled"));
            }
            else
                Write("Not connected to any server!");
        }

        //private void Players(string[] parameters)
        //{
        //    if (!ValidateParameterList(parameters, 0)) return;

        //    string[] players = Main.Multiworld.connection.getPlayers();
        //    if (players.Length == 0)
        //    {
        //        Write("Not connected to any server!");
        //        return;
        //    }

        //    string output = "Multiworld players: ";
        //    foreach (string player in players)
        //        output += player + ", ";
        //    Write(output.Substring(0, output.Length - 2));
        //}
    }
}

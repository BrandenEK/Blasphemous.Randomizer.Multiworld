using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Blasphemous.ModdingAPI;
using System.Collections.Generic;

namespace Blasphemous.Randomizer.Multiworld.AP.Receivers
{
    public class MessageReceiver
    {
        private readonly Queue<string> messageQueue = new();

        public void OnReceiveMessage(ArchipelagoPacketBase packet)
        {
            if (packet.PacketType != ArchipelagoPacketType.PrintJSON)
                return;

            PrintJsonPacket jsonPacket = packet as PrintJsonPacket;
            System.Text.StringBuilder output = new();

            foreach (JsonMessagePart messagePart in jsonPacket.Data)
            {
                string text = messagePart.Text;
                ColorType color = ColorType.NoColor;
                switch (messagePart.Type)
                {
                    case JsonMessagePartType.ItemId:
                        {
                            if (long.TryParse(text, out long itemId))
                            {
                                if (messagePart.Flags == ItemFlags.Advancement)
                                    color = ColorType.ItemProgression;
                                else if (messagePart.Flags == ItemFlags.NeverExclude)
                                    color = ColorType.ItemUseful;
                                else if (messagePart.Flags == ItemFlags.Trap)
                                    color = ColorType.ItemTrap;
                                else
                                    color = ColorType.ItemBasic;

                                text = Main.Multiworld.APManager.GetItemNameForPlayer(itemId, messagePart.Player ?? 0);
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                    case JsonMessagePartType.LocationId:
                        {
                            if (long.TryParse(text, out long locationId))
                            {
                                color = ColorType.Location;
                                text = Main.Multiworld.APManager.GetLocationNameForPlayer(locationId, messagePart.Player ?? 0);
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                    case JsonMessagePartType.PlayerId:
                        {
                            if (int.TryParse(text, out int playerId))
                            {
                                if (playerId == Main.Multiworld.APManager.PlayerSlot)
                                    color = ColorType.PlayerSelf;
                                else
                                    color = ColorType.PlayerOther;

                                text = Main.Multiworld.APManager.GetPlayerAliasFromSlot(playerId);
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                    case JsonMessagePartType.Color:
                        {
                            if (messagePart.Color.HasValue)
                            {
                                if (messagePart.Color.Value == JsonMessagePartColor.Red)
                                    color = ColorType.Red;
                                else if (messagePart.Color.Value == JsonMessagePartColor.Green)
                                    color = ColorType.Location;
                            }
                            else
                            {
                                color = ColorType.Error;
                            }
                            break;
                        }
                }

                if (color == ColorType.NoColor)
                {
                    //No associated color, use default white
                    output.Append(text);
                }
                else
                {
                    // Using custom color
                    output.AppendFormat("<color=#{0}>{1}</color>", colorCodes[color], text);
                }
            }

            lock (APManager.receiverLock)
            {
                string message = output.ToString();
                ModLog.Info("Queueing message: " + message);
                messageQueue.Enqueue(message);
            }
        }

        public void Update()
        {
            if (messageQueue.Count > 0)
            {
                Main.Multiworld.WriteToConsole(messageQueue.Dequeue());
            }
        }

        public void ClearMessageQueue()
        {
            messageQueue.Clear();
        }

        private readonly Dictionary<ColorType, string> colorCodes = new()
        {
            { ColorType.ItemProgression, "AF99EF" },
            { ColorType.ItemUseful, "6D8BE8" },
            { ColorType.ItemTrap, "FA8072" },
            { ColorType.ItemBasic, "00EEEE" },
            { ColorType.Location, "00FF7F" },
            { ColorType.PlayerSelf, "EE00EE" },
            { ColorType.PlayerOther, "FAFAD2" },
            { ColorType.Red, "EE0000" },
            { ColorType.Error, "7F7F7F" },
        };

        private enum ColorType
        {
            ItemProgression,
            ItemUseful,
            ItemTrap,
            ItemBasic,
            Location,
            PlayerSelf,
            PlayerOther,
            Red,
            Error,
            NoColor,
        }

    }
}

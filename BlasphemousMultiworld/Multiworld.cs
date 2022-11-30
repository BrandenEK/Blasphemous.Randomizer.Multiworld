using System;
using System.Collections.Generic;
using UnityEngine;
using BlasphemousRandomizer;
using BlasphemousRandomizer.Structures;

namespace BlasphemousMultiworld
{
    public class Multiworld
    {
        private Dictionary<string, long> apLocationIds;
        public List<Item> allItems;
        private Dictionary<string, Item> newItems;

        public Connection connection { get; private set; }

        public Multiworld()
        {
            connection = new Connection();
            apLocationIds = new Dictionary<string, long>();
            newItems = new Dictionary<string, Item>();
        }

        public void update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                if (allItems == null)
                    Main.Randomizer.Log("All items is empty");
                Main.Randomizer.Log(allItems[0].name);
            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {
                
            }
        }

        public string tryConnect(string server, string playerName)
        {
            // Check if not in game & not connected
            if (connection.connected)
            {
                return "Already connected to a server!";
            }
            // If in game
            bool success = connection.Connect(server, playerName);
            return success ? "Connected to " + server + "!" : "Failed to connect to " + server + "!";
        }

        public void onConnect(string playerName, ArchipelagoLocation[] locations)
        {
            // Init
            apLocationIds.Clear();
            newItems.Clear();

            // Process locations
            for (int i = 0; i < locations.Length; i++)
            {
                // Add conversion from location id to name
                if (apLocationIds.ContainsKey(locations[i].id))
                {
                    Main.Randomizer.Log(locations[i].id + " is a duplicate");
                    continue;
                }
                else
                apLocationIds.Add(locations[i].id, locations[i].ap_id);

                // Add to new list of random items
                if (locations[i].player_name == playerName)
                {
                    // This is an item for this player
                    Item item = itemExists(allItems, locations[i].item);
                    if (item != null)
                        newItems.Add(locations[i].id, item);
                    else
                        Main.Randomizer.Log("Item " + locations[i].item + " doesn't exist!");
                }
                else
                {
                    // This is an item to a different game
                    newItems.Add(locations[i].id, new ArchipelagoItem(locations[i].name, locations[i].player_name));
                }
                Main.Randomizer.Log(locations[i].id + ": " + newItems[locations[i].id]);
            }

            // newItems has been filled with new shuffled items
            Main.Randomizer.Log("newItems has been filled from multiworld");
        }
        
        private Item itemExists(List<Item> items, string name)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].name == name)
                    return items[i];
            }
            return null;
        }

        public void sendLocation(string location)
        {
            if (apLocationIds.ContainsKey(location))
                connection.sendLocation(apLocationIds[location]);
            else
                Main.Randomizer.Log("Location " + location + " does not exist in the multiworld!");
        }
    }
}

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
        private Dictionary<string, string> itemNames;

        public Connection connection { get; private set; }

        public bool gameStatus;
        private List<Item> itemsToGive;
        public Sprite multiworldItemImage;

        public Multiworld()
        {
            connection = new Connection();
            apLocationIds = new Dictionary<string, long>();
            newItems = new Dictionary<string, Item>();
            itemNames = new Dictionary<string, string>();
            itemsToGive = new List<Item>();
            FileUtil.parseFileToDictionary("names_items.dat", itemNames);
            loadMultiworldImage();
        }

        public void update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                
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
            if (itemNames.Count < 1)
            {
                Main.Randomizer.Log("Item names weren't loaded!");
                return;
            }
            itemNames.Remove("CH");
            itemNames.Add("CH", "Child of Moonlight");
            itemNames.Remove("TH");
            itemNames.Add("TH", "Thorn");

            // Process locations
            for (int i = 0; i < locations.Length; i++)
            {
                // Add conversion from location id to name
                apLocationIds.Add(locations[i].id, locations[i].ap_id);

                // Add to new list of random items
                if (locations[i].player_name == playerName)
                {
                    // This is an item for this player
                    Item item = itemExists(allItems, locations[i].name);
                    if (item != null)
                        newItems.Add(locations[i].id, item);
                    else
                    {
                        Main.Randomizer.Log("Item " + locations[i].name + " doesn't exist!");
                        continue;
                    }
                }
                else
                {
                    // This is an item to a different game
                    newItems.Add(locations[i].id, new ArchipelagoItem(locations[i].name, locations[i].player_name));
                }
                Main.Randomizer.Log(locations[i].id + ": " + newItems[locations[i].id].name);
            }

            // newItems has been filled with new shuffled items
            Main.Randomizer.Log("newItems has been filled from multiworld");
        }
        
        private Item itemExists(List<Item> items, string descriptiveName)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (itemNames.ContainsKey(items[i].name) && itemNames[items[i].name] == descriptiveName)
                    return items[i];
            }
            return null;
        }

        public void modifyNewItems(Dictionary<string, Item> shufflerItems)
        {
            shufflerItems.Clear();
            foreach (string key in newItems.Keys)
            {
                shufflerItems.Add(key, newItems[key]);
            }
        }

        public void sendLocation(string location)
        {
            if (apLocationIds.ContainsKey(location))
                connection.sendLocation(apLocationIds[location]);
            else
                Main.Randomizer.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void recieveItem(string itemName)
        {
            Item item = itemExists(allItems, itemName);
            if (item != null)
            {
                itemsToGive.Add(item);
                if (gameStatus)
                    processItems();
            }
        }

        public void processItems()
        {
            for (int i = 0; i < itemsToGive.Count; i++)
            {
                itemsToGive[i].addToInventory();
                Main.Randomizer.itemShuffler.showItemPopUp(itemsToGive[i]);
            }
            itemsToGive.Clear();
        }

        private void loadMultiworldImage()
        {
            // Read bytes from file
            if (!FileUtil.readBytes("multiworld_item.png", out byte[] data))
            {
                Main.Randomizer.Log("Error: Custom multiworld image could not be loaded!");
                return;
            }

            // Create texture
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            multiworldItemImage = Sprite.Create(tex, new Rect(0, 0, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}

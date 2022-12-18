using System.Collections.Generic;
using UnityEngine;
using BlasphemousRandomizer;
using BlasphemousRandomizer.Structures;
using BlasphemousRandomizer.Config;
using BlasphemousMultiworld.Structures;
using Framework.FrameworkCore;
using Framework.Managers;

namespace BlasphemousMultiworld
{
    public class Multiworld : PersistentInterface
    {
        // Data
        private Dictionary<string, long> apLocationIds;
        public List<Item> allItems;
        private Dictionary<string, string> itemNames;
        private Sprite[] multiworldImages;

        // Connection
        public Connection connection { get; private set; }
        private bool gameStatus;
        private List<QueuedItem> queuedItems;
        public string receivedPlayer;

        // Game
        private Dictionary<string, Item> newItems;
        private MainConfig gameConfig; // Move this data to new struct
        private int chosenEnding;
        private int itemsReceived;

        public void Initialize()
        {
            // Create new connection
            connection = new Connection();
            LevelManager.OnLevelLoaded += onLevelLoaded;
            Core.Persistence.AddPersistentManager(this);
            receivedPlayer = "";

            // Initialize data storages
            apLocationIds = new Dictionary<string, long>();
            newItems = new Dictionary<string, Item>();
            itemNames = new Dictionary<string, string>();
            queuedItems = new List<QueuedItem>();

            // Load external data
            if (!FileUtil.parseFileToDictionary("names_items.dat", itemNames))
                Main.Randomizer.Log("Error: Item names could not be loaded!");
            if (!FileUtil.loadImages("multiworld_item.png", 32, 32, 0, out multiworldImages))
                Main.Randomizer.Log("Error: Multiworld images could not be loaded!");
            if (itemNames.Count > 0) updateItemNames();

            Main.Randomizer.Log("Multiworld has been initialized!");
        }

        public void Dispose()
        {
            LevelManager.OnLevelLoaded -= onLevelLoaded;
        }

        // Save game data
        public PersistentManager.PersistentData GetCurrentPersistentState(string dataPath, bool fullSave)
        {
            return new MultiworldPersistenceData
            {
                itemsReceived = itemsReceived
            };
        }

        // Load game data
        public void SetCurrentPersistentState(PersistentManager.PersistentData data, bool isloading, string dataPath)
        {
            MultiworldPersistenceData multiworldData = (MultiworldPersistenceData)data;
            if (multiworldData != null)
            {
                itemsReceived = multiworldData.itemsReceived;
            }
        }

        // Load new game
        public void newGame()
        {
            itemsReceived = 0;
        }

        private void onLevelLoaded(Level oldLevel, Level newLevel)
        {
            gameStatus = newLevel.LevelName != "MainMenu";
            processItems();
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

        public string tryConnect(string server, string playerName, string password)
        {
            // Check if not in game & not connected
            if (connection.connected)
            {
                return "Already connected to a server!";
            }
            string result = connection.Connect(server, playerName, password); // Check if not in game first ?
            Main.Randomizer.Log(result);
            return result;
        }

        public void onConnect(string playerName, ArchipelagoLocation[] locations, MainConfig config, int ending)
        {
            // Init
            apLocationIds.Clear();
            newItems.Clear();
            if (itemNames.Count < 1)
            {
                Main.Randomizer.Log("Item names weren't loaded!");
                return;
            }

            // Save other data
            gameConfig = config;
            chosenEnding = ending;

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
            }

            // newItems has been filled with new shuffled items
            Main.Randomizer.Log("Game variables have been loaded from multiworld!");
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

        // Set randomizer data to updated multiworld data
        public void modifyNewItems(ref Dictionary<string, Item> shufflerItems)
        {
            shufflerItems = newItems;
        }
        public void modifyGameConfig(MainConfig config)
        {
            config.general = gameConfig.general;
            config.items = gameConfig.items;
            config.enemies = gameConfig.enemies;
        }

        public void sendLocation(string location)
        {
            if (apLocationIds.ContainsKey(location))
                connection.sendLocation(apLocationIds[location]);
            else
                Main.Randomizer.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void sendGoal(int ending)
        {
            if (ending >= chosenEnding)
            {
                Main.Randomizer.Log($"Completing goal {chosenEnding} with ending {ending}!");
                connection.sendGoal();
            }
        }

        public void receiveItem(string itemName, int index, string player)
        {
            Main.Randomizer.Log("Receiving item: " + itemName);
            Item item = itemExists(allItems, itemName);
            if (item != null)
            {
                queuedItems.Add(new QueuedItem(item, index, player));
                processItems();
            }
        }

        public void processItems()
        {
            if (!gameStatus)
                return;

            for (int i = 0; i < queuedItems.Count; i++)
            {
                Main.Randomizer.Log($"Item '{queuedItems[i].item.name}' is at index {queuedItems[i].index} with {itemsReceived} items currently received");
                if (queuedItems[i].index > itemsReceived)
                {
                    queuedItems[i].item.addToInventory();
                    receivedPlayer = queuedItems[i].player;
                    Main.Randomizer.itemShuffler.showItemPopUp(queuedItems[i].item);
                    itemsReceived++;
                }
            }
            queuedItems.Clear();
        }

        public Sprite getImage(int idx)
        {
            return idx >= 0 && idx < multiworldImages.Length ? multiworldImages[idx] : null;
        }

        private void updateItemNames()
        {
            itemNames["CH"] = "Child of Moonlight";
            itemNames["Tears[250]"] = "Tears of Atonement (250)";
            itemNames["Tears[300]"] = "Tears of Atonement (300)";
            itemNames["Tears[500]"] = "Tears of Atonement (500)";
            itemNames["Tears[625]"] = "Tears of Atonement (625)";
            itemNames["Tears[750]"] = "Tears of Atonement (750)";
            itemNames["Tears[1000]"] = "Tears of Atonement (1000)";
            itemNames["Tears[1250]"] = "Tears of Atonement (1250)";
            itemNames["Tears[1500]"] = "Tears of Atonement (1500)";
            itemNames["Tears[1750]"] = "Tears of Atonement (1750)";
            itemNames["Tears[2000]"] = "Tears of Atonement (2000)";
            itemNames["Tears[2100]"] = "Tears of Atonement (2100)";
            itemNames["Tears[2500]"] = "Tears of Atonement (2500)";
            itemNames["Tears[2600]"] = "Tears of Atonement (2600)";
            itemNames["Tears[3000]"] = "Tears of Atonement (3000)";
            itemNames["Tears[4300]"] = "Tears of Atonement (4300)";
            itemNames["Tears[5000]"] = "Tears of Atonement (5000)";
            itemNames["Tears[5500]"] = "Tears of Atonement (5500)";
            itemNames["Tears[9000]"] = "Tears of Atonement (9000)";
            itemNames["Tears[10000]"] = "Tears of Atonement (10000)";
            itemNames["Tears[11250]"] = "Tears of Atonement (11250)";
            itemNames["Tears[18000]"] = "Tears of Atonement (18000)";
            itemNames["Tears[30000]"] = "Tears of Atonement (30000)";
        }

        public string GetPersistenID() { return "ID_MULTIWORLD"; }

        public int GetOrder() { return 0; }

        public void ResetPersistence() { }

    }
}

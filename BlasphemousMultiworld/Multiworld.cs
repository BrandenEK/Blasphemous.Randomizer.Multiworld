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
        enum DeathLinkStatus { Nothing, Queued, Killing }

        // Data
        private Dictionary<string, long> apLocationIds;
        private Sprite[] multiworldImages;

        // Connection
        public Connection connection { get; private set; }
        private DeathLinkStatus deathlink;
        private bool gameStatus;
        private bool sentLocations;
        private List<QueuedItem> queuedItems;
        public string receivedPlayer;

        // Game
        private Dictionary<string, Item> newItems;
        public GameData gameData;
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
            queuedItems = new List<QueuedItem>();
            gameData = new GameData();

            // Load external data
            if (!FileUtil.loadImages("multiworld_item.png", 32, 32, 0, true, out multiworldImages))
                Main.Randomizer.LogError("Error: Multiworld images could not be loaded!");
            Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
            if (cherub != null) cherub.name = "Child of Moonlight";

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
            processItems(true);
            sendAllLocations();
        }
        
        public void update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {

            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {

            }

            // If you received a deathlink & are able to die
            bool canKill = gameStatus && !Core.LevelManager.InsideChangeLevel && !Core.Input.HasBlocker("*");
            if (deathlink == DeathLinkStatus.Queued && canKill)
            {
                deathlink = DeathLinkStatus.Killing;
                Core.Logic.Penitent.KillInstanteneously();
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

        public void onConnect(ArchipelagoLocation[] locations, GameData data)
        {
            // Init
            apLocationIds.Clear();
            newItems.Clear();
            gameData = data;

            // Process locations
            for (int i = 0; i < locations.Length; i++)
            {
                // Add conversion from location id to name
                apLocationIds.Add(locations[i].id, locations[i].ap_id);

                // Add to new list of random items
                if (locations[i].player_name == data.playerName)
                {
                    // This is an item for this player
                    Item item = itemExists(locations[i].name);
                    if (item != null)
                        newItems.Add(locations[i].id, item);
                    else
                    {
                        Main.Randomizer.LogError("Item " + locations[i].name + " doesn't exist!");
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
            sendAllLocations();
        }

        public void onDisconnect()
        {
            sentLocations = false;
        }
        
        private Item itemExists(string descriptiveName)
        {
            foreach (Item item in Main.Randomizer.data.items.Values)
            {
                if (item.name == descriptiveName)
                    return item;
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
            config.general = gameData.gameConfig.general;
            config.items = gameData.gameConfig.items;
            config.enemies = gameData.gameConfig.enemies;
        }

        public void sendLocation(string location)
        {
            if (apLocationIds.ContainsKey(location))
                connection.sendLocation(apLocationIds[location]);
            else
                Main.Randomizer.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void sendAllLocations()
        {
            if (sentLocations || !gameStatus)
            {
                return;
            }

            // Send list of all locations already checked
            List<long> checkedLocations = new List<long>();
            foreach (string location in Main.Randomizer.data.itemLocations.Keys)
            {
                if (Core.Events.GetFlag("LOCATION_" + location))
                    checkedLocations.Add(apLocationIds[location]);
            }

            Main.Randomizer.Log($"Sending all locations ({checkedLocations.Count})");
            connection.sendLocations(checkedLocations.ToArray());
            sentLocations = true;
        }

        public void sendGoal(int ending)
        {
            if (ending >= gameData.chosenEnding)
            {
                Main.Randomizer.Log($"Completing goal {gameData.chosenEnding} with ending {ending}!");
                connection.sendGoal();
            }
        }

        public void sendDeathLink()
        {
            if (deathlink == DeathLinkStatus.Killing)
            {
                deathlink = DeathLinkStatus.Nothing;
                return;
            }

            Main.Randomizer.Log("Sending death link!");
            connection.sendDeathLink();
        }

        public void receiveItem(string itemName, int index, string player)
        {
            Main.Randomizer.Log("Receiving item: " + itemName);
            Item item = itemExists(itemName);
            if (item != null)
            {
                queuedItems.Add(new QueuedItem(item, index, player));
                processItems(false);
            }
            else
            {
                Main.Randomizer.LogDisplay("Error: " + itemName + " doesn't exist!");
            }
        }

        public void receiveDeathLink()
        {
            Main.Randomizer.Log("Received death link!");
            deathlink = DeathLinkStatus.Queued;
        }

        public void processItems(bool ignoreLoadingCheck)
        {
            // Wait to process items until inside a save file and the level is loaded
            if (queuedItems.Count == 0 || !gameStatus || (!ignoreLoadingCheck && Core.LevelManager.InsideChangeLevel))
                return;

            for (int i = 0; i < queuedItems.Count; i++)
            {
                Main.Randomizer.Log($"Item '{queuedItems[i].item.id}' is at index {queuedItems[i].index} with {itemsReceived} items currently received");
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

        public string GetPersistenID() { return "ID_MULTIWORLD"; }

        public int GetOrder() { return 0; }

        public void ResetPersistence() { }

    }
}

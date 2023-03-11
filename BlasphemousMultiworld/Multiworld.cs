using System.Collections.Generic;
using UnityEngine;
using BlasphemousRandomizer;
using BlasphemousRandomizer.Structures;
using BlasphemousRandomizer.Config;
using BlasphemousMultiworld.Structures;
using Framework.Managers;
using ModdingAPI;

namespace BlasphemousMultiworld
{
    public class Multiworld : PersistentMod
    {
        public enum DeathLinkStatus { Nothing, Queued, Killing }

        // Data
        private Dictionary<string, long> apLocationIds;
        private Sprite[] multiworldImages;

        // Connection
        public Connection connection { get; private set; }

        public override string PersistentID => "ID_MULTIWORLD";

        public ItemReceiver itemReceiver { get; private set; }
        public DeathLinkStatus deathlink;
        private List<QueuedItem> queuedItems;

        // Game
        private Dictionary<string, Item> newItems;
        public GameData gameData;
        private bool gameStatus;
        private bool sentLocations;
        private int itemsReceived;

        public Multiworld(string modId, string modName, string modVersion) : base(modId, modName, modVersion)
        {
            // Set basic initialization for awake
            gameData = new GameData();
            itemReceiver = new ItemReceiver();
        }

        protected override void Initialize()
        {
            RegisterCommand(new MultiworldCommand());

            // Create new connection
            connection = new Connection();

            // Initialize data storages
            apLocationIds = new Dictionary<string, long>();
            newItems = new Dictionary<string, Item>();
            queuedItems = new List<QueuedItem>();

            // Load external data
            if (!FileUtil.loadDataImages("multiworld_images.png", 32, 32, 32, 0, true, out multiworldImages))
                Main.Multiworld.LogError("Error: Multiworld images could not be loaded!");
            Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
            if (cherub != null) cherub.name = "Child of Moonlight";

            Main.Multiworld.Log("Multiworld has been initialized!");
        }

        public override ModPersistentData SaveGame()
        {
            return new MultiworldPersistenceData
            {
                itemsReceived = itemsReceived
            };
        }

        public override void LoadGame(ModPersistentData data)
        {
            MultiworldPersistenceData multiworldData = (MultiworldPersistenceData)data;
            if (multiworldData != null)
            {
                itemsReceived = multiworldData.itemsReceived;
            }
        }

        public override void NewGame()
        {
            itemsReceived = 0;
        }

        public override void ResetGame() { }

        protected override void LevelLoaded(string oldLevel, string newLevel)
        {
            gameStatus = newLevel != "MainMenu";
            processItems(true);
            sendAllLocations();
        }
        
        protected override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                
            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {

            }
            
            // If you received a deathlink & are able to die
            if (gameData.deathLinkEnabled && deathlink == DeathLinkStatus.Queued && gameStatus && !Core.LevelManager.InsideChangeLevel && !Core.Input.HasBlocker("*"))
            {
                deathlink = DeathLinkStatus.Killing;
                Core.Logic.Penitent.KillInstanteneously();
            }
        }

        public string tryConnect(string server, string playerName, string password)
        {
            string result = connection.Connect(server, playerName, password); // Check if not in game first ?
            Main.Multiworld.Log(result);
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
                        Main.Multiworld.LogError("Item " + locations[i].name + " doesn't exist!");
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
            Main.Multiworld.Log("Game variables have been loaded from multiworld!");
            sendAllLocations();
        }

        public void onDisconnect()
        {
            Main.Multiworld.LogDisplay("Disconnected from multiworld server!");
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
                Main.Multiworld.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void sendAllLocations()
        {
            if (sentLocations || !gameStatus || !connection.connected)
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

            Main.Multiworld.Log($"Sending all locations ({checkedLocations.Count})");
            connection.sendLocations(checkedLocations.ToArray());
            sentLocations = true;
        }

        public void sendGoal(int ending)
        {
            if (ending >= gameData.chosenEnding)
            {
                Main.Multiworld.Log($"Completing goal {gameData.chosenEnding} with ending {ending}!");
                connection.sendGoal();
            }
        }

        public void sendDeathLink()
        {
            if (!gameData.deathLinkEnabled) return;

            Main.Multiworld.Log("Sending death link!");
            connection.sendDeathLink();
        }

        public void receiveItem(string itemName, int index, string player)
        {
            Main.Multiworld.Log("Receiving item: " + itemName);
            Item item = itemExists(itemName);
            if (item != null)
            {
                queuedItems.Add(new QueuedItem(item, index, player));
                processItems(false);
            }
            else
            {
                Main.Multiworld.LogDisplay("Error: " + itemName + " doesn't exist!");
            }
        }

        public void receiveDeathLink(string player)
        {
            if (!gameData.deathLinkEnabled) return;
            if (!Core.Events.GetFlag("CHERUB_RESPAWN"))
            {
                Main.Multiworld.Log("Received death link!");
                deathlink = DeathLinkStatus.Queued;
                itemReceiver.receiveItem(new QueuedItem(null, 0, player));
            }
        }

        public bool toggleDeathLink()
        {
            gameData.deathLinkEnabled = !gameData.deathLinkEnabled;
            connection.setDeathLinkStatus(gameData.deathLinkEnabled);
            Main.Multiworld.Log("Setting deathlink status to " + gameData.deathLinkEnabled.ToString());
            return gameData.deathLinkEnabled;
        }

        public void processItems(bool ignoreLoadingCheck)
        {
            // Wait to process items until inside a save file and the level is loaded
            if (queuedItems.Count == 0 || !gameStatus || (!ignoreLoadingCheck && Core.LevelManager.InsideChangeLevel))
                return;

            for (int i = 0; i < queuedItems.Count; i++)
            {
                Main.Multiworld.Log($"Item '{queuedItems[i].item.id}' is at index {queuedItems[i].index} with {itemsReceived} items currently received");
                if (queuedItems[i].index > itemsReceived)
                {
                    queuedItems[i].item.addToInventory();
                    itemReceiver.receiveItem(queuedItems[i]);
                    itemsReceived++;
                }
            }
            queuedItems.Clear();
        }

        public Sprite getImage(int idx)
        {
            return idx >= 0 && idx < multiworldImages.Length ? multiworldImages[idx] : null;
        }
    }
}

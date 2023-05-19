using System.Collections.Generic;
using UnityEngine;
using BlasphemousRandomizer;
using BlasphemousRandomizer.ItemRando;
using BlasphemousMultiworld.DeathLink;
using BlasphemousMultiworld.Notifications;
using BlasphemousMultiworld.AP;
using Framework.Managers;
using ModdingAPI;

namespace BlasphemousMultiworld
{
    public class Multiworld : PersistentMod
    {
        // Data
        private Sprite[] multiworldImages;
        public Sprite ImageAP => multiworldImages[0];
        public Sprite ImageDeathlink => multiworldImages[1];

        // Managers
        public APManager APManager { get; private set; }
        public DeathLinkManager DeathLinkManager { get; private set; }
        public NotificationManager NotificationManager { get; private set; }

        public override string PersistentID => "ID_MULTIWORLD";

        private List<QueuedItem> queuedItems;

        // Game
        public GameSettings MultiworldSettings { get; private set; }
        private Dictionary<string, string> multiworldMap;
        public bool InGame { get; private set; }

        private bool sentLocations;
        private int itemsReceived;

        public Multiworld(string modId, string modName, string modVersion) : base(modId, modName, modVersion) { }

        protected override void Initialize()
        {
            // Set basic initialization for awake
            MultiworldSettings = new GameSettings();
            APManager = new APManager();
            DeathLinkManager = new DeathLinkManager();
            NotificationManager = new NotificationManager();

            RegisterCommand(new MultiworldCommand());

            // Initialize data storages
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
            itemsReceived = multiworldData.itemsReceived;
        }

        public override void NewGame(bool NGPlus)
        {
            Core.Events.SetFlag("MULTIWORLD", true, false);
        }

        public override void ResetGame()
        {
            itemsReceived = 0;
        }

        protected override void LevelLoaded(string oldLevel, string newLevel)
        {
            InGame = newLevel != "MainMenu";
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
            
            DeathLinkManager.Update();
        }

        public string tryConnect(string server, string playerName, string password)
        {
            string result = APManager.Connect(server, playerName, password); // Check if not in game first ?
            Main.Multiworld.Log(result);
            return result;
        }

        public void OnConnect(ArchipelagoLocation[] locations, GameSettings serverSettings)
        {
            // Init
            //apLocationIds.Clear();
            multiworldMap = new Dictionary<string, string>();
            MultiworldSettings = serverSettings;

            // Process locations
            for (int i = 0; i < locations.Length; i++)
            {
                // Add conversion from location id to name
                //apLocationIds.Add(locations[i].id, locations[i].ap_id);

                // Add to new list of random items
                if (locations[i].player_name == serverSettings.PlayerName)
                {
                    // This is an item for this player
                    if (ItemNameExists(locations[i].name, out string itemId))
                    {
                        multiworldMap.Add(locations[i].id, itemId);
                    }
                    else
                    {
                        Main.Multiworld.LogError("Item " + locations[i].name + " doesn't exist!");
                        continue;
                    }
                }
                else
                {
                    // This is an item to a different game
                    multiworldMap.Add(locations[i].id, "AP");
                    //newItems.Add(locations[i].id, new ArchipelagoItem(locations[i].name, locations[i].player_name));
                }
            }

            // newItems has been filled with new shuffled items
            Main.Multiworld.Log("Game variables have been loaded from multiworld!");
            sendAllLocations();
        }

        public void OnDisconnect()
        {
            Main.Multiworld.LogDisplay("Disconnected from multiworld server!");
            multiworldMap = null;
            sentLocations = false;
        }

        public Dictionary<string, string> LoadMultiworldItems() => multiworldMap;

        public void sendLocation(string location)
        {
            //if (apLocationIds.ContainsKey(location))
            //    APManager.SendLocation(apLocationIds[location]);
            //else
            //    Main.Multiworld.Log("Location " + location + " does not exist in the multiworld!");
        }

        public void sendAllLocations()
        {
            if (sentLocations || !InGame || !APManager.Connected)
            {
                return;
            }

            // Send list of all locations already checked
            List<long> checkedLocations = new List<long>();
            foreach (string location in Main.Randomizer.data.itemLocations.Keys)
            {
                //if (Core.Events.GetFlag("LOCATION_" + location))
                //    checkedLocations.Add(apLocationIds[location]);
            }

            Main.Multiworld.Log($"Sending all locations ({checkedLocations.Count})");
            APManager.SendMultipleLocations(checkedLocations.ToArray());
            sentLocations = true;
        }

        public void receiveItem(string itemName, int index, string player)
        {
            Main.Multiworld.Log("Receiving item: " + itemName);
            if (ItemNameExists(itemName, out string itemId))
            {
                queuedItems.Add(new QueuedItem(itemId, index, player));
                processItems(false);
            }
            else
            {
                Main.Multiworld.LogDisplay("Error: " + itemName + " doesn't exist!");
            }
        }

        public void processItems(bool ignoreLoadingCheck)
        {
            // Wait to process items until inside a save file and the level is loaded
            if (queuedItems.Count == 0 || !InGame || (!ignoreLoadingCheck && Core.LevelManager.InsideChangeLevel))
                return;

            for (int i = 0; i < queuedItems.Count; i++)
            {
                Main.Multiworld.Log($"Item '{queuedItems[i].itemId}' is at index {queuedItems[i].index} with {itemsReceived} items currently received");
                if (queuedItems[i].index > itemsReceived)
                {
                    Main.Randomizer.data.items[queuedItems[i].itemId].addToInventory();
                    NotificationManager.DisplayNotification(queuedItems[i]);
                    itemsReceived++;
                }
            }
            queuedItems.Clear();
        }




        public void ReachedEnding(int ending)
        {
            if (ending >= MultiworldSettings.RequiredEnding)
            {
                Main.Multiworld.Log($"Completing goal {MultiworldSettings.RequiredEnding} with ending {ending}!");
                APManager.SendGoal();
            }
        }

        private bool ItemNameExists(string itemName, out string itemId)
        {
            foreach (Item item in Main.Randomizer.data.items.Values)
            {
                if (item.name == itemName)
                {
                    itemId = item.id;
                    return true;
                }
            }
            itemId = null;
            return false;
        }
    }
}

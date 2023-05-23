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
        // Images
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

        private bool hasSentLocations;
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
                LogError("Error: Multiworld images could not be loaded!");
            Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
            if (cherub != null) cherub.name = "Child of Moonlight";

            Log("Multiworld has been initialized!");
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
            if (APManager.Connected)
                Core.Events.SetFlag("MULTIWORLD", true, false);
        }

        public override void ResetGame()
        {
            itemsReceived = 0;
        }

        protected override void LevelLoaded(string oldLevel, string newLevel)
        {
            InGame = newLevel != "MainMenu";
            ProcessItems(true);
            
            if (!hasSentLocations && InGame && APManager.Connected)
            {
                APManager.SendAllLocations();
                hasSentLocations = true;
            }
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

        public void OnConnect(Dictionary<string, string> mappedItems, GameSettings serverSettings)
        {
            // Get data from server
            multiworldMap = mappedItems;
            MultiworldSettings = serverSettings;

            // MappedItems has been filled with new shuffled items
            Log("Game variables have been loaded from multiworld!");
            if (!hasSentLocations && InGame)
            {
                APManager.SendAllLocations();
                hasSentLocations = true;
            }
        }

        public void OnDisconnect()
        {
            LogDisplay("Disconnected from multiworld server!");
            multiworldMap = null;
            hasSentLocations = false;
            queuedItems.Clear();
        }

        public Dictionary<string, string> LoadMultiworldItems() => multiworldMap;

        public void QueueItem(QueuedItem item)
        {
            queuedItems.Add(item);
            ProcessItems(false);
        }

        public void ProcessItems(bool ignoreLoadingCheck)
        {
            // Wait to process items until inside a save file and the level is loaded
            if (queuedItems.Count == 0 || !InGame || (!ignoreLoadingCheck && Core.LevelManager.InsideChangeLevel))
                return;

            for (int i = 0; i < queuedItems.Count; i++)
            {
                Log($"Item '{queuedItems[i].itemId}' is at index {queuedItems[i].index} with {itemsReceived} items currently received");
                if (queuedItems[i].index > itemsReceived)
                {
                    Main.Randomizer.data.items[queuedItems[i].itemId].addToInventory();
                    NotificationManager.DisplayNotification(queuedItems[i]);
                    itemsReceived++;
                }
            }
            queuedItems.Clear();
        }
    }
}

using BlasphemousRandomizer.ItemRando;
using BlasphemousMultiworld.DeathLink;
using BlasphemousMultiworld.Notifications;
using BlasphemousMultiworld.AP;
using Framework.Managers;
using Gameplay.UI.Others.MenuLogic;
using ModdingAPI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        // Game
        private Dictionary<string, string> multiworldItems;
        private Dictionary<string, string> multiworldDoors;
        public GameSettings MultiworldSettings { get; private set; }
        public bool HasRequiredMods { get; private set; }
        public bool InGame { get; private set; }

        private readonly MultiworldCommand command = new MultiworldCommand();
        private bool hasSentLocations;


        public Multiworld(string modId, string modName, string modVersion) : base(modId, modName, modVersion) { }

        protected override void Initialize()
        {
            // Set basic initialization for awake
            MultiworldSettings = new GameSettings();
            APManager = new APManager();
            DeathLinkManager = new DeathLinkManager();
            NotificationManager = new NotificationManager();

            RegisterCommand(command);

            // Load external data
            if (!FileUtil.loadDataImages("multi-images.png", 30, 30, 30, 0, true, out multiworldImages))
                LogError("Error: Multiworld images could not be loaded!");

            Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
            if (cherub != null)
                cherub.name = "Child of Moonlight";

            Log("Multiworld has been initialized!");
        }

        public override ModPersistentData SaveGame()
        {
            return new MultiworldPersistenceData
            {
                itemsReceived = APManager.ItemReceiver.SaveItemsReceived(),
                scoutedLocations = APManager.SaveScoutedLocations()
            };
        }

        public override void LoadGame(ModPersistentData data)
        {
            MultiworldPersistenceData multiworldData = (MultiworldPersistenceData)data;
            APManager.ItemReceiver.LoadItemsReceived(multiworldData.itemsReceived);
            APManager.LoadScoutedLocations(multiworldData.scoutedLocations);
        }

        public override void NewGame(bool NGPlus)
        {
            if (APManager.Connected)
                Core.Events.SetFlag("MULTIWORLD", true, false);
        }

        public override void ResetGame()
        {
            APManager.ItemReceiver.ResetItemsReceived();
            APManager.ClearScoutedLocations();
        }

        protected override void LevelLoaded(string oldLevel, string newLevel)
        {
            InGame = newLevel != "MainMenu";
            APManager.ProcessAllReceivers();
            
            if (!hasSentLocations && InGame && APManager.Connected)
            {
                APManager.SendAllLocations();
                hasSentLocations = true;
            }
        }

        protected override void LevelUnloaded(string oldLevel, string newLevel)
        {
            InGame = false;
        }

        protected override void Update()
        {
            DeathLinkManager.Update();
            APManager.MessageReceiver.Update();
        }

        public void WriteToConsole(string message)
        {
            command.HackWriteToConsole(message);
        }

        public string tryConnect(string server, string playerName, string password)
        {
            string result = APManager.Connect(server, playerName, password); // Check if not in game first ?
            Main.Multiworld.Log(result);
            return result;
        }

        public void OnConnect(Dictionary<string, string> mappedItems, Dictionary<string, string> mappedDoors, GameSettings serverSettings)
        {
            // Get data from server
            multiworldItems = mappedItems;
            multiworldDoors = mappedDoors;
            MultiworldSettings = serverSettings;
            HasRequiredMods =
                (!MultiworldSettings.Config.ShuffleBootsOfPleading || Main.Randomizer.InstalledBootsMod) &&
                (!MultiworldSettings.Config.ShufflePurifiedHand || Main.Randomizer.InstalledDoubleJumpMod);

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
            multiworldItems = null;
            multiworldDoors = null;
            hasSentLocations = false;
            APManager.ClearAllReceivers();
        }

        public Dictionary<string, string> LoadMultiworldItems() => multiworldItems;
        public Dictionary<string, string> LoadMultiworldDoors() => multiworldDoors;

        private Text m_MultiworldStatusText;
        public Text MultiworldStatusText
        {
            get
            {
                if (m_MultiworldStatusText == null)
                {
                    Log("Creating multiworld status text");
                    Transform textHolder = Object.FindObjectOfType<NewMainMenu>().transform.Find("Settings Menu/Main Section");
                    GameObject randoText = textHolder.Find("Description").gameObject;
                    m_MultiworldStatusText = Object.Instantiate(randoText, textHolder).GetComponent<Text>();
                    m_MultiworldStatusText.rectTransform.anchoredPosition = new Vector2(-175, 182);
                }
                return m_MultiworldStatusText;
            }
        }
    }
}

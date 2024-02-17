using Blasphemous.CheatConsole;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Persistence;
using Blasphemous.Randomizer.ItemRando;
using Blasphemous.Randomizer.Multiworld.AP;
using Blasphemous.Randomizer.Multiworld.DeathLink;
using Blasphemous.Randomizer.Multiworld.Notifications;
using Framework.Managers;
using Gameplay.UI.Others.MenuLogic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.Randomizer.Multiworld;

public class Multiworld : BlasMod, IPersistentMod
{
    internal Multiworld() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    // Images
    private Sprite[] multiworldImages;
    public Sprite ImageAP => multiworldImages[0];
    public Sprite ImageDeathlink => multiworldImages[1];

    // Managers
    public APManager APManager { get; private set; }
    public DeathLinkManager DeathLinkManager { get; private set; }
    public NotificationManager NotificationManager { get; private set; }

    public string PersistentID => "ID_MULTIWORLD";

    // Game
    private Dictionary<string, string> multiworldItems;
    private Dictionary<string, string> multiworldDoors;
    public GameSettings MultiworldSettings { get; private set; }
    public bool HasRequiredMods { get; private set; }
    public bool InGame { get; private set; }

    private readonly MultiworldCommand command = new MultiworldCommand();
    private bool hasSentLocations;

    /// <summary>
    /// Register handlers and create managers
    /// </summary>
    protected override void OnInitialize()
    {
        LocalizationHandler.RegisterDefaultLanguage("en");

        // Set basic initialization for awake
        MultiworldSettings = new GameSettings();
        APManager = new APManager();
        DeathLinkManager = new DeathLinkManager();
        NotificationManager = new NotificationManager();

        // Load external data
        FileHandler.LoadDataAsFixedSpritesheet("multi-images.png", new Vector2(30, 30), out multiworldImages, new ModdingAPI.Files.SpriteImportOptions()
        {
            PixelsPerUnit = 30
        });

        Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
        if (cherub != null)
            cherub.name = "Child of Moonlight";

        Log("Multiworld has been initialized!");
    }

    /// <summary>
    /// Register command
    /// </summary>
    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        provider.RegisterCommand(command);
    }

    public SaveData SaveGame()
    {
        return new MultiworldPersistenceData
        {
            itemsReceived = APManager.ItemReceiver.SaveItemsReceived(),
            scoutedLocations = APManager.SaveScoutedLocations()
        };
    }

    public void LoadGame(SaveData data)
    {
        MultiworldPersistenceData multiworldData = (MultiworldPersistenceData)data;
        APManager.ItemReceiver.LoadItemsReceived(multiworldData.itemsReceived);
        APManager.LoadScoutedLocations(multiworldData.scoutedLocations);
    }

    public void ResetGame()
    {
        APManager.ItemReceiver.ResetItemsReceived();
        APManager.ClearScoutedLocations();
    }

    protected override void OnNewGame()
    {
        if (APManager.Connected)
            Core.Events.SetFlag("MULTIWORLD", true, false);
    }

    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        InGame = newLevel != "MainMenu";

        if (!hasSentLocations && InGame && APManager.Connected)
        {
            APManager.SendAllLocations();
            hasSentLocations = true;
        }
    }

    protected override void OnLevelUnloaded(string oldLevel, string newLevel)
    {
        InGame = false;
    }

    protected override void OnUpdate()
    {
        DeathLinkManager.Update();
        APManager.UpdateAllReceivers();
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

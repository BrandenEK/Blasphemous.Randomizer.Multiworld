using Blasphemous.CheatConsole;
using Blasphemous.Framework.Menus;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Persistence;
using Blasphemous.Randomizer.ItemRando;
using Blasphemous.Randomizer.Multiworld.AP;
using Blasphemous.Randomizer.Multiworld.DeathLink;
using Blasphemous.Randomizer.Multiworld.Models;
using Blasphemous.Randomizer.Multiworld.Notifications;
using Blasphemous.Randomizer.Multiworld.Services;
using Framework.Managers;
using UnityEngine;

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

    /// <summary>
    /// Responsible for scouting all locations on connect and storing item info
    /// </summary>
    public LocationScouter LocationScouter { get; private set; }

    public string PersistentID => "ID_MULTIWORLD";

    public bool InGame { get; private set; }

    /// <summary>
    /// The settings determined by the client
    /// </summary>
    public ClientSettings ClientSettings { get; set; }
    /// <summary>
    /// The settings determined by the server
    /// </summary>
    public ServerSettings ServerSettings { get; set; } = new(new Config(), 0, false);

    private readonly MultiworldCommand command = new MultiworldCommand();
    private bool hasSentLocations;

    /// <summary>
    /// Register handlers and create managers
    /// </summary>
    protected override void OnInitialize()
    {
        LocalizationHandler.RegisterDefaultLanguage("en");

        // Set basic initialization for awake
        APManager = new APManager();
        DeathLinkManager = new DeathLinkManager();
        NotificationManager = new NotificationManager();
        LocationScouter = new LocationScouter();

        APManager.OnDisconnect += OnDisconnect;

        // Load external data
        FileHandler.LoadDataAsFixedSpritesheet("multi-images.png", new Vector2(30, 30), out multiworldImages, new ModdingAPI.Files.SpriteImportOptions()
        {
            PixelsPerUnit = 30
        });

        Main.Randomizer.data.items.TryGetValue("CH", out Item cherub);
        if (cherub != null)
            cherub.name = "Child of Moonlight";

        ModLog.Info("Multiworld has been initialized!");
    }

    /// <summary>
    /// Clear id mapping when exiting game
    /// </summary>
    protected override void OnExitGame()
    {
        LocationScouter.ResetLocationInfo();
    }

    /// <summary>
    /// Register command
    /// </summary>
    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        provider.RegisterCommand(command);

        MultiworldMenu menu = new();
        provider.RegisterNewGameMenu(menu);
        provider.RegisterLoadGameMenu(menu);
    }

    public SaveData SaveGame()
    {
        return new MultiworldPersistenceData
        {
            itemsReceived = APManager.ItemReceiver.SaveItemsReceived(),
            scoutedLocations = APManager.SaveScoutedLocations(),
            server = ClientSettings.Server,
            name = ClientSettings.Name,
            password = ClientSettings.Password,
        };
    }

    public void LoadGame(SaveData data)
    {
        MultiworldPersistenceData multiworldData = (MultiworldPersistenceData)data;
        APManager.ItemReceiver.LoadItemsReceived(multiworldData.itemsReceived);
        APManager.LoadScoutedLocations(multiworldData.scoutedLocations);
        ClientSettings = new ClientSettings(multiworldData.server, multiworldData.name, multiworldData.password);
    }

    public void ResetGame()
    {
        APManager.ItemReceiver.ResetItemsReceived();
        APManager.ClearScoutedLocations();
        ClientSettings = null;
    }

    protected override void OnNewGame()
    {
        if (APManager.Connected)
            Core.Events.SetFlag("MULTIWORLD", true, false);
    }

    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        if (newLevel == "MainMenu")
        {
            ClientSettings = null;
            APManager.Disconnect();
        }

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

    private void OnDisconnect()
    {
        if (InGame)
            ModLog.Display("Disconnected from multiworld server!");

        hasSentLocations = false;
        APManager.ClearAllReceivers();
    }
}

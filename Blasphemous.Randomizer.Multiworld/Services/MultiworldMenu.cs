﻿using Archipelago.MultiClient.Net;
using Blasphemous.Framework.Menus;
using Blasphemous.Framework.Menus.Options;
using Blasphemous.Framework.UI;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Blasphemous.ModdingAPI.Input;
using Blasphemous.Randomizer.Multiworld.Models;
using Gameplay.UI;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.Randomizer.Multiworld.Services;

public class MultiworldMenu : ModMenu
{
    private TextOption _server;
    private TextOption _name;
    private TextOption _password;

    private Text _resultText;
    private float _timeShowingText;

    private int _connectNextFrame = 0;

    protected override int Priority { get; } = int.MaxValue;

    public MultiworldMenu()
    {
        Main.Multiworld.APManager.OnConnect += OnConnect;
    }

    public override void OnUpdate()
    {
        if (Main.Multiworld.InputHandler.GetButtonDown(ButtonCode.UISubmit))
            OnSubmit();
        else if (Main.Multiworld.InputHandler.GetButtonDown(ButtonCode.UICancel))
            OnCancel();

        if (_timeShowingText > 0)
        {
            _timeShowingText -= Time.deltaTime;
            if (_timeShowingText <= 0)
            {
                _resultText.text = string.Empty;
            }
        }

        if (_connectNextFrame > 0)
        {
            _connectNextFrame--;
            if (_connectNextFrame == 0)
            {
                Main.Multiworld.APManager.Connect(_server.CurrentValue, _name.CurrentValue, _password.CurrentValue);
            }
        }
    }

    protected override void CreateUI(Transform ui)
    {
        TextCreator text = new(this)
        {
            TextSize = 54,
            LineSize = 400,
        };

        _server = text.CreateOption("server", ui, new Vector2(0, 100), "Server ip:", false, true, 64);
        _name = text.CreateOption("name", ui, new Vector2(0, 0), "Player name:", false, true, 64);
        _password = text.CreateOption("password", ui, new Vector2(0, -100), "Optional password:", false, true, 64);

        _resultText = UIModder.Create(new RectCreationOptions()
        {
            Name = "Result Text",
            Parent = ui,
            Pivot = new Vector2(0.5f, 1),
            XRange = new Vector2(0, 1),
            YRange = new Vector2(1, 1),
            Position = new Vector2(0, -20),
        }).AddText(new TextCreationOptions()
        {
            Alignment = TextAnchor.UpperCenter,
            FontSize = 40,
        });
    }

    public override void OnStart()
    {
        _resultText.text = string.Empty;
        _timeShowingText = 0;
        _connectNextFrame = 0;

        ClientSettings settings = Main.Multiworld.ClientSettings;
        _server.CurrentValue = settings?.Server ?? string.Empty;
        _name.CurrentValue = settings?.Name ?? string.Empty;
        _password.CurrentValue = settings?.Password ?? string.Empty;
    }

    public override void OnFinish()
    {
        ModLog.Info("Storing client settings from menu");
        Main.Multiworld.ClientSettings = new ClientSettings(_server.CurrentValue, _name.CurrentValue, _password.CurrentValue);
    }

    private void OnSubmit()
    {
        if (_server.CurrentValue.StartsWith("ap:"))
            _server.CurrentValue = _server.CurrentValue.Replace("ap:", "archipelago.gg:");

        ModLog.Info($"Server {_server.CurrentValue} as {_name.CurrentValue} with pass {_password.CurrentValue}");
        OnFinish();

        ShowText("Attempting to connect...", Color.yellow);
        _connectNextFrame = 2;
    }

    private void OnCancel()
    {
        MenuFramework.ShowPreviousMenu();
    }

    private void OnConnect(LoginResult login)
    {
        if (login is LoginFailure failure)
        {
            ShowError(string.Join("\n", failure.Errors));
            return;
        }

        if (login is LoginSuccessful success)
        {
            Config cfg = ((JObject)success.SlotData["cfg"]).ToObject<Config>();
            List<string> missingMods = new();

            // Determine which mods are missing
            if (cfg.ShuffleBootsOfPleading && !Main.Randomizer.InstalledBootsMod)
                missingMods.Add("Boots of Pleading");
            if (cfg.ShufflePurifiedHand && !Main.Randomizer.InstalledDoubleJumpMod)
                missingMods.Add("Double Jump");

            // Handle failed result if mods are missing
            if (missingMods.Count > 0)
            {
                ShowError($"Install the following mods: {string.Join(", ", [.. missingMods])}");
                Main.Multiworld.APManager.Disconnect();
                return;
            }

            // Handle successful result
            UIController.instance.StartCoroutine(FinishMenuAfterLocations(success));
            return;
        }
    }

    private IEnumerator FinishMenuAfterLocations(LoginSuccessful success)
    {
        if (success.SlotData.ContainsKey("locations"))
        {
            yield return Main.Multiworld.LocationScouter.LoadLocationsV1(success);
        }
        else if (success.SlotData.ContainsKey("locationinfo"))
        {
            yield return Main.Multiworld.LocationScouter.LoadLocationsV2(success);
        }
        else
        {
            ShowError("No location info found in slot data");
            yield break;
        }

        ShowText("Successfully connected", Color.green);
        MenuFramework.ShowNextMenu();
    }

    private MenuFramework MenuFramework => ModHelper.TryGetModByName("Menu Framework", out BlasMod mod)
        ? mod as MenuFramework
        : throw new System.Exception("Menu Framework was never loaded");

    // Text results

    private void ShowText(string text, Color color)
    {
        _resultText.color = color;
        _resultText.text = text;
        _timeShowingText = 0;

        ModLog.Info($"Menu result: {text}");
    }

    private void ShowTextTimed(string text, Color color, float time)
    {
        _resultText.color = color;
        _resultText.text = text;
        _timeShowingText = time;

        ModLog.Info($"Menu result: {text}");
    }

    private void ShowError(string text)
    {
        _resultText.color = Color.red;
        _resultText.text = $"Failed to connect:\n{text}";
        _timeShowingText = 5f;

        ModLog.Info($"Menu result: {text}");
    }
}

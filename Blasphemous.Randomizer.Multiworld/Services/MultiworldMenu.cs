using Archipelago.MultiClient.Net;
using Blasphemous.Framework.Menus;
using Blasphemous.Framework.Menus.Options;
using Blasphemous.Framework.UI;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Input;
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

    private void OnSubmit()
    {
        Main.Multiworld.Log($"Server {_server.CurrentValue} as {_name.CurrentValue} with pass {_password.CurrentValue}");

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
            string error = $"Failed to connect:\n{string.Join("\n", failure.Errors)}";

            Main.Multiworld.LogError(error);
            ShowTextTimed(error, Color.red, 5f);
            return;
        }

        if (login is LoginSuccessful success)
        {
            // Check for mod status first

            ShowText("Successfully connected", Color.green);
            MenuFramework.ShowNextMenu();
            return;
        }
    }

    private MenuFramework MenuFramework => Main.Multiworld.IsModLoadedName("Menu Framework", out BlasMod mod)
        ? mod as MenuFramework
        : throw new System.Exception("Menu Framework was never loaded");

    // Text results

    private void ShowText(string text, Color color)
    {
        _resultText.color = color;
        _resultText.text = text;
        _timeShowingText = 0;
    }

    private void ShowTextTimed(string text, Color color, float time)
    {
        _resultText.color = color;
        _resultText.text = text;
        _timeShowingText = time;
    }
}

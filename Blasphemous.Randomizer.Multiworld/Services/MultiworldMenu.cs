using Blasphemous.Framework.Menus;
using Blasphemous.Framework.Menus.Options;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Input;
using UnityEngine;

namespace Blasphemous.Randomizer.Multiworld.Services;

public class MultiworldMenu : ModMenu
{
    private TextOption _server;
    private TextOption _name;
    private TextOption _password;

    protected override int Priority { get; } = int.MaxValue;

    public override void OnUpdate()
    {
        if (Main.Multiworld.InputHandler.GetButtonDown(ButtonCode.UISubmit))
            OnSubmit();
        else if (Main.Multiworld.InputHandler.GetButtonDown(ButtonCode.UICancel))
            OnCancel();
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
    }

    private void OnSubmit()
    {
        Main.Multiworld.Log($"Server {_server.CurrentValue} as {_name.CurrentValue} with pass {_password.CurrentValue}");

        Main.Multiworld.APManager.Connect(_server.CurrentValue, _name.CurrentValue, _password.CurrentValue);
        //MenuFramework.ShowNextMenu();
    }

    private void OnCancel()
    {
        MenuFramework.ShowPreviousMenu();
    }

    private MenuFramework MenuFramework => Main.Multiworld.IsModLoadedName("Menu Framework", out BlasMod mod)
        ? mod as MenuFramework
        : throw new System.Exception("Menu Framework was never loaded");
}

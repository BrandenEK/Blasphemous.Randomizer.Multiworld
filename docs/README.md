# Blasphemous Multiworld Client

<img src="https://img.shields.io/github/downloads/BrandenEK/Blasphemous.Randomizer.Multiworld/total?color=39B7C6&style=for-the-badge">

---

## Contributors

***- Programming and design -*** <br>
[@BrandenEK](https://github.com/BrandenEK), [@TRPG0](https://github.com/TRPG0)

***- Artwork -*** <br>
[@TRPG0](https://github.com/TRPG0)

***- Translations -*** <br>
[@ConanCimmerio](https://github.com/ConanCimmerio), [@EltonZhang777](https://github.com/EltonZhang777), [@RocherBrockas](https://github.com/RocherBrockas)

## Connecting to Archipelago
When starting a new save file, a menu will open to prompt you for your connection details.  Enter the required info like this example:
```
Server ip: ap:55858
Player name: Player1
```

## Playing on linux
Unfortunately, this mod is the only blas1 mod that does not work on native linux.  This is due to the game having a *really* outdated version of websockets that does not support encryption.  There is work being done to make a generic fix for this, but no solution yet.  In the meantime, [this information](LINUX.md) may help with getting it running through wine.

## Available commands
- Press the 'backslash' key to open the debug console
- Type the desired command followed by the parameters all separated by a single space
- The 'PageUp' and 'PageDown' keys can be used to scroll through previous messages

| Command | Parameters | Description |
| ------- | ----------- | ------- |
| `ap help` | none | List all available commands |
| `ap status` | none | Display connection status |
| `ap say` | COMMAND | Sends a command to the AP server |
| `ap hint` | ITEM | Sends a hint command for the item to the AP server |

## Installation
This mod is available for download through the [Blasphemous Mod Installer](https://github.com/BrandenEK/Blasphemous.Modding.Installer) <br>
Required dependencies:
- [Modding API](https://github.com/BrandenEK/Blasphemous.ModdingAPI)
- [Menu Framework](https://github.com/BrandenEK/Blasphemous.Framework.Menus)
- [Cheat Console](https://github.com/BrandenEK/Blasphemous.CheatConsole)
- [Randomizer](https://github.com/BrandenEK/Blasphemous.Randomizer)

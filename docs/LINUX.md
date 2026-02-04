# Running Blasphemous AP multiworld on Linux
## Preface
The Blasphemous Archipelago Multiworld Randomizer mod is incompatible with the native Linux build of Blasphemous (accurate as of January 31st 2026, if you're reading this in the distant future that may have changed) due to an incompatible/old version of websockets.

This page serves to document known workarounds or fixes, it assumes you are familiar with how to install mods to BepInEx manually (the gist of it is unzip the mod(s) and put its contents in {$GameDir}/Modded but you are solely responsible for ensuring the directory structure is correct).

This guide will assume some familiarity with the terminal, shell scripts, or custom steam launch options. Nothing advanced, just the basics. Please be aware that I do not own the game outside of steam so the instructions for non-steam installations are **untested**, though they should be correct or at least close enough to give a good starting point.

The windows-exclusive blasphemous mod installer **may** work but I have not tested it, if you wish to try it you can modify the instructions for wine and test. The guide assumes manual mod installation.
 
## Requirements
* A clean copy of Blasphemous (if you already have BepInEx installed, plese remove it. Back up any mods you wish to keep).
* The **windows version** of the BepInEx based modding tools (please refer to the following [document](https://github.com/BrandenEK/Blasphemous.Modding.Installer/blob/main/docs/install/blas1_windows.md) for instructions).
* MW Randomizer mods and its entire dependency chain (provided at the bottom of this document for posterity).

# Proton/Wine:
## Installing the game
The first step is to install the game, if you're using steam you'll want to right click blasphemous in your library and select 'properties'. You should see a popup window for blasphemous. In this popup window, navigate to **compatibility** and check the box saying "Force the use of a specific Steam Play compatibility tool" and select an appropriate proton version, this has been tested on Proton-GE and Proton-10.0-4 but should  work reasonably well on most versions of proton. This will prompt steam to reinstall the windows version of the game if it was already installed (you may manually have to press the **update** button). If the game is not already installed you may install it now.

Follow the instructions for installing the [windows version](https://github.com/BrandenEK/Blasphemous.Modding.Installer/blob/main/docs/install/blas1_windows.md) of the modding tools, and then unzip and install the relevant mods and their dependencies.

If you're **not using steam** then execute the game's installer using wine, ideally installing it to a fresh wineprefix (something along the lines of `WINEPREFIX=~/.blasphemous-wp wine BlasphemousInstaller.exe`). Using proton outside of steam is considered an advanced configuration and instructions will not be provided at this time (proton is pretty deeply linked to steam and using it outside of steam comes with some caveats standard wine does not, it's by no means impossible but I don't want to and I don't have a non-steam copy of the game to try it with anyhow).

Optionally create a shell script in the blasphemous installation directory (the directory where `Blasphemous.exe` is located. You can name the script anything you want but something like `run.sh` or `start-blasphemous.sh` might be a good idea) and open it in a text editor, write the following contents to it and save:

```
#! /bin/env sh

WINEPREFIX=~/.blasphemous-wp wine Blasphemous.exe
```
Important caveat! replace `WINEPREFIX=~/.blasphemous-wp` with the wineprefix you are actually using. If you are just using the default prefix (`~/.wine`) then you may omit the `WINEPREFIX` entirely, giving you `wine Blasphemous.exe`. If you haven't already, open a terminal and `cd` to your blasphemous installation (the directory where `Blasphemous.exe` is located). Flag the shell script as executable using `chmod u+x run.sh`.

<sub><sup>You can of course also execute the game manually from a terrminal with `WINEPREFIX=~/.blasphemous-wp wine Blasphemous.exe` every time if you prefer!</sub></sup>

## Configuration
Congratulations, you should now have a technically correctly modded blasphemous installation. However if you tried to run it you may notice that it's not actually _loading_ your mods yet. That's what we'll seek to rectify now.

If you're using steam you will again want to right click blasphemous in your library and open properties, but this time we'll stay on the first page of the popup window (if you still have the one from before open you may simply navigate to the **general** tab of the properties window).

Locate the text field labeled '**LAUNCH OPTIONS**', this is for the record required for **all windows BepInEx** mods to work on Linux. Wine and proton provide an inbuilt version of `winhttp.dll` so we need to tell it to load the version we installed with BepInEx! We can do this by changing the **LAUNCH OPTIONS** to

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```
If you're not using steam you'll have two options for this step, you can execute `WINEPREFIX=~/.blasphemous-wp winecfg` and configure this graphically as such:

<img width="409" height="444" alt="image" src="https://github.com/user-attachments/assets/259306e0-29f3-4aaf-b1a6-1bdbaf800a11" />

Or you can edit the launch command (we put it in the shell script from earlier!) by adding `WINEDLLOVERRIDES="winhttp=n,b"` to it.

The script should look like this after modification:

```
#! /bin/env sh

WINEPREFIX=~/.blasphemous-wp WINEDLLOVERRIDES="winhttp=n,b" wine Blasphemous.exe
```
<sub><sup>(You can of course launch it manually without a script if you prefer)</sub></sup>

I don't use lutris or any software like it so I _cannot_ provide a runner script for that, though this will likely serve as a good foundation for that if someone wants to do it.

# Native
The mod is currently broken for the native Linux build, this will be updated and moved above Proton/Wine if and when it works and is stable (and if it works but has stability problems it will be updated but remain here), for now this heading exists only for posterity.

## Tips and Tricks.
The game's screen may go black when you tab out or move the cursor outside the game window\*, but worry not! 

This is an issue with a lot of windows games that can be easily fixed on by switching to a different workspace/virtual desktop (on the same screen) and back. This should let the game keep rendering properly. 

This can be fixed permanently by changing the game to **windowed mode**, you should then be able to force the game to borderless fullscreen (how to do that varies with desktop environment/WM/compositor so I'll leave that as an exercise for the reader).

Minimizing may work though I don't use it in my workflow and have not configured my WM to handle that so I cannot easily test it. 

\* at least for some WMs/compositors - positively environment specific, further testing needed

### Miscellaneous
This document is not written or maintained by [@BrandenEK](https://github.com/BrandenEK), instead direct your complaints and issues to [@TheBeardOfTruth](https://github.com/TheBeardOfTruth) Issues for the Linux documentation likely [belong here](https://github.com/TheBeardOfTruth/Blasphemous.Randomizer.Multiworld) on my fork so I get notified faster.

### Dependencies

* [Multiworld Randomizer](https://github.com/BrandenEK/Blasphemous.Randomizer.Multiworld)
* [Randomizer](https://github.com/BrandenEK/Blasphemous.Randomizer)
* [Modding API](https://github.com/BrandenEK/Blasphemous.ModdingAPI)
* [Credits Framework](https://github.com/BrandenEK/Blasphemous.Framework.Credits)
* [Level Framework](https://github.com/BrandenEK/Blasphemous.Framework.Levels)
* [Menu Framework](https://github.com/BrandenEK/Blasphemous.Framework.Menus)
* [UI Framework](https://github.com/BrandenEK/Blasphemous.Framework.UI)
* [Cheat Console](https://github.com/BrandenEK/Blasphemous.CheatConsole)

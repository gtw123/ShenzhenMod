# ShenzhenMod

A mod for [SHENZHEN I/O](http://www.zachtronics.com/shenzhen-io/).

## Features

* Adds a new prototyping area (sandbox) which is four times the size of the regular one.
* Makes the simulation speed slider a bit wider in the sandbox, for more fine-grained speed control.
* (Experimental) Increases the maximum simulation speed for the first three test runs and in the sandbox.
  * Note: This feature sometimes makes the game crash, so it's not enabled by default. See below for more info.
* Supports both the Steam and GoG versions of SHENZHEN I/O.
* Supports Windows, Linux and macOS.

## Installing

### Windows

* First, back up your SHENZHEN I/O save files. This is **strongly** recommended as there is a risk that using this mod may corrupt your save files. Make a copy of `My Documents\My Games\SHENZHEN IO` and put it somewhere safe.
* Download and unzip the latest release from https://github.com/gtw123/ShenzhenMod/releases
* Run ShenzhenMod.exe
* Follow the steps to install the mod.

### Linux

* First, back up your SHENZHEN I/O save files. This is **strongly** recommended as there is a risk that using this mod may corrupt your save files. Make a copy of `$HOME/.local/share/SHENZHEN IO/` and put it somewhere safe.
* Install Mono if you don't already have it. Run `mono --version` to check. Version 5.0 or later is recommended.
* Download and unzip the latest ShenzhenMod release from https://github.com/gtw123/ShenzhenMod/releases.
* Run `mono ShenzhenMod.exe`
* Follow the steps to install the mod.

### macOS

* First, back up your SHENZHEN I/O save files. This is **strongly** recommended as there is a risk that using this mod may corrupt your save files. Make a copy of `~/Library/Application Support/SHENZHEN IO/` and put it somewhere safe.
* Download and install Mono from http://www.mono-project.com/download/stable/. Version 5.10.1 or later is recommended.
* Download and unzip the latest ShenzhenMod release from https://github.com/gtw123/ShenzhenMod/releases.
* Open a Terminal window and change into the unzipped folder.
  * Tip: Type `cd`, then a space, then drag the unzipped folder into the window then press enter.
* Run `mono32 ShenzhenMod.exe macos`
* Follow the steps to install the mod.

## Using the new features in SHENZHEN I/O

* To use the bigger sandbox, look for "Prototyping bigger ideas" in the puzzle list, just below "Prototyping new ideas".
* Use middle-click-drag or alt-drag to scroll around the circuit board.

## Upgrading from an earlier version of ShenzhenMod

No need to uninstall or unpatch first: simply run the new installer!

## Enabling the "Increase max game speed" feature

This feature increases the maximum simulation speed. Unfortunately it can cause the game to crash on certain puzzles, especially in the bigger prototyping area, so it's not enabled by default.

To enable it:
* Edit Shenzhen.exe.config and set "IncreaseMaxSpeed" to true". (If you're building from source, edit App.config instead.)
* Run the installer again.

## Building from source

### Windows

* Clone or download the repo from https://github.com/gtw123/ShenzhenMod
* Install [Visual Studio 2017](https://www.visualstudio.com/downloads/).
* Open ShenzhenMod.sln in Visual Studio.
* Build.

### Linux

* Clone or download the repo from https://github.com/gtw123/ShenzhenMod
* Install `mono-devel`, version 5.0 or later.
* Open the ShenzhenMod folder.
* Build using `msbuild`
* Launch it via `mono bin/Debug/ShenzhenMod.exe`

### macOS

* Clone or download the repo from https://github.com/gtw123/ShenzhenMod
* Download and install Mono from http://www.mono-project.com/download/stable/. Version 5.10.1 or later is recommended.
* Open a Terminal Window and `cd` into the ShenzhenMod folder.
* Build using `msbuild`
* Launch it via `mono32 bin/Debug/ShenzhenMod.exe macos`

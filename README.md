# ShenzhenMod

A mod for [SHENZHEN I/O](http://www.zachtronics.com/shenzhen-io/).

## What does it do?

* Adds a new prototyping area (sandbox) to the game which is four times the size of the normal one.
* Makes the simulation speed slider a bit wider in the sandbox, for more fine-grained speed control.
* (Experimental) Increases the maximum simulation speed for the first three test runs and in the sandbox.
  * Note: This feature can cause the game to crash, so it is not enabled by default. See below.

Supports both the Steam and GoG versions of Shenzhen I/O. Currently supports Windows only.

## Installing

* First, back up your SHENZHEN I/O save files. This is **strongly** recommended as there is a risk that using this mod may corrupt your save files. Your save files are normally located at ```My Documents\My Games\SHENZHEN IO```. Make a copy of this folder and put it somewhere safe.
* Download and unzip the latest release from https://github.com/gtw123/ShenzhenMod/releases
* Run ShenzhenMod.exe
* Follow the steps to install the mod.
* If successful, you can now run the game as usual. Look for "Prototyping bigger ideas" in the puzzle list, just below "Prototyping new ideas".
* Use middle-click-drag or alt-drag to scroll around the circuit board.

## Upgrading from an earlier version

No need to uninstall or unpatch first: simply run the new installer!

## Enabling the "Increase max game speed" feature

This feature increases the maximum simulation speed. Unfortunately it can cause the game to crash on certain puzzles, especially in the bigger prototyping area, so it's not enabled by default.

To enable it:
* Edit Shenzhen.exe.config and set "IncreaseMaxSpeed" to true". (If you're building from source, edit App.config instead.)
* Run the installer again.

## Building from source

* Clone this repo or download the source.
* Install [Visual Studio 2017](https://www.visualstudio.com/downloads/).
* Open ShenzhenMod.sln in Visual Studio.
* Build.

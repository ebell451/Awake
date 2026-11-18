# Awake

> **Note:** This is an new, actively maintained fork of the original [gdegeneve/Awake](https://github.com/gdegeneve/Awake) repository by Gerald Degeneve, which has been inactive since 2018. This fork modernizes the codebase—starting with an upgrade from **.NET Framework 4.0 to .NET Framework 4.8**, with future plans to migrate to the latest **.NET** release.

Awake is a tool that resides in the system tray and prevents the computer from entering the idle state, thus successfully preventing it from automatically entering sleep, hibernation, or the lock screen.

![awake](https://github.com/ebell451/Awake/blob/master/img/awake_forever.png)

### Purpose

Awake was designed for those who cannot change the timings in their power settings because of some enforced policy.
If you have full administrative access to your PC or no Exchange policy or other Group Policy interfering with your power settings / screen lock timings, you do not need Awake.

### How does it work?

Awake does not change any system settings, therefore it does not require administrative privileges. It merely calls Windows API functions to reset the idle timers. This is basically what video players (at least the better ones) do to avoid screen savers and power settings interfering with video playback.

## Installation

* Download the latest <a href="https://github.com">release</a>.
* Put `awake.exe` somewhere you like to keep it.
* Start it and make your selections.

### Build and Test

Build the solution:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" .\Awake.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /nologo
```

Alternatively, build the solution with the .NET SDK:

```powershell
dotnet build .\Awake.sln -c Release -p:Platform="Any CPU"
```
## Latest Features & Updates

### Improvements

* Updated the project from .NET Framework 4.0 to 4.8
* Improved registry, corrupt-state, idle ticket-wraparound, and resource cleanup

### Lock Screen Actions

This feature allows you to define actions when the lock screen (`WIN+L`) is manually entered.

![awake](https://github.com/gdegeneve/Awake/blob/master/img/lock_screen_action.png)

## Acknowledgments

* Original project and concept by [Gerald Degeneve](https://github.com/gdegeneve/Awake).
* Icons by: <a href="http://www.fasticon.com">FastIcon.com</a>
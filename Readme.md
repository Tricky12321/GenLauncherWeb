# GenLauncher Web

This is a project to rewrite the current GenLauncher (.NET Framework / WPF) application into a more cross-platform solution.

***This project is ONLY for the Steam version of the game.*** At this time, there is no intention to support the non-steam version of the game.


This will be done using the following:
* C\# .NET Core backend (webserver)
* Web - Angular frontend
* Electron wrapper for desktop application

This allows the application to be compiled to natively run on Windows, macOS and Linux, for both x86, ARM and more.


# Development
First all the following requirements are needed

## Requirements
* Node Package Manager [npmjs.com](https://www.npmjs.com/)
* .NET Core 8.0 [Download](https://dotnet.microsoft.com/en-us/download)
* `ElectronNET.CLI` [Github](https://github.com/ElectronNET/Electron.NET)

### Optional
* Node Version Manager [nvm](https://nvm.sh)


## Running the application

1. Run `dotnet restore` in the root of the folder
2. Install `yarn` by installing it using npm `npm install -g yarn`
3. Install npm packages
   1. Go to the `GenlauncherWeb/ClientApp` folder and run `yarn install`
4. Build the project from your favorite IDE ([JetBrains - Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/), [Visual Studio Code](https://code.visualstudio.com/))
5. This will run a webserver with the same layout and functionality as in the electron app on `http://localhost:8002` [Link](http://localhost:8002)


# Coding standards
The project relies on a Model, View, Controller, Service (MVCS) structure.


# Build the final Electron app
This requires the installation og the `ElectronNET.CLI`

```dotnet tool install ElectronNET.CLI -g```

Build for platform and architecture:
```
electronize build /target ["win" | "osx" | "linux"] /electron-arch ["ia32" | "x64" | "armv7l" | "arm64" | "mips64el" | "universal"]
```




# Before a first release, the following need to be solved/implemented:
- [x] Implement base install method using symbolic links
- [x] Implement base install method using copy/move (for non symlink systems)
- [ ] Implement install for Generals
- [x] Implement install for Zero Hour
- [ ] Implement a way to detect games (Both ZH and Gen)
- [x] Implement a way to download from S3
- [x] Implement a way to download from Onedrive
- [x] Implement a way to download from Dropbox

## Optional nice-to-have
- [ ] Language translations
- [x] Implement actual download percentage
- [ ] Show download speed
- [ ] Able to change steam game launch options


# Credit

This example of Angular/C#/Electron: [AngularWithDotNetCoreElectronNET](https://github.com/rajeshsuramalla/AngularWithDotNetCoreElectronNET)

GenLauncher by p0ls3r [GenLauncher](https://github.com/p0ls3r/GenLauncher) | [Discord](https://discord.gg/fFGpudz5hV)



# Documentation

All files are downloaded inside the game folder in steam, and placed inside a "mods" folder. 
From here all files will either be copied or symlinked into the main game folder.

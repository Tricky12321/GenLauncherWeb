# GenLauncher Web

This is a project to rewrite the current GenLauncher (.NET Framework / WPF) application into a more cross-platform solution.

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
2. Install npm packages
   1. Go to the `GenlauncherWeb/ClientApp` folder and run `npm install`
3. Build the project from your favorite IDE ([JetBrains - Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/), [Visual Studio Code](https://code.visualstudio.com/))
4. This will run a webserver with the same layout and functionality as in the electron app on `http://localhost:8002` [Link](http://localhost:8002)


# Coding standards
The project relies on a Model, View, Controller, Service (MVCS) structure.


# Build the final Electron app
This requires the installation og the `ElectronNET.CLI`

```dotnet tool install ElectronNET.CLI -g```

Build for platform and architecture:
```
electronize build /target ["win" | "osx" | "linux"] /electron-arch ["ia32" | "x64" | "armv7l" | "arm64" | "mips64el" | "universal"]
```


# Credit

This example of Angular/C#/Electron: [AngularWithDotNetCoreElectronNET](https://github.com/rajeshsuramalla/AngularWithDotNetCoreElectronNET)

GenLauncher by p0ls3r [GenLauncher](https://github.com/p0ls3r/GenLauncher) | [Discord](https://discord.gg/fFGpudz5hV)
# CrimeSimulatorMods

Mods for **Crime Simulator**, built with **BepInEx 5 + Harmony** and packaged for **Thunderstore** (Thunderstore Mod Manager / r2modman). Same setup as `../BoplMods`.

## Requirements

| What | Why | Status on this machine |
|---|---|---|
| .NET SDK 8+ | builds the mods (target is `netstandard2.1`) | installed |
| Crime Simulator (Steam, app 2737070) | game DLLs are referenced from the install folder | `Z:\SteamLibrary\steamapps\common\Crime Simulator` |
| Thunderstore Mod Manager with a Crime Simulator profile + **BepInExPack 5.4.2305** | loads the mods | `Default` profile exists; **install BepInExPack in it** (Online tab → BepInExPack), then start modded once so `BepInEx/` gets created |
| `ilspycmd` (dotnet global tool) | decompiles the game from the CLI | installed |
| `tcli` (dotnet global tool) | builds Thunderstore zips / publishes | installed |
| `tools/dnSpyEx` | GUI decompiler + debugger | copied from BoplMods |
| `tools/UnityExplorer` | in-game inspector of scenes/objects (copy `tools/UnityExplorer/plugins/*` into the profile's `BepInEx/plugins` when needed) | copied, not installed |

Paths differ on another PC? Copy `Directory.Build.user.props.example` to `Directory.Build.user.props` and edit it.

## Everyday workflow

```bash
# 1. build: compiles and copies the DLL into the mod manager profile
dotnet build mods/CaliMods/CaliMods.csproj -c Release

# 2. Thunderstore Mod Manager -> Crime Simulator -> profile "Default" -> "Start modded"

# 3. check the log
#    %APPDATA%\Thunderstore Mod Manager\DataFolder\CrimeSimulator\profiles\Default\BepInEx\LogOutput.log
```

The dev build is deployed to `BepInEx\plugins\<Team>-<Mod>-dev\`. The mod manager doesn't list it as a mod because it wasn't installed through the manager, but BepInEx still loads it. Delete that folder to remove it.

## Making a release

```bash
dotnet build mods/CaliMods/CaliMods.csproj -c Release -t:PackThunderstore
# -> dist/caligola-CaliMods-1.0.0.zip

build-all.cmd   # every mod -> dist/ (no deploy)
```

- **Try it locally like a user would:** Thunderstore Mod Manager → *Settings* → *Import local mod* → pick the zip. Delete the `-dev` folder first, or the mod loads twice.
- **Publish:** upload the zip on <https://thunderstore.io/c/crime-simulator/create/> or run `tcli publish --file dist/<zip> --token <service-account-token>`.
- Bump `<Version>` in the `.csproj` and update `CHANGELOG.md` before every release.

## Mods

| Mod | Description |
|---|---|
| [CaliMods](mods/CaliMods/README.md) | Feature pack with a **Custom Mods** button in the pause menu to toggle each feature. Request item highlight: requested items get a red outline (still needed) or a yellow one (fulfilled), and pickup prompts show `(owned:N)`. Skill leaflet highlight: red outline on leaflets whose skill you already learned. |

## Adding a new mod

1. Copy `mods/CaliMods` to `mods/MyMod`, rename `CaliMods.csproj` → `MyMod.csproj`, delete `bin/` and `obj/`.
2. In the `.csproj` change `AssemblyName`, `RootNamespace`, `BepInExPluginGuid`, `BepInExPluginName`, `Description`, reset `Version`.
3. In `thunderstore.toml` change `name`, `description` and the `build.copy` DLL path.
4. Replace the code, README, CHANGELOG and icon (256×256 PNG).
5. `dotnet sln add mods/MyMod/MyMod.csproj`

To add a feature to CaliMods instead, see "Adding a feature" in [CLAUDE.md](CLAUDE.md).

## Multiplayer note

Crime Simulator co-op uses **Photon Fusion** (shared mode, the host has state authority). Visual or UI-only mods are client-side safe. A mod that changes gameplay state (networked properties, RPCs, spawning) has to account for authority, and usually every player needs it installed.

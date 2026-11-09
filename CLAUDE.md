# CrimeSimulatorMods — Claude guide

BepInEx 5 + Harmony mods for **Crime Simulator** (Unity 2022.3.62f3, Mono backend, Steam app 2737070, exe `Crime Simulator.exe`), distributed through Thunderstore (community `crime-simulator`). Same structure as `../BoplMods`.

## Layout

```
Directory.Build.props     shared settings: netstandard2.1, BepInEx/Harmony NuGet refs, game DLL refs (Assembly-CSharp publicized, UnityEngine modules, UI/TMP/IMGUI, Fusion, SensorToolkit), paths
Directory.Build.targets   DeployToProfile (after every build) + PackThunderstore target (tcli)
Directory.Build.user.props  (optional, git-ignored) per-machine overrides; see *.example
CrimeSimulatorMods.slnx
mods/<ModName>/           one project per mod: .csproj, Plugin.cs, thunderstore.toml, README.md, CHANGELOG.md, icon.png
decompiled/Assembly-CSharp/  ILSpy output of the game (git-ignored, reference only; never ship or commit)
tools/                    dnSpyEx (GUI decompiler/debugger), UnityExplorer (runtime inspector plugin)
dist/                     Thunderstore zips produced by PackThunderstore
```

## Commands

```bash
dotnet build mods/<Mod>/<Mod>.csproj -c Release                       # build + copy DLL into the Default profile
dotnet build mods/<Mod>/<Mod>.csproj -c Release -t:PackThunderstore   # also build dist/<Team>-<Mod>-<Version>.zip
dotnet build -c Release -p:DeployToProfile=false                      # build all, no deploy
build-all.cmd [Mod ...]                                               # build + pack every mod (or the named ones), no deploy
```

- Game: `Z:\SteamLibrary\steamapps\common\Crime Simulator` (`CsGameDir`), managed DLLs in `Crime Simulator_Data\Managed`.
- Deploy target: `%APPDATA%\Thunderstore Mod Manager\DataFolder\CrimeSimulator\profiles\Default\BepInEx\plugins\<Team>-<Mod>-dev\`. The build only warns, without copying, while the profile has no `BepInEx\` folder (BepInExPack must be installed in the profile and the game started modded once).
- Runtime log: `<profile>\BepInEx\LogOutput.log`. Check it after launching to confirm the plugin loaded and patches didn't throw.
- Re-decompile after a game update: `ilspycmd -p -o decompiled/Assembly-CSharp -r "<game>/Crime Simulator_Data/Managed" "<game>/Crime Simulator_Data/Managed/Assembly-CSharp.dll"` (ilspycmd is in `%USERPROFILE%\.dotnet\tools`, call `ilspycmd.cmd` from Git Bash).
- The user tests in game; Claude builds/deploys and reads the log.

## Conventions

- Version lives only in the `.csproj` `<Version>`; namespace (team) only in `ThunderstoreTeam` in Directory.Build.props (`caligola`). `PackThunderstore` passes both to tcli, overriding `thunderstore.toml`.
- Plugin GUID: `com.crimesimmods.<modname>` via `<BepInExPluginGuid>`; code uses the generated `MyPluginInfo`. `[BepInProcess("Crime Simulator.exe")]`.
- `Assembly-CSharp` is referenced with `Publicize="true"`: private game members can be used directly (`__instance.mainHighlight`).
- Extra game DLLs (Rewired, Fusion.Realtime, Assembly-CSharp-firstpass for ES3/Easy Save, ...) aren't referenced yet; add a `<Reference>` in the mod's csproj (or Directory.Build.props if shared) when needed. Compiling against a type deriving from `Fusion.NetworkBehaviour` needs `Fusion.Runtime` (already shared).
- Don't rely on the plugin's own GameObject for `Update`/`OnGUI`/coroutines: create a `DontDestroyOnLoad` host object (see `CustomModsMenu.CreateHost`). Never `UnpatchSelf()` in `OnDestroy`.
- Prefer Prefix/Postfix over Transpilers; per-object mod state in `ConditionalWeakTable<TComponent, TState>`.
- Harmony analyzer (Harmony003) flags assignments to parameters in any method of a `[HarmonyPatch]` class, helpers included: keep helpers that modify struct parameters outside patch classes.
- Each mod's README is the Thunderstore page; keep it user-facing.

## Crime Simulator specifics

- **Networking: Photon Fusion** (shared mode). `[Networked]` properties (e.g. `ItemRequestUI.isCompleted`) throw unless the object is spawned: check `Object != null && Object.IsValid` (and try/catch). `HasStateAuthority` = host for world objects. RPCs are woven methods (`RPC_*`). Visual/UI-only mods are client-side safe.
- `WorldManager.Instance`: hub. `localPlayer` (`FPP_Player`), `loadedMapName` (`"PlayerHideout"`, `"Lobby"`, job maps), `truckItemsSaver` (`SaveTruckItems`, `sensor.Detections` = GameObjects inside the truck), `pickupableHighlightProfile`, `sharedMoney`, `quotaManager`, `bonusManager`, `isMasterClient`.
- Player: `FPP_Player` (huge, ~9k lines). `inventory` (`PlayerInventory.itemSlots`: `ItemSlot.assignedItem`, `amount`, `damagesPerItem`), `inventory.weaponManager.carriedItem` (big `hasToBeCarried` items held in hands), `interactUI` (`InteractUI.SpawnPrompt(key, text, hold, sprite)` / `DestroyPrompts`), `currentDetected` (looked-at GameObject), `pauseMenu`, `pauseActive`, `ActivatePauseMenu()` / `DisablePauseMenu()`.
- Interaction: `FPP_Player.InteractSensorDetected()` rebuilds the prompts when the looked-at object changes (pickup prompt = `GetTranslation("pick up") + ": " + item.GetItemName(true) + ...`, the requested-item link icon = `requestedSprite`). Every frame the looked-at object gets `Highlight.HighlightMe()` (also from `DroneController`), which enables its `HighlightPlus.HighlightEffect` loaded from `WM.pickupableHighlightProfile` (the green outline). Per-object colors: `outlineColor`, `glowHQColor`, `glowPasses[i].color`, then `UpdateMaterialProperties()`.
- Items: `Item` is a ScriptableObject (`itemName`, `value`, `isLoot`, `isMoney`, `isRequested`, `isVIPItem`, `hasToBeCarried`, `GetItemName(colored)`). World instance = `Pickupable` (`assignedItem`, `currentDamage`, `addAmount`, `blockPickup`), GameObjects tagged `"Pickupable"`.
- Item requests (computer "Item requests" app): `Computer.Instance.requests` (`RequestPage`) → `allRequests` (`ItemRequestUI`, one per reward row: `requests` = list of `ItemRequest.requestedItem`, `isCompleted` networked, `requiredDLC`/`dlcNotOwned`, `requiredQuality`, `pref` = ES3 save key, `Claim()`). Host's `Computer.CheckRequestedItems()` sets `Item.isRequested` on every client via `RPC_SetRequestedItems` (open requests only). `ItemRequestUI.Refresh()` decides "has item": inventory, then truck sensor (or whole scene if `WM.scanWholeSceneForItemRequests`). `RequestCategoryButton` shows "done/total" per category.
- Pause menu: `PauseMenu` (`pages` list, `pages[0]` = main page, `ActivatePage(n)` with -1 = hide all, `quitGameButton`, `PlayClick()`, `layoutRefresh`, `ReturnOrCloseMenu()` on ESC: goes back to page 0 if it's hidden). Buttons are uGUI `Button` + TextMeshProUGUI + I2 `Localize` + `ButtonInformation` (hover text).
- Localization: I2 (`I2.Loc.LocalizationManager.GetTranslation(term)`), in Assembly-CSharp.
- Input: Rewired (`player.rewiredPlayer.GetButtonDown("Use"/"Interact"/...)`). Custom UI cursor: `UICursorManager` (active while paused), so IMGUI clicks work in the pause menu.
- Unity log appears in BepInEx log as `[Info : Unity Log]`.

## Mods

- `mods/CaliMods` — the user's feature pack (GUID `com.crimesimmods.calimods`).
  - `Core/FeatureRegistry.cs`: `FeatureRegistry.Register(config, name, description)` → `ModFeature` (master `Enabled` config entry, section = feature name) + `AddOption(...)` sub-toggles; `feature.IsOn(option)`. Config file `BepInEx/config/com.crimesimmods.calimods.cfg`.
  - `UI/PauseMenuButton.cs`: on `FPP_Player.ActivatePauseMenu` (postfix in `UI/PauseMenuPatches.cs`) clones `pauseMenu.quitGameButton` (or another main-page button) into the same parent, named `CaliMods_CustomModsButton`, label "Custom Mods" (I2 `Localize` removed, `ButtonInformation` hover text replaced, onClick replaced), placed just before the last sibling button. Logs the template path and whether the parent has a LayoutGroup.
  - `UI/CustomModsMenu.cs`: IMGUI window on a DontDestroyOnLoad host, scaled from 1080p. Opening hides the pause pages (`ActivatePage(-1)`) so clicks can't reach them; "Back"/ESC (game's `ReturnOrCloseMenu` → `ActivatePage(0)`) and closing the pause menu close it. Lists every registered feature automatically.
  - `Core/ItemOutline.cs`: the only `Highlight.HighlightMe` postfix. Features register `OutlineRule`s with `ItemOutline.AddRule(OutlinePriority.X, rule)`; rules run lowest priority first and the first returning true handles the item (color null = game's green). Recolors the looked-at object's HighlightEffect outline (+ glow) at full strength (`outline` 1 vs profile 0.75), originals kept in a ConditionalWeakTable and restored; logs the profile values once (game profile: outline 0.75 green, glow 0).
  - Feature "Request item highlight" (`Features/RequestItemHighlight`): `RequestTracker` (status None/Needed/Fulfilled per `Item`: Needed = in an open request and owned < copies required by all open requests; Fulfilled = owned enough, or only in completed requests; falls back to `Item.isRequested` when no `Computer`; owned = undamaged copies (damage <= `MaxDamage` config, default 50: `ItemSlot.damagesPerItem` per stacked copy, `WeaponManager.carriedItemDamage` while `isCarrying`, `Pickupable.currentDamage`) in inventory slots + carried item + truck sensor + every `Pickupable` in the hideout; cached 0.5 s, invalidated on every `InteractSensorDetected`), `OutlineColor` rule (priority `ItemRequests`: `NeededColor`/`FulfilledColor`), `PromptPatches` (prefix/finalizer on `InteractSensorDetected` + prefix `InteractUI.SpawnPrompt`: appends ` (owned:N)` to the "pick up: " prompt).
  - Feature "Skill leaflet highlight" (`Features/SkillLeafletHighlight`): rule priority `SkillLeaflets` (before requests) claims every `Item.isSkillBook` while on; red (`LearnedColor`) when `localPlayer.skillManager.CheckIfSkillLearned(item.skillLearnID)`, otherwise green. When off, leaflets fall through to the request rule. The owned count prompt still applies.

## Adding a feature to CaliMods

1. New folder `Features/<Feature>/` with a static `<Feature>Feature.Register(ConfigFile)` that calls `FeatureRegistry.Register` / `AddOption` (and binds any extra config).
2. Call it from `Plugin.Awake` before `PatchAll`. It appears in the Custom Mods window automatically.
3. Outline colors: register an `OutlineRule` via `ItemOutline.AddRule` (add a constant to `OutlinePriority`) instead of patching `Highlight` again. Other patches in `[HarmonyPatch]` classes in that folder; check `Feature.IsOn(option)` at the start of each patch, so toggling takes effect immediately.
4. Document it in `mods/CaliMods/README.md`, `CHANGELOG.md` and the Mods section above.

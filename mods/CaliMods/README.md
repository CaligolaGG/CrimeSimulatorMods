# CaliMods

Quality-of-life features for **Crime Simulator**. Every feature can be turned on or off in game: open the pause menu (ESC) and click **Custom Mods**.

## Features

### Request item highlight

The computer's **Item requests** page lists items you can collect for extra rewards. With this feature on:

- **Red outline**: you're looking at an item an open item request still needs.
- **Yellow outline**: the item is in an item request that's already fulfilled. Either you claimed the request, or you already own enough copies for it.
- Other items keep the normal green outline.
- **Owned count**: every pickup prompt shows how many copies of that item you already own:
  `[E] Pick up: Cheap toaster (owned:1)`

"Owned" means undamaged copies in your inventory, in your hands and inside your truck. In your hideout it also counts every undamaged copy lying around. Damaged copies don't count.

You can turn the outline and the owned count on or off separately. In `BepInEx/config/com.crimesimmods.calimods.cfg` you can change the colors (`NeededColor`, `FulfilledColor`) and how much damage a copy can have and still count (`MaxDamage`, in %, default 0).

### Skill leaflet highlight

- **Red outline** on a skill leaflet whose skill you've already learned, so you know reading it is useless. Leaflets for skills you don't have yet keep the normal green outline.
- Leaflets ignore the item request colors above. The owned count still shows on their pickup prompt.
- When this feature is off, leaflets are treated like any other item.

The color can be changed in the config (`LearnedColor`).

## Multiplayer

All features are client-side visuals. Only the players who install the mod see them, and other players don't need it.

## Installation

Install with Thunderstore Mod Manager / r2modman (requires BepInExPack).

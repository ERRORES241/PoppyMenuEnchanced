# PoppyMenuEnchanced (v2.7.0)

An enhanced in-game cheat and debug menu for Risk of Rain 2. Press Insert to open it.

A personal project, open source under the MIT license. Pull requests and ideas are welcome.

## Features

Tabs sit on an icon rail on the left, and some tabs have sub-pages. Each setting is a row with a one-line description under its name. The search box at the top finds any setting on any tab. There are three themes (Midnight, Violet, RoR2 gold), and the menu scales with screen height.

### Aimbot
- Silent aim: shots go to the target, the camera stays where you point it. Works with hitscan and projectiles on every survivor.
- Optional hold key. Without one, the aimbot is on whenever it's enabled.
- Target priority: closest to crosshair, closest, lowest HP, highest HP. Options for bosses first, sticky target, line of sight only, range and an FOV limit with an on-screen circle.
- Railgunner: while scoped, aims at the red weak points.
- Magic bullet: your projectiles turn toward the target and your shots go through walls. Host or solo.
- No spread and no recoil.

### Visuals
- ESP through walls for enemies, interactables, the teleporter and portals.
- Interactables can be filtered: chests and terminals, shrines, printers, scrappers, drones and turrets, barrels, everything else.
- Labels show the price in the game's own units. Shrines that don't cost money show no price.
- Void Potentials and Void Cradles get their own purple markers.
- DLC objects: the Wok and Aurelionite Heart (DLC2); Drone Scrapper, Drone Assembly Station, Power Pedestal, power vaults, Solus Heart and Access Codes nodes (DLC3); Wake, Wake offerings and stops, Trial Gong, Hex and Drain altars (DLC4).
- Names come from the game, so they follow the game language. Items from Gorakh's Quality mod show their quality tier.
- Custom camera FOV.

### Movement
- Flight and no-clip. No-clip is flight that also passes through walls and floors.
- Always sprint. It doesn't cut off attacks and doesn't run while you stand still.
- Bunny hop: hold Jump to jump again the moment you land. Wax Quail and H3AD-5T v2 still work.
- Jump pack: Space throws you upward, mid-air too.
- Teleport on every ping, or once to your current ping or to the spot under the crosshair. Both can be bound to a key.

### Player
Three sub-pages for your own character:
- **Player:** god mode, semi-god mode (never below 1 HP), no skill cooldowns, heal, hurt, respawn. Money, experience and lunar coins with an amount you set. Buffs with a duration, damage over time effects, remove all buffs.
- **Stats:** multipliers for damage, attack speed, move speed and max health, bonus armor and crit chance, and a live readout of your current stats.
- **Items:** item and equipment picker with tier colors, DLC4 key items, give all items. Stack (like a Shrine of Order), reroll within each tier, clear, and undo for the last of these. No equipment cooldown. A fixed number of drops for chests opened with Sale Star.

### World
Five sub-pages:
- **World:** freeze the game, freeze the run timer, game speed. Kill all enemies, no enemies, no experience gain, don't save profile. Shrine of Chance that always pays and never closes.
- **Teleporter:** instant charge (also Void Fields cells, Void Locus signals and moon pillars), skip stage, add a Mountain Shrine.
- **Spawn:** monsters and interactables where you aim, with a count and an option to spawn them on your team.
- **Run:** stages cleared, run time, team level, change stage, artifacts.
- **Players:** for each player in the lobby: heal, hurt, kill, teleport to them or bring them to you, change team, give items. The host can kick and ban.

### Fun
Railgunner only:
- **Instant ult charge:** the special is ready to fire with no charge-up.
- **No ult cooldown:** refills the special and skips the 5 s overheat. Other skills keep their cooldowns.
- **Every shot is an ult:** primary fire shoots Supercharge rounds, scoped or not.

### Settings
- **Configs:** save which features are on along with aimbot, ESP and movement settings. Load, overwrite, rename, and pick one to load at startup.
- **Keybinds:** bind any feature or macro to a key or mouse button, side buttons included. Keybinds work while the menu is closed.
- **Settings:** menu key, theme, resolution scaling and a size multiplier, reduce motion, the active list, multiplayer permissions, reload lists, reset window position.
- **Macros:** a list of steps (items, buffs, money, spawns, features, other macros) run in one go, from the menu or a key.

## Controls
- The menu opens with `Insert`. The key can be changed in Settings.
- Drag the top bar to move the window and the dots in the bottom-right corner to resize it.
- Type in the search box to find a setting. Esc clears it.

## Multiplayer
Anything that changes the game state (items, spawns, freezing the game) needs the host, so it works for the host or in solo. Visuals and movement run locally. By default, players without host rights can't use the menu in your game; Settings has a switch for that.

## Installation
Drop `PoppyMenuEnchanced.dll` into `Risk of Rain 2/BepInEx/plugins` or install via r2modman / Thunderstore Mod Manager.

## Credits
Poppy Menu is maintained by Poppy.
*Note: Recent menu restructuring and enhancement updates were developed with AI assistance.*

Based on code and architecture from Umbra Menu (Aquatic Labs), Spektre Menu (BennettStaley), and Lodington's fork.
Inspired by DebugToolkit (harbingerofme) and Aerolt (Lodington).
Decompiled game code analysis made possible with **JetBrains dotPeek**.
Menu icons from [Lucide](https://lucide.dev) (ISC license, see `THIRD_PARTY_NOTICES.md`).

# PoppyMenuEnchanced (v2.7.1)

An enhanced in-game cheat and debug menu for Risk of Rain 2. Press Insert to open it.

A personal project, open source under the MIT license. Pull requests and ideas are welcome.

## Features

Tabs sit on an icon rail on the left, and some tabs have sub-pages. Each setting is a row with a one-line description under its name. The search box at the top finds any setting on any tab. There are three themes (Midnight, Violet, RoR2 gold), and the menu scales with screen height.

Right-click a row to add it to Featured or to bind it to a key, mouse side buttons included. Keybinds work while the menu is closed. Right-click a tab icon to bind a key that opens that tab. Rows marked HOST only work for the host or in solo.

### Featured
The tab the menu opens on: the rows you added from other pages, what is on right now, and a button to turn everything off.

### Player
- **Character:** god mode, semi-god mode (never below 1 HP), no fall damage, no skill cooldowns, no equipment cooldown. Heal, hurt, respawn. Buffs with a duration, damage over time effects, remove all buffs.
- **Movement:** flight and no-clip (flight that also passes through walls and floors). Always sprint, which doesn't cut off attacks and doesn't run while you stand still. Bunny hop: hold Jump to jump again the moment you land; Wax Quail and H3AD-5T v2 still work. Jump pack: Space throws you upward, mid-air too. Teleport on every ping, or once to your current ping or to the spot under the crosshair.
- **Stats:** multipliers for damage, attack speed, move speed, max health and regen, bonus armor and crit chance (up to 100%), and a live readout of your current stats. Money and experience multipliers for the whole team.

### Items
- **Give:** item and equipment picker with tier colors and a rarity filter; quality tiers from Gorakh's Quality mod open as a sublist. Remove a set number of any item you carry. DLC4 key items, give all items. Money, experience and lunar coins with an amount you set. No experience gain.
- **Inventory:** stack (like a Shrine of Order), reroll within each tier, clear, and undo for the last of these. A fixed number of drops for chests opened with Sale Star.

### Combat
- **Aimbot:** silent aim: shots go to the target, the camera stays where you point it. Works with hitscan and projectiles on every survivor. Optional hold key; without one, the aimbot is on whenever it's enabled. Target priority: closest to crosshair, closest, lowest HP, highest HP. Options for bosses first, sticky target, line of sight only, range and an FOV limit with an on-screen circle.
- **Weapons:** no spread, no recoil, magic bullet (your projectiles turn toward the target and your shots go through walls). For Railgunner: the aimbot aims at the red weak points while scoped, instant ult charge, no ult cooldown (other skills keep theirs), and every shot is an ult (primary fire shoots Supercharge rounds, scoped or not).

### Visuals
- ESP through walls for enemies, interactables, the teleporter and portals.
- Interactables can be filtered: chests and terminals, shrines, printers, scrappers, drones and turrets, barrels, everything else.
- Labels show the price in the game's own units. Shrines that don't cost money show no price.
- Void Potentials and Void Cradles get their own purple markers.
- DLC objects: the Wok and Aurelionite Heart (DLC2); Drone Scrapper, Drone Assembly Station, Power Pedestal, power vaults, Solus Heart and Access Codes nodes (DLC3); Wake, Wake offerings and stops, Trial Gong, Hex and Drain altars (DLC4).
- Names come from the game, so they follow the game language. Items from Gorakh's Quality mod show their quality tier.
- Custom camera FOV.

### World
- **Stage:** teleporter instant charge (also Void Fields cells, Void Locus signals and moon pillars), add a Mountain Shrine, spawn all portals, skip stage, go to any scene. Shrine of Chance that always pays and never closes, Shrine of Blood that takes no health.
- **Enemies:** spawn monsters and interactables where you aim, with a count and an option to put them on your team. No enemies, kill all enemies.
- **Run:** freeze the game, freeze the run timer, game speed. Stages cleared, run time, team level, artifacts.

### Lobby
Who may use the menu in your game, and for each player in the lobby: heal, hurt, kill, teleport to them or bring them to you, change team, give items. The host can kick and ban.

### Settings
- **General:** menu key, theme, resolution scaling and a size multiplier, reduce motion, the active list, pause while the menu is open (solo), don't save profile, reload lists, reset window position.
- **Macros:** a list of steps (items, buffs, money, spawns, features, other macros) run in one go, from the menu or a key. Three built-in loadouts: mid-game, end-game and movement items.
- **Configs:** save which features are on along with aimbot, ESP and movement settings. Load, overwrite, rename, and pick one to load at startup.

## Controls
- The menu opens with `Insert`. The key can be changed in Settings.
- Right-click a row for Featured and keybinds.
- Ctrl+click a slider or a number box to type a value.
- Drag the top bar to move the window and the dots in the bottom-right corner to resize it.
- Type in the search box to find a setting. Esc clears it.

## Multiplayer
Anything that changes the game state (items, spawns, freezing the game) needs the host, so it works for the host or in solo. Visuals and movement run locally. By default, players without host rights can't use the menu in your game; Lobby has a switch for that.

## Installation
Drop `PoppyMenuEnchanced.dll` into `Risk of Rain 2/BepInEx/plugins` or install via r2modman / Thunderstore Mod Manager.

## Credits
Poppy Menu is maintained by Poppy.
*Note: Recent menu restructuring and enhancement updates were developed with AI assistance.*

Based on code and architecture from Umbra Menu (Aquatic Labs), Spektre Menu (BennettStaley), and Lodington's fork.
Inspired by DebugToolkit (harbingerofme) and Aerolt (Lodington).
Decompiled game code analysis made possible with **JetBrains dotPeek**.
Menu icons from [Lucide](https://lucide.dev) (ISC license, see `THIRD_PARTY_NOTICES.md`).

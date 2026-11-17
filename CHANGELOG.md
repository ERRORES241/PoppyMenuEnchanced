# Changelog

## 2.7.0
- Updated for the October 2026 game update and DLC4 (GameLibs 1.5.0-r.1, MMHOOK 2026.10.8).
- Fixed input capture for the new buffered input system; the removed equipment cycle inputs no longer break every physics tick.
- Added DLC4 support: key item giver, ESP markers for Wake, Trial Gong, Hex and Drain altars, labels for the Cursed, Page and Hex item tiers and the new cost types.
- Kill All Enemies and No Enemies now cover the Lunar and End of the Cycle teams.
- Inventory Undo, Stack and Reroll use permanent item counts, restore the key item, and only undo for the player that was changed.
- Fixed Respawn doing nothing while dead.
- Fixed a macro that calls itself crashing the game.
- Fixed Time Scale overriding the pause menu.
- Infinite Shrine of Chance Uses now works without the 100% win rate toggle.
- No Spread no longer removes spread from enemy shots when hosting; aimbot now also covers bullets fired through BulletAttack.Fire(FireArgs).
- BunnyHop only applies to the local player.
- Railgunner Fun features only apply to Railgunner, and Ult Spam no longer switches the other two toggles on permanently.
- FOV Changer restores the default FOV when turned off.
- God Mode no longer overrides the game's god state every frame.
- Saved configs are cached instead of being read from disk every frame.
- ESP shows the DLC2 Wok and Aurelionite Heart and the DLC3 Drone Scrapper, Drone Assembly Station and Power Pedestal. Scrapper, drone and other filters now also apply to void potentials, geodes and the Drone Combiner.
- 100% Shrine of Chance lets the game roll the drop, so Chance Doll jackpots and their effects work again.
- Give All Items skips void items (they converted every regular item) and objective items. Stack and Reroll follow the game's Shrine of Order rules and leave hidden, objective and power shape items alone.
- Instant Charge also fills Void Fields cells, Void Locus signals and moon pillars, and no longer sends a request every frame while the teleporter is idle.
- Semi-Godmode survives void implosions and damage boosted after armor.
- Teleporter ESP is now Teleporter & Portals: it also marks every portal (shop, gold, celestial, void, Colossus, Solus, Trial, ChampRush and others). The Other filter adds DLC3 power vaults, the Solus Heart offering and Access Codes nodes.
- Settings are now saved when the menu closes and when the game quits. Before, the config file was only written when a preset was loaded, so most toggles were lost on restart.
- The config and presets now also keep the ESP category filters, aimbot priority and target highlight, Give count, Infinite Shrine of Chance, Instant Charge, No Enemies, Lock Experience, Prevent Profile Saving and the Railgunner Fun toggles. Presets additionally keep the FOV changer, fly speed and Sale Star settings. Presets saved by older versions load without resetting these.
- Saving the config writes the file once instead of once per setting, and sliders no longer write it on every frame while dragged.
- Macro and Home steps store item, equipment and buff names instead of catalog indexes, which shift when the game or mods add content. Steps saved before keep working through the old index.
- Macros have a stable id: renaming a macro no longer breaks its keybind, Home shortcut or calls from other macros. Existing references are migrated on load. A macro file with missing steps or names no longer breaks the Macros tab.
- Renaming a preset happens on the Rename button and refuses a name that is already taken. Before, every keystroke renamed the file and could overwrite another preset.
- Instant Ult Charge alone no longer refills the Railgunner ult; only No Ult Cooldown and Ult Spam do.
- Keybinds and the menu key no longer fire while typing in chat or the console. Rebinding the menu key no longer closes the menu right away.
- Max Health multiplier no longer drains health on every stat recalculation; barrier and hull follow the new max.
- Flight follows the game's movement bindings and gamepad, flies up on Jump without spending jumps, and hands gravity back to the game when turned off instead of forcing it on.
- Remove All Buffs leaves hidden internal buffs alone.
- Disable everything now also turns off BunnyHop, teleport on ping, Semi-Godmode, No Enemies, Lock Experience, No Spread, No Recoil, the Railgunner Fun toggles, Instant Charge, Infinite Shrine of Chance, Sale Star and the FOV changer.
- Zoom items gives the items by definition, so it works in any game language.
- Spawn all portals spawns every destination portal around you right away (shop, gold, celestial, void, deep void, Colossus, Prime Meridian, Solus shop, Eye, Access Codes, Trial, ChampRush), loading the spawn cards on demand.
- Go to scene lists only stages and intermissions; menus and cutscenes that broke the run are gone.
- Spawn ignores trigger volumes and your own body when picking the spawn point. Macro spawn steps store the exact spawn card and can spawn allies.
- ESP refreshes buttons and cleansing pools on every new stage, including a new run on the same first stage. A failing ESP section no longer hides the others.
- Removed the unused character change (Character module, body change operation and macro step).
- Less garbage per frame: picker lists are built when opened, menu styles and the tooltip texture are cached, Magic Bullet reuses its collider buffer, ESP filters no longer lowercase names.
- ESP labels support the Quality mod: the quality icon tag in item names is shown as (Uncommon), (Rare), (Epic) or (Legendary) instead of raw markup, and other text markup is removed.
- Aimbot config defaults now match the code (60 degree cone, 300 m range).
- Spawn picks the exact spawn card instead of the first one with the same name (snowy and sandy shrine variants, void chests).
- New menu design: an icon rail for the tabs, a top bar with the page path, search and close, sub-pages as segments, and settings as rows with the description under each name instead of hover tooltips. Counters and their action share one row (for example Money with its Give button).
- Three themes in Settings: Midnight, Violet and RoR2 gold. They replace the accent color sliders.
- The menu scales with the screen height (1080p is 100%) on top of the Menu size multiplier. Text and icons are drawn at the final size, so they stay sharp instead of being stretched.
- Search box in the top bar finds settings and actions on every tab by name or description. Esc clears it.
- Uses the Segoe UI system font when available and Lucide icons (ISC license, see THIRD_PARTY_NOTICES.md).
- Item, buff and spawn pickers draw only the visible rows, so long lists scroll smoothly, and the search field is focused when a picker opens.
- Stat multiplier sliders show only while their toggle is on.
- Aimbot priority is picked from a dropdown list. Action rows end with a run button instead of an arrow; rows that open a list keep the arrow.
- The aimbot FOV circle is drawn with more, anti-aliased segments that meet without overlapping, so it no longer looks jagged.

## 2.6.0
- Added Teleport to Ping (auto-teleport on ping, teleport to active ping, and teleport to cursor).
- Fixed aimbot targeting non-hostile environmental objects (such as Sky Meadow floating rocks and props).
- Added 100% Shrine of Chance win rate and infinite shrine uses.
- Added configurable Sale Star drop multiplier.
- Restructured menu layout, added native BunnyHop, and updated packaging.
- Removed built-in console tab and console command integration.

## 2.2.3
- Added a short note about AI use and a link to the source repo on GitHub.
- Cleaned up the readme and in-game wording.

## 2.2.2
- Presets can be set to "Load on startup". Mark one as your default and it applies automatically the first time you load into a game each session, so reopening the game restores your setup. Shown with a STARTUP badge.

## 2.2.1
- Homing projectiles are now part of the aimbot itself. Whenever the aimbot has a target, your projectiles curve onto it, no separate toggle. Magic Bullet stays a toggle.

## 2.2.0
- Macros are now fully custom. A step can be give a specific item x N, give a specific buff for N seconds, money/XP/lunar +N, spawn N of something, become a body, heal, run a console command, run a feature, or even run another macro.
- New "My Shortcuts" on Home: pin features and items from search, or turn on Customize to build your own buttons that do any of the above. Reorder and remove them. It's your dashboard.
- Same builder powers macros and Home, so anything you can put in a macro you can also pin to Home and bind to a key.

## 2.1.0
- Home search: type to find any feature or item and run it right from the box.
- Favorites: pin any feature to Home, and every feature shows its bound key inline.
- Custom macros: build a named sequence of features and bind it to one key (new Macros page under Settings).
- Spawn options: pick how many to spawn and whether they're your ally or an enemy.
- Preset sharing: Export copies a preset to a code on your clipboard, Import adds one from a pasted code.
- Undo: restore your inventory to before the last Give All, Clear, Stack, or Reroll.

## 2.0.3
- Give All Items now grants the highest-quality version of each item instead of the base. Items with quality variants (the duplicate-per-quality kind some mods add) get their best one, so if a Legendary exists, that's what you get.
- It gives one item per name now (the best), so it's lighter than dumping every quality.

## 2.0.2
- Give All Items now only hands out normal beneficial items. It skips Lunar downside items (Shaped Glass and the like), untiered/hidden entries, and world-unique items, so it stops killing you, and it heals you to full afterward just in case.
- That also cuts the lag a lot, since it grants far fewer items, though giving hundreds of modded items at once will still hitch.

## 2.0.1
- Item list cleanup: stripped raw markup from item, equipment, and body names (no more "<sprite name=...>" showing).
- Quality variants of the same item now group into one row that opens a sub-list of qualities, with a back option.
- Fixed the picker popup resize so it tracks the mouse like the main window instead of dropping on a fast drag.

## 2.0.0
The big one: merged in the feature sets of two great open mods (DebugToolkit by harbingerofme, Aerolt by Lodington), reimplemented from scratch and credited. Now one build with everything, and the project is open source under MIT.

- New Run tab: set stages cleared, set run time, set team level, toggle every artifact mid-run.
- New Players tab: per-player heal, revive, hurt, kill, teleport, set team, give items, and change body.
- New Console tab: short commands (give, spawn, kill_all, no_enemies, and more) with a searchable reference, plus a fallback to the game's own console.
- New safety toggles in World: no enemies, lock experience, prevent profile saving, and buddha mode in Player.
- Combat tools: heal and hurt by amount, timed buffs, and inflict any damage-over-time effect.
- ESP customization: custom colors per category, a max-distance filter, marker size, and name/distance/health toggles.
- Macros on Home: mid-game, end-game, and a movement-item zoom set.
- Collapsed the dev/full split into a single build with everything included.
- Universal keybinds: bind any feature to any key or mouse button from a new Keybinds tab, saved to disk.
- Customizable menu accent color in Settings, and the tab sidebar now scrolls.
- Reorganized 17 flat tabs into 9 grouped tabs with sub-navigation (Player holds Combat/Aimbot/Stats/Move, World holds Time/Spawn/Run/Teleport, Settings holds Settings/Keybinds/Presets).
- All DebugToolkit command names work in the normal console (give_item, spawn_ai, no_enemies, and the rest).
- Smoother window resize: grabbing the corner now follows the mouse even on a fast drag.
- Aimbot is silent-aim only now; removed the camera-move mode and its smoothing.
- Tidied the in-menu help text so it reads like a person wrote it.

## 1.0.0 Poppy Menu (first release)
A ground-up rewrite and rebrand of the old Umbra Menu for current Risk of Rain 2.

- Aimbot tab: silent aim for any survivor, homing projectiles, magic-bullet wall-pierce, FOV/range/line-of-sight/priority, and a hold key (mouse buttons included).
- No-clip, world time controls (freeze match, freeze timer, time scale), and presets with auto-grant on spawn.
- Host-permission gate so clients can't use the mod in your game unless you allow it.
- A clickable window: draggable, resizable, with a sidebar, a Home dashboard, switch-style toggles, searchable tier-colored item pickers, a status pill, and an active-effects readout while it's closed.
- On-screen popups, an in-game Settings tab with live key rebinding, confirm guards on destructive actions, and a disable-all panic button.
- Cursor and input are handled cleanly while the menu is open, so no flicker and no camera spin.
- Now a BepInEx 5 plugin instead of an injected DLL: no injector, no admin, auto-loads, and it coexists with other mods.
- Reads everything live from the game catalogs, so modded items, equipment, buffs, and bodies show up on their own.
- Works as a non-host client too, routing server actions over R2API networking.

### Features
- Player: God Mode, Infinite Skills, Aimbot, Give Money / XP / Lunar Coins, Respawn, give/remove buffs.
- Stats: per-stat multipliers (damage, attack speed, move speed, armor, crit, max health) + live readout.
- Movement: Flight, Always Sprint, Jump Pack.
- Items: give any item/equipment (searchable, tier-colored), give-all, clear, stack (Shrine of Order), reroll, no equipment cooldown.
- Spawn: spawn any monster/interactable at your crosshair, kill all enemies.
- Teleporter: instant charge, skip stage, mountain shrine stack, spawn shop/gold/celestial portals.
- Character: change into any body (survivors and monsters).
- ESP: enemies, interactables, and the teleporter through walls.

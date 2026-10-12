# Info Pack

See how many of an item you have at home, what a trader's food does to you, how good a brew is, which loot is only worth scrapping, and stop emptied containers from staying highlighted.

Survival Log knows a lot it never puts on screen. You can't see how many planks are already in your storages while you trade, what a trader's food will do to your stats before you buy it, or which brew tier a bottle is. Info Pack adds that to the game's own tooltips and windows, marks junk that is only good for scrap, and lets containers you have already emptied look like plain furniture. It only shows things. It never changes an item, a stat or a save.

## Features

- **Home counts**: item tooltips in the backpack, the trade window and the cooking window get a line `At home: N`: how many of that item are in your bag, in every storage at home (hidden ones included) and in the workbench drawer, added together. That is the same pool the game crafts from. The number refreshes every two seconds.
- **Trade stats**: in the trade window, the tooltip of an item on the trader's shelf says what it does to satiety, mood, energy, health and life, in the game's own wording (for example `Satiety +12, Energy +3`). Items that change no stat get no line.
- **Brew quality**: the tooltip of a brewed drink says where it sits among what its barrel makes, as `Brew quality: 2 of 3` (1 is the lowest). While you set up a brew, before it starts, the brew panel shows the batch's quality score and the score each better tier starts at, for example `Quality 0.62: Normal from 0.40, Strong from 0.80`.
- **Scrap only**: items only worth breaking down get a line `Only useful as scrap` in their tooltips, a small `Scrap` chip on their backpack slots and a `Scrap` tag in the pickup list.
- **Cooking codex**: in the cooking window, hovering an ingredient adds its description and stats to the game's tooltip. Hovering a recipe does the same for the dish it makes.
- **Hide looted highlights**: on explore maps the game colours a container's outline by what you know about it when you hold Alt or point at it. Containers you have searched and emptied lose that colour and look like any other furniture. Containers you have not searched, and ones with items still in them, keep the game's colour.
- **A log file**: what the mod did and anything that went wrong is written to `BepInEx\info-pack.log`.

Every piece has its own setting, so you can turn off the ones you don't want. The tooltip lines draw inside the game's own windows, in the game's own tooltip style.

There are no hotkeys and no buttons. Everything shows up in tooltips and windows the game already has.

Info Pack adds no data to your save and does not edit game files. You can remove it at any time.

## Replaces ItemTotals

The home count does the job of the separate ItemTotals mod. If you have ItemTotals installed, remove `ItemTotals.dll` from `BepInEx\plugins`, or item tooltips will show two count lines: ItemTotals' `Total: N` and this mod's `At home: N`. If you would rather keep ItemTotals, set `BaseCounts` to `false` instead (see [CONFIG.md](CONFIG.md)).

## How it works

**Home counts** add up your bag, every storage on your home map and its home group, hidden ones included, and the workbench drawer. A storage is never counted twice.

**Scrap only** marks an item only when all of these hold:

- the game can shred it;
- no workbench or cooking recipe uses it;
- it is a material, an electrical part, a daily good or has no category;
- it has no other use (not food, not usable, not fuel, not brewable, not a seed, trap or furniture);
- it is worth no more in trade than what shredding it gives back.

The rule leans towards leaving an item unmarked: when in doubt, it is not called scrap.

**Brew quality** reads the brew tiers the game itself uses for each barrel. The tier names in the brew panel come from the game, in your language.

**The pages** in the game's windows keep these additions across reloads: the mod puts them back whenever the game reloads its web view.

## Known issues and limits

Tooltips and windows are finicky. The game draws them as web pages and reloads them often, and this mod adds lines to them. If a line is missing, wrong, shows twice, or a tooltip looks broken, please tell me (see [Feedback](#feedback-and-bug-reports)), even if you are not sure the mod caused it. If one piece misbehaves, turn just that one off in the settings.

- The ingredient lines under a recipe in the cooking window are the game's own and get no counts.
- The scrap rule does not know about wishes, aid demands, robot modules or trap bait, so an item only those ask for can still be marked as scrap. Check before you shred something rare. Anything the game burns as fuel counts as useful and is never marked.
- Items on the trader's side that the game has not loaded yet get no lines in the trade tooltip. Items you sell can take a moment to show their lines the first time.
- The game places its tooltips for a fixed size. A tooltip made taller by these lines can reach past the bottom of the screen near the lower edge.
- The brew panel shows the quality score only while a brew is being set up. Once the barrel is brewing it is hidden.
- An emptied container still gets the plain outline every piece of furniture gets while Alt is held, and the outline when you point at it. Only the search colour goes.
- The mod's own text is in English. The stat wording, item descriptions and brew tier names come from the game, in your language.
- `BepInEx\info-pack.log` is never trimmed. Delete it now and then if it grows large.

## Requirements

- Survival Log 1.1.18317. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Not together with ItemTotals, unless you turn off `BaseCounts` (see [Replaces ItemTotals](#replaces-itemtotals)).

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Download `InfoPack.dll` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-InfoPack/releases/latest) and put it in the `BepInEx\plugins` folder inside the game folder (the folder with the game .exe). Full path example: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\InfoPack.dll`
3. If you use ItemTotals, delete `ItemTotals.dll` from `BepInEx\plugins`.

## Uninstall

Delete `InfoPack.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.infopack.cfg` and `BepInEx\info-pack.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.infopack.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Built against BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for a line starting `===== Info Pack 0.2.0 started`. It lists which pieces are on. The lines after it say whether the looted highlight hook went in, and when the item facts were built for a loaded game (how many items, which language, and how long it took). A game table that could not be read, stat wording that fell back to English, or a page piece that failed each get one line.

The same lines are in `BepInEx\info-pack.log`, which survives restarts.

If no lines show up in tooltips, look in the log for a line starting `could not`. If every setting is off, the log says so and the mod does nothing.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-InfoPack/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\info-pack.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/InfoPack -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\InfoPack\bin\Release\InfoPack.dll`.

The unit tests cover the pure rules (home counts, stat lines, brew tiers, the scrap rule and the looted highlight) and need nothing from the game:

```
dotnet test tests/InfoPack.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).

# Changelog

## 0.8.16

- The tooltips Info Pack shows over another mod's window take that window's size when the window is drawn bigger or smaller with the UI size setting of Exiled's Toolkit 0.10.0 or newer. A change shows at once.
- Tooltips on the game's own screens keep the game's size.
- Made for Survival Log 1.1.19611.

## 0.8.15

- Less stutter while you play: Info Pack does much less work each frame and skips its updates when nothing has changed.
- No more hitch when a save loads or you change the game's language. Info Pack now gathers its item information a little at a time.
- Tooltips and the stat bars are redrawn only when what they show has changed.
- Made for Survival Log 1.1.19611.

## 0.8.14

- Less stutter: the home is counted once per check instead of twice, the count and the live tooltips no longer run in the same frame, and foods you have already eaten are not looked up again.
- Made for Survival Log 1.1.19611.

## 0.8.13

- Other mods' item rows can add a line under the item's name on Info Pack's tooltip. Neighbor Basket uses it to show the progress an item adds.
- Made for Survival Log 1.1.19611.

## 0.8.12

- Effects chips use the game's own effect numbers on the Backpack and Trade tooltips.
- When Info Pack shows its Effects or At home section, the game's own effect rows and home lines on the same tooltip are hidden. With a section set to Off, the game's lines show as before.
- The game's category, description and uses rows are restyled to match Info Pack's sections.
- Made for Survival Log 1.1.19611.

## 0.8.11

- The At home section shows on every tooltip again, without holding Shift or Ctrl. Ctrl still adds the per-storage list.
- Made for Survival Log 1.1.19131.

## 0.8.10

- At home counts, Used in counts and the days of food on the satiety bar leave out what is staged in the neighbor's basket.
- Made for Survival Log 1.1.19131.

## 0.8.9

- Fixed: on the workbench and in other mods' panels, an item with nothing to show until Shift was held got a tooltip with an empty middle. It now shows the item's name and the Shift hint only.
- Made for Survival Log 1.1.19131.

## 0.8.8

- Shorter tooltips: only Effects and Freshness show at first. Holding Shift or Ctrl shows the rest (At home, Used in, Growing and the notes now wait for the key too). Ctrl still adds the per-storage list. Each section can be set back to `Always` in the settings.
- Made for Survival Log 1.1.19131.

## 0.8.7

- Item tooltips show in Storage Hub's panel too (its item list, detail, rotting and keep lists), and show above the panel instead of under it.
- Made for Survival Log 1.1.19131.

## 0.8.6

- On the cooking screen, recipe and food tooltips now open with the name in gold, with the game's other lines (ingredients, what is missing, click hints) dimmed under it. The game's plain stat line is left out because the Effects section already shows those stats.
- Made for Survival Log 1.1.19131.

## 0.8.5

- Fixed: holding Shift or Ctrl did not expand or change a tooltip on Storage Hub's Home tab.
- Other mods can now show a tooltip on any button or label, plain text or an item found by its name. Crafting Hub uses this for its materials, bell and amount buttons.
- Made for Survival Log 1.1.19131.

## 0.8.4

- Item tooltips show when you hover an item on Storage Hub's Home tab, beside a station or the bag. Other mods can get the same tooltip on their own item lists.
- Made for Survival Log 1.1.19131.

## 0.8.3

- New tooltip look, matching New Loop Plus, More Rewards and Cooking Hub: a warm brown panel with an amber line along the top, amber section headings, and round green and red stat chips.
- Freshness shows a bar of how much fresh time is left, coloured green, amber or red.
- Dishes under Used in show their stats as chips instead of a line of text.
- Fixed: fuel burn times and brew rules ignored your talents when a save was loaded before your character appeared.
- Fixed: polluted food showed its time left twice.
- Fixed: station tooltips could stay hidden after letting go of the mouse outside the window.
- Made for Survival Log 1.1.19131.

## 0.8.2

- Item tooltips now work while a station is open: the fuel generator, heater, brewing barrel, workbench, shredder, compressor, dye vat, material rack and rat cage. Items the station shows no tooltip for get one of their own.
- The HUD stat bar lines (how fast a stat drops, when it runs out, how long the food at home lasts) also show when the Stats UI mod replaces the game's stat tooltip.
- Made for Survival Log 1.1.19131.

## 0.8.1

- Dishes under Used in are sorted by what eating them gives in total (satiety, mood, energy, health and life added up), best first, so the three rows shown by default are the best dishes.
- Made for Survival Log 1.1.19131.

## 0.8.0

- Dishes under Used in: each dish an item cooks into now shows its cook time, like `Cooking, 15m`, and what eating it does on a second line, like `+30 Satiety, +5 Mood`.
- How long fuel burns: under Fuel, an item that burns shows how long one lasts in a cooking pot, a heater and a fuel generator, like `Cooks for 1h 51m` and `Runs a generator for 2d 2h`. Where the game has more than one model with different rates, it shows the range. Fuel saving talents count as they were when the world loaded.
- The Chinese brewing lines now use the game's own word for mood.
- Made for Survival Log 1.1.19131.

## 0.7.0

- Brewing under Used in: an item that goes in a brewing barrel now shows the drink a barrel of only that item makes and its quality, like `Brewed alone, quality 0.10`, and what each one adds to any brew, like `In a barrel: 10 satiety, +1 mood each; 3 per bottle`. Quality is the batch's total mood over its total satiety, so a high-mood item lifts a mix and a negative-mood one drags it down. The thresholds and bottle size include your brewing talents as they were when the world loaded.
- Made for Survival Log 1.1.19131.

## 0.6.0

Info Pack now tells you what the game wants from you, and how long your stats and food last:

- Craving: the tooltip of the food you are craving says `You're craving this`.
- Relief requests: an item a relief request still needs says how many more, like `Relief request needs 3 more`, and gets a small gold star on its backpack slot.
- Never tried: food and drink you have never had say `Never tried`.
- Robot modules and trap bait: a robot module's tooltip shows its level, and a bait item shows what kind of bait it is, under Used in.
- Stat bars: hovering a stat bar on the HUD shows how much it drops per game hour and about how long until it is empty. It shows after about half a game hour of play.
- Food forecast: hovering the satiety bar also shows about how many days the food at home lasts you.

Other changes:

- Items a relief request asks for are no longer marked as scrap.
- New Features settings: `Wanted`, `NeverTried`, `StatRates` and `FoodForecast`, all on by default.
- Made for Survival Log 1.1.19131.

## 0.5.0

Item tooltips work like a small wiki now. Below the game's own lines, Info Pack adds one tidy block split into sections, each with a short heading:

- Effects: what the item does to satiety, mood, energy, health and life, green for gains and red for losses.
- Freshness: how long until it spoils, how long it keeps when fresh and what it rots into.
- At home: how many you have at home. Hold Ctrl to see which storages hold them.
- Used in: the workbench recipes, dishes and brewing that use it, and how many you can make now with what you have at home.
- Growing: for seeds, the grow time, harvest window, light need, cold resistance, yield and seed chance.
- Hold Shift for more: value and weight, what it is made from, what it breaks down into, its burn value as fuel, where it can be found and its description.

Other changes:

- New Tooltip settings: when each section shows (always, only while Shift is held, or never), the order of the sections, the two keys, keeping tooltips on screen and spoil badges.
- Tall tooltips no longer run off the bottom of the window.
- Food that spoils within a day gets a small coloured corner on its slot.
- Made for Survival Log 1.1.19131.

## 0.4.1

- After a game update, Mod Manager can tell you if this mod may need an update.
- Made for Survival Log 1.1.19131.

## 0.4.0

- Mod Manager's Mods screen links to this changelog.
- Settings changed on the Mod Manager's Mods screen take effect at once. Turning a feature off takes away what it drew on screen.
- Mod Manager's Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings you change on the Mods screen take effect right away. Without it, they take effect the next time you start the game.

## 0.3.2

- The mod no longer touches container outlines, because the game's own grey outline for searched containers works better.
- The `HideLootedHighlights` setting is removed. If your config file still lists it, it does nothing and can be deleted.

## 0.3.1

- First release.
- Shows what the game knows but never puts on screen: how many of an item you have at home, a trader's food stats, brew quality, scrap-only loot, ingredient info while cooking and how long planning cards last.
- Emptied containers stop staying highlighted.

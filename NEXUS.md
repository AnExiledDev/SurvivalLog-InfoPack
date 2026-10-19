Name: Info Pack
Summary: Shows what the game knows but never puts on screen: how many of an item you have at home, a trader's food stats, brew quality, scrap-only loot, ingredient info while cooking and how long planning cards last. Emptied containers stop staying highlighted. 显示游戏知道却从不显示的信息：一件物品家里有多少、商人食物的属性、酒的品质、只适合回收的物品、烹饪时的食材信息，以及规划卡牌的持续时间。搜空的容器不再保持高亮。

Description (BBCode):
Survival Log knows a lot it never shows you. Info Pack adds that information to the game's own tooltips and windows, in the game's own style. It only shows things; it never changes an item, a stat or a save.

[size=5]Main features[/size]
[list]
[*][b]Home counts[/b]: item tooltips in the backpack, trade and cooking windows show "At home: N", counting your bag, every storage at home and the workbench drawer.[/*]
[*][b]Trade stats[/b]: see what a trader's item does to satiety, mood, energy, health and life before you buy it.[/*]
[*][b]Brew quality[/b]: a brewed drink's tooltip shows its tier, like "Brew quality: 2 of 3". While setting up a brew, the brew panel shows the quality score and what the next tier needs.[/*]
[*][b]Scrap only[/b]: items only worth breaking down get an "Only useful as scrap" line, a Scrap chip on backpack slots and a Scrap tag in the pickup list.[/*]
[*][b]Cooking codex[/b]: hover an ingredient or recipe in the cooking window to see its description and stats.[/*]
[*][b]Planning badges[/b]: cards on the Survival Planning screen get a badge on their picture. Timed cards show how long they last, like "3d" or "24h"; cards that take effect once show "1x". Permanent cards get none. A few cards have a second stage the badge does not count, such as Knuckle Down, which gives 3 Survival Points a day for five days and then takes 1 a day for five more.[/*]
[/list]
Every piece can be turned off on its own in the config. There are no hotkeys.

[size=5]Replaces ItemTotals[/size]
The home count does the job of ItemTotals. If you have ItemTotals installed, remove [font=Courier New]ItemTotals.dll[/font], or tooltips will show two count lines. To keep ItemTotals instead, turn off BaseCounts in the config.

Info Pack adds no data to your save and does not edit game files. You can remove it at any time.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]The scrap rule does not know about wishes, aid demands, robot modules or trap bait. Check before you shred something rare.[/*]
[*]A tooltip made taller by these lines can reach past the bottom of the screen near the lower edge.[/*]
[*]The brew panel score shows only while a brew is being set up, not once it is brewing.[/*]
[*]Ingredient lines under a recipe in the cooking window get no home counts.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.18902. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit]Exiled's Toolkit[/url] 0.4.0 or newer, which applies changed settings at once. Info Pack does not load without it.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Install Exiled's Toolkit the same way: download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\InfoPack.dll[/code][/*]
[*]If you use ItemTotals, delete [font=Courier New]ItemTotals.dll[/font] from the same folder.[/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]InfoPack.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To turn off any piece, edit [font=Courier New]BepInEx\config\com.dev.survivallog.infopack.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-InfoPack/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese).

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or leave a comment here. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\info-pack.log[/font].

[size=5]Changelog[/size]
[list]
[*]0.3.2: The hide looted highlights setting is removed. The game's own grey outline for searched containers works better, so the mod no longer touches container outlines. If your config file still lists HideLootedHighlights, it does nothing and can be deleted.[/*]
[*]0.3.1: The mod's own tooltip lines and tags now show in Chinese when the game is in Chinese, and the setting descriptions use the same words. Built for game version 1.1.18902.[/*]
[*]0.3.0: New planning badges: cards on the Survival Planning screen show how long they last ("3d", "24h") or "1x" for a one-time effect, so timed and permanent cards are easy to tell apart. On by default; turn off PlanningBadges in the config to hide them.[/*]
[*]0.2.2: The setting descriptions are shorter and plainer, and each one now has a Simplified Chinese line. No setting or default changed.[/*]
[*]0.2.1: Hide looted highlights is turned off by default after a crash on expeditions, and reworked to hook a safer spot. If you used 0.2.0, your config file still has it on: set HideLootedHighlights to false to turn it off.[/*]
[/list]

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-InfoPack]github.com/AnExiledDev/SurvivalLog-InfoPack[/url]

[line]
[size=5]简体中文[/size]

Survival Log 知道很多信息，却从不显示给你。Info Pack 把这些信息加进游戏自己的提示和窗口里，风格和游戏一致。它只显示信息，从不改动物品、属性或存档。

[size=5]主要功能[/size]
[list]
[*][b]家中数量[/b]：背包、交易和烹饪界面的物品提示会显示「家中共有：N」，统计背包、家里所有储物家具和工作台抽屉里的数量。[/*]
[*][b]交易属性[/b]：购买前就能看到商人的物品对饱腹、心态、精力、健康和生命的影响。[/*]
[*][b]酒的品质[/b]：酿出的酒在提示里显示档次，例如「酿造品质：第 2 档，共 3 档」。准备酿造时，酿酒面板会显示品质分数，以及下一档需要多少分。[/*]
[*][b]只适合回收[/b]：只值得拆解的物品会多一行「只适合回收」，背包格子上显示「回收」角标，拾取列表里带上「回收」标记。[/*]
[*][b]烹饪图鉴[/b]：在烹饪界面悬停食材或菜谱，可以看到它的描述和属性。[/*]
[*][b]规划角标[/b]：生存规划界面的卡牌图片上会显示角标。有时限的卡牌显示持续时间，例如「3d」或「24h」；只生效一次的卡牌显示「1x」。永久生效的卡牌没有角标。少数卡牌有角标不计算的第二阶段，例如「短期激励」，前五天每天给 3 点生存点，之后五天每天扣 1 点。[/*]
[/list]
每一项都可以在配置中单独关闭。没有快捷键。

[size=5]取代 ItemTotals[/size]
家中数量的功能和 ItemTotals 相同。如果装了 ItemTotals，请删除 [font=Courier New]ItemTotals.dll[/font]，否则提示里会出现两行数量。如果想保留 ItemTotals，请在配置中关闭 BaseCounts。

Info Pack 不会往存档里写入任何内容，也不会修改游戏文件。你可以随时移除它。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]回收规则不了解愿望、援助需求、机器人模块和陷阱诱饵。粉碎稀有物品前请先确认。[/*]
[*]靠近屏幕底部时，因多出这些行而变高的提示可能超出屏幕。[/*]
[*]酿酒面板的分数只在准备酿造时显示，开始酿造后就不再显示。[/*]
[*]烹饪界面里菜谱下方的食材行不显示家中数量。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.18902。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit]Exiled's Toolkit[/url] 0.4.0 或更新版本，用于立即应用改动过的设置。没有它，Info Pack 无法加载。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]用同样的方式安装 Exiled's Toolkit：下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\InfoPack.dll[/code][/*]
[*]如果装了 ItemTotals，请从同一文件夹中删除 [font=Courier New]ItemTotals.dll[/font]。[/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]InfoPack.dll[/font]。

[size=5]配置[/size]
如需关闭任何一项，可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.infopack.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-InfoPack/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在这里留言。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\info-pack.log[/font]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-InfoPack]github.com/AnExiledDev/SurvivalLog-InfoPack[/url]

Name: Info Pack
Summary: Turns Survival Log's item tooltips into a small wiki: effects, freshness, home counts, what an item is used in and made from, and where it is found. Flags the food you crave, what relief requests still need and food you never tried. HUD stat bars show how fast they drop and how long the food at home lasts. Trade stats and brew quality too.

Description (BBCode):
Survival Log knows a lot it never shows you. Info Pack adds that information to the game's own tooltips and windows, in the game's own style. It only shows things; it never changes an item, a stat or a save.

[size=5]Main features[/size]
[list]
[*][b]Tooltip wiki[/b]: item tooltips in the backpack, storage, trade and cooking windows get one tidy Info Pack block below the game's own lines, split into sections: Effects (green for gains, red for losses), Freshness (time left, shelf life, what it rots into), At home, Used in (with how many you can make now, and each dish's cook time and what eating it does, best dishes first), and Growing for seeds. Hold Shift for Value, Made from, Breaks down into, Fuel (with how long it burns in a cooking pot, heater or generator), Found in and About. Sections with nothing to say are left out.[/*]
[*][b]Where is it?[/b]: hold Ctrl over an item to see which storages at home hold it and how many each has.[/*]
[*][b]What the game wants[/b]: the food you are craving says "You're craving this", and an item a relief request still needs says how many more, with a gold star on its backpack slot. Food and drink you have never had say "Never tried". Robot modules show their level and bait its kind.[/*]
[*][b]Stat bars[/b]: hover a stat bar on the HUD to see how much it drops per game hour and about how long until it is empty. The satiety bar also shows about how many days the food at home lasts you.[/*]
[*][b]Spoil badges[/b]: food about to spoil gets a small coloured corner on its slot: red within a day, amber within three days once half its shelf life is gone.[/*]
[*][b]Tooltips stay on screen[/b]: tall tooltips no longer run off the bottom of the window.[/*]
[*][b]Home counts[/b]: item tooltips in the backpack, storage, trade and cooking windows show an At home section, counting your bag, every storage at home and the workbench drawer.[/*]
[*][b]Trade stats[/b]: see what a trader's item does to satiety, mood, energy, health and life before you buy it.[/*]
[*][b]Brew quality[/b]: a brewed drink's tooltip shows its tier, like "Brew quality: 2 of 3". While setting up a brew, the brew panel shows the quality score and what the next tier needs. Brewable items show the drink and quality a barrel of only that item gives, and the satiety and mood each adds to a brew.[/*]
[*][b]Scrap only[/b]: items only worth breaking down get an "Only useful as scrap" line, a Scrap chip on backpack slots and a Scrap tag in the pickup list.[/*]
[*][b]Cooking codex[/b]: hover an ingredient or recipe in the cooking window to see its description and stats.[/*]
[*][b]Planning badges[/b]: cards on the Survival Planning screen get a badge on their picture. Timed cards show how long they last, like "3d" or "24h"; cards that take effect once show "1x". Permanent cards get none. A few cards have a second stage the badge does not count, such as Knuckle Down, which gives 3 Survival Points a day for five days and then takes 1 a day for five more.[/*]
[/list]
Every piece can be turned off on its own in the config. Each tooltip section can show always, only while Shift is held, or never, in the order you choose. The two keys, Shift for more and Ctrl for where, can be changed in the config.

[size=5]Replaces ItemTotals[/size]
The home count does the job of ItemTotals. If you have ItemTotals installed, remove [font=Courier New]ItemTotals.dll[/font], or tooltips will show two count lines. To keep ItemTotals instead, turn off BaseCounts in the config.

Info Pack adds no data to your save and does not edit game files. You can remove it at any time.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]The scrap rule does not know about cravings, robot modules or trap bait. Check before you shred something rare. Items a relief request asks for are never marked.[/*]
[*]A relief request that asks for an amount of any food or material, rather than one item, gets no line or star.[/*]
[*]The food forecast counts only you, not other people at home, and leaves out spoiling and cooking.[/*]
[*]The brew panel score shows only while a brew is being set up, not once it is brewing.[/*]
[*]The brewing lines and fuel burn times use the numbers from when the world loaded. A brewing or fuel saving talent picked later counts after you load the save again.[/*]
[*]Ingredient lines under a recipe in the cooking window get no home counts.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19131. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\InfoPack.dll[/code][/*]
[*]If you use ItemTotals, delete [font=Courier New]ItemTotals.dll[/font] from the same folder.[/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]InfoPack.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To turn off any piece, edit [font=Courier New]BepInEx\config\com.dev.survivallog.infopack.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-InfoPack/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese). Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\info-pack.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Check out my [url=https://sldb.core-directive.com/en/]Survival Log wiki[/url] for items, crafting, maps and more.

[size=5]Changelog[/size]
[list]
[*]0.8.6: The cooking screen's tooltips start with the name in gold above the details, with no plain copy of the stats.[/*]
[*]0.8.5: Shift and Ctrl work on Home tab tooltips; Crafting Hub's buttons and materials get tooltips.[/*]
[*]0.8.4: Item tooltips show on Storage Hub's Home tab too, beside a station or the bag.[/*]
[*]0.8.3: A new look for the tooltip: a warm brown panel, amber section headings, round stat chips, a freshness bar and stat chips under each dish. Talents count in fuel and brew numbers right after a save loads.[/*]
[*]0.8.2: Tooltips work while a station is open (generator, heater, brewing barrel, workbench and the rest), and the HUD stat bar lines also show in the Stats UI mod's tooltip.[/*]
[*]0.8.1: Dishes under Used in are listed best first, by their stats added up.[/*]
[*]0.8.0: Dishes under Used in show their cook time and what eating them does, and fuel shows how long it burns in a cooking pot, a heater and a fuel generator.[/*]
[*]0.7.0: Brewable items show, under Used in, the drink and quality a barrel of only that item gives, and the satiety and mood each adds to a brew.[/*]
[*]0.6.0: Tooltips say when an item is the food you are craving, how many more a relief request needs (with a star on the slot), and when you have never tried a food. Robot modules show their level and bait its kind. Hover a HUD stat bar to see how fast it drops and when it runs out, and the satiety bar for how many days the food at home lasts. Items relief requests ask for are no longer marked as scrap.[/*]
[*]0.5.0: Item tooltips work like a small wiki, split into sections: effects, freshness, home count, what the item is used in, growing, and on Shift its value, what it is made from and breaks down into, its fuel value, where it is found and its description. Hold Ctrl for which storages hold an item. Each section can show always, only on Shift or never, in the order you choose. Tooltips stay on screen, and food about to spoil gets a badge on its slot.[/*]
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
[*][b]提示图鉴[/b]：背包、储物、交易和烹饪界面的物品提示，会在游戏自带内容下方加上一块整齐的 Info Pack 信息，分为几栏：「效果」（增加为绿色，减少为红色）、「保质期」（剩余时间、新鲜时的保质期、会腐烂成什么）、「家中」、「用途」（以及现在能做几份、每道菜的烹饪时间和食用效果，效果最好的菜排在前面）和种子的「种植」。按住 Shift 可查看「价值」、「获取方式」、「可分解为」、「燃料」（以及在灶具、取暖器和发电机中能烧多久）、「搜寻来源」和「描述」。没有内容的栏目不会显示。[/*]
[*][b]放在哪里[/b]：在物品上按住 Ctrl，可以看到家里哪些储物家具放着它，以及各有多少。[/*]
[*][b]游戏想要什么[/b]：你正想吃的食物会显示「正想吃这个」；救助委托还需要的物品会显示还差几个，背包格子上还有一颗金色星星。从没吃过的食物和饮品会显示「从没尝过」。机器人模块显示等级，诱饵显示种类。[/*]
[*][b]状态条[/b]：悬停界面上的状态条，可以看到它每个游戏小时下降多少，以及大约多久后耗尽。饱腹条还会显示家中食物大约够你吃几天。[/*]
[*][b]腐败角标[/b]：快要腐败的食物，格子上会显示一个彩色小角标：一天内腐败为红色，三天内腐败且保质期已过半为橙色。[/*]
[*][b]提示不出屏幕[/b]：变高的提示不会再超出窗口底部。[/*]
[*][b]家中数量[/b]：背包、储物、交易和烹饪界面的物品提示会显示「家中」一栏，统计背包、家里所有储物家具和工作台抽屉里的数量。[/*]
[*][b]交易属性[/b]：购买前就能看到商人的物品对饱腹、心态、精力、健康和生命的影响。[/*]
[*][b]酒的品质[/b]：酿出的酒在提示里显示档次，例如「酿造品质：第 2 档，共 3 档」。准备酿造时，酿酒面板会显示品质分数，以及下一档需要多少分。可酿造的物品会显示只用它酿出的酒和品质，以及每个能为酒桶加多少饱腹和心态。[/*]
[*][b]只适合回收[/b]：只值得拆解的物品会多一行「只适合回收」，背包格子上显示「回收」角标，拾取列表里带上「回收」标记。[/*]
[*][b]烹饪图鉴[/b]：在烹饪界面悬停食材或菜谱，可以看到它的描述和属性。[/*]
[*][b]规划角标[/b]：生存规划界面的卡牌图片上会显示角标。有时限的卡牌显示持续时间，例如「3d」或「24h」；只生效一次的卡牌显示「1x」。永久生效的卡牌没有角标。少数卡牌有角标不计算的第二阶段，例如「短期激励」，前五天每天给 3 点生存点，之后五天每天扣 1 点。[/*]
[/list]
每一项都可以在配置中单独关闭。每个提示栏目都可以设为一直显示、仅在按住 Shift 时显示或不显示，顺序也可以自己定。两个按键（Shift 查看更多，Ctrl 查看存放位置）可以在配置中更改。

[size=5]取代 ItemTotals[/size]
家中数量的功能和 ItemTotals 相同。如果装了 ItemTotals，请删除 [font=Courier New]ItemTotals.dll[/font]，否则提示里会出现两行数量。如果想保留 ItemTotals，请在配置中关闭 BaseCounts。

Info Pack 不会往存档里写入任何内容，也不会修改游戏文件。你可以随时移除它。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]回收规则不了解想吃的食物、机器人模块和陷阱诱饵。粉碎稀有物品前请先确认。救助委托要的物品不会被标记。[/*]
[*]要求任意某类食物或材料数量的救助委托，不会显示提示行或星星。[/*]
[*]食物预估只算你自己，不算家里的其他人，也不计腐坏和烹饪。[/*]
[*]酿酒面板的分数只在准备酿造时显示，开始酿造后就不再显示。[/*]
[*]酿造信息和燃料燃烧时间使用读档时的数值。之后才选的酿造或节省燃料天赋，要重新读档后才会计入。[/*]
[*]烹饪界面里菜谱下方的食材行不显示家中数量。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19131。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\InfoPack.dll[/code][/*]
[*]如果装了 ItemTotals，请从同一文件夹中删除 [font=Courier New]ItemTotals.dll[/font]。[/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]InfoPack.dll[/font]。

[size=5]配置[/size]
如需关闭任何一项，可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.infopack.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-InfoPack/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。没装 Exiled's Toolkit 的话，在「模组」界面的改动改为在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\info-pack.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
也欢迎看看我做的 [url=https://sldb.core-directive.com/zh/]Survival Log 维基[/url]，收录物品、制作、地图等资料。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-InfoPack]github.com/AnExiledDev/SurvivalLog-InfoPack[/url]

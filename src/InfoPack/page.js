// Info Pack's web half. Runs in Root.html, the document every game page iframe sits in. It only reads: it adds a
// small wiki block to the game's own item tooltips (backpack and storage, trade, shop bag, cooking, every station's bag), keeps those
// tooltips inside the window, puts Scrap chips and spoil corners on backpack slots, a tag on the pickup list, a quality
// readout on the brew panel and duration badges on Survival Planning cards, all in the game's own tooltip palette.
// The tooltip block is split into sections (effects, freshness, home count, recipes, sources...) that the player
// shows always, only while the detail key is held, or never. The game pages come and go as iframes and Vue redraws
// them at will, so every addition is checked on a short poll and on changes to the tooltip, and put back when gone.
// Talks to the plugin through vuplex.postMessage("slmod|infopack|<verb>") and is driven by window.__slinfopack.
(function () {
  if (window.__slinfopack) return;

  var PREFIX = 'slmod|infopack|';
  var POLL_MS = 250;
  var INSTANCE_RETRY_MS = 3000;
  var WHERE_RETRY_MS = 2000;
  var COOKING_TIP_WIDTH = '300px';
  // The station pages show an item's tooltip half a second after the cursor stops on it; ours waits as long.
  var STATION_TIP_DELAY_MS = 500;
  var LIST_ROWS = 3;
  // Some materials go into hundreds of recipes; even with the detail key held the tooltip has to fit the window.
  var DETAIL_LIST_ROWS = 12;
  // The game offsets its tooltips 15 page pixels from the cursor; EDGE keeps a moved one off the window's border.
  var CURSOR_GAP = 15;
  var EDGE = 4;
  var SECONDS_PER_DAY = 86400;

  // The tooltip takes the look of the other Exiled mods (New Loop Plus, More Rewards, Cooking Hub): a warm brown
  // panel with an amber line along its top, amber section headings with a bar before and a fading line after, and
  // round green and red stat chips. The game's own tooltip is restyled only while it holds our block.
  var PANEL = 'background:radial-gradient(ellipse at 12% 0%,rgba(150,100,40,.28),transparent 60%),linear-gradient(180deg,#3a3129,#2b241d 55%,#221c16);' +
    'border:1px solid rgba(220,188,120,.3);border-radius:5px;box-shadow:0 18px 50px rgba(0,0,0,.75),0 0 0 1px rgba(0,0,0,.6),inset 0 1px 0 rgba(255,230,180,.08)';
  var HAIRLINE = "content:'';position:absolute;top:0;left:0;right:0;height:2px;border-radius:5px 5px 0 0;background:linear-gradient(90deg,transparent 8%,#ffc83a 50%,transparent 92%);opacity:.75;pointer-events:none";
  var HOSTS = ['.tooltip', '#recipeTooltip', '#itemTooltip', '#itemTip', '#fuelTooltip', '#tooltip'];
  var HOST_PANEL = HOSTS.map(function (host) { return host + ':has(>.slip-tip)'; }).join(',');
  var HOST_LINE = HOSTS.map(function (host) { return host + ':has(>.slip-tip)::before'; }).join(',');

  var CSS = [
    HOST_PANEL + ',.slip-own-tip{' + PANEL + '}',
    HOST_LINE + ',.slip-own-tip::before{' + HAIRLINE + '}',
    '.slip-tip{display:flex;flex-direction:column;min-width:190px;max-width:289px;margin-top:5px;padding-top:8px;border-top:1px solid rgba(220,188,120,.16);font-size:11.75px;line-height:1.5;font-weight:normal;color:#cbbfa8;text-align:left;white-space:normal}',
    '.slip-sec{display:flex;flex-direction:column;gap:2px}',
    '.slip-sec+.slip-sec{margin-top:7px;padding-top:7px;border-top:1px solid rgba(220,188,120,.11)}',
    '.slip-h{display:flex;align-items:center;gap:7px;margin-bottom:3px;font-size:10.5px;font-weight:700;line-height:14px;letter-spacing:2.2px;text-transform:uppercase;color:#ffc83a;text-shadow:0 0 10px rgba(255,200,58,.35)}',
    ".slip-h::before{content:'';flex:none;width:3px;height:11px;background:#ffc83a;box-shadow:0 0 6px rgba(255,200,58,.35)}",
    ".slip-h::after{content:'';flex:1;height:1px;background:linear-gradient(90deg,rgba(255,200,58,.25),transparent)}",
    '.slip-row{display:flex;align-items:baseline;gap:8px}',
    '.slip-txt{flex:1 1 auto;min-width:0;overflow-wrap:break-word}',
    '.slip-meta{flex:none;font-size:11px;color:#8f826c;white-space:nowrap}',
    '.slip-subline{display:block;font-size:11px;line-height:1.35;color:#a89a82}',
    '.slip-where{padding-left:10px;color:#a89a82}',
    '.slip-dim{color:#a89a82}',
    '.slip-good{color:#b5e09a}',
    '.slip-bad{color:#ff9a90}',
    '.slip-amber{color:#ffc83a}',
    '.slip-fresh{color:#b5e09a}',
    '.slip-warn{color:#e0b070}',
    '.slip-more{font-size:11px;color:#a89a82}',
    '.slip-about{color:#a89a82;line-height:1.5}',
    '.slip-ref{color:#f0e8d8;font-weight:600}',
    '.slip-ico{display:inline-block;width:18px;height:18px;object-fit:contain;vertical-align:-5px;margin-right:4px;filter:drop-shadow(0 1px 1px rgba(0,0,0,.7))}',
    '.slip-noico{background:rgba(255,240,210,.08);border-radius:3px}',
    '.slip-n{color:#a89a82;margin-left:3px}',
    // How much of an item's fresh life is left, coloured like the line beside it.
    '.slip-barrow{align-items:center}',
    '.slip-barrow>.slip-txt{flex:none;font-weight:700}',
    '.slip-track{flex:1 1 60px;height:6px;border-radius:3px;background:rgba(10,6,2,.55);border:1px solid rgba(0,0,0,.4);overflow:hidden}',
    '.slip-fill{display:block;height:100%;border-radius:3px;background:linear-gradient(90deg,#8cc56a,#b5e09a);box-shadow:0 0 8px rgba(140,197,106,.45)}',
    '.slip-amber .slip-fill{background:linear-gradient(90deg,#e8a91c,#ffd25a);box-shadow:0 0 8px rgba(255,200,58,.35)}',
    '.slip-bad .slip-fill{background:linear-gradient(90deg,#c8372d,#ff6b5e);box-shadow:0 0 8px rgba(255,107,94,.5)}',
    '.slip-chips{display:flex;flex-wrap:wrap;gap:5px}',
    '.slip-chip{display:inline-flex;align-items:center;gap:4px;padding:0 8px 0 6px;border:1px solid;border-radius:10px;font-size:11.75px;font-weight:700;line-height:18px;font-variant-numeric:tabular-nums}',
    '.slip-chip svg{width:12px;height:12px;flex:none}',
    '.slip-chip-name{font-weight:500;opacity:.85}',
    '.slip-up{color:#b5e09a;background:rgba(140,197,106,.14);border-color:rgba(140,197,106,.4)}',
    '.slip-down{color:#ff9a90;background:rgba(255,107,94,.13);border-color:rgba(255,107,94,.4)}',
    // A recipe's stats under its name: the same chips, smaller.
    '.slip-subchips{gap:4px;margin-top:2px}',
    '.slip-subchips .slip-chip{font-size:10.5px;line-height:15px;padding:0 6px 0 4px;gap:3px}',
    '.slip-subchips .slip-chip svg{width:10px;height:10px}',
    '.slip-key{display:inline-block;font-size:10px;font-weight:700;line-height:15px;color:#f0e8d8;background:linear-gradient(180deg,rgba(92,78,60,.95),rgba(60,50,38,.95));border:1px solid rgba(220,188,120,.4);border-radius:3px;padding:0 5px;box-shadow:0 1px 0 rgba(0,0,0,.6)}',
    '.slip-hint{margin-top:8px;padding-top:6px;border-top:1px solid rgba(220,188,120,.11);text-align:right}',
    // Our own box with nothing above the key hint: the name, then the hint, with no empty space ruled off between.
    '.slip-own-tip>.slip-tip.slip-hint-only{min-width:0;margin-top:4px;padding-top:0;border-top:0}',
    '.slip-hint-only>.slip-hint{margin-top:0;padding-top:0;border-top:0}',
    '.tooltip.slip-own-fresh>.tooltip-shelf-life{display:none}',
    // The game's own stat rows and home stock lines say what our Effects and At home sections say, so while ours
    // show, theirs are hidden. Stat rows are the ones whose value is coloured; the uses row is not and stays.
    '.tooltip.slip-own-effects .tooltip-attr:has(>.value.pos),.tooltip.slip-own-effects .tooltip-attr:has(>.value.neg){display:none}',
    '.tooltip.slip-own-effects .tooltip-attrs:not(:has(>.tooltip-attr>.value:not(.pos):not(.neg))){display:none}',
    '.tooltip.slip-own-home .tooltip-home,.tooltip.slip-own-home .tooltip-where,.tooltip.slip-own-home .tooltip-home-hint{display:none}',
    // What the game still shows above our block, in our type and colours.
    '.tooltip:has(>.slip-tip)>.tooltip-category,.tooltip:has(>.slip-tip)>.tooltip-des{font-size:11.75px;color:#a89a82}',
    '.tooltip:has(>.slip-tip) .tooltip-attrs{min-width:0;padding:0}',
    '.tooltip:has(>.slip-tip) .tooltip-attr{font-size:11.75px;line-height:1.5}',
    '.tooltip:has(>.slip-tip) .tooltip-attr .label,.tooltip:has(>.slip-tip) .tooltip-where-row .label{color:#a89a82;font-weight:normal}',
    '.tooltip:has(>.slip-tip) .tooltip-attr .value,.tooltip:has(>.slip-tip) .tooltip-where-row .value{color:#f0e8d8;font-weight:700;font-variant-numeric:tabular-nums}',
    '.tooltip:has(>.slip-tip) .tooltip-attr .value.pos{color:#b5e09a}',
    '.tooltip:has(>.slip-tip) .tooltip-attr .value.neg{color:#ff9a90}',
    '.tooltip:has(>.slip-tip) .tooltip-home{font-size:11.75px;color:#b5e09a;border-top:1px solid rgba(220,188,120,.16)}',
    '.tooltip:has(>.slip-tip) .tooltip-home-hint{font-size:11px;color:#8f826c}',
    // A fixed tooltip of auto width narrows near the right border, so its size would change as it is moved there;
    // sized by its content it measures the same wherever keepOnScreen puts it.
    '.tooltip:has(>.slip-tip){width:max-content;max-width:309px}',
    '#recipeTooltip:has(>.slip-tip){width:max-content}',
    // The cooking page writes its recipe and item tooltips as plain lines; with our block below them, our header
    // stands in for those lines and the page's own text is kept but not shown.
    '#recipeTooltip.slip-headed{font-size:0}',
    '.slip-head{display:flex;flex-direction:column;gap:1px;font-size:11.75px;line-height:1.45;color:#a89a82;text-align:left;white-space:normal}',
    '.slip-head>.slip-own-name{margin-bottom:2px}',
    '#itemTooltip:has(>.slip-tip),#itemTip:has(>.slip-tip),#fuelTooltip:has(>.slip-tip),#tooltip:has(>.slip-tip){width:max-content;max-width:309px}',
    // These pages write their lines as one text with line breaks; the workbench's tip is markup, where it would add blank lines.
    '#itemTooltip:has(>.slip-tip),#itemTip:has(>.slip-tip),#fuelTooltip:has(>.slip-tip){white-space:pre-line}',
    // A station's own item tooltip, for an item the station page has nothing to say about; it copies their look.
    '.slip-own-tip{position:fixed;display:none;z-index:2147483600;pointer-events:none;width:max-content;max-width:309px;padding:10px 12px;color:#cbbfa8;font-size:12px;line-height:1.5;text-align:left}',
    '.slip-own-name{color:#ffc83a;font-weight:bold;font-size:14px;text-shadow:0 0 12px rgba(255,200,58,.25)}',
    '.slip-own-note{display:none;margin-top:2px;color:#b5e09a;font-size:11.75px;font-weight:700;white-space:pre-line}',
    '.slip-scrap{position:absolute;bottom:5px;left:3px;font-size:10px;font-weight:700;color:#c9a94f;background:rgba(0,0,0,.7);border:1px solid rgba(255,255,255,.12);padding:0 5px;border-radius:8px;line-height:13px;text-shadow:0 1px 2px rgba(0,0,0,.85);box-shadow:0 1px 3px rgba(0,0,0,.45);pointer-events:none;z-index:4}',
    '.slip-spoil{position:absolute;top:0;right:0;width:0;height:0;border-style:solid;border-width:0 13px 13px 0;pointer-events:none;z-index:2;filter:drop-shadow(0 0 1px rgba(0,0,0,.7))}',
    '.slip-spoil-red{border-color:transparent #ef5350 transparent transparent}',
    '.slip-spoil-amber{border-color:transparent #e8a33a transparent transparent}',
    '.slip-tag{font-size:11px;font-weight:bold;color:#c9a94f;background-color:rgba(0,0,0,.35);border:1px solid rgba(201,169,79,.35);padding:1px 6px;border-radius:3px;text-shadow:.7px .7px 1.4px black;flex-shrink:0}',
    '.slip-brew{margin-top:4px;font-size:10px;color:#7e756c;text-align:center}',
    '.slip-plan{position:absolute;right:3px;bottom:3px;font-size:12px;font-weight:700;line-height:15px;padding:0 5px;border-radius:3px;letter-spacing:0;pointer-events:none;z-index:2;box-shadow:0 1px 3px rgba(0,0,0,.6)}',
    '.slip-plan-timed{color:#1a1a1a;background:#ffc83a;border:1px solid rgba(255,235,180,.6)}',
    '.slip-plan-once{color:#0d1f27;background:#6ec6e6;border:1px solid rgba(205,240,255,.6)}',
    '.card.locked .slip-plan{opacity:.45}',
    '.slip-wanted{position:absolute;top:1px;left:3px;font-size:12px;line-height:12px;color:#ffc83a;text-shadow:0 1px 2px rgba(0,0,0,.9);pointer-events:none;z-index:4}',
    '.slip-rate{margin-top:6px;padding-top:5px;border-top:1px solid rgba(220,188,120,.22);font-size:12px;line-height:1.5;color:#cbbfa8}'
  ].join('\n');

  // Everything Info Pack draws, so a change of settings can take it all away again.
  var DRAWN = '.slip-tip, .slip-head, .slip-brew, .slip-scrap, .slip-spoil, .slip-tag, .slip-plan, .slip-wanted, .slip-rate, .slip-own-tip';

  // The HUD bars by the icon the game gives each (CoreUI1's attrList), as the AttrName the plugin measures: a drumstick
  // for satiety, a brain for mood, a bolt for energy, a heart for life.
  var HUD_STATS = [['\uD83C\uDF56', '1'], ['\uD83E\uDDE0', '2'], ['\u26A1', '3'], ['\u2764', '5']];
  var SATIETY = '1';

  // PlanBadges.OneOffBadge on the plugin side: a badge with this text gets the one-off colour, any other the timed one.
  var ONE_OFF_BADGE = '1x';

  // One 12px icon per stat in the order the plugin sends them (fx): a bowl, a face, a bolt, a cross, a heart. They take
  // the chip's colour, so a chip reads as one gain or one loss.
  var STATS = [
    { phrase: 'statSatiety', icon: '<svg viewBox="0 0 12 12" aria-hidden="true"><path d="M1 6.2h10c-.2 2.6-2.3 4.6-5 4.6S1.2 8.8 1 6.2z" fill="currentColor"/><path d="M4.4 1.2c-.7.9.7 1.5 0 2.8M7.6 1.2c-.7.9.7 1.5 0 2.8" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round"/></svg>' },
    { phrase: 'statMood', icon: '<svg viewBox="0 0 12 12" aria-hidden="true"><circle cx="6" cy="6" r="4.9" fill="none" stroke="currentColor" stroke-width="1.2"/><circle cx="4.3" cy="4.9" r=".85" fill="currentColor"/><circle cx="7.7" cy="4.9" r=".85" fill="currentColor"/><path d="M3.7 7.1c1.2 1.6 3.4 1.6 4.6 0" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/></svg>' },
    { phrase: 'statEnergy', icon: '<svg viewBox="0 0 12 12" aria-hidden="true"><path d="M7.2.6 2 7h3.3l-.6 4.4L10 4.9H6.6z" fill="currentColor"/></svg>' },
    { phrase: 'statHealth', icon: '<svg viewBox="0 0 12 12" aria-hidden="true"><path d="M4.4 1h3.2v3.4H11v3.2H7.6V11H4.4V7.6H1V4.4h3.4z" fill="currentColor"/></svg>' },
    { phrase: 'statLife', icon: '<svg viewBox="0 0 12 12" aria-hidden="true"><path d="M6 10.8C2.4 8.3 1 6.5 1 4.4 1 2.9 2.2 1.7 3.6 1.7c1 0 1.9.5 2.4 1.3.5-.8 1.4-1.3 2.4-1.3 1.4 0 2.6 1.2 2.6 2.7 0 2.1-1.4 3.9-5 6.4z" fill="currentColor"/></svg>' }
  ];

  // The tooltip sections, their header phrase (notes has none) and the plugin's defaults, used until it says otherwise.
  var SECTION_TITLES = {
    effects: 'secEffects', fresh: 'secFresh', notes: '', home: 'secHome', usedin: 'secUsedIn', grow: 'secGrow',
    value: 'secValue', madefrom: 'secMadeFrom', breaks: 'secBreaks', fuel: 'secFuel', foundin: 'secFoundIn', about: 'secAbout'
  };
  var DEFAULT_ORDER = ['effects', 'fresh', 'notes', 'home', 'usedin', 'grow', 'value', 'madefrom', 'breaks', 'fuel', 'foundin', 'about'];
  var DEFAULT_MODES = {
    effects: 'always', fresh: 'always', notes: 'detail', home: 'detail', usedin: 'detail', grow: 'detail',
    value: 'detail', madefrom: 'detail', breaks: 'detail', fuel: 'detail', foundin: 'detail', about: 'detail'
  };
  // Lists that can run long: three rows and "+N more" until the detail key is held.
  var LIST_SECTIONS = { usedin: true, madefrom: true, breaks: true, foundin: true };

  var features = null;
  var tipSettings = readTipSettings(null);
  var facts = { desc: {}, dish: {}, brew: {}, scrap: {}, plan: {}, bait: {}, robot: {}, ui: {}, items: {}, wiki: {} };
  var wanted = { wish: {}, aid: {}, untried: {} };
  var stats = { rates: {}, food: null };
  var counts = {};
  var hasCounts = false;
  var keys = { detail: false, where: false };
  var whereRows = {};
  var whereAsked = {};
  var instances = {};
  var asked = {};
  var reported = {};
  // Goes up with every message from the plugin that can change what is drawn, so a drawing keyed on it is redone
  // only when its data changed.
  var dataVersion = 0;
  var isRepaintDue = false;
  // The game's page iframes, a list the browser keeps current.
  var pageFrames = document.getElementsByTagName('iframe');

  function send(message) {
    try {
      if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
    } catch (e) {}
  }

  // One note per kind of failure, so a page a game update reshaped says so once instead of every poll.
  function diag(kind, error) {
    if (reported[kind]) return;

    reported[kind] = true;
    send('diag|' + kind + ': ' + error);
  }

  // ===== Data from the plugin =====

  function setFeatures(flags) {
    features = flags || {};
    tipSettings = readTipSettings(features.tip);
    dataVersion++;
    clearDrawn();
  }

  // Order and modes as the plugin sends them, with any section it leaves out taking its default, so a partial or
  // older settings message still draws every section exactly once.
  function readTipSettings(tip) {
    var order = [];
    var modes = {};

    tip = tip || {};
    (tip.order || []).forEach(function (key) {
      if (SECTION_TITLES.hasOwnProperty(key) && order.indexOf(key) < 0) order.push(key);
    });
    DEFAULT_ORDER.forEach(function (key) {
      if (order.indexOf(key) < 0) order.push(key);
      modes[key] = tip.modes && tip.modes[key] ? tip.modes[key] : DEFAULT_MODES[key];
    });

    return { order: order, modes: modes, detailKey: tip.detailKey || 'Shift', whereKey: tip.whereKey || 'Ctrl' };
  }

  // A change of the settings starts every page clean, so nothing of a feature turned off is left; the next poll draws
  // what is still on.
  function clearDrawn() {
    for (var i = 0; i < pageFrames.length; i++) {
      var doc = null;

      try {
        doc = pageFrames[i].contentDocument;
      } catch (e) {}

      if (doc) clearPage(doc);
    }

    clearPage(document);
  }

  function clearPage(doc) {
    doc.querySelectorAll(DRAWN).forEach(function (el) { el.remove(); });
    ['slip-own-fresh', 'slip-own-effects', 'slip-own-home'].forEach(function (name) {
      doc.querySelectorAll('.' + name).forEach(function (el) { el.classList.remove(name); });
    });
    doc.querySelectorAll('.slip-headed').forEach(function (el) { el.classList.remove('slip-headed'); });
  }

  function setFacts(data) {
    var scrap = {};

    data = data || {};
    (data.scrap || []).forEach(function (id) { scrap[id] = true; });
    facts = {
      desc: data.desc || {}, dish: data.dish || {}, brew: data.brew || {}, scrap: scrap, plan: data.plan || {},
      bait: data.bait || {}, robot: data.robot || {}, ui: data.ui || {}, items: data.items || {}, wiki: data.wiki || {}
    };
    dataVersion++;
  }

  function idSet(ids) {
    var set = {};

    (ids || []).forEach(function (id) { set[id] = true; });

    return set;
  }

  // What the craving and the relief requests want, and the foods never tried, by config id.
  function setWanted(data) {
    data = data || {};
    wanted = { wish: idSet(data.wish), aid: data.aid || {}, untried: idSet(data.untried) };
    changed();
  }

  // rates: {stat: [drop per game hour, hours left]}; food: the days the food at home lasts, when known.
  function setStats(data) {
    data = data || {};
    stats = { rates: data.rates || {}, food: data.food > 0 || data.food === 0 ? data.food : null };
    changed();
  }

  function setCounts(data) {
    counts = data || {};
    hasCounts = true;
    changed();
  }

  function setInstance(id, configId) {
    instances[id] = configId || 0;
    asked[id] = Date.now();
    changed();
  }

  // The plugin reads the detail and breakdown keys itself, since the page never sees modifier keys.
  function setKeys(state) {
    var detail = !!(state && state.detail);
    var where = !!(state && state.where);

    if (detail === keys.detail && where === keys.where) return;

    keys = { detail: detail, where: where };
    changed();
  }

  function setWhere(configId, rows) {
    whereRows[String(configId)] = rows || [];
    changed();
  }

  // Data that changes what a shown tooltip says is drawn before the next frame rather than on the next poll. Several
  // messages in a row share one pass.
  function changed() {
    dataVersion++;

    if (isRepaintDue) return;

    isRepaintDue = true;
    if (window.requestAnimationFrame) window.requestAnimationFrame(dueRepaint);
    else window.setTimeout(dueRepaint, 0);
  }

  function dueRepaint() {
    isRepaintDue = false;
    repaint();
  }

  function repaint() {
    try {
      patchPages();
    } catch (e) {
      diag('repaint', e);
    }
  }

  // The config id of an item the page only knows by instance id. The first ask and every ask after a miss go to the
  // plugin; 0 means "not known yet", and the answer repaints the page.
  function configOfInstance(id) {
    var key = String(id);
    var known = instances[key];

    if (known > 0) return known;
    if (!id || (asked[key] && Date.now() - asked[key] < INSTANCE_RETRY_MS)) return 0;

    asked[key] = Date.now();
    send('inst|' + key);

    return 0;
  }

  // While the breakdown key is held over an item the plugin is asked where it is kept, again every few seconds so the
  // rows follow the stock without a message per poll.
  function askWhere(configId) {
    var key = String(configId);
    var now = Date.now();

    if (!keys.where || !features.counts || !(configId > 0)) return;
    if (whereAsked[key] && now - whereAsked[key] < WHERE_RETRY_MS) return;

    whereAsked[key] = now;
    send('where|' + key);
  }

  // ===== Words and numbers =====

  // The plugin sends the page's words in the game's language (Texts.cs); English stands in until they arrive.
  var ENGLISH = {
    brew: 'Brew quality: {0} of {1}',
    scrap: 'Only useful as scrap',
    scrapChip: 'Scrap',
    brewScore: 'Quality {0}: {1} from {2}, {3} from {4}',
    secEffects: 'Effects', secFresh: 'Freshness', secHome: 'At home', secUsedIn: 'Used in', secGrow: 'Growing',
    secValue: 'Value', secMadeFrom: 'Made from', secBreaks: 'Breaks down into', secFuel: 'Fuel', secFoundIn: 'Found in',
    secAbout: 'About',
    statSatiety: 'Satiety', statMood: 'Mood', statEnergy: 'Energy', statHealth: 'Health', statLife: 'Life',
    freshLeft: '{0} left', freshBase: 'Keeps {0} when fresh', freshRots: 'Rots into {0}', spoiled: 'Spoiled',
    homeCount: '{0} at home', where: '{0}: {1}', whereNone: 'None stored at home',
    makeNow: 'can make {0} now', useBench: 'Workbench', useCook: 'Cooking', useCookTime: 'Cooking, {0}', useBrew: 'Brewed alone, quality {0}',
    useBrewMix: 'In a barrel: {0} satiety, {1} mood each; {2} per bottle', useBrewMood: 'In a barrel: {0} mood each, no satiety',
    more: '+{0} more',
    growTime: 'Grows in {0}', growHarvest: 'Harvest within {0}', growLight: 'Light: {0}', lightLow: 'low',
    lightMid: 'medium', lightHigh: 'high', growCold: 'Cold resistant', growYield: 'Yields {0}',
    growSeed: '{0} chance of a seed',
    valueTrade: 'Trade value {0}', valueWeight: 'Weight {0}', valuePerKg: '{0} trade value per kg',
    satietyPerKg: '{0} satiety per kg',
    makeBench: 'Workbench: {0}', makeCook: 'Cooking: {0}', makePlant: 'Harvest of {0}', makeShred: 'Shredding {0}',
    fuelBurn: 'Burn value {0}', fuelCook: 'Cooks for {0}', fuelHeat: 'Heats for {0}', fuelPower: 'Runs a generator for {0}',
    span: '{0} to {1}', hintMore: '{0}: more', hintWhere: '{0}: where',
    wantWish: "You're craving this", wantAid: 'Relief request needs {0} more', untried: 'Never tried',
    useRobot: 'Robot module, level {0}', useBait: 'Trap bait: {0}', rate: '-{0} per hour, empty in about {1}',
    forecast: 'Food at home lasts you about {0}',
    dayShort: '{0}d', hourShort: '{0}h', minuteShort: '{0}m', kg: '{0} kg', g: '{0} g'
  };

  // LightNeed: 1 grows under cloud, 2 needs sun or a grow lamp, 0 grows in any light and needs no line.
  var LIGHT_PHRASES = { 1: 'lightLow', 2: 'lightHigh' };

  function phrase(key) {
    return facts.ui[key] || ENGLISH[key] || key;
  }

  function say(key) {
    var values = Array.prototype.slice.call(arguments, 1);

    return phrase(key).replace(/\{(\d)\}/g, function (token, slot) { return values[slot]; });
  }

  // say() for lines that hold item references: the phrase split into text and the parts put in its slots, so an icon
  // can sit where the phrase puts the name. A slot takes a string, one reference or a list of parts.
  function sayParts(key) {
    var values = Array.prototype.slice.call(arguments, 1);
    var parts = [];

    phrase(key).split(/(\{\d\})/).forEach(function (piece) {
      var slot = /^\{(\d)\}$/.exec(piece);
      var value = slot ? values[slot[1]] : piece;

      if (value instanceof Array) parts = parts.concat(value);
      else if (value !== '' && value !== undefined && value !== null) parts.push(typeof value === 'object' ? value : String(value));
    });

    return parts;
  }

  // An item named the way the game names it, with its own icon; the name and path go into the drawn key so new facts
  // redraw it.
  function itemRef(id, count) {
    var known = facts.items[id];

    return { ref: id, name: known && known[0] ? known[0] : '#' + id, icon: known && known[1] ? known[1] : '', n: count > 1 ? count : 0 };
  }

  function itemList(pairs) {
    var parts = [];

    (pairs || []).forEach(function (pair, i) {
      if (i > 0) parts.push(', ');

      parts.push(itemRef(pair[0], pair[1]));
    });

    return parts;
  }

  function trimNumber(value, decimals) {
    var scale = Math.pow(10, decimals);

    return String(Math.round(value * scale) / scale);
  }

  function signed(value) {
    var text = trimNumber(value, 1);

    return value > 0 ? '+' + text : text;
  }

  // Game seconds as the two largest units that are not zero: "2d 4h", "5h 15m", "45m".
  function formatSeconds(seconds) {
    var minutes = Math.round(seconds / 60);
    var days = Math.floor(minutes / 1440);
    var hours = Math.floor((minutes % 1440) / 60);
    var rest = minutes % 60;

    if (days > 0) return hours > 0 ? say('dayShort', days) + ' ' + say('hourShort', hours) : say('dayShort', days);
    if (hours > 0) return rest > 0 ? say('hourShort', hours) + ' ' + say('minuteShort', rest) : say('hourShort', hours);

    return say('minuteShort', Math.max(rest, seconds > 0 ? 1 : 0));
  }

  function formatDays(days) {
    return formatSeconds(days * SECONDS_PER_DAY);
  }

  function formatWeight(grams) {
    return grams < 1000 ? say('g', trimNumber(grams, 0)) : say('kg', trimNumber(grams / 1000, 2));
  }

  // Seed_Rate may come as a fraction or as a percentage already.
  function formatChance(rate) {
    return trimNumber(rate <= 1 ? rate * 100 : rate, 0) + '%';
  }

  // ===== What the tooltip says about an item =====

  function isScrap(configId) {
    return !!(features && features.scrap && facts.scrap[configId]);
  }

  function isOpen(mode) {
    return mode === 'always' || (mode === 'detail' && keys.detail);
  }

  // The live days left from the page's own item, or null when the page has none or the item does not spoil (the
  // game sends -1 then, also for food a freezer has stopped).
  function liveDaysLeft(page) {
    var live = page.live;

    return live && typeof live.days === 'number' && live.days >= 0 ? live.days : null;
  }

  // The whole block as plain data: sections in the player's order, each only when its mode lets it show and it has
  // something to say, plus the hint line. Drawing compares this against what is on screen, so it is rebuilt freely.
  // page: { live: {days, expired, polluted} or null, withEffects, stats: the page's own stat values or null }.
  function tipModel(configId, page) {
    var wiki = facts.wiki[configId] || {};
    var context = { id: configId, wiki: wiki, page: page, isRotInFresh: isOpen(tipSettings.modes.fresh) && wiki.rot > 0 };
    var sections = [];
    var owned = {};
    var needsHint = false;

    tipSettings.order.forEach(function (key) {
      var mode = tipSettings.modes[key];
      var rows = mode === 'always' || mode === 'detail' ? sectionRows(key, context) : [];

      if (!rows.length) return;

      owned[key] = true;

      if (!isOpen(mode)) {
        needsHint = true;

        return;
      }

      var shown = keys.detail ? DETAIL_LIST_ROWS : LIST_ROWS;

      if (LIST_SECTIONS[key] && rows.length > shown) {
        rows = rows.slice(0, shown).concat([{ parts: [say('more', rows.length - shown)], cls: 'slip-more' }]);
        if (!keys.detail) needsHint = true;
      }

      sections.push({ key: key, title: SECTION_TITLES[key] ? say(SECTION_TITLES[key]) : '', rows: rows });
    });

    return {
      sections: sections,
      // Sections the game also shows on its own tooltips: ours stand in for theirs whenever ours is on for the item,
      // shown now or waiting for the detail key.
      owns: { effects: owned.effects === true, home: owned.home === true },
      hint: needsHint && !keys.detail ? say('hintMore', tipSettings.detailKey) : '',
      isOwnFresh: isOpen(tipSettings.modes.fresh) && liveDaysLeft(page) > 0 && !page.live.expired && !page.live.polluted
    };
  }

  // tipModel for one tooltip, built again only when the item, the page's own values or the plugin's data changed: the
  // game moves its tooltips on every mouse move, and each move draws the tooltip again.
  function cachedModel(holder, configId, page) {
    var input = dataVersion + '|' + configId + '|' + JSON.stringify(page);

    if (holder.__slipModelFor !== input) {
      holder.__slipModelFor = input;
      holder.__slipModel = configId > 0 ? tipModel(configId, page) : null;
    }

    return holder.__slipModel;
  }

  // What a model says as one text, worked out once per model. The stored text is not enumerable, so it stays out of it.
  function modelKey(model) {
    if (!model.hasOwnProperty('__slipKey')) Object.defineProperty(model, '__slipKey', { value: JSON.stringify(model) });

    return model.__slipKey;
  }

  // Rows are { chips } or { parts, cls, meta, metaCls, sub, subChips, bar }: parts are text and item references, meta
  // sits on the right in small type, sub is a second, quieter line, subChips a line of small stat chips, and bar (0 to
  // 1) draws a bar before the text.
  function sectionRows(key, context) {
    switch (key) {
      case 'effects': return effectRows(context);
      case 'fresh': return freshRows(context);
      case 'notes': return noteRows(context);
      case 'home': return homeRows(context);
      case 'usedin': return usedInRows(context);
      case 'grow': return growRows(context);
      case 'value': return valueRows(context);
      case 'madefrom': return madeFromRows(context);
      case 'breaks': return breakRows(context);
      case 'fuel': return fuelRows(context);
      case 'foundin': return foundInRows(context);
      case 'about': return facts.desc[context.id] ? [{ parts: [facts.desc[context.id]], cls: 'slip-about' }] : [];
      default: return [];
    }
  }

  // Stat names join the icons while the detail key is held, for anyone still learning the icons.
  function effectRows(context) {
    var chips = [];

    if (!context.page.withEffects) return [];

    var own = Array.isArray(context.page.stats) ? context.page.stats : null;

    (own || context.wiki.fx || []).forEach(function (value, i) {
      value = own ? gameStat(value) : value;

      if (!value || !STATS[i]) return;

      chips.push({ stat: i, text: signed(value), name: keys.detail ? say(STATS[i].phrase) : '', isGain: value > 0 });
    });

    return chips.length ? [{ chips: chips }] : [];
  }

  // A stat as the game's bag and trade tooltips round it: down to a whole number, with anything between 0 and 1 as 1.
  function gameStat(value) {
    var n = Number(value) || 0;
    var whole = Math.floor(n);

    return n > 0 && whole === 0 ? 1 : whole;
  }

  // Under a day is red and under three amber; anything longer is still fresh.
  function freshRows(context) {
    var rows = [];
    var wiki = context.wiki;
    var live = context.page.live;
    var days = liveDaysLeft(context.page);

    // Polluted food keeps the game's own time-left line, so ours would say it twice.
    if (days > 0 && !live.expired && !live.polluted) rows.push({ parts: [say('freshLeft', formatDays(days))], cls: days < 1 ? 'slip-bad' : days < 3 ? 'slip-amber' : 'slip-fresh', bar: wiki.life > 0 ? Math.min(1, days / wiki.life) : null });
    else if (days === 0 || (live && live.expired && wiki.life > 0)) rows.push({ parts: [say('spoiled')], cls: 'slip-bad' });

    if (wiki.life > 0) rows.push({ parts: [say('freshBase', formatDays(wiki.life))], cls: 'slip-dim' });
    if (wiki.rot > 0) rows.push({ parts: sayParts('freshRots', itemRef(wiki.rot)) });

    return rows;
  }

  function noteRows(context) {
    var rows = [];
    var tier = facts.brew[context.id];

    if (features.brew && tier) rows.push({ parts: [say('brew', tier[0], tier[1])] });
    if (isScrap(context.id)) rows.push({ parts: [say('scrap')], cls: 'slip-warn' });
    if (features.wanted && wanted.wish[context.id]) rows.push({ parts: [say('wantWish')], cls: 'slip-good' });
    if (features.wanted && wanted.aid[context.id] > 0) rows.push({ parts: [say('wantAid', wanted.aid[context.id])], cls: 'slip-good' });
    if (features.untried && wanted.untried[context.id]) rows.push({ parts: [say('untried')], cls: 'slip-good' });

    return rows;
  }

  // The breakdown key shows where the stock is; until it is held, the key's name sits beside the count as a hint.
  function homeRows(context) {
    var key = String(context.id);
    var have = counts[key] || 0;
    var rows = [];

    if (!features.counts || !hasCounts) return rows;

    rows.push({
      parts: [say('homeCount', have)],
      meta: !keys.where && have > 0 && tipSettings.whereKey ? say('hintWhere', tipSettings.whereKey) : '',
      metaCls: 'slip-key'
    });

    if (!keys.where || !whereRows[key]) return rows;

    if (!whereRows[key].length) rows.push({ parts: [say('whereNone')], cls: 'slip-where' });

    whereRows[key].forEach(function (place) { rows.push({ parts: [say('where', place[0], place[1])], cls: 'slip-where' }); });

    return rows;
  }

  // How many times a workbench recipe could run from what is at home: the scarcest material decides. null when the
  // home counts are off or not in yet.
  function timesCraftable(need) {
    var times = Infinity;

    if (!features.counts || !hasCounts || !need || !need.length) return null;

    need.forEach(function (pair) {
      if (pair[1] > 0) times = Math.min(times, Math.floor((counts[String(pair[0])] || 0) / pair[1]));
    });

    return times === Infinity ? null : times;
  }

  // Recipes you could make right now come first, then a robot module's level and a bait's kind, so the three rows
  // shown by default are the useful ones.
  function usedInRows(context) {
    var ready = [];
    var other = [];
    var rest = [];
    var level = facts.robot[context.id];
    var bait = facts.bait[context.id];

    if (level > 0) other.push({ parts: [say('useRobot', level)] });
    if (bait) other.push({ parts: [say('useBait', bait)] });

    (context.wiki.use || []).forEach(function (use) {
      if (use.k === 'bench' && use.out > 0) {
        var times = timesCraftable(use.need);

        (times > 0 ? ready : rest).push({
          parts: [itemRef(use.out)],
          meta: times > 0 ? say('makeNow', times) : say('useBench'),
          metaCls: times > 0 ? 'slip-good' : 'slip-dim'
        });
      } else if (use.k === 'cook' && use.out > 0) {
        rest.push({ parts: [itemRef(use.out)], meta: use.t > 0 ? say('useCookTime', formatSeconds(use.t)) : say('useCook'), metaCls: 'slip-dim', subChips: dishChips(use.out) });
      } else if (use.k === 'brew') {
        rest.push.apply(rest, brewRows(use));
      }
    });

    return ready.concat(other, rest);
  }

  // What eating the dish does, as the effects section's chips without the stat names.
  function dishChips(dishId) {
    var fx = (facts.wiki[dishId] || {}).fx || [];
    var chips = [];

    fx.forEach(function (value, i) {
      if (value && STATS[i]) chips.push({ stat: i, text: signed(value), name: '', isGain: value > 0 });
    });

    return chips;
  }

  // The drink a barrel of only this item makes, then what the item adds to any batch. An item with no satiety cannot
  // start a brew, so it gets the second line only.
  function brewRows(use) {
    var rows = [];

    if (use.out > 0) rows.push({ parts: [itemRef(use.out)], meta: say('useBrew', fixed(use.q)), metaCls: 'slip-dim' });

    rows.push({ parts: [use.n > 0 ? say('useBrewMix', trimNumber(use.s, 1), signed(use.m), use.n) : say('useBrewMood', signed(use.m))] });

    return rows;
  }

  // How long one item lasts in each kind of burner, as a range when the burners of a kind differ.
  function fuelRows(context) {
    var wiki = context.wiki;
    var times = wiki.bt || {};
    var rows = [];

    if (!(wiki.burn > 0)) return rows;

    rows.push({ parts: [say('fuelBurn', wiki.burn)] });

    if (times.c) rows.push({ parts: [say('fuelCook', burnSpan(times.c))] });
    if (times.h) rows.push({ parts: [say('fuelHeat', burnSpan(times.h))] });
    if (times.g) rows.push({ parts: [say('fuelPower', burnSpan(times.g))] });

    return rows;
  }

  function burnSpan(span) {
    return span[0] === span[1] ? formatSeconds(span[0]) : say('span', formatSeconds(span[0]), formatSeconds(span[1]));
  }

  function growRows(context) {
    var grow = context.wiki.grow;
    var rows = [];

    if (!grow) return rows;

    if (grow.t > 0) rows.push({ parts: [say('growTime', formatSeconds(grow.t))] });
    if (grow.h > 0) rows.push({ parts: [say('growHarvest', formatSeconds(grow.h))] });
    if (LIGHT_PHRASES[grow.light]) rows.push({ parts: [say('growLight', say(LIGHT_PHRASES[grow.light]))] });
    if (grow.cold > 0) rows.push({ parts: [say('growCold')] });
    if (grow.gain && grow.gain.length) rows.push({ parts: sayParts('growYield', itemList(grow.gain.map(function (id) { return [id, 1]; }))) });
    if (grow.seed > 0) rows.push({ parts: [say('growSeed', formatChance(grow.seed))] });

    return rows;
  }

  function valueRows(context) {
    var wiki = context.wiki;
    var kilos = wiki.wt > 0 ? wiki.wt / 1000 : 0;
    var satiety = wiki.fx ? wiki.fx[0] : 0;
    var rows = [];

    if (wiki.tv > 0) rows.push({ parts: [say('valueTrade', wiki.tv)] });
    if (kilos) rows.push({ parts: [say('valueWeight', formatWeight(wiki.wt))] });
    if (kilos && wiki.tv > 0) rows.push({ parts: [say('valuePerKg', trimNumber(wiki.tv / kilos, 1))], cls: 'slip-dim' });
    if (kilos && satiety > 0) rows.push({ parts: [say('satietyPerKg', trimNumber(satiety / kilos, 1))], cls: 'slip-dim' });

    return rows;
  }

  function madeFromRows(context) {
    var rows = [];

    (context.wiki.make || []).forEach(function (make) {
      if (make.k === 'bench' && make.need) rows.push({ parts: sayParts('makeBench', itemList(make.need)) });
      else if (make.k === 'cook' && make.need) rows.push({ parts: sayParts('makeCook', itemList(make.need)), meta: make.t > 0 ? formatSeconds(make.t) : '', metaCls: 'slip-dim' });
      else if (make.k === 'plant' && make.from > 0) rows.push({ parts: sayParts('makePlant', itemRef(make.from)) });
      else if (make.k === 'shred' && make.from > 0) rows.push({ parts: sayParts('makeShred', itemRef(make.from)), meta: make.n > 1 ? 'x' + make.n : '', metaCls: 'slip-dim' });
    });

    return rows;
  }

  // The rot product is listed here only when the freshness section is not already showing it.
  function breakRows(context) {
    var wiki = context.wiki;
    var rows = (wiki.shred || []).map(function (pair) { return { parts: [itemRef(pair[0], pair[1])] }; });

    if (wiki.cut > 0) rows.push({ parts: [itemRef(wiki.cut)] });
    if (wiki.rot > 0 && !context.isRotInFresh) rows.push({ parts: sayParts('freshRots', itemRef(wiki.rot)) });

    return rows;
  }

  function foundInRows(context) {
    return (context.wiki.found || []).map(function (source) {
      return { parts: [source.src], sub: source.at && source.at.length ? source.at.join(', ') : '' };
    });
  }

  // ===== Drawing the block =====

  // The block stays the tooltip's last element, since a line the game shows later (shelf life, say) is inserted
  // where Vue keeps its place, which can be after ours. It is only rebuilt when what it says changes.
  function drawBlock(doc, tip, model) {
    var box = ownChild(tip, '__slipBox', 'slip-tip');

    if (!model || (!model.sections.length && !model.hint)) {
      if (box) box.remove();

      tip.__slipBox = null;

      return false;
    }

    if (!box) {
      box = doc.createElement('div');
      box.className = 'slip-tip';
    }

    tip.__slipBox = box;

    if (tip.lastElementChild !== box) tip.appendChild(box);

    var key = modelKey(model);

    if (box.__slipKey === key) return true;

    box.__slipKey = key;
    box.textContent = '';
    box.classList.toggle('slip-hint-only', !model.sections.length);
    model.sections.forEach(function (section) { box.appendChild(sectionElement(doc, section)); });

    if (model.hint) {
      var hint = addElement(doc, box, 'div', 'slip-hint');

      addElement(doc, hint, 'span', 'slip-key').textContent = model.hint;
    }

    return true;
  }

  // A node of ours directly under parent, kept on the parent in slot so finding it again costs no search. Only this
  // script adds these nodes and it updates the slot each time, so a kept "none" stays true; a node the page or a
  // settings change removed has lost its parent and counts as missing.
  function ownChild(parent, slot, className) {
    var node = parent[slot];

    if (node === undefined) node = parent.querySelector(':scope > .' + className);

    return node && node.parentNode === parent ? node : null;
  }

  function addElement(doc, parent, tag, className) {
    var el = doc.createElement(tag);

    el.className = className;
    parent.appendChild(el);

    return el;
  }

  function sectionElement(doc, section) {
    var el = doc.createElement('div');

    el.className = 'slip-sec';

    if (section.title) addElement(doc, el, 'div', 'slip-h').textContent = section.title;

    section.rows.forEach(function (row) { el.appendChild(row.chips ? chipRow(doc, row.chips) : textRow(doc, row)); });

    return el;
  }

  function chipRow(doc, chips, className) {
    var row = doc.createElement('div');

    row.className = className || 'slip-chips';
    chips.forEach(function (chip) {
      var el = addElement(doc, row, 'span', 'slip-chip ' + (chip.isGain ? 'slip-up' : 'slip-down'));

      el.innerHTML = STATS[chip.stat].icon;

      if (chip.name) addElement(doc, el, 'span', 'slip-chip-name').textContent = chip.name;

      el.appendChild(doc.createTextNode(chip.text));
    });

    return row;
  }

  function textRow(doc, row) {
    var el = doc.createElement('div');
    var text = addElement(doc, el, 'span', 'slip-txt');

    el.className = 'slip-row' + (row.cls ? ' ' + row.cls : '') + (row.bar != null ? ' slip-barrow' : '');

    if (row.bar != null) {
      addElement(doc, addElement(doc, el, 'span', 'slip-track'), 'span', 'slip-fill').style.width = Math.round(row.bar * 100) + '%';
      el.appendChild(text);
    }

    row.parts.forEach(function (part) { text.appendChild(typeof part === 'string' ? doc.createTextNode(part) : refElement(doc, part)); });

    if (row.sub) addElement(doc, text, 'span', 'slip-subline').textContent = row.sub;
    if (row.subChips && row.subChips.length) text.appendChild(chipRow(doc, row.subChips, 'slip-chips slip-subchips'));
    if (row.meta) addElement(doc, el, 'span', 'slip-meta' + (row.metaCls ? ' ' + row.metaCls : '')).textContent = row.meta;

    return el;
  }

  // Page iframes sit two folders below WebUI, so the game's own relative icon path works as it is. A missing icon
  // keeps its square so names stay aligned.
  function refElement(doc, ref) {
    var el = doc.createElement('span');

    el.className = 'slip-ref';

    if (ref.icon) {
      var img = addElement(doc, el, 'img', 'slip-ico');

      img.draggable = false;
      img.onerror = function () { img.className = 'slip-ico slip-noico'; img.removeAttribute('src'); };
      img.src = ref.icon;
    } else {
      addElement(doc, el, 'span', 'slip-ico slip-noico');
    }

    el.appendChild(doc.createTextNode(ref.name));

    if (ref.n) addElement(doc, el, 'span', 'slip-n').textContent = 'x' + ref.n;

    return el;
  }

  // The block's live freshness line replaces the game's own shelf-life line, which says the same in plainer type, and
  // its Effects and At home sections the game's stat rows and home stock lines. Only touched when it changes:
  // re-adding a class still counts as a change to the tooltip's watcher.
  function setOwned(tip, model) {
    setClass(tip, 'slip-own-fresh', !!(model && model.isOwnFresh));
    setClass(tip, 'slip-own-effects', !!(model && model.owns.effects));
    setClass(tip, 'slip-own-home', !!(model && model.owns.home));
  }

  function setClass(el, className, isOn) {
    if (el.classList.contains(className) !== isOn) el.classList.toggle(className, isOn);
  }

  // ===== Keeping a tooltip on screen =====

  // The game places its tooltips as if they were 200x100, so a tall one runs off the bottom. Ours is placed the way
  // the game does it (15px right of and below the cursor), flipped to the other side of the cursor on the axis where it
  // does not fit and pushed inside the window when neither side does. It runs straight after each of the game's own
  // moves (watchTooltip), before the frame is drawn, so the two never show as a jump.
  function keepOnScreen(win, tip) {
    if (!features.onScreen) return;

    if (tip.style.display === 'none') {
      tip.__slipZoom = 0;

      return;
    }

    var rect = tip.getBoundingClientRect();

    if (!rect.width || !rect.height) return;

    var zoom = zoomFor(win, tip);
    var pointer = win.__slipPointer;
    var hasPointer = !!(pointer && pointer.isKnown);
    var left = hasPointer ? besideCursor(pointer.x, rect.width, win.innerWidth, CURSOR_GAP * zoom) : insideWindow(rect.left, rect.width, win.innerWidth);
    var top = hasPointer ? besideCursor(pointer.y, rect.height, win.innerHeight, CURSOR_GAP * zoom) : insideWindow(rect.top, rect.height, win.innerHeight);

    // Sizes and the cursor are window pixels; the tooltip's left and top are in its zoomed page's own pixels.
    moveTip(tip, left / zoom, top / zoom);
  }

  function besideCursor(at, size, room, gap) {
    var after = at + gap;
    var before = at - gap - size;

    if (after + size <= room - EDGE) return after;
    if (before >= EDGE) return before;

    return insideWindow(after, size, room);
  }

  function insideWindow(start, size, room) {
    return Math.max(EDGE, Math.min(start, room - EDGE - size));
  }

  // Writing an unchanged position would wake the tooltip's watcher for nothing.
  function moveTip(tip, left, top) {
    if (!(Math.abs(parseFloat(tip.style.left) - left) <= 0.5)) tip.style.left = left + 'px';
    if (!(Math.abs(parseFloat(tip.style.top) - top) <= 0.5)) tip.style.top = top + 'px';
  }

  // BackpackUI and TradeUI shrink their whole app with CSS zoom on small windows, the cooking window its own way;
  // multiplying up the tree covers all of them.
  function zoomOf(win, el) {
    var zoom = 1;

    for (var node = el; node && node.nodeType === 1; node = node.parentNode) {
      var value = parseFloat(win.getComputedStyle(node).zoom);

      if (value > 0) zoom *= value;
    }

    return zoom;
  }

  // The pages change their zoom only with the window's size, so a shown tooltip's zoom is read once and again only
  // after the window changed size or the tooltip was hidden: reading it walks every element above the tooltip.
  function zoomFor(win, tip) {
    var size = win.innerWidth * 100000 + win.innerHeight;

    if (!(tip.__slipZoom > 0) || tip.__slipZoomSize !== size) {
      tip.__slipZoom = zoomOf(win, tip);
      tip.__slipZoomSize = size;
    }

    return tip.__slipZoom;
  }

  // The cursor as the page last saw it, read in the capture phase so it is current before the game's own handlers run.
  function trackPointer(win, doc) {
    if (win.__slipPointer) return;

    var pointer = { x: 0, y: 0, isKnown: false };
    var follow = function (e) {
      pointer.x = e.clientX;
      pointer.y = e.clientY;
      pointer.isKnown = true;
    };

    win.__slipPointer = pointer;
    doc.addEventListener('mousemove', follow, true);
    doc.addEventListener('mouseover', follow, true);
  }

  // The tooltip is redrawn the moment the game changes it (new item, new position, shown or hidden) instead of on the
  // next poll, so the block never shows the last item's facts and placement happens before the frame is drawn. The
  // changes our own drawing made are taken off the watcher's queue (settle), so they do not wake it again.
  function watchTooltip(win, tip, kind, redraw) {
    if (tip.__slipObserver) return;

    tip.__slipObserver = new win.MutationObserver(function () {
      try {
        redraw();
      } catch (e) {
        diag(kind, e);
      }

      settle(tip);
    });
    tip.__slipObserver.observe(tip, { attributes: true, attributeFilter: ['style', 'class'], childList: true, characterData: true, subtree: true });
  }

  // Drops the changes waiting for a watched element's watcher. Called straight after our own drawing, in the same
  // task: the game's changes from earlier tasks have been handed over already, so only ours are dropped.
  function settle(el) {
    if (el && el.__slipObserver) el.__slipObserver.takeRecords();
  }

  // ===== Shared page helpers =====

  // Checked on every poll, so once it is in place the check is one property read: comparing the sheet's text each
  // time would copy all of it.
  function addStyle(doc) {
    if (doc.__slipStyle && doc.__slipStyle.isConnected) return;

    var style = doc.getElementById('slip-style');

    if (!doc.head) return;

    if (!style) {
      style = doc.createElement('style');
      style.id = 'slip-style';
      doc.head.appendChild(style);
    }

    if (style.textContent !== CSS) style.textContent = CSS;

    doc.__slipStyle = style;
  }

  function vueSetup(doc) {
    var app = doc.getElementById('app');

    return app && app._vnode && app._vnode.component ? app._vnode.component.setupState : null;
  }

  // The game's Vue item tooltip: the element holding .tooltip-name (the same in BackpackUI, TradeUI and ShopUI).
  // Kept on the document while it is still in the page, so a poll costs no search.
  function vueTooltip(doc) {
    var name = doc.__slipTipName;

    if (!name || !name.isConnected || !name.parentNode.classList.contains('tooltip')) name = doc.__slipTipName = doc.querySelector('.tooltip > .tooltip-name');

    return name ? name.parentNode : null;
  }

  // Draws the block for whatever a Vue tooltip shows (configId 0 for nothing of ours) and places the tooltip.
  function drawVueTip(win, doc, tip, configId, page) {
    var model = cachedModel(tip, configId, page);

    askWhere(configId);
    drawBlock(doc, tip, model);
    setOwned(tip, model);
    keepOnScreen(win, tip);
  }

  // A Vue page's tooltip: watched for the game's changes and drawn on every poll.
  function patchVueTooltip(win, doc, kind, draw) {
    var tip = vueTooltip(doc);

    if (!tip) return;

    var redraw = function () {
      var setup = vueSetup(doc);

      if (setup && features) draw(setup, tip);
    };

    trackPointer(win, doc);
    watchTooltip(win, tip, kind + ' tooltip observer', redraw);

    try {
      redraw();
    } catch (e) {
      diag(kind + ' tooltip', e);
    }

    settle(tip);
  }

  var chipSlots = {};

  function setChip(parent, doc, isWanted, className, text) {
    var slot = chipSlots[className] || (chipSlots[className] = '__slip_' + className);
    var chip = ownChild(parent, slot, className);

    if (!isWanted) {
      if (chip) chip.remove();

      parent[slot] = null;

      return;
    }

    if (!chip) {
      chip = doc.createElement('span');
      chip.className = className;
      chip.textContent = text;
      parent.appendChild(chip);
    }

    parent[slot] = chip;
  }

  // ===== Backpack =====

  function patchBackpack(win, doc) {
    var setup = vueSetup(doc);

    if (!setup) return diag('backpack', 'no Vue state');

    patchVueTooltip(win, doc, 'backpack', function (state, tip) {
      var hover = state.tooltipState;
      var item = hover && hover.visible ? hover.item : null;
      var live = item ? { days: item.shelfLifeDays, expired: !!item.isExpired, polluted: !!item.polluted } : null;

      drawVueTip(win, doc, tip, item ? item.cfgId : 0, { live: live, withEffects: true, stats: item ? item.vd : null });
    });

    try {
      patchBackpackSlots(doc, setup);
    } catch (e) {
      diag('backpack slots', e);
    }
  }

  // One .grid-container per visible side, in order, whose .item children follow that side's items array. Any
  // mismatch (a redraw in flight) skips the side until the next poll rather than mark the wrong slot.
  function patchBackpackSlots(doc, setup) {
    var sides = setup.visibleSides;
    // A list the browser keeps current, kept on the document instead of searching it each poll.
    var grids = doc.__slipGrids || (doc.__slipGrids = doc.getElementsByClassName('grid-container'));

    if (!sides || grids.length !== sides.length) return;

    var scrapText = say('scrapChip');

    for (var s = 0; s < sides.length; s++) {
      var bag = setup.getBag(sides[s]);
      var items = bag && bag.items ? bag.items : [];
      var cells = grids[s].querySelectorAll(':scope > .item');

      if (cells.length !== items.length) continue;

      for (var i = 0; i < cells.length; i++) {
        var item = items[i];

        setChip(cells[i], doc, isScrap(item.cfgId) && !(item.useTimesMax > 1), 'slip-scrap', scrapText);
        setSpoilBadge(cells[i], doc, spoilLevel(item));
        setChip(cells[i], doc, !!(features.wanted && wanted.aid[item.cfgId] > 0), 'slip-wanted', '\u2605');
      }
    }
  }

  // Red for food that spoils within a day. Amber under three days, but only once half the item's shelf life is gone,
  // so food that keeps only a few days when fresh is not amber the moment it is picked up. Items that do not spoil
  // (the game sends -1) and spoiled ones (the game's own red mask) get nothing.
  function spoilLevel(item) {
    var days = item.shelfLifeDays;
    var wiki = facts.wiki[item.cfgId];
    var life = wiki ? wiki.life : 0;

    if (!features.badges || item.isExpired || !(days > 0)) return '';
    if (days < 1) return 'red';
    if (days < 3 && !(life > 0 && days >= life / 2)) return 'amber';

    return '';
  }

  // The top-right corner, under the game's story and wish marks (both rare and drawn above it), clear of the use
  // counter and Scrap chip at the bottom left and the stack count at the bottom right.
  var SPOIL_CLASSES = { red: 'slip-spoil slip-spoil-red', amber: 'slip-spoil slip-spoil-amber' };

  function setSpoilBadge(cell, doc, level) {
    var badge = ownChild(cell, '__slipSpoil', 'slip-spoil');
    var className = SPOIL_CLASSES[level];

    if (!level) {
      if (badge) badge.remove();

      cell.__slipSpoil = null;

      return;
    }

    if (!badge) {
      badge = doc.createElement('span');
      cell.appendChild(badge);
    }

    cell.__slipSpoil = badge;

    if (badge.className !== className) badge.className = className;
  }

  // ===== Trade =====

  // Shelf items carry their config id as id. Everything else is named by instance id, which the plugin looks up.
  // Effects on shelf items follow the trade stats setting: those are the ones you are deciding whether to buy.
  function patchTrade(win, doc) {
    if (!vueSetup(doc)) return diag('trade', 'no Vue state');

    patchVueTooltip(win, doc, 'trade', function (state, tip) {
      var hover = state.tooltipState;
      var item = hover && hover.visible ? hover.item : null;
      var configId = 0;
      var isShelf = false;

      if (item && !state.kinTipText && !item.dimTip) {
        var shelf = state.shelf && state.shelf.items ? state.shelf.items : [];

        isShelf = shelf.indexOf(item) >= 0;
        configId = isShelf ? item.id : configOfInstance(item.id);
      }

      drawVueTip(win, doc, tip, configId, { live: null, withEffects: !isShelf || !!features.stats, stats: item ? item.vd : null });
    });
  }

  // ===== Shop =====

  // The shop's bag tooltip is the same Vue tooltip as the backpack's, naming items by instance id. Items in the cart
  // (isShop) are the shop's, which the plugin cannot look up. The shop list's own parameter panel is left alone.
  function patchShop(win, doc) {
    if (!vueSetup(doc)) return diag('shop', 'no Vue state');

    patchVueTooltip(win, doc, 'shop', function (state, tip) {
      var hover = state.tooltipState;
      var item = hover && hover.visible ? hover.item : null;

      drawVueTip(win, doc, tip, item && !item.isShop ? configOfInstance(item.id) : 0, { live: null, withEffects: true });
    });
  }

  // ===== Cooking =====

  // The cooking page is plain script: its showItemTip is a global, so wrapping it here wraps every call the page
  // makes. Recipe tooltips are written inline instead, so those are caught by watching the tooltip. The page writes
  // the tooltip as one text node; the block goes after it as an element.
  function patchCooking(win, doc) {
    var tip = doc.getElementById('recipeTooltip');
    var original = win.showItemTip;

    if (!tip) return;

    if (win.__slipCooking) {
      drawCookingTip(win, doc, tip);
      settle(tip);

      return;
    }

    if (typeof original !== 'function') return diag('cooking', 'showItemTip is missing');

    win.__slipCooking = true;
    win.showItemTip = function (item) {
      var result = original.apply(this, arguments);

      try {
        claimCookingItemTip(win, doc, tip, item);
      } catch (e) {
        diag('cooking item tooltip', e);
      }

      return result;
    };

    trackPointer(win, doc);
    tip.__slipObserver = new win.MutationObserver(function () {
      try {
        onCookingTipChanged(win, doc, tip);
      } catch (e) {
        diag('cooking recipe tooltip', e);
      }

      settle(tip);
    });
    tip.__slipObserver.observe(tip, { attributes: true, attributeFilter: ['style'], childList: true, characterData: true, subtree: true });
  }

  // The tooltip's text without our block: what the page itself wrote.
  function pageTextOf(tip) {
    var text = '';

    for (var node = tip.firstChild; node; node = node.nextSibling) {
      if (!(node.nodeType === 1 && (node.classList.contains('slip-tip') || node.classList.contains('slip-head')))) text += node.textContent;
    }

    return text;
  }

  // The page's showItemTip writes nothing for an item it has nothing to say about, so the tooltip is only ours to
  // extend when it starts with this item's name.
  function claimCookingItemTip(win, doc, tip, item) {
    var text = pageTextOf(tip);

    if (!item || !item.name || tip.style.display !== 'block' || text.split('\n')[0] !== item.name) return;

    tip.__slipBase = text;
    tip.__slipSubject = { item: item };
    drawCookingTip(win, doc, tip);
  }

  // Only a change of the page's own text means a new tooltip; our block and our moves leave it as it was. A recipe
  // tooltip starts with the recipe's name, and its dish is what the block describes.
  function onCookingTipChanged(win, doc, tip) {
    var text = pageTextOf(tip);

    if (text !== tip.__slipBase) {
      tip.__slipBase = text;
      tip.__slipSubject = recipeSubject(win, text);
    }

    drawCookingTip(win, doc, tip);
  }

  function recipeSubject(win, text) {
    var state = win.State;
    var firstLine = text.split('\n')[0];

    if (!state || !state.lastRecipes) return null;

    for (var i = 0; i < state.lastRecipes.length; i++) {
      var recipe = state.lastRecipes[i];

      if (recipe && recipe.name === firstLine && facts.dish[recipe.recipeId]) return { dish: facts.dish[recipe.recipeId], effectText: recipe.effectText };
    }

    return null;
  }

  // The cooking window names most items by config id; only those it leaves at 0 need the instance lookup.
  function cookingConfigId(subject) {
    if (!subject) return 0;
    if (!subject.item) return subject.dish;

    return subject.item.configId > 0 ? subject.item.configId : configOfInstance(subject.item.itemId);
  }

  var NO_LIVE_PAGE = { live: null, withEffects: true };

  function drawCookingTip(win, doc, tip) {
    var isShown = tip.style.display === 'block';
    var configId = features && features.codex ? cookingConfigId(tip.__slipSubject) : 0;
    var model = cachedModel(tip, configId, NO_LIVE_PAGE);
    var isDrawn = drawBlock(doc, tip, model);
    var width = isDrawn ? COOKING_TIP_WIDTH : '';

    if (tip.style.maxWidth !== width) tip.style.maxWidth = width;

    drawCookingHead(doc, tip, isDrawn ? cookingHeadLines(tip, model) : null);

    if (!isShown) return;

    askWhere(configId);
    keepOnScreen(win, tip);
  }

  // headLines for the tooltip, worked out again only when the page's text, the subject or the block changed.
  function cookingHeadLines(tip, model) {
    var base = tip.__slipBase || '';

    if (tip.__slipLinesBase !== base || tip.__slipLinesSubject !== tip.__slipSubject || tip.__slipLinesModel !== model) {
      tip.__slipLinesBase = base;
      tip.__slipLinesSubject = tip.__slipSubject;
      tip.__slipLinesModel = model;
      tip.__slipLines = headLines(base, tip.__slipSubject, model);
    }

    return tip.__slipLines;
  }

  // The page's lines as our header: the name first, then the rest, without the effect line when the block's EFFECTS
  // section already shows those stats.
  function headLines(text, subject, model) {
    var lines = text.split('\n').map(function (line) { return line.trim(); }).filter(function (line) { return !!line; });
    var hasEffects = model.sections.some(function (section) { return section.key === 'effects'; });
    var effectText = subject && subject.effectText ? String(subject.effectText).trim() : '';

    if (!lines.length) return null;

    return {
      name: lines[0],
      rest: lines.slice(1).filter(function (line) { return !(hasEffects && line === effectText); })
    };
  }

  // Only rebuilt when its lines change: each rebuild is a change the tooltip's watcher sees and answers by drawing again.
  function drawCookingHead(doc, tip, lines) {
    var head = ownChild(tip, '__slipHead', 'slip-head');
    var isHeaded = !!lines;

    if (tip.classList.contains('slip-headed') !== isHeaded) tip.classList.toggle('slip-headed', isHeaded);

    if (!lines) {
      if (head) head.remove();

      tip.__slipHead = null;

      return;
    }

    if (head && head.__slipLines === lines && tip.firstChild === head) return;

    var key = JSON.stringify(lines);

    if (head && head.__slipKey === key && tip.firstChild === head) {
      tip.__slipHead = head;
      head.__slipLines = lines;

      return;
    }

    if (!head) {
      head = doc.createElement('div');
      head.className = 'slip-head';
    }

    tip.__slipHead = head;
    head.__slipLines = lines;
    head.__slipKey = key;
    head.textContent = '';
    addElement(doc, head, 'div', 'slip-own-name').textContent = lines.name;
    lines.rest.forEach(function (line) { addElement(doc, head, 'div', '').textContent = line; });

    if (tip.firstChild !== head) tip.insertBefore(head, tip.firstChild);
  }

  // ===== Brew panel =====

  // The panel's quality bar shows the tier but not how close the mix is to the next one. Shown before brewing only,
  // when the panel itself shows its marker. Thresholds are normalised the way the panel draws them, and a tier is
  // reached at its threshold (the game ranks a mix below P1 lowest, at or above P2 highest).
  function patchBrew(win, doc) {
    var state = win.State;
    var text = win.i18nText || {};
    var labels = doc.__slipLabels && doc.__slipLabels.isConnected ? doc.__slipLabels : (doc.__slipLabels = doc.querySelector('.qbar > .qlabels'));
    var box = doc.getElementById('slip-brew');
    var isShown = !!(features && features.brew && state && labels && state.brewState === 0 && state.materialCount > 0 && state.resultCount > 0);

    if (!isShown) {
      if (box && box.style.display !== 'none') box.style.display = 'none';

      return;
    }

    var low = state.tierP1;
    var high = state.tierP2;

    if (!(high > low)) {
      low = 0;
      high = 0.5;
    }

    if (!box) {
      box = doc.createElement('div');
      box.id = 'slip-brew';
      box.className = 'slip-brew';
    }

    if (labels.nextSibling !== box) labels.parentNode.insertBefore(box, labels.nextSibling);

    var line = say('brewScore', fixed(state.moodDensity), text.tierMid || 'Normal', fixed(low), text.tierHigh || 'Strong', fixed(high));

    // Writing the same again would still be a change the page has to lay out.
    if (box.style.display) box.style.display = '';
    if (box.textContent !== line) box.textContent = line;
  }

  function fixed(value) {
    return typeof value === 'number' && isFinite(value) ? value.toFixed(2) : '?';
  }

  // ===== Item pickup =====

  function patchPickup(doc) {
    var setup = vueSetup(doc);
    var items = setup && setup.state ? setup.state.items : null;
    var rows = doc.querySelectorAll('li.pickup-item');

    if (!items) return diag('pickup', 'no Vue state');
    if (rows.length !== items.length) return;

    var scrapText = say('scrapChip');

    for (var i = 0; i < rows.length; i++) {
      var info = rows[i].__slipInfo;

      if (!info || !rows[i].contains(info)) info = rows[i].__slipInfo = rows[i].querySelector('.item-info');
      if (info) setChip(info, doc, isScrap(items[i].configId), 'slip-tag', scrapText);
    }
  }

  // ===== Survival Planning =====

  // The page keys its card slots by position, so slot i always shows cards[i], and card.id is the talent id. A refresh
  // swaps the cards under the same slots, which is why every badge is checked against its card on each poll. A slot
  // count that differs from the list (a redraw in flight) skips the poll rather than badge the wrong card.
  function patchPlanning(doc) {
    var setup = vueSetup(doc);
    var cards = setup ? setup.cards : null;
    var slots = doc.querySelectorAll('.card-grid > .card-slot');

    if (!cards) return diag('planning', 'no Vue state');
    if (slots.length !== cards.length) return;

    for (var i = 0; i < slots.length; i++) {
      var frame = slots[i].__slipFrame;

      if (!frame || !frame.parentNode || frame.parentNode.parentNode !== slots[i]) frame = slots[i].__slipFrame = slots[i].querySelector(':scope > .card > .icon-frame');
      if (frame) setPlanBadge(frame, doc, cards[i] ? facts.plan[cards[i].id] : null);
    }
  }

  // The poll alone can leave the old card's badge on a new card for a moment after a refresh, so the grid is also
  // watched. The changes our own badges make are taken off its queue, so they do not wake it again.
  function patchPlanningPage(win, doc) {
    var grid = doc.__slipGrid && doc.__slipGrid.isConnected ? doc.__slipGrid : (doc.__slipGrid = doc.querySelector('.card-grid'));

    if (grid && !grid.__slipObserver) {
      grid.__slipObserver = new win.MutationObserver(function () {
        try {
          patchPlanning(doc);
        } catch (e) {
          diag('planning observer', e);
        }

        settle(grid);
      });
      grid.__slipObserver.observe(grid, { childList: true, subtree: true });
    }

    patchPlanning(doc);
    settle(grid);
  }

  var PLAN_CLASSES = { once: 'slip-plan slip-plan-once', timed: 'slip-plan slip-plan-timed' };

  // The badge sits in the picture's bottom-right corner, clear of the game's exclusive seal, lock badge and
  // "Activated" stamp, which all sit at the top of the card.
  function setPlanBadge(frame, doc, text) {
    var badge = ownChild(frame, '__slipPlan', 'slip-plan');
    var className = text === ONE_OFF_BADGE ? PLAN_CLASSES.once : PLAN_CLASSES.timed;

    if (!text) {
      if (badge) badge.remove();

      frame.__slipPlan = null;

      return;
    }

    if (!badge) {
      badge = doc.createElement('span');
      frame.appendChild(badge);
    }

    frame.__slipPlan = badge;

    if (badge.className !== className) badge.className = className;
    if (badge.textContent !== text) badge.textContent = text;
  }

  // ===== Station bags =====

  // Every station page (generator, heater, brewing barrel, dye vat, shredder, compressor, rat cage, material rack,
  // workbench...) draws the player's bag with the game's shared webui-bag.js: each item is a .item whose id is the
  // item's instance id. The pages show their own small tooltip for some items and none for others, under different
  // ids. The block goes into the page's tooltip while it shows the hovered item, and into a tooltip of ours when the
  // page shows none.
  var STATION_TIPS = ['itemTooltip', 'itemTip', 'recipeTooltip', 'fuelTooltip', 'tooltip'];
  var VUE_OR_OWN_PAGES = /(BackpackUI|TradeUI|ShopUI|Cooking)\.html/;

  function isStationPage(win, src) {
    return typeof win.createBagDragCoordinator === 'function' && !VUE_OR_OWN_PAGES.test(src);
  }

  function patchStation(win, doc) {
    if (!win.__slipStation) watchStation(win, doc);

    drawStationTip(win, doc);
  }

  function watchStation(win, doc) {
    var hover = { id: '', since: 0, isPressed: false };
    var redraw = function () {
      try {
        drawStationTip(win, doc);
      } catch (e) {
        diag('station tooltip', e);
      }
    };

    win.__slipStation = hover;
    trackPointer(win, doc);
    doc.addEventListener('mouseover', function (e) {
      var item = e.target && e.target.closest ? e.target.closest('.item') : null;
      var id = item && /^\d+$/.test(item.id) ? item.id : '';

      if (id !== hover.id) {
        hover.id = id;
        hover.since = Date.now();
        redraw();
        if (id) redrawLater(win, hover, redraw);
      }
    }, true);
    doc.addEventListener('mouseout', function (e) {
      if (e.relatedTarget) return;

      hover.id = '';
      redraw();
    }, true);
    doc.addEventListener('mousedown', function () { hover.isPressed = true; redraw(); }, true);
    doc.addEventListener('mouseup', function () { hover.isPressed = false; hover.since = Date.now(); }, true);
    // A button let go outside the window sends no mouseup here, so a move with no button held, or the window losing
    // focus, also ends the press.
    win.addEventListener('blur', function () { hover.isPressed = false; }, true);
    doc.addEventListener('mousemove', function (e) {
      var own = findOwn(doc);

      if (hover.isPressed && e.buttons === 0) {
        hover.isPressed = false;
        hover.since = Date.now();
      }

      if (own && own.style.display === 'block') keepOnScreen(win, own);
    }, true);

    STATION_TIPS.forEach(function (id) {
      var tip = doc.getElementById(id);

      if (tip) watchTooltip(win, tip, 'station tooltip observer', redraw);
    });
  }

  // One waiting redraw per page: a new wait replaces the old one, whose redraw would have found nothing new.
  function redrawLater(win, hover, redraw) {
    if (hover.timer) win.clearTimeout(hover.timer);

    hover.timer = win.setTimeout(function () {
      hover.timer = 0;
      redraw();
    }, STATION_TIP_DELAY_MS);
  }

  // A pass with no item to describe after one that left nothing drawn has nothing to do, so it stops at once.
  function drawStationTip(win, doc) {
    var hover = win.__slipStation;
    var configId = hover && hover.id && !hover.isPressed ? configOfInstance(hover.id) : 0;
    var model = cachedModel(hover, configId, NO_LIVE_PAGE);
    var pageTip = null;
    var i;

    if (!model && hover.isBlank) return;

    for (i = 0; i < STATION_TIPS.length; i++) {
      var tip = doc.getElementById(STATION_TIPS[i]);
      var isShown = !!tip && tip.style.display === 'block' && !tip.classList.contains('kin-mode');

      if (isShown && !pageTip && model) {
        pageTip = tip;
      } else if (tip) {
        drawBlock(doc, tip, null);
      }
    }

    if (pageTip) {
      askWhere(configId);
      drawBlock(doc, pageTip, model);
      keepOnScreen(win, pageTip);
    }

    var isOwn = !pageTip && !!model && Date.now() - hover.since >= STATION_TIP_DELAY_MS;

    // A tagged row (another mod's list) owns our tooltip while the cursor is on it.
    if (isTaggedHover(win)) {
      hover.isBlank = false;
    } else {
      drawOwnTip(win, doc, isOwn ? configId : 0, isOwn ? model : null);
      hover.isBlank = !model;
    }

    for (i = 0; i < STATION_TIPS.length; i++) settle(doc.getElementById(STATION_TIPS[i]));
  }

  // Our own tooltip on this page, kept on the document. Only ownTip adds it, and a removed one is no longer connected.
  function findOwn(doc) {
    var own = doc.__slipOwn;

    if (own === undefined) own = doc.__slipOwn = doc.getElementById('slip-own-tip');

    return own && own.isConnected ? own : null;
  }

  function ownTip(doc) {
    var own = findOwn(doc);

    if (own) return own;

    own = doc.createElement('div');
    own.id = 'slip-own-tip';
    own.className = 'slip-own-tip';
    own.__slipName = addElement(doc, own, 'div', 'slip-own-name');
    own.__slipNote = addElement(doc, own, 'div', 'slip-own-note');
    doc.body.appendChild(own);
    doc.__slipOwn = own;

    return own;
  }

  // note is the tagged row's data-slip-note, shown under the name; the station bags have none.
  function drawOwnTip(win, doc, configId, model, note) {
    var own = findOwn(doc);

    if (!model) {
      if (own && own.style.display !== 'none') own.style.display = 'none';

      return;
    }

    own = ownTip(doc);

    var known = facts.items[configId];
    var name = ownChild(own, '__slipName', 'slip-own-name');
    var text = known && known[0] ? known[0] : '';

    if (name.textContent !== text) name.textContent = text;

    drawNote(doc, own, note);
    askWhere(configId);

    if (!drawBlock(doc, own, model)) {
      own.style.display = 'none';

      return;
    }

    if (own.style.display !== 'block') own.style.display = 'block';

    keepOnScreen(win, own);
  }

  // ===== Other mods' item rows =====

  // Any element with data-slip-item="<config id>", on any of the game's pages, gets our own tooltip after the same
  // short wait as the station bags. Other mods (Storage Hub's Home tab) tag their item rows this way; the tag does
  // nothing when this mod is not installed. data-slip-name="<item name>" does the same for a mod that only knows the
  // name, and data-slip-tip="<words>" shows just those words in the same box (Crafting Hub's buttons). A name that
  // matches no item falls back to the element's words.
  // data-slip-note="<words>" on a tagged row adds those words under the item's name (Neighbor Basket's progress line).
  var TAGGED = '[data-slip-item],[data-slip-name],[data-slip-tip]';

  function patchTagged(win, doc) {
    if (win.__slipTagged) {
      // Held keys and late breakdown rows change what the tooltip says, so a shown one is drawn again each poll.
      if (isTaggedHover(win)) redrawTagged(win, doc);

      return;
    }

    if (!doc.body) return;

    var hover = { configId: 0, text: '', since: 0, isPressed: false };
    var redraw = function () { redrawTagged(win, doc); };

    win.__slipTagged = hover;
    trackPointer(win, doc);
    doc.addEventListener('mouseover', function (e) {
      var row = e.target && e.target.closest ? e.target.closest(TAGGED) : null;
      var configId = row ? taggedItem(row) : 0;
      var text = row && !configId ? row.getAttribute('data-slip-tip') || '' : '';

      if (configId === hover.configId && text === hover.text) return;

      var wasTagged = isTaggedHover(win);

      hover.configId = configId;
      hover.text = text;
      hover.since = Date.now();
      if (configId || text || wasTagged) redraw();
      if (configId || text) redrawLater(win, hover, redraw);
    }, true);
    doc.addEventListener('mouseout', function (e) {
      if (e.relatedTarget || !isTaggedHover(win)) return;

      hover.configId = 0;
      hover.text = '';
      redraw();
    }, true);
    doc.addEventListener('mousedown', function () {
      hover.isPressed = true;
      if (isTaggedHover(win)) redraw();
    }, true);
    doc.addEventListener('mouseup', function () {
      hover.isPressed = false;
      hover.since = Date.now();
      if (isTaggedHover(win)) redrawLater(win, hover, redraw);
    }, true);
    win.addEventListener('blur', function () { hover.isPressed = false; }, true);
    doc.addEventListener('mousemove', function () {
      var own = findOwn(doc);

      if (isTaggedHover(win) && own && own.style.display === 'block') keepOnScreen(win, own);
    }, true);
  }

  function redrawTagged(win, doc) {
    try {
      drawTaggedTip(win, doc);
    } catch (e) {
      diag('tagged tooltip', e);
    }
  }

  function taggedItem(row) {
    var configId = parseInt(row.getAttribute('data-slip-item'), 10) || 0;
    var name = row.getAttribute('data-slip-name');

    return configId || (name ? itemByName(name) : 0);
  }

  // Item names as the facts give them, for tags that only carry a name. Built again when new facts arrive.
  var namesFor = null;
  var idsByName = {};

  function itemByName(name) {
    if (namesFor !== facts.items) {
      namesFor = facts.items;
      idsByName = {};
      Object.keys(facts.items).forEach(function (id) {
        var known = facts.items[id];
        var key = known && known[0] ? String(known[0]).toLowerCase() : '';

        if (key && !idsByName[key]) idsByName[key] = parseInt(id, 10) || 0;
      });
    }

    return idsByName[String(name).toLowerCase()] || 0;
  }

  function isTaggedHover(win) {
    var hover = win.__slipTagged;

    return !!(hover && (hover.configId > 0 || hover.text));
  }

  function drawTaggedTip(win, doc) {
    var hover = win.__slipTagged;
    var isShown = isTaggedHover(win) && !hover.isPressed && Date.now() - hover.since >= STATION_TIP_DELAY_MS;

    addStyle(doc);

    var note = isShown ? hoveredNote(win, doc) : '';

    if (isShown && !hover.configId) return drawOwnText(win, doc, hover.text, note);

    var model = isShown ? cachedModel(hover, hover.configId, NO_LIVE_PAGE) : null;
    var known = facts.items[hover.configId];

    // An item with nothing more to say still shows its name.
    if (model && !model.sections.length && !model.hint) return drawOwnText(win, doc, known && known[0] ? known[0] : hover.text, note);

    drawOwnTip(win, doc, isShown ? hover.configId : 0, model, note);
  }

  // The note of the tagged row under the cursor, read on every draw: the mod that drew the row may have drawn it
  // again since the cursor arrived, with new words.
  function hoveredNote(win, doc) {
    var pointer = win.__slipPointer;
    var under = pointer && pointer.isKnown ? doc.elementFromPoint(pointer.x, pointer.y) : null;
    var row = under && under.closest ? under.closest(TAGGED) : null;

    return row ? row.getAttribute('data-slip-note') || '' : '';
  }

  function drawNote(doc, own, note) {
    var line = ownChild(own, '__slipNote', 'slip-own-note');

    if (!line) {
      line = doc.createElement('div');
      line.className = 'slip-own-note';
      ownChild(own, '__slipName', 'slip-own-name').after(line);
      own.__slipNote = line;
    }

    if (line.textContent !== (note || '')) line.textContent = note || '';

    var display = note ? 'block' : 'none';

    if (line.style.display !== display) line.style.display = display;
  }

  // The words alone, in the box an item tooltip uses.
  function drawOwnText(win, doc, text, note) {
    if (!text) return drawOwnTip(win, doc, 0, null);

    var own = ownTip(doc);
    var name = ownChild(own, '__slipName', 'slip-own-name');

    if (name.textContent !== text) name.textContent = text;

    drawNote(doc, own, note);
    drawBlock(doc, own, null);
    if (own.style.display !== 'block') own.style.display = 'block';

    keepOnScreen(win, own);
  }

  // ===== HUD stat bars =====

  // Each bar's hover box gets how fast the stat drops and when it runs out, and the satiety bar how long the food at
  // home lasts. The rows follow attrList; a mismatch (a redraw in flight) waits for the next poll.
  function patchHud(win, doc) {
    watchStatsUi(win, doc);

    var setup = vueSetup(doc);
    var list = setup && setup.state ? setup.state.attrList : null;

    if (!list || isHudDrawn(doc, list)) return;

    var tips = doc.querySelectorAll('.status-row > .progress-bar-wrapper > .status-tooltip');

    if (tips.length !== list.length) return;

    var hud = { version: dataVersion, icons: [], tips: tips };

    for (var i = 0; i < tips.length; i++) {
      var icon = list[i] && list[i].attrIcon;
      var lines = hudLines(hudStat(icon));
      // Stats UI hides the game's box and opens its own in the same bar while the cursor is on it.
      var other = tips[i].parentNode.querySelector(':scope > .stui-stat-tip');

      hud.icons.push(icon);
      setRateLines(tips[i], doc, lines, hud);
      if (other) setRateLines(other, doc, lines, hud);
    }

    doc.__slipHud = hud;
  }

  // A poll has nothing to do when no data came in since the last pass, the bars are the same ones, and every box that
  // pass drew in (and every Stats UI box now open) still holds what it put there.
  function isHudDrawn(doc, list) {
    var hud = doc.__slipHud;
    var others = doc.__slipStui || (doc.__slipStui = doc.getElementsByClassName('stui-stat-tip'));
    var i;

    if (!hud || hud.version !== dataVersion || hud.icons.length !== list.length) return false;

    for (i = 0; i < list.length; i++) {
      if (hud.icons[i] !== (list[i] && list[i].attrIcon) || !isRateDrawn(hud.tips[i], hud)) return false;
    }

    for (i = 0; i < others.length; i++) {
      if (!isRateDrawn(others[i], hud)) return false;
    }

    return true;
  }

  function isRateDrawn(tip, hud) {
    var box = tip.__slipRate;

    return tip.isConnected && tip.__slipRateBy === hud && (!box || box.parentNode === tip);
  }

  // Stats UI builds a new box each time its numbers change, so ours is put back into the new one before it is drawn.
  // This stays a synchronous pass rather than waiting for the next frame, so the new box is never shown without our
  // lines; the boxes it adds are then dropped from the queue, so they do not wake it again.
  function watchStatsUi(win, doc) {
    if (win.__slipStatsUi || !doc.body) return;

    var observer = new win.MutationObserver(function (records) {
      for (var r = 0; r < records.length; r++) {
        for (var n = 0; n < records[r].addedNodes.length; n++) {
          var node = records[r].addedNodes[n];

          if (node.nodeType === 1 && node.classList.contains('stui-stat-tip')) {
            try {
              patchHud(win, doc);
            } catch (e) {
              diag('stats ui tooltip', e);
            }

            observer.takeRecords();

            return;
          }
        }
      }
    });

    win.__slipStatsUi = observer;
    observer.observe(doc.body, { childList: true, subtree: true });
  }

  function hudStat(icon) {
    for (var i = 0; icon && i < HUD_STATS.length; i++) {
      if (String(icon).indexOf(HUD_STATS[i][0]) >= 0) return HUD_STATS[i][1];
    }

    return '';
  }

  function hudLines(stat) {
    var lines = [];
    var rate = stats.rates[stat];

    if (!stat) return lines;
    if (features.rates && rate && rate[0] > 0) lines.push(say('rate', trimNumber(rate[0], 1), formatSeconds(rate[1] * 3600)));
    if (features.forecast && stat === SATIETY && stats.food !== null) lines.push(say('forecast', formatDays(stats.food)));

    return lines;
  }

  // Appended after the game's own lines in the hover box; the game sets those by text, so its updates leave ours alone.
  // hud marks the pass that drew the box, which tells a later poll this box needs nothing.
  function setRateLines(tip, doc, lines, hud) {
    var box = ownChild(tip, '__slipRate', 'slip-rate');
    var text = lines.join('\n');

    tip.__slipRateBy = hud;

    if (!lines.length) {
      if (box) box.remove();
      tip.__slipRate = null;

      return;
    }

    if (!box) {
      box = doc.createElement('div');
      box.className = 'slip-rate';
      box.style.whiteSpace = 'pre-line';
      tip.appendChild(box);
    }

    tip.__slipRate = box;
    if (box.textContent !== text) box.textContent = text;
  }

  // ===== Finding the pages =====

  // The item tooltips always get the block; which sections it shows is the player's choice in the tooltip settings.
  function always() {
    return true;
  }

  var PAGES = [
    { pattern: /BackpackUI\.html/, isOn: always, patch: patchBackpack },
    { pattern: /TradeUI\.html/, isOn: always, patch: patchTrade },
    { pattern: /(^|\/)ShopUI\.html/, isOn: always, patch: patchShop },
    { pattern: /Cooking\.html/, isOn: function () { return features.codex; }, patch: patchCooking },
    { pattern: /BrewPanel\.html/, isOn: function () { return features.brew; }, patch: patchBrew },
    { pattern: /ItemPickup\.html/, isOn: function () { return features.scrap; }, patch: function (win, doc) { patchPickup(doc); } },
    { pattern: /RogueSelect\.html/, isOn: function () { return features.plan; }, patch: patchPlanningPage },
    { pattern: /CoreUI1\.html/, isOn: function () { return features.rates || features.forecast; }, patch: patchHud },
    { pattern: /\.html/, isOn: always, isFor: isStationPage, patch: patchStation }
  ];

  // The game's pages are same-origin iframes of this document, created and dropped whenever the game likes. Which
  // pages match a frame is worked out again only when its address changes.
  function patchPages() {
    if (!features) return;

    for (var i = 0; i < pageFrames.length; i++) {
      var frame = pageFrames[i];
      var src = frame.src || '';

      if (frame.__slipSrc !== src) {
        frame.__slipSrc = src;
        frame.__slipPages = PAGES.filter(function (page) { return page.pattern.test(src); });
      }

      for (var p = 0; p < frame.__slipPages.length; p++) {
        if (frame.__slipPages[p].isOn()) patchFrame(frame, frame.__slipPages[p], src);
      }

      watchTagged(frame);
    }

    // Other mods' panels drawn over the game (Storage Hub's) live in this page itself, not in a game page.
    watchTaggedHere();
  }

  function watchTaggedHere() {
    try {
      if (document.readyState !== 'loading') patchTagged(window, document);
    } catch (e) {}
  }

  function patchFrame(frame, page, src) {
    var win = null;
    var doc = null;

    try {
      win = frame.contentWindow;
      doc = frame.contentDocument;
    } catch (e) {}

    if (!win || !doc) return diag('page access', 'cannot reach ' + src);
    if (doc.readyState === 'loading') return;
    if (page.isFor && !page.isFor(win, src)) return;

    try {
      addStyle(doc);
      page.patch(win, doc);
    } catch (e) {
      diag(String(page.pattern), e);
    }
  }

  // Every page gets the tagged-row watcher, patched by this mod or not; it costs nothing until a tagged row shows.
  function watchTagged(frame) {
    try {
      var win = frame.contentWindow;
      var doc = frame.contentDocument;

      if (win && doc && doc.readyState !== 'loading') patchTagged(win, doc);
    } catch (e) {}
  }

  setInterval(repaint, POLL_MS);

  window.__slinfopack = {
    setFeatures: setFeatures, setFacts: setFacts, setCounts: setCounts, setInstance: setInstance,
    setKeys: setKeys, setWhere: setWhere, setWanted: setWanted, setStats: setStats,
    // Other mods check this before leaving their data-slip-tip, data-slip-name and data-slip-note tooltips to this mod.
    tags: ['item', 'name', 'tip', 'note']
  };
  send('ready');
})();

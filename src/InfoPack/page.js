// Info Pack's web half. Runs in Root.html, the document every game page iframe sits in. It only reads: it adds a
// small wiki block to the game's own item tooltips (backpack and storage, trade, shop bag, cooking), keeps those
// tooltips inside the window, puts Scrap chips and spoil corners on backpack slots, a tag on the pickup list, a quality
// readout on the brew panel and duration badges on Survival Planning cards, all in the game's own tooltip palette.
// The tooltip block is split into sections (effects, freshness, home count, recipes, sources...) that the player
// shows always, only while the detail key is held, or never. The game pages come and go as iframes and Vue redraws
// them at will, so every addition is checked on a short poll and on changes to the tooltip, and put back when gone.
// Talks to the plugin through vuplex.postMessage("slmod|infopack|<verb>") and is driven by window.__slinfopack.
(function () {
  if (window.__slinfopack) return;

  var PREFIX = 'slmod|infopack|';
  var POLL_MS = 200;
  var INSTANCE_RETRY_MS = 3000;
  var WHERE_RETRY_MS = 2000;
  var COOKING_TIP_WIDTH = '300px';
  var LIST_ROWS = 3;
  // Some materials go into hundreds of recipes; even with the detail key held the tooltip has to fit the window.
  var DETAIL_LIST_ROWS = 12;
  // The game offsets its tooltips 15 page pixels from the cursor; EDGE keeps a moved one off the window's border.
  var CURSOR_GAP = 15;
  var EDGE = 4;
  var SECONDS_PER_DAY = 86400;

  // The block takes the game's tooltip look: its 12px text, #aaa for secondary lines, its green and red for gains
  // and losses, and thin dividers like the trade tooltip's description line. Section headers are small capitals so
  // the eye can jump between them. The chip, tag and spoil corner copy the backpack's own slot badges.
  var CSS = [
    '.slip-tip{display:flex;flex-direction:column;min-width:190px;max-width:280px;margin-top:3px;padding-top:7px;border-top:1px solid rgba(255,255,255,.12);font-size:12px;line-height:1.45;font-weight:normal;color:#c9c9c9;text-align:left;white-space:normal}',
    '.slip-sec{display:flex;flex-direction:column;gap:2px}',
    '.slip-sec+.slip-sec{margin-top:6px;padding-top:6px;border-top:1px solid rgba(255,255,255,.07)}',
    '.slip-h{font-size:10px;font-weight:700;line-height:14px;letter-spacing:.08em;text-transform:uppercase;color:#8e97a8}',
    '.slip-row{display:flex;align-items:baseline;gap:8px}',
    '.slip-txt{flex:1 1 auto;min-width:0;overflow-wrap:break-word}',
    '.slip-meta{flex:none;font-size:11px;color:#8a8f99;white-space:nowrap}',
    '.slip-subline{display:block;font-size:11px;line-height:1.35;color:#8a8f99}',
    '.slip-where{padding-left:10px;color:#aaa}',
    '.slip-dim{color:#8a8f99}',
    '.slip-good{color:#66bb6a}',
    '.slip-bad{color:#ef5350}',
    '.slip-amber{color:#e8a33a}',
    '.slip-fresh{color:#9ccc65}',
    '.slip-warn{color:#c9a94f}',
    '.slip-more{font-size:11px;color:#8a8f99}',
    '.slip-about{color:#b9b9b9;line-height:1.5}',
    '.slip-ref{color:#e2e2e2}',
    '.slip-ico{display:inline-block;width:16px;height:16px;object-fit:contain;vertical-align:-4px;margin-right:3px;filter:drop-shadow(0 1px 1px rgba(0,0,0,.6))}',
    '.slip-noico{background:rgba(255,255,255,.08);border-radius:3px}',
    '.slip-n{color:#aaa;margin-left:3px}',
    '.slip-chips{display:flex;flex-wrap:wrap;gap:4px}',
    '.slip-chip{display:inline-flex;align-items:center;gap:3px;padding:0 6px 0 4px;border:1px solid;border-radius:9px;font-size:12px;font-weight:700;line-height:17px;font-variant-numeric:tabular-nums}',
    '.slip-chip svg{width:12px;height:12px;flex:none}',
    '.slip-chip-name{font-weight:600}',
    '.slip-up{color:#7fd67a;background:rgba(102,187,106,.13);border-color:rgba(102,187,106,.35)}',
    '.slip-down{color:#f07a76;background:rgba(239,83,80,.13);border-color:rgba(239,83,80,.35)}',
    '.slip-key{display:inline-block;font-size:10px;line-height:14px;color:#9aa3b5;background:rgba(255,255,255,.06);border:1px solid rgba(255,255,255,.1);border-radius:3px;padding:0 4px}',
    '.slip-hint{margin-top:7px;text-align:right}',
    '.tooltip.slip-own-fresh>.tooltip-shelf-life{display:none}',
    // A fixed tooltip of auto width narrows near the right border, so its size would change as it is moved there;
    // sized by its content it measures the same wherever keepOnScreen puts it.
    '.tooltip:has(>.slip-tip){width:max-content;max-width:300px}',
    '#recipeTooltip:has(>.slip-tip){width:max-content}',
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
    '.slip-rate{margin-top:6px;padding-top:5px;border-top:1px dashed rgba(255,255,255,.18);font-size:12px;line-height:1.5;color:#ccc}'
  ].join('\n');

  // Everything Info Pack draws, so a change of settings can take it all away again.
  var DRAWN = '.slip-tip, .slip-brew, .slip-scrap, .slip-spoil, .slip-tag, .slip-plan, .slip-wanted, .slip-rate';

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
    effects: 'always', fresh: 'always', notes: 'always', home: 'always', usedin: 'always', grow: 'always',
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
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc) continue;

      doc.querySelectorAll(DRAWN).forEach(function (el) { el.remove(); });
      doc.querySelectorAll('.slip-own-fresh').forEach(function (el) { el.classList.remove('slip-own-fresh'); });
    }
  }

  function setFacts(data) {
    var scrap = {};

    data = data || {};
    (data.scrap || []).forEach(function (id) { scrap[id] = true; });
    facts = {
      desc: data.desc || {}, dish: data.dish || {}, brew: data.brew || {}, scrap: scrap, plan: data.plan || {},
      bait: data.bait || {}, robot: data.robot || {}, ui: data.ui || {}, items: data.items || {}, wiki: data.wiki || {}
    };
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
    repaint();
  }

  // rates: {stat: [drop per game hour, hours left]}; food: the days the food at home lasts, when known.
  function setStats(data) {
    data = data || {};
    stats = { rates: data.rates || {}, food: data.food > 0 || data.food === 0 ? data.food : null };
    repaint();
  }

  function setCounts(data) {
    counts = data || {};
    hasCounts = true;
    repaint();
  }

  function setInstance(id, configId) {
    instances[id] = configId || 0;
    asked[id] = Date.now();
    repaint();
  }

  // The plugin reads the detail and breakdown keys itself, since the page never sees modifier keys.
  function setKeys(state) {
    keys = { detail: !!(state && state.detail), where: !!(state && state.where) };
    repaint();
  }

  function setWhere(configId, rows) {
    whereRows[String(configId)] = rows || [];
    repaint();
  }

  // Data that changes what a shown tooltip says is drawn now rather than on the next poll.
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
    makeNow: 'can make {0} now', useBench: 'Workbench', useCook: 'Cooking', useCookTime: 'Cooking, {0}', dishStat: '{0} {1}', useBrew: 'Brewed alone, quality {0}',
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
  // page: { live: {days, expired, polluted} or null, withEffects }.
  function tipModel(configId, page) {
    var wiki = facts.wiki[configId] || {};
    var context = { id: configId, wiki: wiki, page: page, isRotInFresh: isOpen(tipSettings.modes.fresh) && wiki.rot > 0 };
    var sections = [];
    var needsHint = false;

    tipSettings.order.forEach(function (key) {
      var mode = tipSettings.modes[key];
      var rows = mode === 'always' || mode === 'detail' ? sectionRows(key, context) : [];

      if (!rows.length) return;

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
      hint: needsHint && !keys.detail ? say('hintMore', tipSettings.detailKey) : '',
      isOwnFresh: isOpen(tipSettings.modes.fresh) && liveDaysLeft(page) > 0 && !page.live.expired && !page.live.polluted
    };
  }

  // Rows are { chips } or { parts, cls, meta, metaCls, sub }: parts are text and item references, meta sits on the
  // right in small type, sub is a second, quieter line.
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

    (context.wiki.fx || []).forEach(function (value, i) {
      if (!value || !STATS[i]) return;

      chips.push({ stat: i, text: signed(value), name: keys.detail ? say(STATS[i].phrase) : '', isGain: value > 0 });
    });

    return chips.length ? [{ chips: chips }] : [];
  }

  // Under a day is red and under three amber; anything longer is still fresh.
  function freshRows(context) {
    var rows = [];
    var wiki = context.wiki;
    var live = context.page.live;
    var days = liveDaysLeft(context.page);

    if (days > 0 && !live.expired) rows.push({ parts: [say('freshLeft', formatDays(days))], cls: days < 1 ? 'slip-bad' : days < 3 ? 'slip-amber' : 'slip-fresh' });
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
        rest.push({ parts: [itemRef(use.out)], meta: use.t > 0 ? say('useCookTime', formatSeconds(use.t)) : say('useCook'), metaCls: 'slip-dim', sub: dishStats(use.out) });
      } else if (use.k === 'brew') {
        rest.push.apply(rest, brewRows(use));
      }
    });

    return ready.concat(other, rest);
  }

  // What eating the dish does, the way the effects section words it: "+30 Satiety, +5 Mood".
  function dishStats(dishId) {
    var fx = (facts.wiki[dishId] || {}).fx || [];
    var stats = [];

    fx.forEach(function (value, i) {
      if (value && STATS[i]) stats.push(say('dishStat', signed(value), say(STATS[i].phrase)));
    });

    return stats.join(', ');
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
    var box = tip.querySelector(':scope > .slip-tip');

    if (!model || (!model.sections.length && !model.hint)) {
      if (box) box.remove();

      return false;
    }

    if (!box) {
      box = doc.createElement('div');
      box.className = 'slip-tip';
    }

    if (tip.lastElementChild !== box) tip.appendChild(box);

    var key = JSON.stringify(model);

    if (box.__slipKey === key) return true;

    box.__slipKey = key;
    box.textContent = '';
    model.sections.forEach(function (section) { box.appendChild(sectionElement(doc, section)); });

    if (model.hint) {
      var hint = addElement(doc, box, 'div', 'slip-hint');

      addElement(doc, hint, 'span', 'slip-key').textContent = model.hint;
    }

    return true;
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

  function chipRow(doc, chips) {
    var row = doc.createElement('div');

    row.className = 'slip-chips';
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

    el.className = 'slip-row' + (row.cls ? ' ' + row.cls : '');
    row.parts.forEach(function (part) { text.appendChild(typeof part === 'string' ? doc.createTextNode(part) : refElement(doc, part)); });

    if (row.sub) addElement(doc, text, 'span', 'slip-subline').textContent = row.sub;
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

  // The block's live freshness line replaces the game's own shelf-life line, which says the same in plainer type.
  // Only touched when it changes: re-adding a class still counts as a change to the tooltip's watcher.
  function setOwnFresh(tip, isOwn) {
    if (tip.classList.contains('slip-own-fresh') !== isOwn) tip.classList.toggle('slip-own-fresh', isOwn);
  }

  // ===== Keeping a tooltip on screen =====

  // The game places its tooltips as if they were 200x100, so a tall one runs off the bottom. Ours is placed the way
  // the game does it (15px right of and below the cursor), flipped to the other side of the cursor on the axis where it
  // does not fit and pushed inside the window when neither side does. It runs straight after each of the game's own
  // moves (watchTooltip), before the frame is drawn, so the two never show as a jump.
  function keepOnScreen(win, tip) {
    if (!features.onScreen || tip.style.display === 'none') return;

    var rect = tip.getBoundingClientRect();

    if (!rect.width || !rect.height) return;

    var zoom = zoomOf(win, tip);
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
  // next poll, so the block never shows the last item's facts and placement happens before the frame is drawn. Our own
  // writes wake it once more; that pass finds nothing to change, so it settles.
  function watchTooltip(win, tip, kind, redraw) {
    if (tip.__slipWatched) return;

    tip.__slipWatched = true;
    new win.MutationObserver(function () {
      try {
        redraw();
      } catch (e) {
        diag(kind, e);
      }
    }).observe(tip, { attributes: true, attributeFilter: ['style', 'class'], childList: true, characterData: true, subtree: true });
  }

  // ===== Shared page helpers =====

  function addStyle(doc) {
    var style = doc.getElementById('slip-style');

    if (!doc.head) return;

    if (!style) {
      style = doc.createElement('style');
      style.id = 'slip-style';
      doc.head.appendChild(style);
    }

    if (style.textContent !== CSS) style.textContent = CSS;
  }

  function vueSetup(doc) {
    var app = doc.getElementById('app');

    return app && app._vnode && app._vnode.component ? app._vnode.component.setupState : null;
  }

  // The game's Vue item tooltip: the element holding .tooltip-name (the same in BackpackUI, TradeUI and ShopUI).
  function vueTooltip(doc) {
    var name = doc.querySelector('.tooltip > .tooltip-name');

    return name ? name.parentNode : null;
  }

  // Draws the block for whatever a Vue tooltip shows (configId 0 for nothing of ours) and places the tooltip.
  function drawVueTip(win, doc, tip, configId, page) {
    var model = configId > 0 ? tipModel(configId, page) : null;

    askWhere(configId);
    drawBlock(doc, tip, model);
    setOwnFresh(tip, !!(model && model.isOwnFresh));
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
  }

  function setChip(parent, doc, isWanted, className, text) {
    var chip = parent.querySelector(':scope > .' + className);

    if (!isWanted) {
      if (chip) chip.remove();

      return;
    }

    if (chip) return;

    chip = doc.createElement('span');
    chip.className = className;
    chip.textContent = text;
    parent.appendChild(chip);
  }

  // ===== Backpack =====

  function patchBackpack(win, doc) {
    var setup = vueSetup(doc);

    if (!setup) return diag('backpack', 'no Vue state');

    patchVueTooltip(win, doc, 'backpack', function (state, tip) {
      var hover = state.tooltipState;
      var item = hover && hover.visible ? hover.item : null;
      var live = item ? { days: item.shelfLifeDays, expired: !!item.isExpired, polluted: !!item.polluted } : null;

      drawVueTip(win, doc, tip, item ? item.cfgId : 0, { live: live, withEffects: true });
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
    var grids = doc.querySelectorAll('.grid-container');

    if (!sides || grids.length !== sides.length) return;

    for (var s = 0; s < sides.length; s++) {
      var bag = setup.getBag(sides[s]);
      var items = bag && bag.items ? bag.items : [];
      var cells = grids[s].querySelectorAll(':scope > .item');

      if (cells.length !== items.length) continue;

      for (var i = 0; i < cells.length; i++) {
        var item = items[i];

        setChip(cells[i], doc, isScrap(item.cfgId) && !(item.useTimesMax > 1), 'slip-scrap', say('scrapChip'));
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
  function setSpoilBadge(cell, doc, level) {
    var badge = cell.querySelector(':scope > .slip-spoil');
    var className = 'slip-spoil slip-spoil-' + level;

    if (!level) {
      if (badge) badge.remove();

      return;
    }

    if (!badge) {
      badge = doc.createElement('span');
      cell.appendChild(badge);
    }

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

      drawVueTip(win, doc, tip, configId, { live: null, withEffects: !isShelf || !!features.stats });
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

    if (win.__slipCooking) return drawCookingTip(win, doc, tip);

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
    new win.MutationObserver(function () {
      try {
        onCookingTipChanged(win, doc, tip);
      } catch (e) {
        diag('cooking recipe tooltip', e);
      }
    }).observe(tip, { attributes: true, attributeFilter: ['style'], childList: true, characterData: true, subtree: true });
  }

  // The tooltip's text without our block: what the page itself wrote.
  function pageTextOf(tip) {
    var text = '';

    for (var node = tip.firstChild; node; node = node.nextSibling) {
      if (!(node.nodeType === 1 && node.classList.contains('slip-tip'))) text += node.textContent;
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

      if (recipe && recipe.name === firstLine && facts.dish[recipe.recipeId]) return { dish: facts.dish[recipe.recipeId] };
    }

    return null;
  }

  // The cooking window names most items by config id; only those it leaves at 0 need the instance lookup.
  function cookingConfigId(subject) {
    if (!subject) return 0;
    if (!subject.item) return subject.dish;

    return subject.item.configId > 0 ? subject.item.configId : configOfInstance(subject.item.itemId);
  }

  function drawCookingTip(win, doc, tip) {
    var isShown = tip.style.display === 'block';
    var configId = features && features.codex ? cookingConfigId(tip.__slipSubject) : 0;
    var isDrawn = drawBlock(doc, tip, configId > 0 ? tipModel(configId, { live: null, withEffects: true }) : null);
    var width = isDrawn ? COOKING_TIP_WIDTH : '';

    if (tip.style.maxWidth !== width) tip.style.maxWidth = width;

    if (!isShown) return;

    askWhere(configId);
    keepOnScreen(win, tip);
  }

  // ===== Brew panel =====

  // The panel's quality bar shows the tier but not how close the mix is to the next one. Shown before brewing only,
  // when the panel itself shows its marker. Thresholds are normalised the way the panel draws them, and a tier is
  // reached at its threshold (the game ranks a mix below P1 lowest, at or above P2 highest).
  function patchBrew(win, doc) {
    var state = win.State;
    var text = win.i18nText || {};
    var labels = doc.querySelector('.qbar > .qlabels');
    var box = doc.getElementById('slip-brew');
    var isShown = !!(features && features.brew && state && labels && state.brewState === 0 && state.materialCount > 0 && state.resultCount > 0);

    if (!isShown) {
      if (box) box.style.display = 'none';

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

    box.style.display = '';
    box.textContent = say('brewScore', fixed(state.moodDensity), text.tierMid || 'Normal', fixed(low), text.tierHigh || 'Strong', fixed(high));
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

    for (var i = 0; i < rows.length; i++) {
      var info = rows[i].querySelector('.item-info');

      if (info) setChip(info, doc, isScrap(items[i].configId), 'slip-tag', say('scrapChip'));
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
      var frame = slots[i].querySelector(':scope > .card > .icon-frame');

      if (frame) setPlanBadge(frame, doc, cards[i] ? facts.plan[cards[i].id] : null);
    }
  }

  // The poll alone can leave the old card's badge on a new card for a moment after a refresh, so the grid is also
  // watched. Our own badge writes trigger one more pass that finds nothing to change, so it does not loop.
  function patchPlanningPage(win, doc) {
    var grid = doc.querySelector('.card-grid');

    if (grid && win.__slipPlanGrid !== grid) {
      win.__slipPlanGrid = grid;
      new win.MutationObserver(function () {
        try {
          patchPlanning(doc);
        } catch (e) {
          diag('planning observer', e);
        }
      }).observe(grid, { childList: true, subtree: true });
    }

    patchPlanning(doc);
  }

  // The badge sits in the picture's bottom-right corner, clear of the game's exclusive seal, lock badge and
  // "Activated" stamp, which all sit at the top of the card.
  function setPlanBadge(frame, doc, text) {
    var badge = frame.querySelector(':scope > .slip-plan');
    var className = 'slip-plan ' + (text === ONE_OFF_BADGE ? 'slip-plan-once' : 'slip-plan-timed');

    if (!text) {
      if (badge) badge.remove();

      return;
    }

    if (!badge) {
      badge = doc.createElement('span');
      frame.appendChild(badge);
    }

    if (badge.className !== className) badge.className = className;
    if (badge.textContent !== text) badge.textContent = text;
  }

  // ===== HUD stat bars =====

  // Each bar's hover box gets how fast the stat drops and when it runs out, and the satiety bar how long the food at
  // home lasts. The rows follow attrList; a mismatch (a redraw in flight) waits for the next poll.
  function patchHud(win, doc) {
    var setup = vueSetup(doc);
    var list = setup && setup.state ? setup.state.attrList : null;
    var tips = doc.querySelectorAll('.status-row > .progress-bar-wrapper > .status-tooltip');

    if (!list || tips.length !== list.length) return;

    for (var i = 0; i < tips.length; i++) setRateLines(tips[i], doc, hudLines(hudStat(list[i] && list[i].attrIcon)));
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
  function setRateLines(tip, doc, lines) {
    var box = tip.querySelector(':scope > .slip-rate');
    var text = lines.join('\n');

    if (!lines.length) {
      if (box) box.remove();

      return;
    }

    if (!box) {
      box = doc.createElement('div');
      box.className = 'slip-rate';
      box.style.whiteSpace = 'pre-line';
      tip.appendChild(box);
    }

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
    { pattern: /CoreUI1\.html/, isOn: function () { return features.rates || features.forecast; }, patch: patchHud }
  ];

  // The game's pages are same-origin iframes of this document, created and dropped whenever the game likes.
  function patchPages() {
    var frames = document.querySelectorAll('iframe');

    if (!features) return;

    for (var i = 0; i < frames.length; i++) {
      var src = frames[i].src || '';

      for (var p = 0; p < PAGES.length; p++) {
        if (PAGES[p].pattern.test(src) && PAGES[p].isOn()) patchFrame(frames[i], PAGES[p], src);
      }
    }
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

    try {
      addStyle(doc);
      page.patch(win, doc);
    } catch (e) {
      diag(String(page.pattern), e);
    }
  }

  setInterval(repaint, POLL_MS);

  window.__slinfopack = {
    setFeatures: setFeatures, setFacts: setFacts, setCounts: setCounts, setInstance: setInstance,
    setKeys: setKeys, setWhere: setWhere, setWanted: setWanted, setStats: setStats
  };
  send('ready');
})();

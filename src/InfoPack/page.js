// Info Pack's web half. Runs in Root.html, the document every game page iframe sits in. It only reads: it adds lines
// to the game's own tooltips (backpack, trade, cooking), a Scrap chip to backpack slots, a tag to the pickup list, a
// quality readout to the brew panel and duration badges to Survival Planning cards, all styled with the game's own
// classes and colours. The game pages come and go as iframes and Vue redraws them at will, so every addition is
// checked on a short poll and put back when it is gone.
// Talks to the plugin through vuplex.postMessage("slmod|infopack|<verb>") and is driven by window.__slinfopack.
(function () {
  if (window.__slinfopack) return;

  var PREFIX = 'slmod|infopack|';
  var POLL_MS = 200;
  var INSTANCE_RETRY_MS = 3000;
  var COOKING_TIP_WIDTH = '300px';

  // Lines in a game tooltip take the page's own tooltip-category look (12px, #aaa); the stat line takes the colour the
  // trade tooltip gives an item description, and the scrap line the colour the backpack gives compost output. The chip
  // copies the backpack's .item-use badge into the bottom-left corner, which scrap items never use, and the pickup tag
  // copies the pickup list's own count badge at a smaller size.
  var CSS = [
    '.slip-lines{display:flex;flex-direction:column;gap:4px}',
    '.slip-line.slip-stats{color:#c9c9c9}',
    '.slip-line.slip-warn{color:#c9a94f}',
    '.slip-scrap{position:absolute;bottom:5px;left:3px;font-size:10px;font-weight:700;color:#c9a94f;background:rgba(0,0,0,.7);border:1px solid rgba(255,255,255,.12);padding:0 5px;border-radius:8px;line-height:13px;text-shadow:0 1px 2px rgba(0,0,0,.85);box-shadow:0 1px 3px rgba(0,0,0,.45);pointer-events:none;z-index:4}',
    '.slip-tag{font-size:11px;font-weight:bold;color:#c9a94f;background-color:rgba(0,0,0,.35);border:1px solid rgba(201,169,79,.35);padding:1px 6px;border-radius:3px;text-shadow:.7px .7px 1.4px black;flex-shrink:0}',
    '.slip-brew{margin-top:4px;font-size:10px;color:#7e756c;text-align:center}',
    '.slip-plan{position:absolute;right:3px;bottom:3px;font-size:12px;font-weight:700;line-height:15px;padding:0 5px;border-radius:3px;letter-spacing:0;pointer-events:none;z-index:2;box-shadow:0 1px 3px rgba(0,0,0,.6)}',
    '.slip-plan-timed{color:#1a1a1a;background:#ffc83a;border:1px solid rgba(255,235,180,.6)}',
    '.slip-plan-once{color:#0d1f27;background:#6ec6e6;border:1px solid rgba(205,240,255,.6)}',
    '.card.locked .slip-plan{opacity:.45}'
  ].join('\n');

  // PlanBadges.OneOffBadge on the plugin side: a badge with this text gets the one-off colour, any other the timed one.
  var ONE_OFF_BADGE = '1x';

  var features = null;
  var facts = { stats: {}, desc: {}, dish: {}, brew: {}, scrap: {}, plan: {}, ui: {} };
  var counts = {};
  var hasCounts = false;
  var instances = {};
  var asked = {};
  var reported = {};
  var cookingTip = null;

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
  }

  function setFacts(data) {
    var scrap = {};

    data = data || {};
    (data.scrap || []).forEach(function (id) { scrap[id] = true; });
    facts = { stats: data.stats || {}, desc: data.desc || {}, dish: data.dish || {}, brew: data.brew || {}, scrap: scrap, plan: data.plan || {}, ui: data.ui || {} };
  }

  function setCounts(data) {
    counts = data || {};
    hasCounts = true;
  }

  function setInstance(id, configId) {
    instances[id] = configId || 0;
    asked[id] = Date.now();

    if (cookingTip && String(cookingTip.item.itemId) === id) refreshCookingItemTip();
  }

  // The config id of an item the page only knows by instance id. The first ask and every ask after a miss go to the
  // plugin; 0 means "not known yet", and the answer lands on the next poll.
  function configOfInstance(id) {
    var key = String(id);
    var known = instances[key];

    if (known > 0) return known;
    if (!id || (asked[key] && Date.now() - asked[key] < INSTANCE_RETRY_MS)) return 0;

    asked[key] = Date.now();
    send('inst|' + key);

    return 0;
  }

  // ===== What to say about an item =====

  // The plugin sends the page's words in the game's language (Texts.cs); English stands in until they arrive.
  var ENGLISH = {
    home: 'At home: {0}',
    brew: 'Brew quality: {0} of {1}',
    scrap: 'Only useful as scrap',
    scrapChip: 'Scrap',
    brewScore: 'Quality {0}: {1} from {2}, {3} from {4}'
  };

  function say(key) {
    var values = Array.prototype.slice.call(arguments, 1);

    return (facts.ui[key] || ENGLISH[key]).replace(/\{(\d)\}/g, function (token, slot) { return values[slot]; });
  }

  function isScrap(configId) {
    return !!(features && features.scrap && facts.scrap[configId]);
  }

  // Each line is { text, kind }: kind picks the colour, '' for the plain tooltip grey.
  function itemLines(configId, withStats) {
    var lines = [];
    var tier = facts.brew[configId];

    if (!features || !(configId > 0)) return lines;

    if (withStats && features.stats && facts.stats[configId]) lines.push({ text: facts.stats[configId], kind: 'slip-stats' });
    if (features.counts && hasCounts) lines.push({ text: say('home', counts[configId] || 0), kind: '' });
    if (features.brew && tier) lines.push({ text: say('brew', tier[0], tier[1]), kind: '' });
    if (isScrap(configId)) lines.push({ text: say('scrap'), kind: 'slip-warn' });

    return lines;
  }

  // The cooking codex: what the item does, then everything itemLines says.
  function codexLines(configId) {
    var lines = [];

    if (features && features.codex && configId > 0) {
      if (facts.desc[configId]) lines.push(facts.desc[configId]);
      if (facts.stats[configId]) lines.push(facts.stats[configId]);
    }

    itemLines(configId, false).forEach(function (line) { lines.push(line.text); });

    return lines;
  }

  // ===== Shared page helpers =====

  function addStyle(doc) {
    if (doc.getElementById('slip-style') || !doc.head) return;

    var style = doc.createElement('style');
    style.id = 'slip-style';
    style.textContent = CSS;
    doc.head.appendChild(style);
  }

  function vueSetup(doc) {
    var app = doc.getElementById('app');

    return app && app._vnode && app._vnode.component ? app._vnode.component.setupState : null;
  }

  // The game's Vue tooltip: the element holding .tooltip-name. Our block stays its last element, since a line the game
  // shows later (shelf life, say) is inserted where Vue keeps its place, which can be after ours.
  function showTooltipLines(doc, lines) {
    var name = doc.querySelector('.tooltip > .tooltip-name');
    var tip = name ? name.parentNode : null;
    var box = tip ? tip.querySelector(':scope > .slip-lines') : null;
    var key = lines.map(function (line) { return line.kind + ':' + line.text; }).join('\n');

    if (!tip) return;

    if (!lines.length) {
      if (box) box.remove();

      return;
    }

    if (!box) {
      box = doc.createElement('div');
      box.className = 'slip-lines';
    }

    if (tip.lastElementChild !== box) tip.appendChild(box);
    if (box.__slipKey === key) return;

    box.__slipKey = key;
    box.textContent = '';
    lines.forEach(function (line) {
      var row = doc.createElement('div');
      row.className = 'tooltip-category slip-line' + (line.kind ? ' ' + line.kind : '');
      row.textContent = line.text;
      box.appendChild(row);
    });
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

  function patchBackpack(doc) {
    var setup = vueSetup(doc);

    if (!setup) return diag('backpack', 'no Vue state');

    try {
      patchBackpackTooltip(doc, setup);
    } catch (e) {
      diag('backpack tooltip', e);
    }

    try {
      patchBackpackChips(doc, setup);
    } catch (e) {
      diag('backpack scrap chips', e);
    }
  }

  function patchBackpackTooltip(doc, setup) {
    var state = setup.tooltipState;
    var item = state && state.visible ? state.item : null;

    showTooltipLines(doc, item ? itemLines(item.cfgId, false) : []);
  }

  // One .grid-container per visible side, in order, whose .item children follow that side's items array. Any
  // mismatch (a redraw in flight) skips the side until the next poll rather than chip the wrong slot.
  function patchBackpackChips(doc, setup) {
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
      }
    }
  }

  // ===== Trade =====

  // Shelf items carry their config id as id. Everything else is named by instance id, which the plugin looks up.
  // The stat line goes on shelf items only: those are the ones you are deciding whether to buy.
  function patchTrade(doc) {
    var setup = vueSetup(doc);
    var state = setup ? setup.tooltipState : null;
    var item = state && state.visible ? state.item : null;
    var lines = [];

    if (!setup) return diag('trade', 'no Vue state');

    if (item && !setup.kinTipText && !item.dimTip) {
      var shelf = setup.shelf && setup.shelf.items ? setup.shelf.items : [];
      var isShelf = shelf.indexOf(item) >= 0;

      lines = itemLines(isShelf ? item.id : configOfInstance(item.id), isShelf);
    }

    showTooltipLines(doc, lines);
  }

  // ===== Cooking =====

  // The cooking page is plain script: its showItemTip is a global, so wrapping it here wraps every call the page
  // makes. Recipe tooltips are written inline instead, so those are caught by watching the tooltip.
  function patchCooking(win, doc) {
    var tip = doc.getElementById('recipeTooltip');
    var original = win.showItemTip;

    if (win.__slipCooking || !tip) return;
    if (typeof original !== 'function') return diag('cooking', 'showItemTip is missing');

    win.__slipCooking = true;
    win.showItemTip = function (item) {
      var result = original.apply(this, arguments);

      try {
        showCookingItemTip(tip, item);
      } catch (e) {
        diag('cooking item tooltip', e);
      }

      return result;
    };

    new win.MutationObserver(function () {
      try {
        onCookingTipChanged(win, tip);
      } catch (e) {
        diag('cooking recipe tooltip', e);
      }
    }).observe(tip, { childList: true, characterData: true, subtree: true });
  }

  // The page's showItemTip writes nothing for an item it has nothing to say about, so the tooltip is only ours to
  // extend when it starts with this item's name.
  function showCookingItemTip(tip, item) {
    if (!item || !item.name || tip.style.display !== 'block' || tip.textContent.split('\n')[0] !== item.name) return;

    tip.__slipText = tip.textContent;
    cookingTip = { tip: tip, item: item, base: tip.textContent };
    refreshCookingItemTip();
  }

  function refreshCookingItemTip() {
    var tip = cookingTip.tip;

    // The tooltip moved on to something else since; leave it alone.
    if (tip.style.display !== 'block' || (tip.__slipText !== undefined && tip.textContent !== tip.__slipText)) return;

    var item = cookingTip.item;

    // The cooking window names most items by config id; only those it leaves at 0 need the instance lookup.
    writeCookingTip(tip, cookingTip.base, codexLines(item.configId > 0 ? item.configId : configOfInstance(item.itemId)));
  }

  // Our own writes are remembered, so only the page's writes count as a new tooltip. A recipe tooltip starts with
  // the recipe's name; its dish is what the codex lines describe.
  function onCookingTipChanged(win, tip) {
    var text = tip.textContent;
    var state = win.State;

    if (text === tip.__slipText) return;

    cookingTip = null;
    tip.__slipText = text;
    tip.style.maxWidth = '';

    if (!features || !features.codex || !state || !state.lastRecipes) return;

    var firstLine = text.split('\n')[0];

    for (var i = 0; i < state.lastRecipes.length; i++) {
      var recipe = state.lastRecipes[i];

      if (recipe && recipe.name === firstLine && facts.dish[recipe.recipeId]) {
        writeCookingTip(tip, text, codexLines(facts.dish[recipe.recipeId]));

        return;
      }
    }
  }

  function writeCookingTip(tip, base, lines) {
    var text = lines.length ? base + '\n' + lines.join('\n') : base;

    tip.__slipText = text;
    tip.style.maxWidth = lines.length ? COOKING_TIP_WIDTH : '';

    if (tip.textContent !== text) tip.textContent = text;
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

  // ===== Finding the pages =====

  var PAGES = [
    { pattern: /BackpackUI\.html/, isOn: function () { return features.counts || features.brew || features.scrap; }, patch: function (win, doc) { patchBackpack(doc); } },
    { pattern: /TradeUI\.html/, isOn: function () { return features.counts || features.stats || features.brew || features.scrap; }, patch: function (win, doc) { patchTrade(doc); } },
    { pattern: /Cooking\.html/, isOn: function () { return features.counts || features.brew || features.scrap || features.codex; }, patch: patchCooking },
    { pattern: /BrewPanel\.html/, isOn: function () { return features.brew; }, patch: patchBrew },
    { pattern: /ItemPickup\.html/, isOn: function () { return features.scrap; }, patch: function (win, doc) { patchPickup(doc); } },
    { pattern: /RogueSelect\.html/, isOn: function () { return features.plan; }, patch: patchPlanningPage }
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

  setInterval(function () {
    try {
      patchPages();
    } catch (e) {
      diag('poll', e);
    }
  }, POLL_MS);

  window.__slinfopack = { setFeatures: setFeatures, setFacts: setFacts, setCounts: setCounts, setInstance: setInstance };
  send('ready');
})();

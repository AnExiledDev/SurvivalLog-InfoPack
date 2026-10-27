// A fake root page for page.js's own tooltip on other mods' tagged rows: elements with a box, the cursor, timers and a
// clock the tests move, and a fake Exiled's Toolkit that sizes panels.
var window = this;
var now = 100000;
Date.now = function () { return now; };

// The own tooltip's box at zoom 1; every other element has none unless a test gives it one.
var TIP_WIDTH = 200;
var TIP_HEIGHT = 60;

var timers = [];
var nextTimer = 1;
var polls = [];

function setTimeout(fn, ms) {
  var id = nextTimer++;

  timers.push({ id: id, at: now + (ms || 0), fn: fn });

  return id;
}

function clearTimeout(id) {
  timers = timers.filter(function (t) { return t.id !== id; });
}

function setInterval(fn) {
  polls.push(fn);

  return 0;
}

// Moves the clock and runs the timers that came due.
function wait(ms) {
  now += ms;

  var due = timers.filter(function (t) { return t.at <= now; });

  timers = timers.filter(function (t) { return t.at > now; });
  due.forEach(function (t) { t.fn(); });
}

function poll() {
  polls.forEach(function (fn) { fn(); });
}

class FakeEl {
  constructor(tag) {
    var el = this;

    this.tagName = String(tag).toUpperCase();
    this.nodeType = 1;
    this.children = [];
    this.parentNode = null;
    this.style = {};
    this.attributes = {};
    this.className = '';
    this.id = '';
    this.textContent = '';
    this.classList = {
      add: function (name) { if (!el.classList.contains(name)) el.className = (el.className + ' ' + name).trim(); },
      remove: function (name) { el.className = el.className.split(' ').filter(function (c) { return c !== name; }).join(' '); },
      toggle: function (name, on) { if (on) el.classList.add(name); else el.classList.remove(name); },
      contains: function (name) { return el.className.split(' ').indexOf(name) >= 0; }
    };
  }

  get lastElementChild() { return this.children[this.children.length - 1] || null; }

  get isConnected() {
    for (var node = this; node; node = node.parentNode) {
      if (node === document) return true;
    }

    return false;
  }

  appendChild(child) {
    if (child.parentNode) child.remove();
    child.parentNode = this;
    this.children.push(child);

    return child;
  }

  remove() {
    var parent = this.parentNode;

    if (!parent) return;

    parent.children.splice(parent.children.indexOf(this), 1);
    this.parentNode = null;
  }

  after(node) {
    var parent = this.parentNode;

    if (node.parentNode) node.remove();
    node.parentNode = parent;
    parent.children.splice(parent.children.indexOf(this) + 1, 0, node);
  }

  getAttribute(name) {
    return Object.prototype.hasOwnProperty.call(this.attributes, name) ? this.attributes[name] : null;
  }

  setAttribute(name, value) {
    this.attributes[name] = String(value);
  }

  // Only the attribute lists page.js asks for, like "[data-slip-item],[data-slip-tip]".
  closest(selector) {
    var names = selector.split(',').map(function (part) { return part.trim().replace(/^\[|\]$/g, ''); });

    for (var node = this; node && node.nodeType === 1; node = node.parentNode) {
      var found = node;

      if (names.some(function (name) { return found.getAttribute(name) !== null; })) return node;
    }

    return null;
  }

  querySelector() { return null; }

  querySelectorAll() { return []; }

  addEventListener() {}

  // Drawn at its own size times its zoom and every parent's, the way the browser draws CSS zoom.
  getBoundingClientRect() {
    var zoom = 1;

    for (var node = this; node && node.nodeType === 1; node = node.parentNode) zoom *= Number(node.style.zoom) || 1;

    var isTip = this.className === 'slip-own-tip';

    return { left: 0, top: 0, width: (isTip ? TIP_WIDTH : 0) * zoom, height: (isTip ? TIP_HEIGHT : 0) * zoom };
  }
}

function findById(node, id) {
  if (node.id === id) return node;

  for (var i = 0; i < node.children.length; i++) {
    var found = findById(node.children[i], id);

    if (found) return found;
  }

  return null;
}

var under = null;
var document = {
  nodeType: 9,
  readyState: 'complete',
  listeners: {},
  createElement: function (tag) { return new FakeEl(tag); },
  getElementById: function (id) { return findById(this.head, id) || findById(this.body, id); },
  getElementsByTagName: function () { return []; },
  querySelectorAll: function () { return []; },
  addEventListener: function (type, fn) { (this.listeners[type] = this.listeners[type] || []).push(fn); },
  elementFromPoint: function () { return under; }
};

document.head = new FakeEl('head');
document.head.parentNode = document;
document.body = new FakeEl('body');
document.body.parentNode = document;
window.innerWidth = 1920;
window.innerHeight = 1080;
window.getComputedStyle = function (el) { return { zoom: el.style.zoom || '1' }; };
window.addEventListener = function () {};

// The cursor arriving on target at x, y.
function hover(target, x, y) {
  var e = { target: target, clientX: x, clientY: y, relatedTarget: null };

  under = target;
  (document.listeners.mouseover || []).forEach(function (fn) { fn(e); });
  (document.listeners.mousemove || []).forEach(function (fn) { fn(e); });
}

// A panel another mod draws in this page, and a tagged row in it (or on the body).
function panel() {
  return document.body.appendChild(new FakeEl('div'));
}

function tagged(words, parent) {
  var row = new FakeEl('div');

  row.setAttribute('data-slip-tip', words);

  return (parent || document.body).appendChild(row);
}

// The Toolkit as the mods see it: zoomAt multiplies the zooms it wrote on the element and the ones around it, and
// leaves out a zoom the page set itself.
window.ExiledUi = {
  version: 1,
  size: function () {},
  zoomAt: function (el) {
    var zoom = 1;

    for (var node = el; node && node.style; node = node.parentNode) {
      if (node.__toolkitZoom) zoom *= node.__toolkitZoom;
    }

    return zoom;
  }
};

function sized(el, zoom) {
  el.__toolkitZoom = zoom;
  el.style.zoom = String(zoom);
}

function ownTip() {
  return document.getElementById('slip-own-tip');
}

(function () {
  'use strict';

  var $ = function (id) { return document.getElementById(id); };

  // --- Bildirishnoma ----------------------------------------------------------
  var toastTimer;
  function toast(text) {
    var t = $('toast');
    t.textContent = text;
    t.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { t.classList.remove('show'); }, 2200);
  }

  // --- Konvertor ------------------------------------------------------------
  var input = $('input'), output = $('output'), count = $('count'), chars = $('chars');
  var lastText = '';

  function renderParts(target, parts, popLast) {
    target.textContent = '';
    var marks = [];
    parts.forEach(function (p) {
      if (p.changed) {
        var m = document.createElement('mark');
        m.textContent = p.text;
        target.appendChild(m);
        marks.push(m);
      } else {
        target.appendChild(document.createTextNode(p.text));
      }
    });
    return marks;
  }

  function render() {
    var result = Imlo.convert(input.value);
    lastText = result.text;
    renderParts(output, result.parts);
    chars.textContent = input.value.length + ' belgi';
    count.textContent = result.changes ? result.changes + ' ta harf yangilandi' : 'Özgariş yöq';
    count.classList.toggle('has', result.changes > 0);
  }

  input.addEventListener('input', render);

  $('clear').addEventListener('click', function () { input.value = ''; render(); input.focus(); });

  $('paste').addEventListener('click', function () {
    if (!navigator.clipboard || !navigator.clipboard.readText) { toast('Ctrl+V bilan qöying'); input.focus(); return; }
    navigator.clipboard.readText().then(function (text) {
      input.value = text;
      render();
    }, function () { toast('Ctrl+V bilan qöying'); input.focus(); });
  });

  $('copy').addEventListener('click', function () {
    var btn = this;
    if (!lastText) { toast('Avval matn kiriting'); input.focus(); return; }
    navigator.clipboard.writeText(lastText).then(function () {
      btn.textContent = 'Nusxa olindi ✓';
      btn.classList.add('done');
      setTimeout(function () { btn.textContent = 'Nusxa oliş'; btn.classList.remove('done'); }, 1600);
    }, function () { toast('Natijani belgilab, Ctrl+C bosing'); });
  });

  $('download').addEventListener('click', function () {
    if (!lastText) { toast('Avval matn kiriting'); return; }
    var blob = new Blob(['﻿' + lastText], { type: 'text/plain;charset=utf-8' });
    var a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'yangi-alifbo.txt';
    a.click();
    setTimeout(function () { URL.revokeObjectURL(a.href); }, 1000);
  });

  Array.prototype.forEach.call(document.querySelectorAll('[data-ex]'), function (chip) {
    chip.addEventListener('click', function () {
      input.value = chip.getAttribute('data-ex');
      render();
    });
  });

  // .txt faylni sudrab tashlash
  var inPane = input.parentNode;
  ['dragenter', 'dragover'].forEach(function (ev) {
    inPane.addEventListener(ev, function (e) { e.preventDefault(); inPane.classList.add('dragging'); });
  });
  ['dragleave', 'drop'].forEach(function (ev) {
    inPane.addEventListener(ev, function (e) { e.preventDefault(); inPane.classList.remove('dragging'); });
  });
  inPane.addEventListener('drop', function (e) {
    var file = e.dataTransfer.files && e.dataTransfer.files[0];
    if (!file) return;
    if (/\.docx?$/i.test(file.name)) { toast('Word fayllar uçun dasturdagi «Fayllarni ögiriş» dan foydalaning'); return; }
    var reader = new FileReader();
    reader.onload = function () { input.value = reader.result; render(); toast(file.name + ' yuklandi'); };
    reader.readAsText(file, 'utf-8');
  });

  render();

  // --- Bosh ekrandagi yozish namoyishi -------------------------------------
  var demo = $('demo-text');
  var phrases = ["O'zbekiston — go'zal yurt", "Choy ichamizmi?", "Shahar chiroqlari", "Ma'no va mohiyat", "To'g'ri yo'l"];
  var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  function showDemo(text) {
    var before = demo.querySelectorAll('mark').length;
    var marks = renderParts(demo, Imlo.convert(text).parts);
    if (marks.length > before) marks[marks.length - 1].classList.add('pop');
  }

  if (reduced) {
    showDemo(phrases[0]);
  } else {
    // Birinchi misol to'liq ko'rinib turadi, keyin o'chirilib keyingisi yoziladi.
    var pi = 0, ci = phrases[0].length, deleting = true;
    showDemo(phrases[0]);
    setTimeout(function tick() {
      var phrase = phrases[pi];
      if (!deleting) {
        ci++;
        showDemo(phrase.slice(0, ci));
        if (ci === phrase.length) { deleting = true; return setTimeout(tick, 2000); }
        return setTimeout(tick, 70 + Math.random() * 70);
      }
      ci = Math.max(0, ci - 3);
      showDemo(phrase.slice(0, ci));
      if (ci === 0) { deleting = false; pi = (pi + 1) % phrases.length; return setTimeout(tick, 400); }
      setTimeout(tick, 40);
    }, 2400);
  }

  // --- Klaviatura -----------------------------------------------------------
  // [kod, belgi yoki yorliq, kenglik]
  var rows = [
    [['Backquote', '`'], ['Digit1', '1'], ['Digit2', '2'], ['Digit3', '3'], ['Digit4', '4'], ['Digit5', '5'], ['Digit6', '6'],
     ['Digit7', '7'], ['Digit8', '8'], ['Digit9', '9'], ['Digit0', '0'], ['Minus', '-'], ['Equal', '='], ['Backspace', '⌫', 2]],
    [['Tab', 'Tab', 1.5], ['KeyQ', 'q'], ['KeyW', 'w'], ['KeyE', 'e'], ['KeyR', 'r'], ['KeyT', 't'], ['KeyY', 'y'], ['KeyU', 'u'],
     ['KeyI', 'i'], ['KeyO', 'o'], ['KeyP', 'p'], ['BracketLeft', '['], ['BracketRight', ']'], ['Backslash', '\\', 1.5]],
    [['CapsLock', 'Caps', 1.75], ['KeyA', 'a'], ['KeyS', 's'], ['KeyD', 'd'], ['KeyF', 'f'], ['KeyG', 'g'], ['KeyH', 'h'], ['KeyJ', 'j'],
     ['KeyK', 'k'], ['KeyL', 'l'], ['Semicolon', ';'], ['Quote', "'"], ['Enter', 'Enter', 2.25]],
    [['ShiftLeft', 'Shift', 2.25], ['KeyZ', 'z'], ['KeyX', 'x'], ['KeyC', 'c'], ['KeyV', 'v'], ['KeyB', 'b'], ['KeyN', 'n'], ['KeyM', 'm'],
     ['Comma', ','], ['Period', '.'], ['Slash', '/'], ['ShiftRight', 'Shift', 2.75]],
    [['ControlLeft', 'Ctrl', 1.5], ['MetaLeft', 'Win', 1.25], ['AltLeft', 'Alt', 1.25], ['Space', '', 7.5],
     ['AltRight', 'AltGr', 1.5], ['ControlRight', 'Ctrl', 2]]
  ];
  var altgr = { o: 'ö', g: 'ğ', s: 'ş', c: 'ç', "'": 'ʼ', ',': '«', '.': '»' };
  var mainOnly = { '[': 'ö', ']': 'ğ', ';': 'ş', '\\': 'ç' };
  var notes = {
    main: 'Köp işlatiladigan harflar bitta bosişda: [ → ö, ] → ğ, ; → ş, \\ → ç. Bu tugmalardagi asl belgilar AltGr bilan çiqadi.',
    us: 'US tartibi özgarmaydi — yangi harflar AltGr (öng Alt) bilan: AltGr+O → ö, AltGr+G → ğ, AltGr+S → ş, AltGr+C → ç. Dasturçilar uçun qulay.'
  };
  var keyboard = $('keyboard'), note = $('variant-note');
  var keyEls = {};

  function drawKeyboard(variant) {
    keyboard.textContent = '';
    keyEls = {};
    note.textContent = notes[variant];
    rows.forEach(function (row) {
      var r = document.createElement('div');
      r.className = 'row';
      row.forEach(function (k) {
        var code = k[0], label = k[1], w = k[2] || 1;
        var key = document.createElement('div');
        key.className = 'key';
        key.style.setProperty('--w', w);
        keyEls[code] = key;
        if (label.length !== 1) {
          key.className += ' label';
          key.textContent = label;
        } else {
          var base = label, ag = altgr[label] || '';
          if (variant === 'main' && mainOnly[label]) {
            base = mainOnly[label];
            ag = label;
            key.className += ' new';
          }
          var b = document.createElement('span');
          b.className = 'base';
          b.textContent = base;
          key.appendChild(b);
          if (ag) {
            var a = document.createElement('span');
            a.className = 'ag';
            a.textContent = ag;
            key.appendChild(a);
          }
        }
        r.appendChild(key);
      });
      keyboard.appendChild(r);
    });
    sizeKeyboard();
  }

  // Tugma o'lchami konteyner kengligiga moslanadi: 15 birlik + 14 oraliq.
  // Telefonda 24px dan kichraymaydi — o'qib bo'lmay qoladi; sxema yon tomonga suriladi.
  function sizeKeyboard() {
    var width = keyboard.clientWidth;
    var gap = width < 600 ? 4 : 7;
    var unit = Math.max(24, Math.min(56, Math.floor((width - 14 * gap) / 15)));
    keyboard.style.setProperty('--g', gap + 'px');
    keyboard.style.setProperty('--u', unit + 'px');
  }
  window.addEventListener('resize', sizeKeyboard);

  document.addEventListener('keydown', function (e) { var k = keyEls[e.code]; if (k) k.classList.add('pressed'); });
  document.addEventListener('keyup', function (e) { var k = keyEls[e.code]; if (k) k.classList.remove('pressed'); });
  window.addEventListener('blur', function () {
    Object.keys(keyEls).forEach(function (c) { keyEls[c].classList.remove('pressed'); });
  });

  var tabs = document.querySelectorAll('.seg button');
  Array.prototype.forEach.call(tabs, function (tab) {
    tab.addEventListener('click', function () {
      Array.prototype.forEach.call(tabs, function (t) { t.setAttribute('aria-selected', String(t === tab)); });
      drawKeyboard(tab.getAttribute('data-variant'));
    });
  });
  drawKeyboard('main');

  // --- Alifbo (rasmiy jadval tartibida) ----------------------------------------
  var alphabet = [
    ['Aa'], ['Bb'], ['Dd'], ['Ee'], ['Ff'], ['Gg'], ['Ğğ', 'Gʻ gʻ'], ['Hh'], ['Ii'], ['Jj'],
    ['Kk'], ['Ll'], ['Mm'], ['Nn'], ['Oo'], ['Öö', 'Oʻ oʻ'], ['Pp'], ['Qq'], ['Rr'], ['Ss'],
    ['Şş', 'Sh sh'], ['Çç', 'Ch ch'], ['Tt'], ['Uu'], ['Vv'], ['Xx'], ['Yy'], ['Zz'], ['ʼ']
  ];
  var grid = $('alphabet');
  alphabet.forEach(function (item) {
    var el = document.createElement('div');
    el.className = 'letter' + (item[1] ? ' new' : '');
    var front = document.createElement('div');
    front.className = 'face front';
    front.textContent = item[0];
    if (item[0] === 'ʼ') {
      var s0 = document.createElement('small');
      s0.textContent = 'tutuq';
      front.appendChild(s0);
    }
    el.appendChild(front);
    if (item[1]) {
      var tag = document.createElement('small');
      tag.textContent = 'yangi';
      front.appendChild(tag);
      var back = document.createElement('div');
      back.className = 'face back';
      back.textContent = item[1];
      var s = document.createElement('small');
      s.textContent = 'oldin';
      back.insertBefore(s, back.firstChild);
      el.appendChild(back);
      el.tabIndex = 0;
      el.title = 'Oldin: ' + item[1];
    }
    grid.appendChild(el);
  });

  // --- Paydo bo'lish animatsiyasi -----------------------------------------
  var reveals = document.querySelectorAll('.reveal');
  if (!('IntersectionObserver' in window) || reduced) {
    Array.prototype.forEach.call(reveals, function (el) { el.classList.add('in'); });
  } else {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
      });
    }, { rootMargin: '0px 0px -8% 0px' });
    Array.prototype.forEach.call(reveals, function (el) { io.observe(el); });
  }
})();

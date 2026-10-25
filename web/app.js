(function () {
  'use strict';

  // --- Konvertor ------------------------------------------------------------
  var input = document.getElementById('input');
  var output = document.getElementById('output');
  var count = document.getElementById('count');
  var status = document.getElementById('status');
  var lastText = '';

  function render() {
    var result = Imlo.convert(input.value);
    lastText = result.text;
    output.textContent = '';
    result.parts.forEach(function (p) {
      if (p.changed) {
        var m = document.createElement('mark');
        m.textContent = p.text;
        output.appendChild(m);
      } else {
        output.appendChild(document.createTextNode(p.text));
      }
    });
    count.hidden = result.changes === 0;
    count.textContent = result.changes + ' ta özgariş';
    status.textContent = '';
  }

  input.addEventListener('input', render);

  document.getElementById('copy').addEventListener('click', function () {
    if (!lastText) { status.textContent = 'Avval matn kiriting.'; return; }
    navigator.clipboard.writeText(lastText).then(
      function () { status.textContent = 'Nusxa olindi.'; },
      function () { status.textContent = 'Nusxa olib bölmadi: natijani belgilab Ctrl+C bosing.'; });
  });

  document.getElementById('clear').addEventListener('click', function () {
    input.value = '';
    render();
    input.focus();
  });

  document.getElementById('example').addEventListener('click', function () {
    input.value = "O'zbekiston — go'zal yurt. Toshkent shahri chiroyli, tog'lari baland.\n" +
      "Ma'naviyat va ta'lim — kelajak poydevori. Bog'larda shaftoli gullaydi.\n" +
      "Batafsil: https://lex.uz/docs/8548754";
    render();
  });

  // --- Alifbo (rasmiy jadval tartibida) ----------------------------------------
  var alphabet = [
    ['A a'], ['B b'], ['D d'], ['E e'], ['F f'], ['G g'], ['Ğ ğ', 'Gʻ gʻ'], ['H h'], ['I i'], ['J j'],
    ['K k'], ['L l'], ['M m'], ['N n'], ['O o'], ['Ö ö', 'Oʻ oʻ'], ['P p'], ['Q q'], ['R r'], ['S s'],
    ['Ş ş', 'Sh sh'], ['Ç ç', 'Ch ch'], ['T t'], ['U u'], ['V v'], ['X x'], ['Y y'], ['Z z'], ['ʼ', 'tutuq']
  ];
  var grid = document.getElementById('alphabet');
  alphabet.forEach(function (item) {
    var el = document.createElement('div');
    el.className = 'letter' + (item[1] && item[1] !== 'tutuq' ? ' new' : '');
    var b = document.createElement('b');
    b.textContent = item[0];
    el.appendChild(b);
    if (item[1]) {
      var s = document.createElement('small');
      s.textContent = item[1] === 'tutuq' ? 'tutuq' : 'oldin ' + item[1];
      el.appendChild(s);
    }
    grid.appendChild(el);
  });

  // --- Klaviatura sxemasi ---------------------------------------------------
  // Har bir tugma: [asosiy, AltGr] ikkala variant uçun; null = özgarişsiz US.
  var rows = [
    ['`', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '='],
    ['Tab', 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', '[', ']', '\\'],
    ['Caps', 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', "'", 'Enter'],
    ['Shift', 'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/', 'Shift']
  ];
  var altgr = { o: 'ö', g: 'ğ', s: 'ş', c: 'ç', "'": 'ʼ', ',': '«', '.': '»' };
  var mainOnly = { '[': 'ö', ']': 'ğ', ';': 'ş', '\\': 'ç' };
  var notes = {
    main: 'Eng köp işlatiladigan harflar bir bosişda: [ → ö, ] → ğ, ; → ş, \\ → ç. ' +
          'Bu tugmalardagi asl belgilar AltGr bilan çiqadi.',
    us: 'US tartibi özgarmaydi, yangi harflar faqat AltGr bilan: AltGr+O → ö, AltGr+G → ğ, AltGr+S → ş, AltGr+C → ç. ' +
        'Dasturçilar uçun qulay.'
  };
  var keyboard = document.getElementById('keyboard');
  var note = document.getElementById('variant-note');

  function drawKeyboard(variant) {
    keyboard.textContent = '';
    note.textContent = notes[variant];
    rows.forEach(function (row) {
      var r = document.createElement('div');
      r.className = 'row';
      row.forEach(function (label) {
        var key = document.createElement('div');
        key.className = 'key';
        if (label.length > 1) {
          key.className += label === 'Tab' || label === 'Caps' ? ' wide' : ' wider';
          key.textContent = label;
          key.style.display = 'grid';
          key.style.placeItems = 'center';
          r.appendChild(key);
          return;
        }
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
        r.appendChild(key);
      });
      keyboard.appendChild(r);
    });
  }

  var tabs = document.querySelectorAll('.tab');
  Array.prototype.forEach.call(tabs, function (tab) {
    tab.addEventListener('click', function () {
      Array.prototype.forEach.call(tabs, function (t) { t.setAttribute('aria-selected', String(t === tab)); });
      drawKeyboard(tab.getAttribute('data-variant'));
    });
  });
  drawKeyboard('main');
})();

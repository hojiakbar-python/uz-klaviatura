// app/Imlo.cs bilan bir xil qoidalar. Ikkalasi tests/imlo-cases.tsv bilan tekshiriladi.
(function (root) {
  'use strict';

  var TUTUQ = 'ʼ';
  var APOSTROPHES = '\'`´ʻʼ‘’';
  var LETTER = /\p{L}/u;
  var SPACE = /\s/;

  function isApostrophe(c) { return c !== '' && APOSTROPHES.indexOf(c) >= 0; }
  function isLetter(c) { return c !== '' && LETTER.test(c); }
  function isUpper(c) { return c !== c.toLowerCase(); }

  // o/g/s/c + ikkinchi belgi -> yangi harf, aks holda ''.
  function combine(first, second) {
    var upper = isUpper(first);
    var f = first.toLowerCase();
    if (isApostrophe(second)) {
      if (f === 'o') return upper ? 'Ö' : 'ö';
      if (f === 'g') return upper ? 'Ğ' : 'ğ';
    } else if (second === 'h' || second === 'H') {
      if (f === 's') return upper ? 'Ş' : 'ş';
      if (f === 'c') return upper ? 'Ç' : 'ç';
    }
    return '';
  }

  // { text, changes, parts: [{ text, changed }] } — parts sahifada özgarişlarni belgilaş uçun.
  function convert(text) {
    if (!text) return { text: text || '', changes: 0, parts: [] };
    var out = '';
    var changes = 0;
    var parts = [];
    var i = 0;
    function emit(s, changed) {
      out += s;
      var last = parts[parts.length - 1];
      if (last && last.changed === changed) last.text += s;
      else parts.push({ text: s, changed: changed });
    }
    while (i < text.length) {
      // Havolalar va e-pochtalar o'zgarmaydi.
      if (i === 0 || SPACE.test(text[i - 1])) {
        var end = i;
        while (end < text.length && !SPACE.test(text[end])) end++;
        var token = text.slice(i, end);
        if (token.indexOf('://') >= 0 || token.indexOf('@') >= 0 || /^www\./i.test(token)) {
          emit(token, false);
          i = end;
          continue;
        }
      }

      var c = text[i];
      var next = i + 1 < text.length ? text[i + 1] : '';
      var prevLetter = i > 0 && isLetter(text[i - 1]);
      var afterNextLetter = i + 2 < text.length && isLetter(text[i + 2]);
      var combined = combine(c, next);

      if (combined) {
        var f = c.toLowerCase();
        // o' faqat harfdan oldin (o'zbek); g' so'z oxirida ham bo'ladi (tog').
        var ok = f === 'o' ? afterNextLetter : f === 'g' ? (afterNextLetter || prevLetter) : true;
        if (ok) {
          emit(combined, true);
          changes++;
          i += 2;
          continue;
        }
      }

      if (isApostrophe(c) && c !== TUTUQ && prevLetter && isLetter(next)) {
        emit(TUTUQ, true);
        changes++;
        i++;
        continue;
      }

      emit(c, false);
      i++;
    }
    return { text: out, changes: changes, parts: parts };
  }

  var api = { convert: convert, TUTUQ: TUTUQ };
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.Imlo = api;
})(this);

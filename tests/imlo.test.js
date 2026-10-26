// node tests/imlo.test.js — web/imlo.js ni umumiy holatlar bilan tekshiradi.
const fs = require('fs');
const path = require('path');
const { convert } = require('../web/imlo.js');

const lines = fs.readFileSync(path.join(__dirname, 'imlo-cases.tsv'), 'utf8').split(/\r?\n/);
let failures = 0, total = 0;
for (const line of lines) {
  if (!line || line.startsWith('#')) continue;
  const [input, expected] = line.split('\t').map(s => s.replace(/\\n/g, '\n'));
  const actual = convert(input).text;
  total++;
  if (actual !== expected) {
    failures++;
    console.log(`XATO ${JSON.stringify(input)} -> ${JSON.stringify(actual)} (kutilgan: ${JSON.stringify(expected)})`);
  }
}
// Sayt matnlari lotincha: tasodifan kirgan kirill harfi (masalan "е") ko'rinmaydi, lekin qidiruvni buzadi.
const webDir = path.join(__dirname, '..', 'web');
for (const file of fs.readdirSync(webDir)) {
  const text = fs.readFileSync(path.join(webDir, file), 'utf8');
  const m = /[Ѐ-ӿ]/.exec(text);
  total++;
  if (m) {
    failures++;
    console.log(`XATO web/${file}: kirill harfi ${JSON.stringify(text.slice(Math.max(0, m.index - 15), m.index + 5))}`);
  }
}

console.log(failures === 0 ? `Hammasi to'g'ri (${total} ta).` : `${failures} ta xato.`);
process.exit(failures === 0 ? 0 : 1);

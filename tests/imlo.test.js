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
console.log(failures === 0 ? `Hammasi to'g'ri (${total} ta).` : `${failures} ta xato.`);
process.exit(failures === 0 ? 0 : 1);

const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join, resolve } = require('node:path');

const [installation] = process.argv.slice(2);
assert(installation, 'Pass the path of the installed fountain-js package.');
const { Fountain } = require(resolve(installation));
const examples = join(__dirname, '..', 'docs', 'examples');

const expected = {
  stage: {
    centered: ['CHARACTERS', 'SETTING', 'TIME', 'ACT I', 'END OF ACT I'],
    scene_heading: ['SCENE 1', 'SCENE 2'],
    action: ['A light glows in the window.', 'A door closes.'],
    dialogue: ['I found the key.', 'We can leave now.'],
  },
  television: {
    centered: ['ACT ONE', 'END OF ACT ONE', 'ACT TWO', 'END OF ACT TWO'],
    scene_heading: ['INT. STATION - NIGHT', 'EXT. PLATFORM - NIGHT'],
    action: ['The clock stops.', 'A light flickers.'],
    dialogue: ['I missed the last train.', 'I can wait with you.'],
  },
  feature: {
    scene_heading: ['EXT. HILL ROAD - DAWN'],
    action: ['A silver car rests beside a bend in the road.'],
    dialogue: ['We missed the turn.', 'The road must go somewhere.'],
  },
};

for (const [example, elements] of Object.entries(expected)) {
  const source = readFileSync(join(examples, example + '.fountain'), 'utf8');
  const { tokens, html } = new Fountain().parse(source, true);
  for (const [type, values] of Object.entries(elements)) {
    for (const text of values) {
      assert(tokens.some(token => token.type === type && token.text === text), example + ' missing ' + type + ' ' + text);
      assert(html.script.includes(text), example + ' does not print ' + text);
    }
  }

  console.log(example + ' preserves visible script elements in fountain-js');
}

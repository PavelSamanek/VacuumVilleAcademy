#!/usr/bin/env node
/**
 * VacuumVilleAcademy — static project validator
 *
 * Catches the categories of bugs fixed during development:
 *   1. Scene YAML: parent-child hierarchy conflicts
 *   2. Scene YAML: m_Transitions (typo) instead of m_Transition
 *   3. Scene YAML: Buttons missing m_TargetGraphic
 *   4. Scene YAML: fake / placeholder asset GUIDs
 *   5. Scene YAML: off-screen UI (anchor-bottom + negative y)
 *   6. Scripts: BaseMinigame subclasses missing IsSetupComplete() override
 *   7. Scripts: hardcoded unsupported Unicode glyphs in UI text
 *   8. Scripts: LocalizationManager.Get() calls with unrecognised key prefixes
 *   9. Localization: keys referenced in scripts exist in both JSON tables
 *  10. Localization: btn_* labels contain unsupported emoji / symbol chars
 *
 * Usage:  node tests/validate_project.js
 * Requires: Node.js ≥ 16, no npm packages.
 */

'use strict';
const fs   = require('fs');
const path = require('path');

// ─── paths ────────────────────────────────────────────────────────────────────
const ROOT      = path.resolve(__dirname, '..');
const SCENES    = path.join(ROOT, 'Assets', 'Scenes');
const SCRIPTS   = path.join(ROOT, 'Assets', 'Scripts');
const MINIGAMES = path.join(SCRIPTS, 'Minigames');
const LOC_EN    = path.join(ROOT, 'Assets', 'Resources', 'Localization', 'en-US');
const LOC_CS    = path.join(ROOT, 'Assets', 'Resources', 'Localization', 'cs-CZ');

// ─── tiny test harness ────────────────────────────────────────────────────────
let passed = 0, failed = 0;
const failures = [];

function test(name, fn) {
  try {
    fn();
    process.stdout.write(`  \x1b[32m✓\x1b[0m ${name}\n`);
    passed++;
  } catch (e) {
    process.stdout.write(`  \x1b[31m✗\x1b[0m ${name}\n    → ${e.message}\n`);
    failures.push({ name, message: e.message });
    failed++;
  }
}

function assert(cond, msg) {
  if (!cond) throw new Error(msg);
}

function section(title) {
  console.log(`\n\x1b[1m${title}\x1b[0m`);
}

// ─── helpers ──────────────────────────────────────────────────────────────────
function readText(filePath) {
  return fs.readFileSync(filePath, 'utf8');
}

function sceneFiles() {
  return fs.readdirSync(SCENES)
    .filter(f => f.endsWith('.unity'))
    .map(f => path.join(SCENES, f));
}

function minigameScripts() {
  return fs.readdirSync(MINIGAMES)
    .filter(f => f.endsWith('.cs') && f !== 'BaseMinigame.cs' && f !== 'MinigameVFX.cs' && f !== 'BaseMinigameStub.cs')
    .map(f => path.join(MINIGAMES, f));
}

function allScripts() {
  const results = [];
  function walk(dir) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) walk(full);
      else if (entry.name.endsWith('.cs')) results.push(full);
    }
  }
  walk(SCRIPTS);
  return results;
}

function loadLocKeys(dir) {
  const stringsPath = path.join(dir, 'strings.json');
  const pluralsPath = path.join(dir, 'plurals.json');
  const keys = new Set();
  if (fs.existsSync(stringsPath)) {
    const data = JSON.parse(readText(stringsPath));
    (data.entries || []).forEach(e => keys.add(e.key));
  }
  if (fs.existsSync(pluralsPath)) {
    const data = JSON.parse(readText(pluralsPath));
    (data.entries || []).forEach(e => keys.add(e.key));
  }
  return keys;
}

// ─── SCENE YAML HELPERS ───────────────────────────────────────────────────────

/** Returns Map<fileID, {children:Set, father:string, type:string}> */
function parseSceneObjects(content) {
  const objects = new Map();

  // Collect all defined object IDs and their type tag
  for (const m of content.matchAll(/^--- !u!(\d+) &(\d+)/gm)) {
    objects.set(m[2], { type: m[1], children: new Set(), father: null, lines: [] });
  }

  // Split into per-object blocks
  const blocks = content.split(/^--- !u!\d+ &\d+$/m);
  const headers = [...content.matchAll(/^--- !u!\d+ &(\d+)$/gm)];

  headers.forEach((hdr, i) => {
    const id    = hdr[1];
    const block = blocks[i + 1] || '';
    const obj   = objects.get(id);
    if (!obj) return;

    // m_Father
    const fatherMatch = block.match(/m_Father:\s*\{fileID:\s*(\d+)/);
    if (fatherMatch) obj.father = fatherMatch[1];

    // m_Children
    for (const cm of block.matchAll(/- \{fileID:\s*(\d+)/g)) {
      // Only count lines that appear under m_Children (not other arrays)
      // We check that the block contains m_Children: before this match
      obj.children.add(cm[1]);
    }
  });

  // Re-parse children more carefully using line-by-line
  const lines = content.split('\n');
  let currentId = null;
  let inChildren = false;
  for (const line of lines) {
    const hdrMatch = line.match(/^--- !u!\d+ &(\d+)/);
    if (hdrMatch) { currentId = hdrMatch[1]; inChildren = false; continue; }
    if (!currentId) continue;
    if (/^\s+m_Children:/.test(line)) { inChildren = true; continue; }
    if (inChildren) {
      const childMatch = line.match(/^\s+- \{fileID:\s*(\d+)/);
      if (childMatch) {
        const obj = objects.get(currentId);
        if (obj) obj.children.add(childMatch[1]);
      } else if (/^\s+\w/.test(line) && !line.trim().startsWith('-')) {
        inChildren = false; // left children block
      }
    }
  }

  return objects;
}

// ─── FAKE GUID PATTERNS ───────────────────────────────────────────────────────
// Sequential fakes like 1234567890abcdef..., all-same-digit patterns, etc.
const FAKE_GUID_RE = /guid:\s*([0-9a-f]{32})/gi;
function isFakeGuid(guid) {
  if (/^0+$/.test(guid)) return false;                    // explicit null — OK
  if (/^0{16}[ef]0{15}$/.test(guid)) return false;       // Unity built-in
  // Fake: more than 20 consecutive same hex digit
  if (/(.)\1{19,}/.test(guid)) return true;
  // Fake: obvious sequential like 1234567890abcdef1234567890abcdef
  if (/^(0123456789ab|1234567890ab|abcdef012345)/i.test(guid)) return true;
  // Fake: ends in fffffff (common placeholder suffix)
  if (/[0-9a-f]{9}fffffff$/i.test(guid)) return true;
  return false;
}

// ─── UNSUPPORTED GLYPH CHECK ─────────────────────────────────────────────────
// LiberationSans SDF does not cover emoji, Wingdings arrows, enclosed alphanumerics, etc.
function hasUnsupportedGlyph(str) {
  // Emoji range (broad)
  if (/[\u{1F000}-\u{1FFFF}]/u.test(str)) return true;
  // Dingbats / Enclosed chars
  if (/[\u2460-\u27FF]/.test(str)) return true;
  // Arrow symbols that LiberationSans lacks: ↺ ↻
  if (/[\u21BA-\u21BB]/.test(str)) return true;
  return false;
}

// ─── 1. SCENE: parent-child consistency ─────────────────────────────────────
section('1. Scene YAML — parent-child hierarchy');
for (const scenePath of sceneFiles()) {
  const name    = path.basename(scenePath);
  const content = readText(scenePath);
  const objects = parseSceneObjects(content);

  test(`${name}: no conflicting m_Father vs m_Children`, () => {
    const conflicts = [];
    for (const [id, obj] of objects) {
      if (!obj.father || obj.father === '0') continue;
      const parent = objects.get(obj.father);
      if (!parent) continue; // external reference — skip
      if (!parent.children.has(id)) {
        // Parent doesn't list this child — check if the parent claims a different parent
        // Only flag if canvas/another object ALSO claims this object as child
        for (const [otherId, other] of objects) {
          if (otherId !== obj.father && other.children.has(id)) {
            conflicts.push(`Object &${id}: m_Father=&${obj.father} but &${otherId} also lists it as child`);
          }
        }
      }
    }
    assert(conflicts.length === 0, conflicts.join('\n    '));
  });
}

// ─── 2. SCENE: m_Transitions typo ────────────────────────────────────────────
section('2. Scene YAML — m_Transition field name');
for (const scenePath of sceneFiles()) {
  const name    = path.basename(scenePath);
  const content = readText(scenePath);
  test(`${name}: no m_Transitions (plural typo)`, () => {
    const matches = [...content.matchAll(/\bm_Transitions:/g)];
    assert(matches.length === 0,
      `Found ${matches.length} occurrence(s) of 'm_Transitions:' — should be 'm_Transition:'`);
  });
}

// ─── 3. SCENE: Buttons missing m_TargetGraphic ───────────────────────────────
section('3. Scene YAML — Button m_TargetGraphic');
for (const scenePath of sceneFiles()) {
  const name    = path.basename(scenePath);
  const content = readText(scenePath);

  test(`${name}: every Button MonoBehaviour has m_TargetGraphic`, () => {
    // Split into MonoBehaviour blocks and check each Button-typed one
    const blocks = content.split(/^--- !u!114 &\d+$/m);
    const missing = [];
    let blockIdx = 0;
    for (const hdr of content.matchAll(/^--- !u!114 &(\d+)$/gm)) {
      blockIdx++;
      const block = blocks[blockIdx] || '';
      if (!block.includes('UnityEngine.UI.Button')) continue;
      if (!block.includes('m_TargetGraphic:')) {
        missing.push(`Button &${hdr[1]} has no m_TargetGraphic`);
      }
    }
    assert(missing.length === 0, missing.join('\n    '));
  });
}

// ─── 4. SCENE: fake / placeholder GUIDs ──────────────────────────────────────
section('4. Scene YAML — no fake asset GUIDs');
for (const scenePath of sceneFiles()) {
  const name    = path.basename(scenePath);
  const content = readText(scenePath);

  test(`${name}: no fake/placeholder GUIDs`, () => {
    const fakes = [];
    for (const m of content.matchAll(FAKE_GUID_RE)) {
      if (isFakeGuid(m[1])) {
        const lineNo = content.slice(0, m.index).split('\n').length;
        fakes.push(`line ${lineNo}: guid ${m[1]}`);
      }
    }
    assert(fakes.length === 0, fakes.join('\n    '));
  });
}

// ─── 5. SCENE: off-screen UI (bottom-edge anchor + negative y) ───────────────
section('5. Scene YAML — no off-screen UI elements');
for (const scenePath of sceneFiles()) {
  const name    = path.basename(scenePath);
  const content = readText(scenePath);

  test(`${name}: no RectTransform with bottom-edge anchor and negative anchoredPosition.y`, () => {
    const lines    = content.split('\n');
    const offscreen = [];
    let inRT       = false;
    let rtId       = '';
    let anchorY    = null;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (/^--- !u!224 &(\d+)/.test(line)) {
        inRT    = true;
        rtId    = line.match(/&(\d+)/)[1];
        anchorY = null;
        continue;
      }
      if (/^--- !u!/.test(line)) { inRT = false; anchorY = null; continue; }
      if (!inRT) continue;

      const minMatch = line.match(/m_AnchorMin:\s*\{x:[^,]+,\s*y:\s*([\d.]+)/);
      if (minMatch) { anchorY = parseFloat(minMatch[1]); }

      const posMatch = line.match(/m_AnchoredPosition:\s*\{x:[^,]+,\s*y:\s*(-[\d.]+)/);
      if (posMatch && anchorY !== null && anchorY <= 0.05) {
        const yVal = parseFloat(posMatch[1]);
        if (yVal < -100) {
          offscreen.push(`RectTransform &${rtId}: anchorMin.y=${anchorY}, anchoredPosition.y=${yVal}`);
        }
      }
    }
    assert(offscreen.length === 0,
      `${offscreen.length} element(s) likely off-screen:\n    ${offscreen.join('\n    ')}`);
  });
}

// ─── 6. SCRIPTS: every BaseMinigame subclass has IsSetupComplete() ────────────
section('6. Scripts — BaseMinigame.IsSetupComplete() guard');
for (const scriptPath of minigameScripts()) {
  const name    = path.basename(scriptPath);
  const content = readText(scriptPath);

  test(`${name}: overrides IsSetupComplete()`, () => {
    assert(content.includes('IsSetupComplete()'),
      'Missing IsSetupComplete() override — minigame will always auto-complete if scene refs are null');
  });
}

// ─── 7. SCRIPTS: no hardcoded unsupported Unicode glyphs ─────────────────────
section('7. Scripts — no unsupported Unicode glyphs in string literals');
for (const scriptPath of allScripts().filter(p => !p.includes('/Editor/') && !p.includes('\\Editor\\'))) {
  const name    = path.basename(scriptPath);
  const content = readText(scriptPath);

  test(`${name}: no unsupported glyphs in string literals`, () => {
    const literals = content.match(/"[^"]*"/g) || [];
    const bad = literals.filter(hasUnsupportedGlyph);
    assert(bad.length === 0,
      `Unsupported glyphs found in: ${bad.join(', ')}`);
  });
}

// ─── 8. SCRIPTS: LocalizationManager.Get() uses known key prefixes ────────────
section('8. Scripts — LocalizationManager key prefixes are recognised');

const KNOWN_PREFIXES = [
  'level_', 'room_', 'q_', 'char_', 'minigame_instruction_',
  'reteach_', 'shape_', 'btn_', 'feedback_', 'streak_', 'hint_',
  'sequence_', 'tap_region_', 'coins_', 'sticker_', 'level_complete',
  'app_', 'select_', 'settings_', 'achievements_', 'parent_',
  'unlock_', 'confirm_', 'error_', 'loading_',
  'target_sum', 'find_number', 'all_collected', 'saved_points',
  'next_number', 'back_button', 'session_break_prompt',
];

for (const scriptPath of allScripts()) {
  const name    = path.basename(scriptPath);
  const content = readText(scriptPath);

  test(`${name}: LocalizationManager key literals match known prefixes`, () => {
    // Match .Get("key") and .GetPlural("key", ...) — literal string args only
    const calls = [...content.matchAll(/LocalizationManager\.Instance\.Get(?:Plural)?\("([^"]+)"/g)];
    const unknown = calls
      .map(m => m[1])
      .filter(key => {
        // Skip interpolated-looking keys (contain { or format args) or are pure constant names
        if (key.includes('{') || key.includes('_')) {
          return !KNOWN_PREFIXES.some(p => key.startsWith(p));
        }
        return false;
      });
    assert(unknown.length === 0,
      `Unrecognised key prefix(es): ${[...new Set(unknown)].join(', ')}`);
  });
}

// ─── 9. LOCALIZATION: keys referenced in scripts exist in JSON tables ─────────
section('9. Localization — referenced keys exist in JSON tables');

const enKeys = loadLocKeys(LOC_EN);
const csKeys = loadLocKeys(LOC_CS);

// Collect all literal keys used in scripts (non-interpolated)
const LITERAL_KEY_RE = /LocalizationManager\.Instance\.Get(?:Plural)?\("([a-z_][a-z0-9_]*)"/g;
const referencedKeys = new Set();
for (const scriptPath of allScripts()) {
  const content = readText(scriptPath);
  for (const m of content.matchAll(LITERAL_KEY_RE)) {
    referencedKeys.add(m[1]);
  }
}

test('All literal loc keys exist in en-US/strings.json', () => {
  const missing = [...referencedKeys].filter(k => !enKeys.has(k));
  assert(missing.length === 0, `Missing in en-US: ${missing.join(', ')}`);
});

test('All literal loc keys exist in cs-CZ/strings.json', () => {
  const missing = [...referencedKeys].filter(k => !csKeys.has(k));
  assert(missing.length === 0, `Missing in cs-CZ: ${missing.join(', ')}`);
});

// ─── 10. LOCALIZATION: button labels have no unsupported glyphs ──────────────
section('10. Localization — button label values have no unsupported glyphs');

for (const [lang, dir] of [['en-US', LOC_EN], ['cs-CZ', LOC_CS]]) {
  const stringsPath = path.join(dir, 'strings.json');
  if (!fs.existsSync(stringsPath)) continue;
  const data = JSON.parse(readText(stringsPath));

  test(`${lang}: no unsupported glyphs in btn_* values`, () => {
    const bad = (data.entries || [])
      .filter(e => e.key.startsWith('btn_') && hasUnsupportedGlyph(e.value));
    assert(bad.length === 0,
      `Bad entries: ${bad.map(e => `${e.key}="${e.value}"`).join(', ')}`);
  });
}

// ─── SUMMARY ──────────────────────────────────────────────────────────────────
console.log('\n' + '─'.repeat(60));
const total = passed + failed;
if (failed === 0) {
  console.log(`\x1b[32m✓ All ${total} tests passed.\x1b[0m`);
} else {
  console.log(`\x1b[31m${failed} of ${total} tests FAILED:\x1b[0m`);
  failures.forEach(f => console.log(`  • ${f.name}`));
  console.log();
}
process.exit(failed > 0 ? 1 : 0);

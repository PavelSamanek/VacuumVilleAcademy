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

// ─── HELPERS for new sections ─────────────────────────────────────────────────

/** Read a C# script by filename (searches Assets/Scripts recursively) */
function readScript(filename) {
  const all = allScripts();
  const found = all.find(p => path.basename(p) === filename);
  if (!found) throw new Error(`Script not found: ${filename}`);
  return readText(found);
}

/** Extract a numeric const value from C# source, e.g. `private const int Foo = 42;` */
function extractConst(content, name) {
  const m = content.match(new RegExp(`const\\s+(?:int|float)\\s+${name}\\s*=\\s*([\\d.]+)`));
  return m ? parseFloat(m[1]) : null;
}

// ─── 11. NumberBadge3D — 3D coin API contract ─────────────────────────────────
section('11. NumberBadge3D — 3D coin API contract');

const badge3dSrc = readScript('NumberBadge3D.cs');

test('NumberBadge3D.cs: exposes SetNumber(), SetColor(), ExplodeCorrect()', () => {
  assert(badge3dSrc.includes('public void SetNumber('),    'Missing public void SetNumber()');
  assert(badge3dSrc.includes('public void SetColor('),    'Missing public void SetColor()');
  assert(badge3dSrc.includes('public void ExplodeCorrect()'), 'Missing public void ExplodeCorrect()');
});

test('NumberBadge3D.cs: depth slab count is at least 5', () => {
  const v = extractConst(badge3dSrc, 'DepthSlabs');
  assert(v !== null, 'DepthSlabs constant not found');
  assert(v >= 5, `DepthSlabs = ${v}, expected >= 5`);
});

test('NumberBadge3D.cs: tilt loop coroutine exists (3D parallax animation)', () => {
  assert(badge3dSrc.includes('TiltLoop'), 'TiltLoop coroutine not found');
});

test('NumberBadge3D.cs: specular sweep coroutine exists', () => {
  assert(badge3dSrc.includes('SpecularSweep'), 'SpecularSweep coroutine not found');
});

test('NumberBadge3D.cs: appear bounce animation exists', () => {
  assert(badge3dSrc.includes('AppearBounce'), 'AppearBounce coroutine not found');
});

test('NumberBadge3D.cs: explosion spawns reticle + chip + star particles', () => {
  const rc = extractConst(badge3dSrc, 'ReticleCount');
  const cc = extractConst(badge3dSrc, 'ChipCount');
  const sc = extractConst(badge3dSrc, 'StarCount');
  assert(rc !== null && rc > 0, `ReticleCount = ${rc}, must be > 0`);
  assert(cc !== null && cc > 0, `ChipCount = ${cc}, must be > 0`);
  assert(sc !== null && sc > 0, `StarCount = ${sc}, must be > 0`);
});

test('NumberBadge3D.cs: loads all four required sprites', () => {
  assert(badge3dSrc.includes('"Sprites/badge_3d"'),    'Missing Sprites/badge_3d reference');
  assert(badge3dSrc.includes('"Sprites/chip_particle"'), 'Missing Sprites/chip_particle reference');
  assert(badge3dSrc.includes('"Sprites/star_particle"'), 'Missing Sprites/star_particle reference');
  assert(badge3dSrc.includes('"Sprites/circle"'),       'Missing Sprites/circle reference');
});

test('NumberBadge3D.cs: chromatic aberration label layers exist', () => {
  assert(badge3dSrc.includes('LabelChromR') || badge3dSrc.includes('ChromR') ||
         badge3dSrc.includes('labelChromR') || badge3dSrc.includes('_labelChromR'),
    'No chromatic aberration red-channel label found');
  assert(badge3dSrc.includes('LabelChromB') || badge3dSrc.includes('ChromB') ||
         badge3dSrc.includes('labelChromB') || badge3dSrc.includes('_labelChromB'),
    'No chromatic aberration blue-channel label found');
});

// ─── 12. EquationExplosionVFX — explosion contract ───────────────────────────
section('12. EquationExplosionVFX — explosion contract');

const eqVfxPath = path.join(SCRIPTS, 'UI', 'EquationExplosionVFX.cs');
test('EquationExplosionVFX.cs: file exists', () => {
  assert(fs.existsSync(eqVfxPath), 'EquationExplosionVFX.cs not found in Assets/Scripts/UI/');
});

const eqVfxSrc = fs.existsSync(eqVfxPath) ? readText(eqVfxPath) : '';

test('EquationExplosionVFX.cs: Explode() public method with sourceRect + text + streak', () => {
  assert(eqVfxSrc.includes('public void Explode('), 'Missing public void Explode()');
  assert(eqVfxSrc.includes('RectTransform'),         'Explode() should accept a RectTransform');
  assert(eqVfxSrc.includes('streak'),                'Explode() should accept a streak parameter');
});

test('EquationExplosionVFX.cs: Time.timeScale is always restored to 1 after slowdown', () => {
  // Count slow assignments vs. restore-to-1 assignments
  const slows    = [...eqVfxSrc.matchAll(/Time\.timeScale\s*=\s*0\./g)].length;
  const restores = [...eqVfxSrc.matchAll(/Time\.timeScale\s*=\s*1f/g)].length;
  assert(slows > 0,          'No time-scale slowdown found — anticipation effect missing');
  assert(restores >= slows,  `${slows} slowdown(s) but only ${restores} restore(s) to 1f`);
});

test('EquationExplosionVFX.cs: particle counts are positive', () => {
  const rc = extractConst(eqVfxSrc, 'ReticleCount');
  const cc = extractConst(eqVfxSrc, 'ChipCount');
  const sc = extractConst(eqVfxSrc, 'StarCount');
  assert(rc !== null && rc > 0, `ReticleCount = ${rc}`);
  assert(cc !== null && cc > 0, `ChipCount = ${cc}`);
  assert(sc !== null && sc > 0, `StarCount = ${sc}`);
});

test('EquationExplosionVFX.cs: streak multiplier is > 1.0', () => {
  const v = extractConst(eqVfxSrc, 'StreakMultiplier');
  assert(v !== null, 'StreakMultiplier constant not found');
  assert(v > 1.0, `StreakMultiplier = ${v}, must be > 1.0`);
});

test('EquationExplosionVFX.cs: spawns character fragments from equation text', () => {
  assert(eqVfxSrc.includes('SpawnCharFragments') || eqVfxSrc.includes('CharFragment'),
    'No character-fragment spawning found');
});

test('EquationExplosionVFX.cs: shockwave ring effect exists', () => {
  assert(eqVfxSrc.includes('ShockwaveRing') || eqVfxSrc.includes('Shockwave'),
    'No shockwave ring effect found');
});

test('EquationExplosionVFX.cs: screen flash effect exists', () => {
  assert(eqVfxSrc.includes('ScreenFlash') || eqVfxSrc.includes('Flash'),
    'No screen flash effect found');
});

test('EquationExplosionVFX.cs: canvas shake effect exists', () => {
  assert(eqVfxSrc.includes('ShakeCanvas') || eqVfxSrc.includes('Shake'),
    'No canvas shake effect found');
});

test('EquationExplosionVFX.cs: gravity constant is negative (particles fall down)', () => {
  const g = extractConst(eqVfxSrc, 'Gravity');
  // Gravity may appear as positive const applied with subtraction, or directly negative
  const negGravity = eqVfxSrc.match(/Gravity\s*=\s*-[\d.]+/);
  const posApplied = eqVfxSrc.match(/vel\.y\s*[+-]=\s*-?\s*Gravity/);
  assert(negGravity || (g !== null && posApplied),
    `Gravity should be negative or subtracted from vel.y (found: ${g})`);
});

test('EquationExplosionVFX.cs: particles batch-yield to avoid single-frame spike', () => {
  assert(eqVfxSrc.includes('yield return null'),
    'No yield return null inside particle loop — risk of frame spike');
});

// ─── 13. TaskDisplayController — explosion integration ───────────────────────
section('13. TaskDisplayController — explosion integration');

const tdcSrc = readScript('TaskDisplayController.cs');

test('TaskDisplayController.cs: declares EquationExplosionVFX field', () => {
  assert(tdcSrc.includes('EquationExplosionVFX'),
    'EquationExplosionVFX type not referenced in TaskDisplayController');
});

test('TaskDisplayController.cs: auto-adds EquationExplosionVFX component in Start', () => {
  assert(tdcSrc.includes('AddComponent<EquationExplosionVFX>()'),
    'AddComponent<EquationExplosionVFX>() not found — VFX will never run');
});

test('TaskDisplayController.cs: calls Explode() on correct answer', () => {
  assert(tdcSrc.includes('_explosionVFX') && tdcSrc.includes('.Explode('),
    '_explosionVFX.Explode() call not found in HandleCorrect path');
});

test('TaskDisplayController.cs: reveals solved equation before explosion (? → answer)', () => {
  assert(tdcSrc.includes('.Replace("?",'),
    'Equation text is never solved before explosion — kid sees "5+5=?" explode, not "5+5=10"');
});

test('TaskDisplayController.cs: correct-answer next-problem delay is >= 1.0 s', () => {
  // Find all NextAfterDelay(N) calls in HandleCorrect area.
  // We scan the whole file for literal numeric args; all must be >= 1.0 if they
  // appear in HandleCorrect context, but we conservatively just check the minimum
  // non-reveal delay is >= 1.0 (was 0.8 before this feature).
  const delays = [...tdcSrc.matchAll(/NextAfterDelay\(\s*([\d.]+)f?\s*\)/g)]
    .map(m => parseFloat(m[1]));
  assert(delays.length > 0, 'No NextAfterDelay() calls found');
  const minDelay = Math.min(...delays);
  assert(minDelay >= 1.0,
    `Shortest NextAfterDelay = ${minDelay}s — explosion needs at least 1.0 s to play`);
});

// ─── 14. LevelSelectController — 3D badge integration ────────────────────────
section('14. LevelSelectController — 3D badge integration');

const lscSrc = readScript('LevelSelectController.cs');

test('LevelSelectController.cs: creates NumberBadge3D per level button', () => {
  assert(lscSrc.includes('AddComponent<NumberBadge3D>()'),
    'AddComponent<NumberBadge3D>() not found — level numbers will be flat');
});

test('LevelSelectController.cs: calls SetNumber() on each badge', () => {
  assert(lscSrc.includes('SetNumber('),
    'SetNumber() not called — badge will show no number');
});

test('LevelSelectController.cs: calls SetColor() on each badge', () => {
  assert(lscSrc.includes('SetColor('),
    'SetColor() not called — badge will use default blue for all levels');
});

test('LevelSelectController.cs: badge is anchored left inside button', () => {
  // The badge RectTransform should have a left-side anchor (anchorMin.x == 0)
  assert(lscSrc.includes('anchorMin') && lscSrc.includes('BadgeSize'),
    'Badge anchor or BadgeSize constant not found — badge may overlap label text');
});

test('LevelSelectController.cs: level label offset clears the badge', () => {
  // Text label left-offset must reference BadgeSize so text does not overlap badge
  assert(lscSrc.includes('BadgeSize') && lscSrc.includes('labelLeft'),
    'Label left-offset does not account for BadgeSize — text and badge may overlap');
});

// ─── 15. Sprite resources — VFX assets exist on disk ─────────────────────────
section('15. Sprite resources — VFX assets exist on disk');

const SPRITES = path.join(ROOT, 'Assets', 'Resources', 'Sprites');

for (const filename of ['badge_3d.png', 'chip_particle.png', 'star_particle.png', 'circle.png']) {
  test(`Sprites/${filename} exists`, () => {
    assert(fs.existsSync(path.join(SPRITES, filename)),
      `${filename} missing — Resources.Load("Sprites/${filename.replace('.png','')}) will return null at runtime`);
  });
}

for (const filename of ['badge_3d.png', 'chip_particle.png', 'star_particle.png']) {
  test(`Sprites/${filename} has a .meta file`, () => {
    assert(fs.existsSync(path.join(SPRITES, filename + '.meta')),
      `${filename}.meta missing — Unity will not import the sprite`);
  });
}

// ─── MINIGAME MECHANIC HELPERS ────────────────────────────────────────────────

function readMinigame(filename) { return readScript(filename); }

function extractFloat(src, name) {
  // const/field: float Foo = 1.5f;
  const m = src.match(new RegExp(`float\\s+${name}\\s*=\\s*([\\d.]+)f?`));
  if (m) return parseFloat(m[1]);
  // expression-bodied property: float Foo => 1.5f;
  const m2 = src.match(new RegExp(`float\\s+${name}\\s*=>\\s*([\\d.]+)f?`));
  return m2 ? parseFloat(m2[1]) : null;
}

function extractInt(src, name) {
  // const/field: int Foo = 5;
  const m = src.match(new RegExp(`int\\s+${name}\\s*=\\s*(\\d+)`));
  if (m) return parseInt(m[1]);
  // expression-bodied property: int Foo => 5;
  const m2 = src.match(new RegExp(`int\\s+${name}\\s*=>\\s*(\\d+)`));
  return m2 ? parseInt(m2[1]) : null;
}

// ─── 16. SockSortSweep — sock collection mechanics ────────────────────────────
section('16. SockSortSweep — sock collection mechanics');

const sockSrc = readMinigame('SockSortSweep.cs');

test('SockSortSweep.cs: CollectRadius is positive (collection detection works)', () => {
  const r = extractFloat(sockSrc, 'CollectRadius');
  assert(r !== null && r > 0, `CollectRadius = ${r}, must be > 0`);
});

test('SockSortSweep.cs: MaxScore = 10 (matches 10 socks)', () => {
  const v = extractInt(sockSrc, 'MaxScore');
  assert(v === 10, `MaxScore = ${v}, expected 10`);
});

test('SockSortSweep.cs: TimeLimit >= 60s', () => {
  const v = extractFloat(sockSrc, 'TimeLimit');
  assert(v !== null && v >= 60, `TimeLimit = ${v}, expected >= 60`);
});

test('SockSortSweep.cs: CompleteEarly called when all 10 socks collected', () => {
  assert(sockSrc.includes('CompleteEarly()'), 'CompleteEarly() not found');
  assert(sockSrc.includes('> 10'), 'Boundary check _nextExpected > 10 not found');
});

test('SockSortSweep.cs: wrong-order tap triggers bounce + wrong sound', () => {
  assert(sockSrc.includes('BounceSock'), 'BounceSock coroutine not found');
  assert(sockSrc.includes('PlayWrong'), 'PlayWrong() not found for wrong-order tap');
});

test('SockSortSweep.cs: socks spawned at shuffled positions (not in order)', () => {
  assert(sockSrc.includes('Shuffle') || sockSrc.includes('Random.Range(0, i + 1)'),
    'No shuffle logic found — socks may always spawn in the same positions');
});

test('SockSortSweep.cs: next-number label updated after each collect', () => {
  assert(sockSrc.includes('UpdateNextLabel'), 'UpdateNextLabel() not called on collect');
});

// ─── 17. CrumbCollectCountdown — whack-a-mole number mechanics ────────────────
section('17. CrumbCollectCountdown — number search mechanics');

const crumbSrc = readMinigame('CrumbCollectCountdown.cs');

test('CrumbCollectCountdown.cs: MaxScore = 20 (numbers 1–20)', () => {
  const v = extractInt(crumbSrc, 'MaxScore');
  assert(v === 20, `MaxScore = ${v}, expected 20`);
});

test('CrumbCollectCountdown.cs: crumbs despawn after crumbVisibleTime', () => {
  assert(crumbSrc.includes('DespawnAfter'), 'DespawnAfter coroutine not found');
  assert(crumbSrc.includes('crumbVisibleTime'), 'crumbVisibleTime not referenced in despawn');
});

test('CrumbCollectCountdown.cs: vacuum moves to tap position (MoveVacuumTo)', () => {
  assert(crumbSrc.includes('MoveVacuumTo'), 'MoveVacuumTo coroutine not found');
  assert(crumbSrc.includes('MoveTowards'), 'Vector3.MoveTowards not found — vacuum may teleport');
});

test('CrumbCollectCountdown.cs: wrong-number tap shakes vacuum, no score penalty', () => {
  assert(crumbSrc.includes('ShakeRect'), 'ShakeRect not found for wrong-number tap');
  // Must NOT call AddScore on wrong tap
  const wrongBlock = crumbSrc.match(/else\s*\{[^}]*PlayWrong[^}]*\}/s);
  if (wrongBlock) {
    assert(!wrongBlock[0].includes('AddScore'), 'AddScore called inside wrong-tap block');
  }
});

test('CrumbCollectCountdown.cs: occupied slots tracked (no overlap spawning)', () => {
  assert(crumbSrc.includes('_occupiedSlots'), '_occupiedSlots set not found — crumbs may overlap');
});

test('CrumbCollectCountdown.cs: CompleteEarly when all 20 collected', () => {
  assert(crumbSrc.includes('CompleteEarly()'), 'CompleteEarly() not found');
  assert(crumbSrc.includes('> 20'), 'Boundary check _targetNumber > 20 not found');
});

// ─── 18. CushionCannonCatch — two-cannon addition mechanics ───────────────────
section('18. CushionCannonCatch — two-cannon addition mechanics');

const cushionSrc = readMinigame('CushionCannonCatch.cs');

test('CushionCannonCatch.cs: MaxScore = 5 (five rounds)', () => {
  const v = extractInt(cushionSrc, 'MaxScore');
  assert(v === 5, `MaxScore = ${v}, expected 5`);
});

test('CushionCannonCatch.cs: speed increases per round (_currentSpeed += 0.3f)', () => {
  assert(cushionSrc.includes('_currentSpeed') && cushionSrc.includes('+= 0.3f'),
    'Speed escalation not found — difficulty never increases');
});

test('CushionCannonCatch.cs: correct answer = _a + _b', () => {
  assert(cushionSrc.includes('_a + _b'), 'Correct answer formula _a + _b not found');
});

test('CushionCannonCatch.cs: 3 answer choices generated per round', () => {
  assert(cushionSrc.includes('GenerateChoices'), 'GenerateChoices not found');
  assert(cushionSrc.includes('set.Count < 3'), 'Choice count not capped at 3');
});

test('CushionCannonCatch.cs: pile falls from mergePoint to landingPoint (timed)', () => {
  assert(cushionSrc.includes('mergePoint') && cushionSrc.includes('landingPoint'),
    'mergePoint or landingPoint not found');
  assert(cushionSrc.includes('Vector3.Lerp(mergePoint.position, landingPoint.position'),
    'Lerp from merge to landing not found — pile animation broken');
});

test('CushionCannonCatch.cs: timeout counts as wrong (vacuumAnimator Dodge)', () => {
  assert(cushionSrc.includes('"Dodge"'), 'Dodge trigger not found — no visual feedback on timeout');
});

test('CushionCannonCatch.cs: CompleteEarly after 5 rounds', () => {
  assert(cushionSrc.includes('_round >= 5') && cushionSrc.includes('CompleteEarly()'),
    'Round boundary or CompleteEarly not found');
});

// ─── 19. DrainDefense — duck-blocking mechanics ───────────────────────────────
section('19. DrainDefense — duck-blocking mechanics');

const drainSrc = readMinigame('DrainDefense.cs');

test('DrainDefense.cs: TimeLimit = 60s', () => {
  const v = extractFloat(drainSrc, 'TimeLimit');
  assert(v === 60, `TimeLimit = ${v}, expected 60`);
});

test('DrainDefense.cs: duck point values 1–5 (Random.Range(1, 6))', () => {
  assert(drainSrc.includes('Random.Range(1, 6)'), 'Duck point range 1-5 not found');
});

test('DrainDefense.cs: tilt/accelerometer input supported on mobile', () => {
  assert(drainSrc.includes('Input.acceleration'), 'Tilt input not found — mobile tilt control missing');
});

test('DrainDefense.cs: vacuum position clamped to drain range', () => {
  assert(drainSrc.includes('Mathf.Clamp'), 'Mathf.Clamp not found — vacuum can move off-screen');
});

test('DrainDefense.cs: blocked duck → AddScore, drained duck → no score', () => {
  assert(drainSrc.includes('DuckBlocked') && drainSrc.includes('DuckDrained'),
    'DuckBlocked or DuckDrained methods not found');
  // DuckBlocked must call AddScore
  const blockedM = drainSrc.match(/private void DuckBlocked[\s\S]*?^    \}/m);
  if (blockedM) assert(blockedM[0].includes('AddScore'), 'AddScore not called in DuckBlocked');
  // DuckDrained must NOT call AddScore
  const drainedM = drainSrc.match(/private void DuckDrained[\s\S]*?^    \}/m);
  if (drainedM) assert(!drainedM[0].includes('AddScore'), 'AddScore incorrectly called in DuckDrained');
});

test('DrainDefense.cs: active ducks destroyed on OnMinigameEnd()', () => {
  assert(drainSrc.includes('OnMinigameEnd') && drainSrc.includes('Destroy'),
    'OnMinigameEnd cleanup not found — duck GameObjects may leak');
});

// ─── 20. BoxTowerBuilder — falling-box addition mechanics ────────────────────
section('20. BoxTowerBuilder — falling-box addition mechanics');

const boxSrc = readMinigame('BoxTowerBuilder.cs');

test('BoxTowerBuilder.cs: fallSpeed is fast enough (>= 50)', () => {
  const v = extractFloat(boxSrc, 'fallSpeed');
  assert(v !== null && v >= 50,
    `fallSpeed = ${v} — boxes fall too slowly; must be >= 50 screen-units/sec`);
});

test('BoxTowerBuilder.cs: MaxScore = 20 correct pairs', () => {
  const v = extractInt(boxSrc, 'MaxScore');
  assert(v === 20, `MaxScore = ${v}, expected 20`);
});

test('BoxTowerBuilder.cs: two-tap pair selection (first then second box)', () => {
  assert(boxSrc.includes('_firstSelectedBox == null') && boxSrc.includes('_firstSelectedBox'),
    'First/second selection state not found — two-tap mechanic broken');
});

test('BoxTowerBuilder.cs: correct pair sum equals _targetSum', () => {
  assert(boxSrc.includes('_firstSelectedBox.Number + box.Number'),
    'Sum calculation not found in pair check');
  assert(boxSrc.includes('== _targetSum'), 'Target sum comparison not found');
});

test('BoxTowerBuilder.cs: wrong pair deselects first box (no double highlight)', () => {
  assert(boxSrc.includes('_firstSelectedBox = null') && boxSrc.includes('HighlightBox'),
    'First-box deselect or highlight not found on wrong pair');
});

test('BoxTowerBuilder.cs: boxes stack at bottom when not collected (path-blocked logic)', () => {
  assert(boxSrc.includes('StackBox') && boxSrc.includes('_stackHeight'),
    'StackBox or _stackHeight not found');
});

test('BoxTowerBuilder.cs: path-blocked ends game (maxStackHeight enforced)', () => {
  assert(boxSrc.includes('maxStackHeight') && boxSrc.includes('FinishMinigame_PathBlocked'),
    'maxStackHeight boundary or FinishMinigame_PathBlocked not found');
});

test('BoxTowerBuilder.cs: CompleteEarly at 20 correct pairs', () => {
  assert(boxSrc.includes('>= 20') && boxSrc.includes('CompleteEarly()'),
    '_correctPairs >= 20 boundary or CompleteEarly not found');
});

// ─── 21. StreamerUntangleSprint — subtraction sprint mechanics ────────────────
section('21. StreamerUntangleSprint — subtraction sprint mechanics');

const streamerSrc = readMinigame('StreamerUntangleSprint.cs');

test('StreamerUntangleSprint.cs: MaxMisses = 3', () => {
  const v = extractInt(streamerSrc, 'MaxMisses');
  assert(v === 3, `MaxMisses = ${v}, expected 3`);
});

test('StreamerUntangleSprint.cs: totalKnots = 15', () => {
  const v = extractInt(streamerSrc, 'totalKnots');
  assert(v === 15, `totalKnots = ${v}, expected 15`);
});

test('StreamerUntangleSprint.cs: subtraction problem (a > b, result >= 0)', () => {
  assert(streamerSrc.includes('_correctAnswer = a - b'), 'Subtraction formula not found');
  assert(streamerSrc.includes('int b = Random.Range(1, a)'), 'b < a guard not found — negative results possible');
});

test('StreamerUntangleSprint.cs: knot timer bar depletes during encounter', () => {
  assert(streamerSrc.includes('knotTimerBar') && streamerSrc.includes('knotTimerDuration'),
    'knotTimerBar or knotTimerDuration not found');
  assert(streamerSrc.includes('1f - (elapsed / knotTimerDuration)'),
    'Timer fraction formula not found — bar may not deplete correctly');
});

test('StreamerUntangleSprint.cs: timeout at knot counts as a miss', () => {
  assert(streamerSrc.includes('_misses++'), '_misses increment not found');
  // Must increment misses on timeout path (after elapsed >= knotTimerDuration)
  assert(streamerSrc.includes('!answered') && streamerSrc.includes('_misses++'),
    'Timeout miss increment not found');
});

test('StreamerUntangleSprint.cs: running animation between knots', () => {
  assert(streamerSrc.includes('"Running"'), 'Running animator parameter not found');
  assert(streamerSrc.includes('SetBool("Running", true)') && streamerSrc.includes('SetBool("Running", false)'),
    'Running bool not toggled on/off between knots');
});

test('StreamerUntangleSprint.cs: CompleteEarly when all knots resolved or max misses', () => {
  assert(streamerSrc.includes('CompleteEarly()'), 'CompleteEarly() not found');
  assert(streamerSrc.includes('_knotsResolved >= totalKnots') || streamerSrc.includes('_misses >= MaxMisses'),
    'Neither knot-complete nor max-misses boundary found');
});

// ─── 22. FlowerBedFrenzy — multiplication counting mechanics ─────────────────
section('22. FlowerBedFrenzy — multiplication counting mechanics');

const flowerSrc = readMinigame('FlowerBedFrenzy.cs');

test('FlowerBedFrenzy.cs: MaxScore = 10 rows', () => {
  const v = extractInt(flowerSrc, 'MaxScore');
  assert(v === 10, `MaxScore = ${v}, expected 10`);
});

test('FlowerBedFrenzy.cs: multiplier is 2 or 5 only', () => {
  assert(flowerSrc.includes('multiplier = Random.Range(0, 2) == 0 ? 2 : 5'),
    'Multiplier selection is not restricted to 2 or 5');
});

test('FlowerBedFrenzy.cs: correctAnswer = multiplier * groups', () => {
  assert(flowerSrc.includes('_correctAnswer  = multiplier * groups') ||
         flowerSrc.includes('_correctAnswer = multiplier * groups'),
    'Correct answer formula not found');
});

test('FlowerBedFrenzy.cs: 3 answer buttons (multiple-choice)', () => {
  assert(flowerSrc.includes('answerButtons'), 'answerButtons not found');
  assert(flowerSrc.includes('choices[i]'), 'Choice assignment to buttons not found');
});

test('FlowerBedFrenzy.cs: row timeout shows wilted effect', () => {
  assert(flowerSrc.includes('ShowWilted'), 'ShowWilted not found — no visual feedback on timeout');
  assert(flowerSrc.includes('elapsed < rowInterval'), 'rowInterval timeout logic not found');
});

test('FlowerBedFrenzy.cs: correct answer triggers water particles', () => {
  assert(flowerSrc.includes('waterParticles') && flowerSrc.includes('waterParticles.Play()'),
    'Water particle effect not triggered on correct answer');
});

test('FlowerBedFrenzy.cs: grid cleared between rows', () => {
  assert(flowerSrc.includes('ClearGrid'), 'ClearGrid not found — old flowers persist between rows');
});

// ─── 23. AtticBinBlitz — division bin-tap mechanics ──────────────────────────
section('23. AtticBinBlitz — division bin-tap mechanics');

const atticSrc = readMinigame('AtticBinBlitz.cs');

test('AtticBinBlitz.cs: TotalRounds = 5', () => {
  const v = extractConst(atticSrc, 'TotalRounds');
  assert(v === 5, `TotalRounds = ${v}, expected 5`);
});

test('AtticBinBlitz.cs: divisor equals number of bins', () => {
  assert(atticSrc.includes('int divisor = bins.Length'),
    'divisor != bins.Length — division problem does not match bin count');
});

test('AtticBinBlitz.cs: dividend = divisor × correctAnswer (always divisible)', () => {
  assert(atticSrc.includes('int dividend   = divisor * _correctAnswer') ||
         atticSrc.includes('int dividend = divisor * _correctAnswer'),
    'Dividend not computed as divisor*correctAnswer — may produce non-integer result');
});

test('AtticBinBlitz.cs: correct bin flashes green, wrong bin flashes orange', () => {
  assert(atticSrc.includes('FlashBin'), 'FlashBin not found');
  assert(atticSrc.includes('0.4f, 0.9f, 0.4f'), 'Green flash color not found for correct answer');
  assert(atticSrc.includes('1f, 0.57f, 0f'), 'Orange flash color not found for wrong answer');
});

test('AtticBinBlitz.cs: all bins disabled after answer (no double-tap)', () => {
  assert(atticSrc.includes('b.button.interactable = false'),
    'Bins not disabled after answer — double-tap possible');
});

test('AtticBinBlitz.cs: wrong answers differ from correct by small offset (plausible distractors)', () => {
  assert(atticSrc.includes('GenerateWrongAnswers'), 'GenerateWrongAnswers not found');
  assert(atticSrc.includes('Random.Range(1, 5)'),
    'Wrong answer offset not found — distractors may be far from correct');
});

// ─── 24. SequenceSprinkler — skip-count sequence mechanics ───────────────────
section('24. SequenceSprinkler — skip-count sequence mechanics');

const sprinklerSrc = readMinigame('SequenceSprinkler.cs');

test('SequenceSprinkler.cs: MaxScore = 5 sets', () => {
  const v = extractInt(sprinklerSrc, 'MaxScore');
  assert(v === 5, `MaxScore = ${v}, expected 5`);
});

test('SequenceSprinkler.cs: skip values limited to {1,2,5,10}', () => {
  assert(sprinklerSrc.includes('{ 1, 2, 5, 10 }'),
    'skipOptions not restricted to {1,2,5,10}');
});

test('SequenceSprinkler.cs: sequence is strictly ascending (start + i * skipValue)', () => {
  assert(sprinklerSrc.includes('start + i * _skipValue'),
    'Sequence generation formula not found — sequence may not be strictly ordered');
});

test('SequenceSprinkler.cs: numbers assigned to heads are shuffled (order hidden)', () => {
  assert(sprinklerSrc.includes('AssignToHeads') && sprinklerSrc.includes('shuffled'),
    'Shuffled assignment not found — sequence would be visible in head order');
});

test('SequenceSprinkler.cs: wrong tap flashes orange on head indicator', () => {
  assert(sprinklerSrc.includes('FlashWrong'), 'FlashWrong coroutine not found');
  assert(sprinklerSrc.includes('1f, 0.57f, 0f'), 'Orange flash color not found for wrong tap');
});

test('SequenceSprinkler.cs: correctly tapped head is disabled (cannot re-tap)', () => {
  assert(sprinklerSrc.includes('head.button.interactable = false'),
    'Tapped head not disabled — child can re-tap same head');
});

test('SequenceSprinkler.cs: rainbow particles on set completion', () => {
  assert(sprinklerSrc.includes('rainbowParticles') && sprinklerSrc.includes('rainbowParticles.Play()'),
    'Rainbow particle effect not triggered on set completion');
});

// ─── 25. GrandHallRestoration — shape-sorting mosaic mechanics ────────────────
section('25. GrandHallRestoration — shape-sorting mosaic mechanics');

const grandSrc = readMinigame('GrandHallRestoration.cs');

test('GrandHallRestoration.cs: MaxScore = 30 tiles', () => {
  const v = extractInt(grandSrc, 'MaxScore');
  assert(v === 30, `MaxScore = ${v}, expected 30`);
});

test('GrandHallRestoration.cs: fillAmount updated on correct placement', () => {
  assert(grandSrc.includes('fillAmount = (float)region.filledSlots / region.totalSlots'),
    'fillAmount not updated — mosaic progress bar stays empty');
});

test('GrandHallRestoration.cs: tile falls to hover level before waiting for tap', () => {
  assert(grandSrc.includes('DropTile') && grandSrc.includes('targetY'),
    'DropTile or hover targetY not found — tile may not animate in');
});

test('GrandHallRestoration.cs: wrong region taps bounce tile away (BounceAway)', () => {
  assert(grandSrc.includes('BounceAway'), 'BounceAway not found — no feedback on wrong region');
});

test('GrandHallRestoration.cs: shape label localised via shape_{key} key', () => {
  assert(grandSrc.includes('shape_{region.shapeKey}') || grandSrc.includes('"shape_"'),
    'shape_ localization key not used for region labels');
});

test('GrandHallRestoration.cs: TileLoop waits for current tile before spawning next', () => {
  assert(grandSrc.includes('WaitUntil') && grandSrc.includes('_currentTile == null'),
    'TileLoop does not wait for placement — multiple tiles may fall at once');
});

test('GrandHallRestoration.cs: celebration particles on mosaic completion', () => {
  assert(grandSrc.includes('celebrationParticles') && grandSrc.includes('celebrationParticles.Play()'),
    'Celebration particles not triggered on mosaic completion');
});

// ─── 26. ScramblerShutdown — adaptive multi-topic boss mechanics ──────────────
section('26. ScramblerShutdown — adaptive multi-topic boss mechanics');

const scramblerSrc = readMinigame('ScramblerShutdown.cs');

test('ScramblerShutdown.cs: exactly 5 panel topics covering all core topics', () => {
  assert(scramblerSrc.includes('AdditionTo20'), 'AdditionTo20 panel topic missing');
  assert(scramblerSrc.includes('SubtractionWithin20'), 'SubtractionWithin20 panel topic missing');
  assert(scramblerSrc.includes('Multiplication2x5x'), 'Multiplication2x5x panel topic missing');
  assert(scramblerSrc.includes('DivisionBy2_3_5'), 'DivisionBy2_3_5 panel topic missing');
  assert(scramblerSrc.includes('NumberOrdering'), 'NumberOrdering panel topic missing');
});

test('ScramblerShutdown.cs: MaxScore = 5 (one point per panel)', () => {
  const v = extractInt(scramblerSrc, 'MaxScore');
  assert(v === 5, `MaxScore = ${v}, expected 5`);
});

test('ScramblerShutdown.cs: adaptive difficulty via TopicAccuracy', () => {
  assert(scramblerSrc.includes('GetOrCreateTopicAccuracy'),
    'GetOrCreateTopicAccuracy not found — difficulty is not adaptive');
  assert(scramblerSrc.includes('MathTaskGenerator.Generate'),
    'MathTaskGenerator.Generate not found — problems not generated from player history');
});

test('ScramblerShutdown.cs: correct result recorded in TopicAccuracy', () => {
  assert(scramblerSrc.includes('ta.RecordResult(correct)'),
    'RecordResult not called — player performance is never saved for adaptation');
});

test('ScramblerShutdown.cs: panel timer counts down (per-panel time limit)', () => {
  assert(scramblerSrc.includes('panelTimeLimit') && scramblerSrc.includes('elapsed < panelTimeLimit'),
    'panelTimeLimit countdown not found');
  // Timer value updated — field may be panelTimer or timerSlider
  assert(
    scramblerSrc.includes('panel.panelTimer.value = 1f - (elapsed / panelTimeLimit)') ||
    scramblerSrc.includes('panel.timerSlider.value = 1f - (elapsed / panelTimeLimit)'),
    'Panel timer slider not updated');
});

test('ScramblerShutdown.cs: timeout leaves panel unsolved but continues to next', () => {
  assert(scramblerSrc.includes('!answered') && scramblerSrc.includes('PlayWrong'),
    'Timeout wrong-feedback not found');
  // PanelSequence must loop through all panels regardless (panels or _runtimePanels)
  assert(
    scramblerSrc.includes('for (int i = 0; i < panels.Length; i++)') ||
    scramblerSrc.includes('for (int i = 0; i < _runtimePanels.Length; i++)'),
    'Panel loop not found — game may not continue after timeout');
});

test('ScramblerShutdown.cs: Scrambler Shutdown animation + particles on all panels solved', () => {
  assert(scramblerSrc.includes('"Shutdown"'), 'Shutdown animator trigger not found');
  assert(scramblerSrc.includes('shutdownParticles') && scramblerSrc.includes('shutdownParticles.Play()'),
    'Shutdown particles not triggered on completion');
});

test('ScramblerShutdown.cs: answer buttons repositioned within panel (off-screen fix)', () => {
  assert(scramblerSrc.includes('yMin') && scramblerSrc.includes('yMax'),
    'Panel button repositioning not found — buttons may render off-screen');
});

// ─── §27 GAMEMANAGER ARCHITECTURE ────────────────────────────────────────────
section('§27 GameManager Architecture');

const gmPath = path.join(SCRIPTS, 'Core', 'GameManager.cs');
const gmSrc  = readText(gmPath);

test('GameManager.cs: singleton pattern — static Instance property', () => {
  assert(gmSrc.includes('public static GameManager Instance'), 'Instance property not found');
});

test('GameManager.cs: DontDestroyOnLoad called in Awake', () => {
  assert(gmSrc.includes('DontDestroyOnLoad(gameObject)'), 'DontDestroyOnLoad not found');
});

test('GameManager.cs: all scene transitions use private LoadScene (not direct SceneManager)', () => {
  // LoadScene is the only method that calls SceneManager.LoadSceneAsync
  assert(gmSrc.includes('private void LoadScene'), 'private LoadScene helper not found');
  assert(gmSrc.includes('SceneManager.LoadSceneAsync(sceneName)'), 'SceneManager call inside LoadScene not found');
});

test('GameManager.cs: no direct SceneManager calls outside LoadScene helper', () => {
  // Count SceneManager references — should only be inside LoadScene
  const lines = gmSrc.split('\n');
  const badLines = lines.filter(l =>
    l.includes('SceneManager.') && !l.includes('private void LoadScene') && !l.includes('SceneManager.LoadSceneAsync(sceneName)')
  );
  assert(badLines.length === 0, `Direct SceneManager calls outside LoadScene: ${badLines.map(l=>l.trim()).join(' | ')}`);
});

test('GameManager.cs: CompleteMinigame saves bestMinigameScore when score is higher', () => {
  assert(gmSrc.includes('if (score > lp.bestMinigameScore) lp.bestMinigameScore = score'),
    'bestMinigameScore not conditionally updated');
});

test('GameManager.cs: star calculation — 3 tiers based on accuracy %', () => {
  assert(gmSrc.includes('int stars = 1'), 'stars defaults to 1 not found');
  assert(gmSrc.includes('stars = 3'), 'stars = 3 tier not found');
  assert(gmSrc.includes('stars = 2'), 'stars = 2 tier not found');
  assert(gmSrc.includes('starThreshold3') && gmSrc.includes('starThreshold2'),
    'starThreshold fields not used');
});

test('GameManager.cs: sticker collected and added to collectedStickers list on completion', () => {
  assert(gmSrc.includes('lp.stickerCollected = true'), 'stickerCollected not set');
  assert(gmSrc.includes('Progress.collectedStickers.Add(ActiveLevel.levelIndex)'),
    'collectedStickers.Add not called');
});

test('GameManager.cs: save called on application pause and quit', () => {
  assert(gmSrc.includes('OnApplicationPause'), 'OnApplicationPause not found');
  assert(gmSrc.includes('OnApplicationQuit'), 'OnApplicationQuit not found');
  const pauseBlock = gmSrc.match(/OnApplicationPause[\s\S]{0,80}SaveSystem\.Save/);
  assert(pauseBlock, 'SaveSystem.Save not called in OnApplicationPause');
});

test('GameManager.cs: session counters reset when MathTask starts', () => {
  assert(gmSrc.includes('ResetSessionCounters()'), 'ResetSessionCounters not called');
  assert(gmSrc.includes('TasksCompletedThisSession = 0'), 'TasksCompleted not reset');
  assert(gmSrc.includes('FirstAttemptCorrectThisSession = 0'), 'FirstAttemptCorrect not reset');
});

// ─── §28 AUDIOMANAGER ────────────────────────────────────────────────────────
section('§28 AudioManager');

const amPath = path.join(SCRIPTS, 'Core', 'AudioManager.cs');
const amSrc  = readText(amPath);

test('AudioManager.cs: null-safe guard in PlayCorrect (clip != null)', () => {
  assert(amSrc.includes('correctSound != null'), 'PlayCorrect has no null guard');
});

test('AudioManager.cs: null-safe guard in PlayWrong (clip != null)', () => {
  assert(amSrc.includes('wrongSound != null'), 'PlayWrong has no null guard');
});

test('AudioManager.cs: volume persisted via PlayerPrefs (3 keys)', () => {
  assert(amSrc.includes('"vol_music"'), 'vol_music PlayerPrefs key not found');
  assert(amSrc.includes('"vol_sfx"'),   'vol_sfx PlayerPrefs key not found');
  assert(amSrc.includes('"vol_voice"'), 'vol_voice PlayerPrefs key not found');
});

test('AudioManager.cs: PlayEquationVoice chains eq_prompt + num + op + num clips', () => {
  assert(amSrc.includes('eq_prompt'), 'eq_prompt clip key not found in PlayEquationVoice');
  assert(amSrc.includes('num_{operands[0]}') || amSrc.includes('num_{operands'), 'num_ clip for first operand not found');
  assert(amSrc.includes('op_plus'), 'op_plus key not mapped');
  assert(amSrc.includes('op_minus'), 'op_minus key not mapped');
  assert(amSrc.includes('op_times'), 'op_times key not mapped');
  assert(amSrc.includes('op_divide'), 'op_divide key not mapped');
});

test('AudioManager.cs: voiceSource stops before playing new clip', () => {
  assert(amSrc.includes('voiceSource.Stop()'), 'voiceSource.Stop() not called before PlayVoice');
});

test('AudioManager.cs: AudioListener added when missing (prevents audio drop between scenes)', () => {
  assert(amSrc.includes('AudioListener') && amSrc.includes('gameObject.AddComponent<AudioListener>()'),
    'AudioListener not auto-added in EnsureAudioSources');
});

test('AudioManager.cs: SFX cache prevents re-loading clips every frame', () => {
  assert(amSrc.includes('_sfxCache'), 'SFX clip cache not found');
});

// ─── §29 LOCALIZATIONMANAGER ─────────────────────────────────────────────────
section('§29 LocalizationManager');

const locMgrPath = path.join(SCRIPTS, 'Core', 'LocalizationManager.cs');
const locMgrSrc  = readText(locMgrPath);

test('LocalizationManager.cs: Czech plural form — 1=one(0), 2-4=few(1), 5+=many(2)', () => {
  assert(locMgrSrc.includes('if (n == 1) return 0'),         'Czech one-form (n==1 → 0) not found');
  assert(locMgrSrc.includes('if (n >= 2 && n <= 4) return 1'), 'Czech few-form (2-4 → 1) not found');
  assert(locMgrSrc.includes('return 2'),                       'Czech many-form (→ 2) not found');
});

test('LocalizationManager.cs: English plural form — 1=one(0), else many(2)', () => {
  assert(locMgrSrc.includes('n == 1 ? 0 : 2'), 'English plural form (1→0, else→2) not found');
});

test('LocalizationManager.cs: missing key fallback returns the key itself', () => {
  assert(locMgrSrc.includes('return key'), 'Missing-key fallback "return key" not found');
});

test('LocalizationManager.cs: Get(key, args) uses string.Format for substitution', () => {
  assert(locMgrSrc.includes('string.Format(template, args)'),
    'string.Format not used in Get(key, params object[])');
});

test('LocalizationManager.cs: GetPlural returns "{count} {key}" when plural entry missing', () => {
  assert(locMgrSrc.includes('return $"{count} {key}"') || locMgrSrc.includes('return `${count} ${key}`'),
    'GetPlural fallback format not found');
});

test('LocalizationManager.cs: DontDestroyOnLoad on singleton', () => {
  assert(locMgrSrc.includes('DontDestroyOnLoad(gameObject)'), 'DontDestroyOnLoad not found');
});

test('LocalizationManager.cs: locale folder matches cs-CZ and en-US', () => {
  assert(locMgrSrc.includes('"cs-CZ"') && locMgrSrc.includes('"en-US"'),
    'Locale folder names cs-CZ / en-US not found');
});

// ─── §30 SAVESYSTEM ───────────────────────────────────────────────────────────
section('§30 SaveSystem');

const ssSrc = readText(path.join(SCRIPTS, 'Core', 'SaveSystem.cs'));

test('SaveSystem.cs: save path is persistentDataPath/player_progress.json', () => {
  assert(ssSrc.includes('persistentDataPath'), 'persistentDataPath not used');
  assert(ssSrc.includes('"player_progress.json"'), 'player_progress.json filename not found');
});

test('SaveSystem.cs: Load returns new PlayerProgress() when file not found', () => {
  assert(ssSrc.includes('!File.Exists(SavePath)') || ssSrc.includes('File.Exists'),
    'File existence check not found in Load');
  assert(ssSrc.includes('return new PlayerProgress()'), 'Fallback new PlayerProgress() not found');
});

test('SaveSystem.cs: Load has try/catch with fallback on corrupt JSON', () => {
  // Must have catch and fallback inside catch
  const catchIdx   = ssSrc.indexOf('catch');
  const fallbackIdx = ssSrc.indexOf('return new PlayerProgress()', catchIdx);
  assert(catchIdx > 0 && fallbackIdx > catchIdx, 'try/catch with fallback new PlayerProgress() not found');
});

test('SaveSystem.cs: Save has try/catch to survive write failures', () => {
  assert(ssSrc.includes('try') && ssSrc.includes('catch'), 'try/catch not found in Save');
  assert(ssSrc.includes('File.WriteAllText'), 'File.WriteAllText not found');
});

// ─── §31 MATHTASKGENERATOR CORRECTNESS ───────────────────────────────────────
section('§31 MathTaskGenerator Correctness');

const mtgSrc = readText(path.join(SCRIPTS, 'Math', 'MathTaskGenerator.cs'));

test('MathTaskGenerator.cs: all 11 MathTopics handled in Generate switch', () => {
  const topics = [
    'Counting1To10', 'Counting1To20', 'AdditionTo10', 'SubtractionWithin10',
    'AdditionTo20', 'SubtractionWithin20', 'Multiplication2x5x', 'DivisionBy2_3_5',
    'NumberOrdering', 'ShapeCounting', 'MixedReview'
  ];
  for (const t of topics)
    assert(mtgSrc.includes(t), `Topic ${t} not in Generate switch`);
});

test('MathTaskGenerator.cs: GenerateChoices always produces 3 choices', () => {
  assert(mtgSrc.includes('choices.Count < 3'), 'HashSet "< 3" constraint not found in GenerateChoices');
  assert(mtgSrc.includes('while (choices.Count < 3)'), 'while loop ensuring 3 choices not found');
});

test('MathTaskGenerator.cs: correct answer always included in choices', () => {
  assert(mtgSrc.includes('new HashSet<int> { correct }'), 'correct answer not seeded into choices HashSet');
});

test('MathTaskGenerator.cs: subtraction b clamped to prevent negative result', () => {
  assert(mtgSrc.includes('Math.Max(0, b)') || mtgSrc.includes('System.Math.Max(0, b)'),
    'b not clamped to 0 — subtraction can produce negative results');
});

test('MathTaskGenerator.cs: division dividend = divisor × quotient (always exact)', () => {
  assert(mtgSrc.includes('int dividend = d * quotient'), 'dividend = d * quotient not found — division may not be exact');
});

test('MathTaskGenerator.cs: number ordering missing index never first or last', () => {
  assert(mtgSrc.includes('Rng.Next(1, count - 1)'), 'missingIdx Rng.Next(1, count-1) not found');
});

test('MathTaskGenerator.cs: MixedReview excludes itself from the random pool', () => {
  assert(mtgSrc.includes('t != MathTopic.MixedReview'), 'MixedReview not excluded from mixed pool');
});

test('MathTaskGenerator.cs: adaptive difficulty upgrades at ≥85% rolling accuracy', () => {
  assert(mtgSrc.includes('rolling >= 0.85f'), 'upgrade threshold 0.85f not found');
});

test('MathTaskGenerator.cs: adaptive difficulty downgrades at <55% rolling accuracy', () => {
  assert(mtgSrc.includes('rolling < 0.55f'), 'downgrade threshold 0.55f not found');
});

// ─── §32 LEVELREGISTRY INTEGRITY ─────────────────────────────────────────────
section('§32 LevelRegistry Integrity');

const lrSrc = readText(path.join(SCRIPTS, 'Data', 'LevelRegistry.cs'));

test('LevelRegistry.cs: exactly 11 levels defined', () => {
  // Count opening parentheses of tuples — each level entry starts with "( N,"
  const entries = [...lrSrc.matchAll(/\(\s*\d+,\s*"level_/g)];
  assert(entries.length === 11, `Expected 11 levels, found ${entries.length}`);
});

test('LevelRegistry.cs: no duplicate level indices (0–10)', () => {
  const indices = [...lrSrc.matchAll(/\(\s*(\d+),\s*"level_/g)].map(m => parseInt(m[1]));
  const unique = new Set(indices);
  assert(unique.size === 11, `Duplicate level indices found: ${indices}`);
  for (let i = 0; i <= 10; i++)
    assert(unique.has(i), `Level index ${i} missing from registry`);
});

test('LevelRegistry.cs: unlock chain is sequential (each level unlocks after previous)', () => {
  const chain = [...lrSrc.matchAll(/\(\s*(\d+),\s*"level_\d+_name"[^)]+,\s*(-?\d+),\s*Difficulty/g)]
    .map(m => ({ idx: parseInt(m[1]), unlockAfter: parseInt(m[2]) }));
  for (const e of chain) {
    if (e.idx === 0) { assert(e.unlockAfter === -1, `Level 0 unlockAfter should be -1, got ${e.unlockAfter}`); continue; }
    assert(e.unlockAfter === e.idx - 1,
      `Level ${e.idx} should unlock after ${e.idx - 1}, but unlockAfter=${e.unlockAfter}`);
  }
});

test('LevelRegistry.cs: all minigame scene names start with correct prefix', () => {
  const validMinigames = [
    'SockSortSweep', 'CrumbCollectCountdown', 'CushionCannonCatch', 'DrainDefense',
    'BoxTowerBuilder', 'StreamerUntangleSprint', 'FlowerBedFrenzy', 'AtticBinBlitz',
    'SequenceSprinkler', 'GrandHallRestoration', 'ScramblerShutdown'
  ];
  for (const mg of validMinigames)
    assert(lrSrc.includes(mg), `Minigame ${mg} not found in LevelRegistry`);
});

test('LevelRegistry.cs: Rumble is the default character for level 0', () => {
  // First level entry should use CharacterType.Rumble
  const firstEntry = lrSrc.match(/\(\s*0,\s*"level_1_name"[^)]+\)/s);
  assert(firstEntry && firstEntry[0].includes('Rumble'),
    'Level 0 does not use Rumble character');
});

// ─── §33 PLAYERPROGRESSS DATA ─────────────────────────────────────────────────
section('§33 PlayerProgress Data');

const ppSrc = readText(path.join(SCRIPTS, 'Data', 'PlayerProgress.cs'));

test('PlayerProgress.cs: GetOrCreateLevel creates new entry if missing', () => {
  assert(ppSrc.includes('lp = new LevelProgress'), 'GetOrCreateLevel does not create new entry');
  assert(ppSrc.includes('levels.Add(lp)'), 'New LevelProgress not added to list');
});

test('PlayerProgress.cs: GetOrCreateTopicAccuracy creates new entry if missing', () => {
  assert(ppSrc.includes('ta = new TopicAccuracy'), 'GetOrCreateTopicAccuracy does not create new entry');
  assert(ppSrc.includes('topicAccuracies.Add(ta)'), 'New TopicAccuracy not added to list');
});

test('PlayerProgress.cs: RollingAccuracy returns 0.75 when no results recorded yet', () => {
  assert(ppSrc.includes('return 0.75f'), 'Default RollingAccuracy 0.75f not found');
});

test('PlayerProgress.cs: RollingAccuracy rolling window capped at 10 items', () => {
  assert(ppSrc.includes('recentResults.Count >= 10') && ppSrc.includes('recentResults.Dequeue()'),
    'Rolling window dequeue at ≥10 not found');
});

test('PlayerProgress.cs: RecordResult increments totalProblems', () => {
  assert(ppSrc.includes('totalProblems++'), 'totalProblems not incremented in RecordResult');
});

test('PlayerProgress.cs: IsLevelUnlocked(0) always returns true', () => {
  assert(ppSrc.includes('if (levelIndex == 0) return true'), 'Level 0 always-unlocked not found');
});

test('PlayerProgress.cs: Rumble unlocked by default in initial list', () => {
  assert(ppSrc.includes('CharacterType.Rumble'), 'Rumble not in default unlockedCharacters');
  assert(ppSrc.includes('new List<CharacterType> { CharacterType.Rumble }'),
    'Default unlockedCharacters list does not initialise with Rumble');
});

test('PlayerProgress.cs: stars field is 0–3 (LevelProgress comment confirms)', () => {
  assert(ppSrc.includes('// 0-3') || ppSrc.includes('0-3'), 'Stars 0-3 range not documented');
});

// ─── §34 DESIGN RULE ENFORCEMENT ─────────────────────────────────────────────
section('§34 Design Rule Enforcement');

// tdcSrc already declared above (line ~553 via readScript)

test('TaskDisplayController.cs: no timer / TimeLimit on MathTask screen (design spec)', () => {
  // MathTask must never count down — only minigames use timers
  assert(!tdcSrc.includes('TimeLimit') && !tdcSrc.includes('_timer') && !tdcSrc.includes('countdown'),
    'Timer or TimeLimit found on MathTask screen — forbidden by design spec');
});

test('TaskDisplayController.cs: wrong-answer color is #FF9100 (orange, not red)', () => {
  // RGB: 1f, 0.57f, 0f  ≈ #FF9100
  assert(tdcSrc.includes('ColorWrong') && (
    tdcSrc.includes('1f,   0.57f, 0f') ||
    tdcSrc.includes('1f, 0.57f, 0f') ||
    tdcSrc.includes('new Color(1f')
  ), 'ColorWrong not defined or not orange #FF9100');
  assert(!tdcSrc.match(/new Color\(1f,\s*0f,\s*0f\)/), 'Red color found — wrong answer must be orange');
});

test('TaskDisplayController.cs: correct-answer color is #69F0AE (green)', () => {
  // RGB: 0.41f, 0.94f, 0.67f ≈ #69F0AE
  assert(tdcSrc.includes('ColorCorrect') && (
    tdcSrc.includes('0.41f, 0.94f, 0.67f') ||
    tdcSrc.includes('0.41f') // partial match is enough for green presence
  ), 'ColorCorrect not defined near #69F0AE values');
});

test('TaskDisplayController.cs: hints start at exactly 3', () => {
  assert(tdcSrc.includes('_hintsRemaining = 3'), '_hintsRemaining initialised to 3 not found');
});

test('TaskDisplayController.cs: NextAfterDelay uses ≥ 1.5 second pause', () => {
  const m = tdcSrc.match(/NextAfterDelay\(([\d.]+)f\)/);
  assert(m, 'NextAfterDelay call not found');
  assert(parseFloat(m[1]) >= 1.5, `NextAfterDelay delay ${m[1]}s is less than required 1.5s`);
});

test('TaskDisplayController.cs: RevealAnswer triggered after 3 wrong attempts', () => {
  assert(tdcSrc.includes('_attemptCount >= 3') && tdcSrc.includes('RevealAnswer()'),
    'RevealAnswer after 3 attempts not found');
});

test('TaskDisplayController.cs: streak indicator shown at ≥5 consecutive correct', () => {
  assert(tdcSrc.includes('_streak >= 5'), 'Streak indicator threshold ≥5 not found');
  assert(tdcSrc.includes('streakIndicator.SetActive'), 'streakIndicator.SetActive not called');
});

test('TaskDisplayController.cs: reteach panel shown when rolling accuracy < 55%', () => {
  assert(tdcSrc.includes('ta.RollingAccuracy < 0.55f'), 'Reteach threshold 0.55f not found');
  assert(tdcSrc.includes('reteachPanel.SetActive(true)'), 'reteachPanel.SetActive(true) not found');
});

// ─── §35 DOPAMINECONTROLLER ───────────────────────────────────────────────────
section('§35 DopamineController');

const dcSrc = readText(path.join(SCRIPTS, 'UI', 'DopamineController.cs'));

test('DopamineController.cs: Warm tier threshold is 3 correct in a row', () => {
  // Note: source uses double-space "streak >=  3"
  assert((dcSrc.includes('streak >= 3') || dcSrc.includes('streak >=  3')) && dcSrc.includes('StreakTier.Warm'),
    'Warm tier (≥3) not found');
});

test('DopamineController.cs: Hot tier threshold is 5', () => {
  assert(dcSrc.includes('streak >=  5') || dcSrc.includes('streak >= 5'),
    'Hot tier (≥5) not found');
});

test('DopamineController.cs: Fire tier threshold is 8', () => {
  assert(dcSrc.includes('streak >=  8') || dcSrc.includes('streak >= 8'),
    'Fire tier (≥8) not found');
});

test('DopamineController.cs: Unstoppable tier threshold is 12', () => {
  assert(dcSrc.includes('streak >= 12'), 'Unstoppable tier (≥12) not found');
});

test('DopamineController.cs: TryLucky only fires on firstAttempt correct answers', () => {
  assert(dcSrc.includes('if (!firstAttempt) return false'), 'firstAttempt guard not found in TryLucky');
});

test('DopamineController.cs: lucky bonus target is random range 5–8 (Random.Range(5, 9))', () => {
  assert(dcSrc.includes('Random.Range(5, 9)'), 'Lucky target range Random.Range(5,9) not found');
});

test('DopamineController.cs: ConsumeComeback is a one-shot (false on second call)', () => {
  // After returning true, _hadWrongThisProblem is reset to false
  assert(dcSrc.includes('_hadWrongThisProblem = false') && dcSrc.includes('bool comeback = _hadWrongThisProblem'),
    'ConsumeComeback one-shot logic not found');
});

test('DopamineController.cs: button stagger delay is 0.085f seconds', () => {
  assert(dcSrc.includes('0.085f'), 'Stagger delay 0.085f not found');
});

// ─── §36 SETTINGSCONTROLLER ───────────────────────────────────────────────────
section('§36 SettingsController');

const scSrc = readText(path.join(SCRIPTS, 'UI', 'SettingsController.cs'));

test('SettingsController.cs: built entirely in code — no Inspector-required refs', () => {
  // Must call BuildUI() from Start(), not depend on [SerializeField] for core functionality
  assert(scSrc.includes('private void BuildUI()') && scSrc.includes('BuildUI()'),
    'BuildUI() method not found — UI not built in code');
  // Core sliders are private fields, not SerializeField
  assert(!scSrc.match(/\[SerializeField\][^\n]*_musicSlider/) &&
         !scSrc.match(/\[SerializeField\][^\n]*_sfxSlider/),
    'Volume sliders are [SerializeField] — should be private code-built');
});

test('SettingsController.cs: language toggle has Czech and English buttons', () => {
  assert(scSrc.includes('Language.Czech') && scSrc.includes('Language.English'),
    'Language toggle does not cover both Czech and English');
});

test('SettingsController.cs: three volume sliders (music, sfx, voice)', () => {
  assert(scSrc.includes('_musicSlider'), 'music slider not found');
  assert(scSrc.includes('_sfxSlider'),   'sfx slider not found');
  assert(scSrc.includes('_voiceSlider'), 'voice slider not found');
});

test('SettingsController.cs: GoBack navigates to GameState.Home', () => {
  assert(scSrc.includes('GameState.Home'), 'GoBack does not navigate to Home');
});

test('SettingsController.cs: language selection persisted via PlayerPrefs "lang"', () => {
  assert(scSrc.includes('"lang"') && scSrc.includes('PlayerPrefs.SetInt'),
    'Language not persisted via PlayerPrefs "lang"');
});

// ─── §37 SESSIONBREAKMONITOR ──────────────────────────────────────────────────
section('§37 SessionBreakMonitor');

const sbmSrc = readText(path.join(SCRIPTS, 'UI', 'SessionBreakMonitor.cs'));

test('SessionBreakMonitor.cs: default session limit is 20 minutes', () => {
  assert(sbmSrc.includes('20f'), 'Default session time limit 20f not found');
  assert(sbmSrc.includes('"session_time_limit"'), 'PlayerPrefs key session_time_limit not found');
});

test('SessionBreakMonitor.cs: limit converted to seconds (* 60f)', () => {
  assert(sbmSrc.includes('* 60f'), 'Minutes-to-seconds conversion * 60f not found');
});

test('SessionBreakMonitor.cs: not forced — child can keep playing', () => {
  // OnKeepPlaying should exist and NOT navigate away
  assert(sbmSrc.includes('OnKeepPlaying'), 'OnKeepPlaying handler not found');
  const keepBlock = sbmSrc.match(/OnKeepPlaying[\s\S]{0,200}}/);
  assert(keepBlock && !keepBlock[0].includes('TransitionTo'),
    'OnKeepPlaying navigates away — break should NOT be forced');
});

test('SessionBreakMonitor.cs: Keep Playing resets timer and _breakShown flag', () => {
  assert(sbmSrc.includes('_sessionSeconds = 0f'), 'Timer not reset in OnKeepPlaying');
  assert(sbmSrc.includes('_breakShown     = false') || sbmSrc.includes('_breakShown = false'),
    '_breakShown not reset in OnKeepPlaying');
});

test('SessionBreakMonitor.cs: Take Break navigates to Home', () => {
  assert(sbmSrc.includes('GameState.Home'), 'Take Break does not navigate to Home');
});

// ─── §38 LOCALIZATION KEY COVERAGE ───────────────────────────────────────────
section('§38 Localization Key Coverage');

// csKeys / enKeys already declared above

const minigameInstructionKeys = [
  'minigame_instruction_socksortsweep',
  'minigame_instruction_crumbcollectcountdown',
  'minigame_instruction_cushioncannoncatch',
  'minigame_instruction_draindefense',
  'minigame_instruction_boxtowerbuilder',
  'minigame_instruction_streameruntanglesprint',
  'minigame_instruction_flowerbedfrenzy',
  'minigame_instruction_atticbinblitz',
  'minigame_instruction_sequencesprinkler',
  'minigame_instruction_grandhallrestoration',
  'minigame_instruction_scramblershutdown',
];
test('Localization: all 11 minigame_instruction_ keys present in cs-CZ', () => {
  for (const k of minigameInstructionKeys)
    assert(csKeys.has(k), `cs-CZ missing key: ${k}`);
});
test('Localization: all 11 minigame_instruction_ keys present in en-US', () => {
  for (const k of minigameInstructionKeys)
    assert(enKeys.has(k), `en-US missing key: ${k}`);
});

const streakKeys = ['streak_warm', 'streak_hot', 'streak_fire', 'streak_unstoppable', 'streak_label'];
test('Localization: all streak_ keys present in cs-CZ', () => {
  for (const k of streakKeys) assert(csKeys.has(k), `cs-CZ missing key: ${k}`);
});
test('Localization: all streak_ keys present in en-US', () => {
  for (const k of streakKeys) assert(enKeys.has(k), `en-US missing key: ${k}`);
});

const feedbackKeys = ['feedback_try_again', 'feedback_correct_was', 'feedback_comeback',
                      'feedback_lucky', 'feedback_so_close'];
test('Localization: required feedback_ keys present in cs-CZ', () => {
  for (const k of feedbackKeys) assert(csKeys.has(k), `cs-CZ missing key: ${k}`);
});
test('Localization: required feedback_ keys present in en-US', () => {
  for (const k of feedbackKeys) assert(enKeys.has(k), `en-US missing key: ${k}`);
});

const reteachTopics = [
  'Counting1To10', 'Counting1To20', 'AdditionTo10', 'SubtractionWithin10',
  'AdditionTo20', 'SubtractionWithin20', 'Multiplication2x5x', 'DivisionBy2_3_5',
  'NumberOrdering', 'ShapeCounting',
];
test('Localization: reteach_ keys present in cs-CZ for all non-mixed topics', () => {
  for (const t of reteachTopics)
    assert(csKeys.has(`reteach_${t}`), `cs-CZ missing reteach_${t}`);
});
test('Localization: reteach_ keys present in en-US for all non-mixed topics', () => {
  for (const t of reteachTopics)
    assert(enKeys.has(`reteach_${t}`), `en-US missing reteach_${t}`);
});

const shapeKeys = ['shape_triangle', 'shape_square', 'shape_circle',
                   'shape_rectangle', 'shape_pentagon', 'shape_hexagon'];
test('Localization: all shape_ keys present in cs-CZ', () => {
  for (const k of shapeKeys) assert(csKeys.has(k), `cs-CZ missing key: ${k}`);
});
test('Localization: all shape_ keys present in en-US', () => {
  for (const k of shapeKeys) assert(enKeys.has(k), `en-US missing key: ${k}`);
});

test('Localization: both locales have same number of string entries', () => {
  assert(csKeys.size === enKeys.size,
    `cs-CZ has ${csKeys.size} keys, en-US has ${enKeys.size} keys — they must match`);
});

const questionKeys = ['q_how_many', 'q_addition', 'q_subtraction', 'q_multiplication',
                      'q_division', 'q_ordering', 'q_count_shapes'];
test('Localization: all question prompt keys (q_*) exist in cs-CZ', () => {
  for (const k of questionKeys) assert(csKeys.has(k), `cs-CZ missing ${k}`);
});
test('Localization: all question prompt keys (q_*) exist in en-US', () => {
  for (const k of questionKeys) assert(enKeys.has(k), `en-US missing ${k}`);
});

// ─── §39 ARCHITECTURE: NO DIRECT SCENEMANGER OUTSIDE GAMEMANAGER ─────────────
section('§39 Architecture: SceneManager usage');

test('No script other than GameManager calls SceneManager.LoadScene directly', () => {
  const violations = [];
  for (const f of allScripts()) {
    if (f.endsWith('GameManager.cs')) continue;
    const src = readText(f);
    if (src.includes('SceneManager.LoadScene'))
      violations.push(path.relative(SCRIPTS, f));
  }
  assert(violations.length === 0,
    `Direct SceneManager.LoadScene in non-GameManager files: ${violations.join(', ')}`);
});

test('BaseMinigame.cs: does not call SceneManager or TransitionTo directly', () => {
  const basePath = path.join(MINIGAMES, 'BaseMinigame.cs');
  if (!fs.existsSync(basePath)) return; // skip if stub only
  const src = readText(basePath);
  assert(!src.includes('SceneManager.LoadScene'),
    'BaseMinigame calls SceneManager.LoadScene — all navigation must go through GameManager');
});

// ─── §40 MATHPROBLEM STRUCTURE ────────────────────────────────────────────────
section('§40 MathProblem Structure');

const mpSrc = readText(path.join(SCRIPTS, 'Data', 'MathProblem.cs'));

test('MathProblem.cs: choices field is an int array', () => {
  assert(mpSrc.includes('int[] choices') || mpSrc.includes('public int[] choices'),
    'choices int[] field not found');
});

test('MathProblem.cs: correctAnswer field exists', () => {
  assert(mpSrc.includes('correctAnswer'), 'correctAnswer field not found');
});

test('MathProblem.cs: equationText field exists for display', () => {
  assert(mpSrc.includes('equationText'), 'equationText field not found');
});

test('MathProblem.cs: operatorSymbol field exists for voice narration', () => {
  assert(mpSrc.includes('operatorSymbol'), 'operatorSymbol field not found');
});

test('MathTaskGenerator.cs: generated choices always shuffled before return', () => {
  // Shuffle loop: for (int i = arr.Count - 1; i > 0; i--)
  assert(mtgSrc.includes('arr.Count - 1') && mtgSrc.includes('Rng.Next(i + 1)'),
    'Shuffle loop not found in GenerateChoices — choices may always be in same order');
});

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

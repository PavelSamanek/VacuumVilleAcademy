#!/usr/bin/env node
/**
 * VacuumVilleAcademy — test watcher
 *
 * Re-runs validate_project.js whenever source files change.
 * Watches: Assets/Scripts/**\/*.cs, Assets/Resources/Localization/**\/*.json
 *
 * Usage:  node tests/watch.js
 */
'use strict';
const fs    = require('fs');
const path  = require('path');
const { execSync } = require('child_process');

const ROOT    = path.resolve(__dirname, '..');
const SCRIPTS = path.join(ROOT, 'Assets', 'Scripts');
const LOC     = path.join(ROOT, 'Assets', 'Resources', 'Localization');
const VALIDATOR = path.join(__dirname, 'validate_project.js');

let debounce = null;

function runTests(changedFile) {
  if (changedFile)
    process.stdout.write(`\n\x1b[36m↻  Change detected: ${path.relative(ROOT, changedFile)}\x1b[0m\n`);
  else
    process.stdout.write(`\n\x1b[36m↻  Running tests…\x1b[0m\n`);

  try {
    const output = execSync(`node "${VALIDATOR}"`, { encoding: 'utf8' });
    process.stdout.write(output);
  } catch (e) {
    // execSync throws on non-zero exit — still print the output
    process.stdout.write((e.stdout || '') + (e.stderr || ''));
  }
}

function scheduleRun(changedFile) {
  clearTimeout(debounce);
  debounce = setTimeout(() => runTests(changedFile), 200);
}

function watchDir(dir, extensions) {
  if (!fs.existsSync(dir)) return;

  fs.watch(dir, { recursive: true }, (event, filename) => {
    if (!filename) return;
    const ext = path.extname(filename);
    if (extensions.includes(ext))
      scheduleRun(path.join(dir, filename));
  });
}

watchDir(SCRIPTS, ['.cs']);
watchDir(LOC,     ['.json']);

process.stdout.write(`\x1b[1mVacuumVilleAcademy test watcher\x1b[0m\n`);
process.stdout.write(`Watching: Assets/Scripts/**/*.cs  Assets/Resources/Localization/**/*.json\n`);
process.stdout.write(`Press Ctrl+C to stop.\n`);

// Run once at startup
runTests(null);

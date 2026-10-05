#!/usr/bin/env node
// Dependency freshness guard (user request 2026-09-09): a git pull that changes
// ide/package.json dependencies must not require a manual `npm install` in ide/.
// Same incident family as the stale xsc binary (D69): node_modules already existed, the
// launcher's "install only if node_modules is missing" check skipped the new @xterm/node-pty
// dependencies, and the console IDE failed at startup ("xterm.js failed to load").
// Mechanism: a marker file inside node_modules stores a short hash of the dependency sections
// of package.json. A missing marker, a missing node_modules, or a hash mismatch triggers
// `npm install`; the marker is written only after a successful install, so a failed install
// retries on the next launch instead of being masked. Version-only bumps do NOT trigger a
// reinstall (only the dependency sections are hashed). Plain Node only (fs/path/crypto).
const { spawnSync } = require('child_process');
const crypto = require('crypto');
const path = require('path');
const fs = require('fs');

const MARKER_NAME = '.deps-signature';

// Short hash over ONLY the dependency sections: deps changes force a reinstall, version or
// script edits do not (an IDE version bump accompanies most stages — reinstalling then would
// slow every launch for no reason).
function depsSignature(pkg) {
  const { dependencies = {}, devDependencies = {} } = pkg || {};
  return crypto.createHash('sha256')
    .update(JSON.stringify({ dependencies, devDependencies }))
    .digest('hex')
    .slice(0, 16);
}

function markerPath(ideRoot) {
  return path.join(ideRoot, 'node_modules', MARKER_NAME);
}

function readMarker(ideRoot) {
  try {
    return fs.readFileSync(markerPath(ideRoot), 'utf8').trim() || null;
  } catch {
    return null;
  }
}

function writeMarker(ideRoot, signature) {
  try {
    fs.writeFileSync(markerPath(ideRoot), signature + '\n', 'utf-8');
    return true;
  } catch {
    return false;
  }
}

// Ensures node_modules matches package.json. Returns { status, signature, installed }:
// status 'fresh' (marker matches, nothing done), 'installed' (npm install ran and the marker
// was written), 'install-failed' (npm install failed — marker left untouched so the next
// launch retries), 'no-package' (no readable package.json — nothing sensible to do).
// opts.runInstall / opts.installedSignature are injection points for the sandbox spec.
function ensureDeps(ideRoot, opts = {}) {
  let pkg = null;
  try {
    pkg = JSON.parse(fs.readFileSync(path.join(ideRoot, 'package.json'), 'utf8'));
  } catch {
    return { status: 'no-package', signature: null, installed: false };
  }
  const signature = depsSignature(pkg);
  const marker = opts.installedSignature ? opts.installedSignature() : readMarker(ideRoot);
  const nodeModulesExists = fs.existsSync(path.join(ideRoot, 'node_modules'));
  if (nodeModulesExists && marker === signature) {
    return { status: 'fresh', signature, installed: false };
  }

  let installOk = false;
  if (opts.runInstall) {
    installOk = opts.runInstall() === true;
  } else {
    // npm is npm.cmd on Windows — shell:true resolves it; output streams to the launcher
    // console so a failing install is visible in the run-ide.bat window.
    const r = spawnSync('npm', ['install'], {
      cwd: ideRoot,
      stdio: 'inherit',
      shell: process.platform === 'win32',
      timeout: opts.installTimeoutMs || 600000,
    });
    installOk = r.status === 0;
  }
  if (installOk) {
    // A failed marker write degrades to a reinstall on the next launch — npm install is
    // idempotent, so that is safe.
    writeMarker(ideRoot, signature);
    return { status: 'installed', signature, installed: true };
  }
  return { status: 'install-failed', signature, installed: false };
}

// Launcher entry: prints what it does; exit 0 when dependencies are usable, 1 otherwise.
function main() {
  const ideRoot = path.join(__dirname, '..');
  const r = ensureDeps(ideRoot);
  if (r.status === 'fresh') {
    console.log('npm dependencies: up to date');
    process.exit(0);
  }
  if (r.status === 'installed') {
    console.log('npm dependencies: installed/updated (package.json changed or first run)');
    process.exit(0);
  }
  if (r.status === 'no-package') {
    console.error('ide/package.json is missing or unreadable — cannot check dependencies');
    process.exit(1);
  }
  console.error('npm install FAILED. Close any running instance of the IDE and try again.');
  process.exit(1);
}

if (require.main === module) main();

module.exports = { depsSignature, readMarker, writeMarker, ensureDeps, MARKER_NAME };

// D89: the Open dialog must start in the folder the user last visited (open or save-as),
// not in the shell default (Downloads on Windows). Pure helpers so the logic is testable
// without Electron (same style as scripts/ensure-deps.js + deps-freshness.spec.js).
const path = require('path');

// A stale persisted dir (deleted/renamed since) must not break the dialog: it is dropped
// and the dialog falls back to the system default. `exists` is injected for testing.
function resolveDefaultDir(lastDir, exists) {
  if (typeof lastDir !== 'string' || !lastDir) return undefined;
  try {
    if (!exists(lastDir)) return undefined;
  } catch {
    return undefined;
  }
  return lastDir;
}

// Folder to remember after a successful pick: the picked file's own directory.
function dirOfPickedFile(pickedPath) {
  if (typeof pickedPath !== 'string' || !pickedPath) return null;
  try {
    return path.dirname(pickedPath) || null;
  } catch {
    return null;
  }
}

module.exports = { resolveDefaultDir, dirOfPickedFile };

const fs = require('node:fs');
const path = require('node:path');
const { execFile } = require('node:child_process');
const { promisify } = require('node:util');

const execFileAsync = promisify(execFile);

// Node's Dirent does not expose Windows Hidden/System attributes. Enumerate on
// Windows through .NET so those attributes can be checked before descending.
const WINDOWS_ENUMERATOR = String.raw`
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$root = $env:AXIOM_PROJECT_ROOT
$pending = [System.Collections.Generic.Stack[string]]::new()
$files = [System.Collections.Generic.List[string]]::new()
$pending.Push($root)
while ($pending.Count -gt 0) {
  $directory = $pending.Pop()
  try {
    foreach ($entry in [System.IO.Directory]::EnumerateFileSystemEntries($directory)) {
      try {
        $attributes = [System.IO.File]::GetAttributes($entry)
        if (($attributes -band [System.IO.FileAttributes]::Hidden) -ne 0 -or
            ($attributes -band [System.IO.FileAttributes]::System) -ne 0) { continue }
        if (($attributes -band [System.IO.FileAttributes]::Directory) -ne 0) {
          if (($attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { continue }
          $name = [System.IO.Path]::GetFileName($entry)
          if ($name -eq 'node_modules' -or $name -eq '.git' -or $name -eq 'bin' -or $name -eq 'obj') { continue }
          $pending.Push($entry)
        } elseif (($attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0 -and
                  [System.IO.Path]::GetExtension($entry) -ieq '.axm') {
          $files.Add($entry)
        }
      } catch {
        # A disappearing or inaccessible entry should not hide its siblings.
      }
    }
  } catch {
    # An inaccessible directory should not hide files from other branches.
  }
}
$json = ConvertTo-Json -InputObject ([object[]]$files.ToArray()) -Compress
[Console]::Out.Write($json)
`;

function listWithWindowsAttributes(projectDir) {
  const encodedScript = Buffer.from(WINDOWS_ENUMERATOR, 'utf16le').toString('base64');
  const env = { ...process.env, AXIOM_PROJECT_ROOT: path.resolve(projectDir) };
  return execFileAsync('powershell.exe', [
    '-NoLogo', '-NoProfile', '-NonInteractive', '-EncodedCommand', encodedScript,
  ], {
    encoding: 'utf8',
    windowsHide: true,
    maxBuffer: 64 * 1024 * 1024,
    timeout: 60000,
    env,
  }).then(({ stdout }) => {
    const parsed = JSON.parse(stdout.trim() || '[]');
    return (Array.isArray(parsed) ? parsed : [parsed])
      .filter(file => typeof file === 'string' && /\.axm$/i.test(file));
  });
}

function listWithoutWindowsAttributes(projectDir) {
  const result = [];
  const visit = dir => {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (entry.name.startsWith('.') || ['node_modules', '.git', 'bin', 'obj'].includes(entry.name)) continue;
        visit(full);
      } else if (entry.isFile() && !entry.name.startsWith('.') && /\.axm$/i.test(entry.name)) {
        result.push(full);
      }
    }
  };
  visit(projectDir);
  return result;
}

async function listProjectFiles(projectDir) {
  if (typeof projectDir !== 'string' || !projectDir) return [];
  try {
    const files = process.platform === 'win32'
      ? await listWithWindowsAttributes(projectDir)
      : listWithoutWindowsAttributes(projectDir);
    return files.sort((a, b) => a.localeCompare(b));
  } catch {
    return [];
  }
}

module.exports = { listProjectFiles };

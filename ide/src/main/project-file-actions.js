const fs = require('node:fs');
const path = require('node:path');
const { execFile } = require('node:child_process');

const transferScript = String.raw`
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$operation = $env:AXIOM_PROJECT_ACTION | ConvertFrom-Json
function Copy-ProjectDirectory([string]$sourcePath, [string]$targetPath) {
  foreach ($entryPath in [System.IO.Directory]::EnumerateFileSystemEntries($sourcePath)) {
    $entryAttrs = [System.IO.File]::GetAttributes($entryPath)
    if (($entryAttrs -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Копирование ссылок на другие пути не поддерживается.' }
    $entryTarget = [System.IO.Path]::Combine($targetPath, [System.IO.Path]::GetFileName($entryPath))
    if (($entryAttrs -band [System.IO.FileAttributes]::Directory) -ne 0) {
      if ([System.IO.Directory]::Exists($entryTarget) -or [System.IO.File]::Exists($entryTarget)) { throw 'Назначение уже существует.' }
      [void][System.IO.Directory]::CreateDirectory($entryTarget)
      Copy-ProjectDirectory $entryPath $entryTarget
    } else { [System.IO.File]::Copy($entryPath, $entryTarget, $false) }
  }
}
try {
  if ($operation.cut) {
    if ($operation.directory) { [System.IO.Directory]::Move($operation.source, $operation.target) }
    else { [System.IO.File]::Move($operation.source, $operation.target) }
  } elseif ($operation.directory) { Copy-ProjectDirectory $operation.source $operation.target }
  else { [System.IO.File]::Copy($operation.source, $operation.target, $false) }
  [Console]::Out.Write('{"ok":true}')
} catch {
  [Console]::Out.Write((ConvertTo-Json -Compress @{ok=$false;error=$_.Exception.Message}))
  exit 1
}
`;
function transfer(source, target, cut, directory) {
  if (directory && !cut) {
    try { fs.mkdirSync(target); }
    catch (error) { return Promise.resolve({ ok: false, error: error.message }); }
  }
  return new Promise(resolve => execFile('powershell.exe', ['-NoLogo', '-NoProfile', '-NonInteractive', '-EncodedCommand', Buffer.from(transferScript, 'utf16le').toString('base64')], {
    windowsHide: true, encoding: 'utf8', timeout: 60000,
    env: { ...process.env, AXIOM_PROJECT_ACTION: JSON.stringify({ source, target, cut, directory }) },
  }, (error, stdout, stderr) => {
    try { resolve(JSON.parse(stdout.replace(/^\uFEFF/, ''))); }
    catch { resolve({ ok: false, error: error?.message || stderr || 'Не удалось выполнить файловую операцию.' }); }
  }));
}
const same = (left, right) => process.platform === 'win32' ? left.toLowerCase() === right.toLowerCase() : left === right;
const within = (parent, child) => { const relative = path.relative(parent, child); return relative !== '..' && !relative.startsWith('..' + path.sep) && !path.isAbsolute(relative); };

class ProjectFileActions {
  constructor(options = {}) {
    this.clipboard = null; this.pending = false; this.transfer = options.transfer || transfer;
    this.trashItem = options.trash || (target => require('electron').shell.trashItem(target));
  }
  existing(root, item, allowRoot = false) {
    if (typeof root !== 'string' || typeof item !== 'string' || !path.isAbsolute(root) || !path.isAbsolute(item)) throw new Error('Нужен полный путь проекта и элемента.');
    const project = fs.realpathSync(root), original = path.resolve(item);
    if (!fs.statSync(project).isDirectory()) throw new Error('Папка проекта недоступна.');
    if (fs.lstatSync(original).isSymbolicLink()) throw new Error('Действия со ссылками не поддерживаются.');
    const canonical = fs.realpathSync(original);
    if (!within(project, canonical) || (!allowRoot && same(project, canonical))) throw new Error('Элемент должен находиться внутри папки проекта.');
    return canonical;
  }
  capture(root, item, cut) {
    if (this.pending) throw new Error('Дождитесь завершения файловой операции.');
    if (typeof cut !== 'boolean') throw new Error('Неверный вид файловой операции.');
    const source = this.existing(root, item);
    this.clipboard = { root: fs.realpathSync(root), source, cut, directory: fs.statSync(source).isDirectory() };
    return { ok: true, ...this.clipboard };
  }
  async paste(root, directory) {
    if (this.pending) return { ok: false, error: 'Файловая операция уже выполняется.' };
    const clip = this.clipboard;
    if (!clip) return { ok: false, error: 'Нет файлов для вставки.' };
    this.pending = true;
    let target;
    try {
      if (!fs.existsSync(clip.source)) { this.clipboard = null; throw new Error('Исходный файл или папка больше не существует.'); }
      const source = this.existing(clip.root, clip.source), destination = this.existing(root, directory, true);
      if (!fs.statSync(destination).isDirectory()) throw new Error('Назначение должно быть папкой.');
      if (clip.directory && within(source, destination)) throw new Error('Нельзя вставить папку в себя или своего потомка.');
      target = path.join(destination, path.basename(source));
      if (fs.existsSync(target)) throw new Error('Назначение уже существует: ' + target);
      const result = await this.transfer(source, target, clip.cut, clip.directory);
      if (!result?.ok) return { ok: false, error: result?.error || 'Файловая операция не завершена.',
        partialTarget: fs.existsSync(target) ? target : null, sourceExists: fs.existsSync(source) };
      if (clip.cut && this.clipboard === clip) this.clipboard = null;
      return { ok: true, source, target, cut: clip.cut, directory: clip.directory };
    } catch (error) { return { ok: false, error: error.message }; }
    finally { this.pending = false; }
  }
  properties(root, item) {
    const fullPath = this.existing(root, item, true), stat = fs.statSync(fullPath);
    return { name: path.basename(fullPath), path: fullPath, kind: stat.isDirectory() ? 'directory' : 'file',
      size: stat.isDirectory() ? null : stat.size, modified: stat.mtime.toISOString(), created: stat.birthtime.toISOString(), readonly: !(stat.mode & 0o222) };
  }
  async trash(root, item) {
    if (this.pending) return { ok: false, error: 'Файловая операция уже выполняется.' };
    this.pending = true;
    try {
      const source = this.existing(root, item);
      await this.trashItem(source);
      if (fs.existsSync(source)) throw new Error('Файл или папка остались на месте после запроса корзины.');
      if (this.clipboard && within(source, this.clipboard.source)) this.clipboard = null;
      return { ok: true, source };
    } catch (error) { return { ok: false, error: error.message }; }
    finally { this.pending = false; }
  }
}
module.exports = { ProjectFileActions };

const { ipcMain, BrowserWindow } = require('electron');
const path = require('node:path');
const fs = require('node:fs');
const { pathToFileURL } = require('node:url');
const { randomUUID } = require('node:crypto');
const { BackendClient } = require('./backend-client');
const { sourceFingerprint } = require('./backend-fingerprint');
const { resolveHostPath } = require('./host-ensure');
const { CanvasWindow } = require('./canvas-window');
const { sameSourceText } = require('./file-service');
const { portableRuntime } = require('./portable-runtime');

let client;
let owner;
let sessionId;
let graphicsSessionId;
let closing;
let recovering;
const canvas = new CanvasWindow({
  acknowledge: (id, frameId) => id === graphicsSessionId ? getClient().request('graphics.ack', { frameId }, id) : Promise.resolve(),
  stop: id => id === sessionId ? getClient().request('execution.stop', {}, id) : Promise.resolve(),
  input: (id, data) => id === sessionId ? getClient().request('graphics.key', data, id) : Promise.resolve(),
  mouse: (id, data) => id === sessionId ? getClient().request('graphics.mouse', data, id) : Promise.resolve(),
});
const root = path.resolve(__dirname, '../../..');
const rendererUrl = pathToFileURL(path.join(root, 'ide/src/renderer/index.html')).href;
function checkSender(event) {
  const window = BrowserWindow.fromWebContents(event.sender);
  if (!window || window.isDestroyed() || event.senderFrame !== event.sender.mainFrame || event.senderFrame.url !== rendererUrl)
    throw new Error('Недопустимый отправитель команды IDE.');
  if (owner && owner !== event.sender && !owner.isDestroyed()) throw new Error('Служебный процесс принадлежит другому окну.');
  owner = event.sender;
}
function send(channel, payload) { if (owner && !owner.isDestroyed()) owner.send(channel, payload); }
function getClient() {
  if (client && !client.failure && !client.closed) return client;
  const executable = resolveHostPath(process.env, root).exe;
  client = new BackendClient({ executable, expectedFingerprint: portableRuntime(root)?.sourceFingerprint || sourceFingerprint(root) });
  client.on('event', message => {
    if (message.sessionId && message.sessionId !== sessionId) return;
    send('backend-event', message);
    if (message.event === 'execution.finished') sessionId = null;
  });
  client.on('frame', frame => {
    if (frame.header.sessionId !== graphicsSessionId) return;
    canvas.frame({ header: frame.header, pixels: frame.pixels });
  });
  client.on('fault', fault => { sessionId = null; graphicsSessionId = null; canvas.reset(); send('backend-fault', fault); });
  return client;
}
function setupBackendIpc() {
  const handle = (name, action) => ipcMain.handle(name, async (event, ...args) => {
    checkSender(event);
    try { await recovering; return { ok: true, payload: await action(...args) }; }
    catch (error) { return { ok: false, error: { code: error.code || 'HOST_DISCONNECTED', message: error.message } }; }
  });
  handle('backend-ready', () => getClient().ready());
  handle('backend-language-metadata', () => getClient().request('language.metadata'));
  handle('backend-document', (method, snapshot) => {
    if (!['document.compile', 'document.analyse', 'document.format', 'document.editTransition', 'document.normalizeIdentifiers'].includes(method) ||
        !snapshot || typeof snapshot.sourceText !== 'string' || typeof snapshot.documentId !== 'string' ||
        !Number.isInteger(snapshot.documentRevision) || snapshot.documentRevision < 0 || snapshot.documentRevision > 2147483647)
      throw new Error('Недопустимый запрос документа.');
    return getClient().request(method, snapshot);
  });
  handle('backend-start', async document => {
    if (sessionId) throw Object.assign(new Error('Программа ещё выполняется.'), { code: 'BUSY' });
    if (!document || typeof document.sourceFilePath !== 'string' || !path.isAbsolute(document.sourceFilePath) || typeof document.sourceText !== 'string' ||
        !Number.isSafeInteger(document.documentRevision) || document.documentRevision < 0 || typeof document.documentId !== 'string' ||
        !['run', 'debug'].includes(document.launchKind)) throw new Error('Некорректный снимок документа.');
    // Запуск должен соответствовать действительно сохранённому файлу.
    const saved = fs.readFileSync(document.sourceFilePath, 'utf8').replace(/^\uFEFF/, '');
    if (!sameSourceText(saved, document.sourceText)) throw new Error('Документ изменён после сохранения. Сохраните его перед запуском.');
    canvas.reset();
    sessionId = randomUUID();
    graphicsSessionId = sessionId;
    const current = sessionId;
    try { await getClient().request('execution.start', { ...document, resourceBaseDirectory: path.dirname(document.sourceFilePath) }, current); }
    catch (error) { if (sessionId === current) sessionId = null; throw error; }
    return { sessionId: current };
  });
  handle('backend-stop', async id => {
    if (!sessionId || id !== sessionId) throw new Error('Указанная сессия уже завершена.');
    return getClient().request('execution.stop', {}, id);
  });
  handle('backend-key', async (id, data) => {
    if (id !== sessionId || !data || !['down', 'up', 'clear'].includes(data.action) ||
        (data.action !== 'clear' && (typeof data.key !== 'string' || !data.key.length || data.key.length > 32)) ||
        (data.text !== undefined && (typeof data.text !== 'string' || data.text.length > 32)))
      throw new Error('Недопустимое событие клавиатуры программы.');
    return getClient().request('graphics.key', data, id);
  });
  handle('backend-input', async (id, requestId, text) => {
    if (typeof text !== 'string' || typeof requestId !== 'string' || id !== sessionId) throw new Error('Недопустимый ввод.');
    return getClient().request('input.submit', { inputRequestId: requestId, text }, id);
  });
  handle('backend-debug', async (id, command, stopId) => {
    if (id !== sessionId || typeof stopId !== 'string' || !['continue', 'stepOver', 'stepInto', 'stepOut', 'stepBack'].includes(command))
      throw new Error('Недопустимая команда отладки.');
    return getClient().request('debug.' + command, { stopId }, id);
  });
  handle('backend-frame-ack', async (id, frameId) => {
    if (id !== sessionId || typeof frameId !== 'string') throw new Error('Устаревший кадр.');
    return getClient().request('graphics.ack', { frameId }, id);
  });
  handle('backend-output-ack', async (id, outputVersion) => {
    if (id !== sessionId || typeof outputVersion !== 'string') throw new Error('Устаревший вывод.');
    return getClient().request('output.ack', { outputVersion }, id);
  });
}
function closeBackend() { graphicsSessionId = null; canvas.reset(); if (!closing) closing = client?.close() || Promise.resolve(); return closing; }
function recoverBackend() {
  if (recovering) return recovering;
  sessionId = null; graphicsSessionId = null; canvas.reset();
  const previous = client;
  // Поздние события прежнего процесса не должны менять новую сессию.
  previous?.removeAllListeners('event'); previous?.removeAllListeners('frame'); previous?.removeAllListeners('fault');
  recovering = (previous?.close() || Promise.resolve()).finally(() => {
    if (client === previous) client = null;
    recovering = null;
  });
  return recovering;
}
async function writeLockedFile(payload) { await recovering; return getClient().request('file.save', payload); }
module.exports = { setupBackendIpc, closeBackend, recoverBackend, writeLockedFile };

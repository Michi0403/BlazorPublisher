import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const source = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js'), 'utf8');

test('recording preview survives Blazor video element replacement', () => {
  assert.match(source, /recordingPreviewTimer: 0/);
  assert.match(source, /recordingPreviewElement: null/);
  assert.match(source, /function ensureRecordingPreview\(state\)/);
  assert.match(source, /preview\.srcObject !== state\.stream/);
  assert.match(source, /state\.recordingPreviewElement !== preview/);
  assert.match(source, /setInterval\([\s\S]*ensureRecordingPreview\(state\)[\s\S]*1000\)/);
});

test('recording cleanup stops watchdog and benign play interruptions are not reported', () => {
  assert.match(source, /function detachRecordingPreview\(state\)/);
  assert.ok((source.match(/detachRecordingPreview\(state\)/g) || []).length >= 4);
  assert.match(source, /name !== 'NotAllowedError'/);
  assert.match(source, /!isInterruptedPlaybackError\(error\)/);
  assert.doesNotMatch(source, /promise-catch@1400/);
});

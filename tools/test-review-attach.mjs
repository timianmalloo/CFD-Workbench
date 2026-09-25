import assert from 'node:assert/strict';
import { attachReview } from '../docs/coordination/review-attach.mjs';
import { verifyReceipt, verifyLiveLaunch } from './check-review-attach.mjs';
import { createHash } from 'node:crypto';
import { runInNewContext } from 'node:vm';

const request = { appPath: '/private/tmp/test/CFD Workbench.app',
  sessionReset: true,
  bundleId: 'com.cfdworkbench.test', expectedTitle: 'CFD Workbench · REVIEW test',
  launchReceiptSha256: 'a'.repeat(64) };
const state = `0 standard window ${request.expectedTitle}, Secondary Actions: Raise\n1 container ID: FoilViewport\n2 tab group ID: DocumentTabs`;
const actions = [];
const app = { getAXState: async () => state,
  performSecondaryAction: async (...args) => actions.push(args),
  getScreenshot: async () => new Uint8Array([1, 2, 3]) };

// Reproduce the historical failure at the supported adapter boundary. Process
// liveness cannot clear this negative; this fixture is not a platform RCA claim.
let calls = 0;
const failed = await attachReview({ getApp: async () => {
  calls++; throw new Error('Computer Use server error -10005: cgWindowNotFound');
} }, request);
assert.equal(calls, 3);
assert.equal(failed.receipt.status, 'blocked');
assert.equal(failed.app, null);
assert.ok(failed.receipt.attempts.every(a => a.error.includes('cgWindowNotFound')));
assert.ok(failed.receipt.next.includes('schedule-independent-work'));
assert.equal(failed.receipt.humanAction, false);

calls = 0;
const recovered = await attachReview({ getApp: async () => {
  if (++calls === 1) throw new Error('cgWindowNotFound');
  return app;
} }, request);
assert.equal(recovered.receipt.status, 'ready');
assert.equal(recovered.receipt.attempts.length, 2);
assert.deepEqual(actions, [[0, 'Raise']]);
assert.equal(recovered.receipt.attempts[1].target, request.bundleId);

for (const broken of [
  { ...app, getAXState: async () => state.replace(request.expectedTitle, 'Unrelated user window') },
  { ...app, getAXState: async () => state.replace('ID: FoilViewport', 'loading') },
  { ...app, getScreenshot: async () => new Uint8Array() }
]) {
  const result = await attachReview({ getApp: async () => broken }, request);
  assert.equal(result.receipt.status, 'blocked');
}
console.log('PASS: missing window, bounded retry, supported Raise, wrong window, incomplete surface, missing capture; blocked node does not request human focus');
const launch = { reviewApp: request.appPath, reviewBundleId: request.bundleId };
const launchBytes = JSON.stringify(launch);
const bound = { ...recovered.receipt,
  launchReceiptSha256: createHash('sha256').update(launchBytes).digest('hex') };
assert.equal(verifyReceipt(bound, launch, launchBytes).status, 'ready');
for (const invalid of [failed.receipt, { ...bound, humanAction: true },
  { ...bound, sessionReset: false },
  { ...bound, pendingOperation: true }, { ...bound, elapsedMs: 30001 },
  { ...bound, appPath: '/other.app' }, { ...bound, launchReceiptSha256: 'b'.repeat(64) },
  { exitCodeAfterFiveSeconds: null, pid: 42 }]) {
  assert.throws(() => verifyReceipt(invalid, launch, launchBytes), /NATIVE_REVIEW_BLOCKED/);
}
console.log('PASS: receipt gate refuses launch-only, failed, human-assisted, timeout, mismatched identity and stale evidence');
const identity = { pid: 42, start: 'Thu Sep 24 20:00:00 2026',
  command: request.appPath + '/Contents/MacOS/CfdWorkbench.Desktop' };
const liveLaunch = { ...launch, pid: 42, pidIdentity: identity, exitCodeAfterFiveSeconds: null };
assert.deepEqual(verifyLiveLaunch(liveLaunch, () => identity.start + '   ' + identity.command), identity);
assert.throws(() => verifyLiveLaunch({ ...liveLaunch, exitCodeAfterFiveSeconds: -6 }), /NOT_LIVE/);
assert.throws(() => verifyLiveLaunch(liveLaunch, () => 'different process'), /IDENTITY_CHANGED/);
console.log('PASS: exited launch and PID reuse refused before CUA auto-relaunch can mask them');
const crossRealm = await attachReview({ getApp: async () => ({ ...app,
  getScreenshot: async () => runInNewContext('new Uint8Array([255, 216, 255, 224])') }) }, request);
assert.equal(crossRealm.receipt.status, 'ready', 'CUA screenshot bytes can come from a different JS realm');
let raised = false;
const changedDuringRaise = await attachReview({ getApp: async () => ({ ...app,
  performSecondaryAction: async () => { raised = true; },
  getAXState: async () => raised ? state.replace(request.expectedTitle, 'Unrelated window') +
    '\n3 text ' + request.expectedTitle : state }) }, request);
assert.equal(changedDuringRaise.receipt.status, 'blocked');
const originalSetTimeout = globalThis.setTimeout;
const originalClearTimeout = globalThis.clearTimeout;
let timeoutCalls = 0;
try {
  globalThis.setTimeout = (callback) => { queueMicrotask(callback); return 1; };
  globalThis.clearTimeout = () => {};
  const timedOut = await attachReview({ getApp: () => { timeoutCalls++; return new Promise(() => {}); } }, request);
  assert.equal(timedOut.receipt.status, 'blocked');
  assert.equal(timedOut.receipt.pendingOperation, true);
  assert.equal(timeoutCalls, 1, 'An uncancellable timed-out operation must not overlap a retry');
  assert.match(timedOut.receipt.attempts[0].error, /ATTACH_DEADLINE/);
} finally {
  globalThis.setTimeout = originalSetTimeout;
  globalThis.clearTimeout = originalClearTimeout;
}
console.log('PASS: post-Raise window change and active uncancellable timeout fail closed');

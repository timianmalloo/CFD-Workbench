// Run this function in the supported cua_repl runtime; no alternate UI APIs.
// Its returned receipt is a gate, not evidence of scientific or product correctness.
export async function attachReview(cua, request) {
  const { appPath, bundleId, expectedTitle, launchReceiptSha256 } = request;
  if (request.sessionReset !== true || !appPath?.endsWith('.app') || !bundleId || !expectedTitle?.includes('REVIEW') ||
      !/^[a-f0-9]{64}$/.test(launchReceiptSha256 || '')) {
    throw new Error('Review attachment requires a fresh CUA session, exact app, title and launch receipt');
  }
  const started = Date.now();
  const receipt = { schema: 1, appPath, bundleId, expectedTitle, launchReceiptSha256,
    startedUtc: new Date(started).toISOString(), status: 'blocked', attempts: [],
    humanAction: false, sessionReset: true, deadlineMs: 30000, maxAttempts: 3 };
  let pendingTimeout = false;
  async function bounded(operation) {
    const remaining = 30000 - (Date.now() - started);
    if (remaining <= 0) throw new Error('ATTACH_DEADLINE');
    let timer;
    try {
      return await Promise.race([operation(), new Promise((_, reject) => {
        timer = setTimeout(() => { pendingTimeout = true; reject(new Error('ATTACH_DEADLINE')); }, remaining);
      })]);
    } finally { clearTimeout(timer); }
  }
  let app;
  function reviewWindow(state) {
    return state.split('\n').find(line => {
      const match = line.match(/^\d+ standard window (.*)$/);
      return match && (match[1] === expectedTitle ||
        match[1].startsWith(expectedTitle + ', Secondary Actions:') ||
        match[1].startsWith(expectedTitle + ', ID:'));
    });
  }
  for (let index = 0; index < 3 && Date.now() - started < 30000; index++) {
    const attempt = { number: index + 1, target: index === 1 ? bundleId : appPath };
    receipt.attempts.push(attempt);
    try {
      app = await bounded(() => cua.getApp(attempt.target));
      let state = await bounded(() => app.getAXState({ emit: false, disableDiffing: true }));
      let row = reviewWindow(state);
      if (!row) throw new Error('EXPECTED_REVIEW_WINDOW_ABSENT');
      attempt.window = row;
      // Raise only the exact window whose current AX state advertises the action.
      if (row.includes('Secondary Actions: Raise')) {
        await bounded(() => app.performSecondaryAction(Number(row.match(/^\d+/)[0]), 'Raise'));
        attempt.raised = true;
        state = await bounded(() => app.getAXState({ emit: false, disableDiffing: true }));
      }
      row = reviewWindow(state);
      if (!row || !state.includes('ID: FoilViewport') ||
          !state.includes('ID: DocumentTabs')) throw new Error('REVIEW_SURFACE_NOT_READY');
      attempt.window = row;
      const screenshot = await bounded(() => app.getScreenshot({ emit: false }));
      if (!ArrayBuffer.isView(screenshot) || Object.prototype.toString.call(screenshot) !== '[object Uint8Array]' || screenshot.byteLength === 0)
        throw new Error('REVIEW_SCREENSHOT_ABSENT');
      attempt.screenshotBytes = screenshot.byteLength;
      attempt.status = 'ready';
      receipt.status = 'ready';
      break;
    } catch (error) {
      attempt.status = 'failed';
      attempt.error = String(error);
      // A timed-out CUA operation cannot be cancelled by this API. Do not overlap
      // another UI operation; release only after the tool invocation has ended.
      if (pendingTimeout) break;
    }
  }
  receipt.elapsedMs = Date.now() - started;
  receipt.pendingOperation = pendingTimeout;
  receipt.next = receipt.status === 'ready' ? 'review' :
    'block-native-review-only; retain-error; schedule-independent-work; no-foreground-request';
  return { app: receipt.status === 'ready' ? app : null, receipt };
}

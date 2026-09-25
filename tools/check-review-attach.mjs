import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import { execFileSync } from 'node:child_process';

export function verifyLiveLaunch(launch, read = (pid) => execFileSync('/bin/ps',
  ['-p', String(pid), '-o', 'lstart=,command='], { encoding: 'utf8' })) {
  const identity = launch.pidIdentity;
  if (launch.exitCodeAfterFiveSeconds !== null || !Number.isInteger(launch.pid) || launch.pid <= 0 ||
      identity?.pid !== launch.pid || !identity.start ||
      identity.command !== launch.reviewApp + '/Contents/MacOS/CfdWorkbench.Desktop')
    throw new Error('NATIVE_LAUNCH_NOT_LIVE: launch identity absent or process exited');
  const live = read(launch.pid).trim();
  if (!live.startsWith(identity.start) || live.slice(identity.start.length).trim() !== identity.command)
    throw new Error('NATIVE_LAUNCH_IDENTITY_CHANGED');
  return identity;
}

export function verifyReceipt(receipt, launch, launchBytes) {
  const hash = createHash('sha256').update(launchBytes).digest('hex');
  const attempts = receipt.attempts;
  const last = attempts?.at(-1);
  if (receipt.schema !== 1 || receipt.status !== 'ready' || receipt.humanAction !== false || receipt.sessionReset !== true ||
      receipt.pendingOperation !== false || receipt.launchReceiptSha256 !== hash ||
      receipt.appPath !== launch.reviewApp || receipt.bundleId !== launch.reviewBundleId ||
      receipt.deadlineMs !== 30000 || receipt.maxAttempts !== 3 ||
      !Number.isFinite(receipt.elapsedMs) || receipt.elapsedMs < 0 || receipt.elapsedMs > 30000 ||
      !Array.isArray(attempts) || attempts.length < 1 || attempts.length > 3 ||
      attempts.some((a, i) => a.number !== i + 1 ||
        a.target !== (i === 1 ? receipt.bundleId : receipt.appPath)) ||
      last.status !== 'ready' || !Number.isInteger(last.screenshotBytes) || last.screenshotBytes <= 0 ||
      !last.window?.includes(receipt.expectedTitle) || !receipt.expectedTitle?.includes('REVIEW') ||
      receipt.next !== 'review') {
    throw new Error('NATIVE_REVIEW_BLOCKED: exact launch-bound AX/screenshot readiness is absent');
  }
  return { status: 'ready', appPath: receipt.appPath, attempts: attempts.length,
    elapsedMs: receipt.elapsedMs, launchReceiptSha256: hash };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    if (process.argv.length !== 4) throw new Error('Usage: node tools/check-review-attach.mjs ATTACH.json LAUNCH.json');
    const bytes = readFileSync(process.argv[3]);
    const launch = JSON.parse(bytes);
    const liveIdentity = verifyLiveLaunch(launch);
    if (process.argv[2] === '--live') console.log(JSON.stringify({ liveIdentity }));
    else console.log(JSON.stringify({ ...verifyReceipt(JSON.parse(readFileSync(process.argv[2], 'utf8')),
      launch, bytes), liveIdentity }));
  } catch (error) {
    console.error(String(error));
    process.exitCode = 1;
  }
}

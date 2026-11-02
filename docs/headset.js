export const PACKAGE_NAME = "com.Ace.Ace";
export const OBB_DIRECTORY = `/sdcard/Android/obb/${PACKAGE_NAME}`;

const MAIN_ACTIVITY = `${PACKAGE_NAME}/com.unity3d.player.UnityPlayerActivity`;
const FREE_SPACE_MARGIN = 256 * 1024 * 1024;
const VR_READY_MARKERS = ["xrBeginSession", "CompositorOpenXR::Initialize"];
const CONTENT_FAILURE_MARKERS = ["ApkAddCentralDirectory : Unable to open", "Error loading subsystem: Display"];
const LAUNCH_WATCH_MS = 75_000;
const LAUNCH_POLL_MS = 3_000;

export function expectedFileName(versionCode) {
  return `main.${versionCode}.${PACKAGE_NAME}.obb`;
}

export function parsePackage(dumpsysOutput) {
  const versionCode = /versionCode=(\d+)/.exec(dumpsysOutput);
  const appId = /(?:appId|userId)=(\d+)/.exec(dumpsysOutput);
  if (!versionCode || !appId) {
    return { installed: false };
  }
  const versionName = /versionName=(\S+)/.exec(dumpsysOutput);
  return {
    installed: true,
    versionCode: Number(versionCode[1]),
    versionName: versionName ? versionName[1] : "?",
    appUser: `u0_a${Number(appId[1]) - 10000}`,
  };
}

export function parseObbListing(statOutput) {
  return statOutput
    .split("\n")
    .map((line) => line.trim().split("|"))
    .filter((fields) => fields.length === 5 && /^\d+$/.test(fields[4]))
    .map(([path, owner, group, mode, size]) => ({
      path,
      owner,
      group,
      mode,
      size: Number(size),
      fileName: path.slice(path.lastIndexOf("/") + 1),
      worldReadable: mode.length > 0 && (Number(mode.at(-1)) & 4) !== 0,
    }));
}

export function sizeText(bytes) {
  return `${(bytes / 1_000_000_000).toFixed(1)} GB`;
}

export function parseAvailableBytes(dfOutput) {
  const lines = dfOutput.split("\n").map((line) => line.trim()).filter(Boolean);
  const columns = (lines.at(-1) ?? "").split(/\s+/);
  const kilobytes = Number(columns[3]);
  return columns.length >= 4 && Number.isFinite(kilobytes) ? kilobytes * 1024 : Number.MAX_SAFE_INTEGER;
}

export function checksumsMatch(md5Output) {
  const checksums = md5Output
    .split("\n")
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => line.split(/\s+/)[0]);
  return checksums.length === 2 && checksums[0] === checksums[1];
}

export function classifyLaunch(logcat) {
  if (CONTENT_FAILURE_MARKERS.some((marker) => logcat.includes(marker))) {
    return "content-unreadable";
  }
  if (VR_READY_MARKERS.some((marker) => logcat.includes(marker))) {
    return "reached-vr";
  }
  return "undetermined";
}

export async function inspectPackage(shell) {
  const result = await shell(`dumpsys package ${PACKAGE_NAME}`);
  return parsePackage(result.stdout + "\n" + result.stderr);
}

export async function listContentFiles(shell) {
  const result = await shell(`stat -c '%n|%U|%G|%a|%s' ${OBB_DIRECTORY}/*`);
  return parseObbListing(result.stdout);
}

export async function inspectContent(shell, pkg) {
  const entries = await listContentFiles(shell);
  const contentFile = entries.find((entry) => entry.fileName === expectedFileName(pkg.versionCode));
  if (!contentFile) {
    return { state: "not-downloaded", entries };
  }
  const readable = contentFile.owner === pkg.appUser || contentFile.worldReadable;
  return { state: readable ? "readable" : "unreadable", contentFile, entries };
}

export async function repairContentFile(shell, contentFile, report) {
  const original = contentFile.path;
  const copy = `${original}.acefix`;
  const retired = `${original}.aceold`;

  await shell(`am force-stop ${PACKAGE_NAME}`);
  await shell(`rm -f ${copy} ${retired}`);

  const freeBytes = parseAvailableBytes((await shell("df -k /sdcard")).stdout);
  if (freeBytes < contentFile.size + FREE_SPACE_MARGIN) {
    return { succeeded: false, message: `The headset needs ${sizeText(contentFile.size)} of free space to make the fix and only has ${sizeText(freeBytes)}. Free up some space on the headset and try again.` };
  }

  report("working", `Making a copy of the content file that Ace is allowed to read (${sizeText(contentFile.size)}). This usually takes under a minute…`);
  const copyResult = await shell(`cp ${original} ${copy} && chmod 666 ${copy}`);
  if (copyResult.exitCode !== 0) {
    await shell(`rm -f ${copy}`);
    return { succeeded: false, message: `Couldn't make the copy. The headset said: ${combined(copyResult)}` };
  }

  report("working", "Checking the copy is identical to the original…");
  const checksumResult = await shell(`md5sum ${original} ${copy}`);
  if (checksumResult.exitCode !== 0 || !checksumsMatch(checksumResult.stdout)) {
    await shell(`rm -f ${copy}`);
    return { succeeded: false, message: `The copy didn't match the original, so it was thrown away. The headset said: ${combined(checksumResult)}` };
  }

  report("working", "Putting the readable copy in place of the original…");
  const swapResult = await shell(`mv ${original} ${retired} && mv ${copy} ${original} && rm -f ${retired}`);
  if (swapResult.exitCode !== 0) {
    await shell(`[ -e ${original} ] || mv ${retired} ${original}`);
    await shell(`rm -f ${copy}`);
    return { succeeded: false, message: `Couldn't swap the copy into place. The headset said: ${combined(swapResult)}` };
  }

  const replaced = (await listContentFiles(shell)).find((entry) => entry.path === original);
  if (!replaced || !replaced.worldReadable) {
    return { succeeded: false, message: "The swap finished but the content file still isn't readable. Please send the results to Ace support." };
  }
  return { succeeded: true, message: `The content file now belongs to '${replaced.owner}' with permissions ${replaced.mode}, which Ace can read.` };
}

export async function launchAndWatch(shell) {
  await shell(`am force-stop ${PACKAGE_NAME}`);
  await shell("logcat -c");
  const start = await shell(`am start -n ${MAIN_ACTIVITY}`);
  if (start.exitCode !== 0) {
    return { outcome: "undetermined", detail: `Couldn't start Ace: ${combined(start)}` };
  }
  const deadline = Date.now() + LAUNCH_WATCH_MS;
  while (Date.now() < deadline) {
    await delay(LAUNCH_POLL_MS);
    const log = await shell("logcat -d -s Unity:* OVRPlugin:* OpenXR-Loader:*");
    const outcome = classifyLaunch(log.stdout);
    if (outcome !== "undetermined") {
      return { outcome, detail: "" };
    }
  }
  return { outcome: "undetermined", detail: "Ace started but didn't report either way within a minute." };
}

function combined(result) {
  return `${result.stdout}\n${result.stderr}`.trim();
}

function delay(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

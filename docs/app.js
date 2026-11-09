import { Adb, AdbDaemonTransport } from "https://esm.sh/@yume-chan/adb@2.6.4";
import { AdbDaemonWebUsbDeviceManager } from "https://esm.sh/@yume-chan/adb-daemon-webusb@2.3.2?deps=@yume-chan/adb@2.6.4";
import AdbWebCredentialStore from "https://esm.sh/@yume-chan/adb-credential-web@2.1.0?deps=@yume-chan/adb@2.6.4";
import { expectedFileName, inspectContent, inspectPackage, launchAndWatch, repairContentFile, sizeText } from "./headset.js";
import { text } from "./text.js";
import { DeviceBusyError, openAdbConnection } from "./usbConnection.js";

const fixButton = document.getElementById("fix");
const results = document.getElementById("results");
const copyButton = document.getElementById("copy");
const aboutButton = document.getElementById("about");
const aboutDialog = document.getElementById("about-dialog");
const guideButton = document.getElementById("guide");
const guideDialog = document.getElementById("guide-dialog");
document.getElementById("why").textContent = text.why;

const glyphs = { working: "…", passed: "✔", failed: "✖", instruction: "➜", info: "•" };
const credentialStore = new AdbWebCredentialStore("Ace Headset Fix");
let running = false;

fixButton.addEventListener("click", () => runFix());
copyButton.addEventListener("click", () => copyResults());
aboutButton.addEventListener("click", () => openDialog(aboutDialog));
guideButton.addEventListener("click", () => openDialog(guideDialog));
for (const dialog of [guideDialog, aboutDialog]) {
  for (const close of dialog.querySelectorAll("[data-close]")) {
    close.addEventListener("click", () => dialog.close());
  }
  dialog.addEventListener("click", (event) => {
    if (event.target === dialog) {
      dialog.close();
    }
  });
}

function openDialog(dialog) {
  dialog.showModal();
  dialog.scrollTop = 0;
}

function offerGuide(body) {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "secondary inline show-how";
  button.textContent = "How to turn on USB debugging";
  button.addEventListener("click", () => openDialog(guideDialog));
  body.append(document.createElement("br"), button);
}

async function copyResults() {
  const lines = [...results.querySelectorAll(".line, .summary")].map((line) => {
    if (line.classList.contains("summary")) {
      return `\n${line.textContent}`;
    }
    const glyph = line.querySelector(".glyph")?.textContent ?? "";
    const body = line.querySelector(".body");
    const detail = body?.querySelector(".detail");
    const message = detail ? body.textContent.slice(0, body.textContent.length - detail.textContent.length) : body?.textContent ?? "";
    return detail ? `${glyph} ${message}\n    ${detail.textContent.replace(/\n/g, "\n    ")}` : `${glyph} ${message}`;
  });
  try {
    await navigator.clipboard.writeText(`Ace Headset Fix results\n${lines.join("\n")}\n`);
    copyButton.textContent = "Copied";
  } catch {
    copyButton.textContent = "Could not copy";
  }
  setTimeout(() => { copyButton.textContent = "Copy results"; }, 1500);
}

function report(kind, message, detail) {
  const line = document.createElement("div");
  line.className = `line ${kind}`;
  const glyph = document.createElement("span");
  glyph.className = "glyph";
  glyph.setAttribute("aria-hidden", "true");
  glyph.textContent = glyphs[kind];
  const body = document.createElement("span");
  body.className = "body";
  body.textContent = message;
  if (detail) {
    const detailElement = document.createElement("span");
    detailElement.className = "detail";
    detailElement.textContent = detail;
    body.append(detailElement);
  }
  line.append(glyph, body);
  results.append(line);
  results.scrollTop = results.scrollHeight;
  return body;
}

function summarize(summary) {
  const line = document.createElement("div");
  line.className = `summary ${summary.problems > 0 ? "bad" : "good"}`;
  line.textContent = summary.problems > 0 ? text.summaryProblems : summary.fixed > 0 ? text.summaryFixed : text.summaryNothingToFix;
  results.append(line);
  results.scrollTop = results.scrollHeight;
}

async function runFix() {
  if (running) {
    return;
  }
  running = true;
  fixButton.disabled = true;
  copyButton.disabled = false;
  results.replaceChildren();
  const summary = { fixed: 0, alreadyFine: 0, problems: 0 };
  let adb;
  try {
    adb = await connectHeadset();
    if (adb) {
      await fixHeadset(adb, summary);
    } else {
      summary.problems++;
    }
  } catch (error) {
    console.error(error);
    report("failed", "Something unexpected happened.", `${text.unexpectedAdvice}\n${error?.message ?? error}`);
    summary.problems++;
  } finally {
    await closeQuietly(adb);
    summarize(summary);
    fixButton.disabled = false;
    running = false;
  }
}

async function connectHeadset() {
  report("working", "Checking this browser…");
  const manager = AdbDaemonWebUsbDeviceManager.BROWSER;
  if (!manager) {
    report("failed", text.browserUnsupported);
    return undefined;
  }
  report("passed", "This browser can connect to the headset.");

  report("working", "Choose the headset in the list…");
  const device = await pickDevice(manager);
  if (!device) {
    report("instruction", text.noDevicePicked);
    offerGuide(report("info", text.debuggingOffHint));
    return undefined;
  }
  console.log("picked", describeDevice(device));
  report("passed", `Found your ${device.raw?.productName || "headset"}.`);

  report("working", "Connecting to the headset…");
  const traffic = { sent: [], received: [], stale: 0 };
  const events = {
    onSent: (name) => { traffic.sent.push(name); console.log("→ headset", name); },
    onReceived: (name) => { traffic.received.push(name); console.log("headset →", name); },
    onStale: (name) => { traffic.stale++; console.log("headset → (leftover)", name); },
    onNoise: () => { traffic.stale++; },
  };
  let connection;
  try {
    connection = await openAdbConnection(device.raw, events);
    console.log("connected on", connection.description);
  } catch (error) {
    if (error instanceof DeviceBusyError) {
      report("failed", text.deviceBusy);
    } else {
      report("failed", text.connectFailed, `${error?.name ?? ""} ${error?.message ?? error}`.trim());
    }
    return undefined;
  }

  const transport = await authenticateWithHints(device, connection, traffic);
  if (!transport) {
    return undefined;
  }
  report("passed", "Connected to the headset.");
  return new Adb(transport);
}

async function authenticateWithHints(device, connection, traffic) {
  let promptShown = false;
  const watcher = setInterval(() => {
    const authRequests = traffic.received.filter((name) => name === "AUTH").length;
    if (authRequests >= 2 && !promptShown) {
      promptShown = true;
      report("instruction", text.allowPrompt);
    }
  }, 500);
  const silentTimer = setTimeout(() => {
    if (traffic.received.length === 0) {
      report("instruction", text.headsetSilent);
    }
  }, 10_000);
  const stillWaitingTimer = setTimeout(() => report("info", text.stillWaitingForAllow), 45_000);
  try {
    return await AdbDaemonTransport.authenticate({ serial: device.serial, connection, credentialStore });
  } catch (error) {
    report("failed", text.authenticateFailed, `${error?.name ?? ""} ${error?.message ?? error}`.trim());
    await connection.close();
    return undefined;
  } finally {
    clearInterval(watcher);
    clearTimeout(silentTimer);
    clearTimeout(stillWaitingTimer);
  }
}

async function pickDevice(manager) {
  const remembered = await manager.getDevices();
  if (remembered.length === 1) {
    return remembered[0];
  }
  return manager.requestDevice();
}

function describeDevice(device) {
  const name = device.raw?.productName || device.name || "headset";
  return `${name} (${device.serial})`;
}

async function fixHeadset(adb, summary) {
  const shell = makeShell(adb);
  report("working", "Checking Ace on the headset…");
  const pkg = await inspectPackage(shell);
  if (!pkg.installed) {
    report("failed", text.aceNotInstalled);
    summary.problems++;
    return;
  }
  report("passed", `Ace ${pkg.versionName} is installed.`);

  report("working", "Checking Ace's content file…");
  const content = await inspectContent(shell, pkg);
  if (content.state === "not-downloaded") {
    console.log(describeEntries(pkg, content.entries));
    report("info", "No content file for this version of Ace was found. Starting Ace to check whether it needs one…");
    const launch = await launchAndWatch(shell);
    if (launch.outcome === "reached-vr") {
      report("passed", "Ace started and is running without a separate content file. Nothing to fix on this headset.");
      summary.alreadyFine++;
      return;
    }
    report("failed", text.contentNotDownloaded);
    summary.problems++;
    return;
  }
  if (content.state === "readable") {
    console.log(describeEntries(pkg, content.entries));
    report("passed", text.alreadyReadable);
    await confirmLaunch(shell, summary, false);
    return;
  }

  const file = content.contentFile;
  console.log(`content file owner ${file.owner} mode ${file.mode}; Ace runs as ${pkg.appUser}`);
  report("info", text.foundTheProblem);
  const repair = await repairContentFile(shell, file, report);
  if (!repair.succeeded) {
    report("failed", repair.message, repair.detail);
    summary.problems++;
    return;
  }
  console.log(repair.message);
  report("passed", "Fixed. Ace can read its content file again.");
  await confirmLaunch(shell, summary, true);
}

async function confirmLaunch(shell, summary, fixedJustNow) {
  report("working", "Starting Ace on the headset to confirm the fix…");
  const launch = await launchAndWatch(shell);
  if (launch.outcome === "reached-vr") {
    report("passed", "Ace started and is open on the headset now.");
  } else if (launch.outcome === "content-unreadable") {
    report("failed", text.launchStillBroken);
    summary.problems++;
    return;
  } else {
    console.log(launch.detail);
    report("info", text.launchUndetermined);
  }
  if (fixedJustNow) {
    summary.fixed++;
  } else {
    summary.alreadyFine++;
  }
}

function describeEntries(pkg, entries) {
  const expected = `Expected: ${expectedFileName(pkg.versionCode)}`;
  const contentFiles = entries.filter((entry) => entry.fileName.toLowerCase().includes(".obb"));
  if (contentFiles.length === 0) {
    return `${expected}\nFound: no content file on the headset`;
  }
  const found = contentFiles.map((entry) => `${entry.fileName}  owner ${entry.owner}  permissions ${entry.mode}  ${sizeText(entry.size)}`);
  return `${expected}\nFound: ${found.join("\n       ")}`;
}

function makeShell(adb) {
  return async (command) => {
    console.log("shell:", command);
    const result = adb.subprocess.shellProtocol
      ? await runWithShellProtocol(adb, command)
      : await runWithNoneProtocol(adb, command);
    console.log("exit", result.exitCode, result.stdout.slice(0, 400), result.stderr.slice(0, 400));
    return result;
  };
}

async function runWithShellProtocol(adb, command) {
  const process = await adb.subprocess.shellProtocol.spawn(command);
  const [stdout, stderr, exitCode] = await Promise.all([readAll(process.stdout), readAll(process.stderr), process.exited]);
  return { stdout, stderr, exitCode };
}

async function runWithNoneProtocol(adb, command) {
  const marker = "__ACE_EXIT__";
  const process = await adb.subprocess.noneProtocol.spawn(`${command}; echo ${marker}$?`);
  const output = await readAll(process.output);
  const match = new RegExp(`${marker}(\\d+)\\s*$`).exec(output);
  return {
    stdout: match ? output.slice(0, match.index) : output,
    stderr: "",
    exitCode: match ? Number(match[1]) : -1,
  };
}

async function readAll(stream) {
  const decoder = new TextDecoder();
  let collected = "";
  for await (const chunk of stream) {
    collected += decoder.decode(chunk, { stream: true });
  }
  return collected + decoder.decode();
}

async function closeQuietly(adb) {
  try {
    await adb?.close();
  } catch {
  }
}

import { Adb, AdbDaemonTransport } from "https://esm.sh/@yume-chan/adb@2.6.4";
import { AdbDaemonWebUsbDevice, AdbDaemonWebUsbDeviceManager } from "https://esm.sh/@yume-chan/adb-daemon-webusb@2.3.2?deps=@yume-chan/adb@2.6.4";
import AdbWebCredentialStore from "https://esm.sh/@yume-chan/adb-credential-web@2.1.0?deps=@yume-chan/adb@2.6.4";
import { expectedFileName, inspectContent, inspectPackage, launchAndWatch, repairContentFile, sizeText } from "./headset.js";
import { text } from "./text.js";

const fixButton = document.getElementById("fix");
const results = document.getElementById("results");
const copyButton = document.getElementById("copy");
const aboutButton = document.getElementById("about");
const aboutDialog = document.getElementById("about-dialog");
const closeAbout = document.getElementById("close-about");
document.getElementById("why").textContent = text.why;

const glyphs = { working: "…", passed: "✔", failed: "✖", instruction: "➜", info: "•" };
const credentialStore = new AdbWebCredentialStore("Ace Headset Fix");
let running = false;

fixButton.addEventListener("click", () => runFix());
copyButton.addEventListener("click", () => navigator.clipboard.writeText(results.innerText));
aboutButton.addEventListener("click", () => aboutDialog.showModal());
closeAbout.addEventListener("click", () => aboutDialog.close());

function report(kind, message, detail) {
  const line = document.createElement("div");
  line.className = `line ${kind}`;
  const glyph = document.createElement("span");
  glyph.className = "glyph";
  glyph.textContent = glyphs[kind];
  const body = document.createElement("span");
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
    report("failed", `Something unexpected went wrong: ${error?.message ?? error}`, text.unexpectedAdvice);
    summary.problems++;
  } finally {
    await closeQuietly(adb);
    summarize(summary);
    fixButton.disabled = false;
    running = false;
  }
}

async function connectHeadset() {
  report("working", "Checking this browser can talk to USB devices…");
  const manager = AdbDaemonWebUsbDeviceManager.BROWSER;
  if (!manager) {
    report("failed", text.browserUnsupported);
    return undefined;
  }
  report("passed", "This browser can talk to USB devices.");

  report("working", "Asking you to pick the headset…");
  const device = await pickDevice(manager);
  if (!device) {
    report("instruction", text.noDevicePicked);
    report("info", text.debuggingOffHint);
    return undefined;
  }
  report("passed", `Picked ${describeDevice(device)}.`);

  report("working", "Opening the connection…");
  let connection;
  try {
    connection = await device.connect();
  } catch (error) {
    if (error instanceof AdbDaemonWebUsbDevice.DeviceBusyError) {
      report("failed", text.deviceBusy);
    } else {
      report("failed", text.connectFailed, `${error?.name ?? ""} ${error?.message ?? error}`.trim());
    }
    return undefined;
  }

  const transport = await authenticateWithHints(device, connection);
  if (!transport) {
    return undefined;
  }
  report("passed", "The headset trusts this computer.");
  return new Adb(transport);
}

const commandNames = { 0x4e584e43: "CNXN", 0x48545541: "AUTH", 0x4e45504f: "OPEN", 0x59414b4f: "OKAY", 0x45534c43: "CLSE", 0x45545257: "WRTE" };

async function authenticateWithHints(device, connection) {
  let packets = 0;
  let authRequests = 0;
  let promptShown = false;
  const trace = [];
  let sent = 0;
  const traceLine = report("info", "Headset traffic: none yet");
  const updateTrace = () => {
    traceLine.textContent = `Headset traffic: sent ${sent}, received ${trace.length ? trace.join(", ") : "none yet"} (${describeInterfaces(device)})`;
  };
  const observed = {
    writable: new WritableStream({
      async write(chunk) {
        sent++;
        console.log("→ headset", commandNames[chunk.value?.command] ?? chunk.value?.command);
        updateTrace();
        const writer = connection.writable.getWriter();
        try {
          await writer.write(chunk);
        } finally {
          writer.releaseLock();
        }
      },
      close() {
        return connection.writable.close();
      },
    }),
    readable: connection.readable.pipeThrough(new TransformStream({
      transform(packet, controller) {
        packets++;
        const name = commandNames[packet.command] ?? packet.command.toString(16);
        console.log("headset →", name);
        trace.push(name);
        updateTrace();
        if (name === "AUTH") {
          authRequests++;
        }
        if (authRequests >= 2 && !promptShown) {
          promptShown = true;
          report("instruction", text.allowPrompt);
        }
        controller.enqueue(packet);
      },
    })),
  };

  const silentTimer = setTimeout(() => {
    if (packets === 0) {
      report("instruction", text.headsetSilent);
    }
  }, 10_000);
  const stillWaitingTimer = setTimeout(() => report("info", text.stillWaitingForAllow), 45_000);
  try {
    return await AdbDaemonTransport.authenticate({ serial: device.serial, connection: observed, credentialStore });
  } catch (error) {
    report("failed", text.authenticateFailed, `${error?.name ?? ""} ${error?.message ?? error}`.trim());
    return undefined;
  } finally {
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

function describeInterfaces(device) {
  const raw = device.raw;
  if (!raw?.configuration) {
    return "no USB configuration";
  }
  return raw.configuration.interfaces
    .map((usbInterface) => {
      const alternate = usbInterface.alternate;
      const endpoints = alternate.endpoints.map((endpoint) => `${endpoint.direction === "in" ? "in" : "out"}${endpoint.endpointNumber}`).join("/");
      return `#${usbInterface.interfaceNumber} ${alternate.interfaceClass}.${alternate.interfaceSubclass}.${alternate.interfaceProtocol} ${endpoints}${usbInterface.claimed ? " claimed" : ""}`;
    })
    .join("; ");
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
    report("info", "No content file for this version of Ace is on the headset. Starting Ace to see whether it needs one…", describeEntries(pkg, content.entries));
    const launch = await launchAndWatch(shell);
    if (launch.outcome === "reached-vr") {
      report("passed", "Ace started and reached VR without a separate content file. Nothing to fix on this headset.");
      summary.alreadyFine++;
      return;
    }
    report("failed", text.contentNotDownloaded);
    summary.problems++;
    return;
  }
  if (content.state === "readable") {
    report("passed", text.alreadyReadable, describeEntries(pkg, content.entries));
    await confirmLaunch(shell, summary, false);
    return;
  }

  const file = content.contentFile;
  report("info", `Found the problem: the content file belongs to '${file.owner}' with permissions ${file.mode}, so Ace (which runs as '${pkg.appUser}') can't read it.`);
  const repair = await repairContentFile(shell, file, report);
  if (!repair.succeeded) {
    report("failed", repair.message);
    summary.problems++;
    return;
  }
  report("passed", "Fixed. Ace can now read its content file.", repair.message);
  await confirmLaunch(shell, summary, true);
}

async function confirmLaunch(shell, summary, fixedJustNow) {
  report("working", "Starting Ace on the headset to confirm it works…");
  const launch = await launchAndWatch(shell);
  if (launch.outcome === "reached-vr") {
    report("passed", "Ace started and reached VR. It is open on the headset now.");
  } else if (launch.outcome === "content-unreadable") {
    report("failed", text.launchStillBroken);
    summary.problems++;
    return;
  } else {
    report("info", text.launchUndetermined, launch.detail);
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

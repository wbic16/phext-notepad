import { spawn } from "node:child_process";
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const port = 4173;
const profile = mkdtempSync(`${tmpdir()}/phext-notepad-chrome-`);
const server = spawn(
  "python3",
  ["-m", "http.server", String(port), "--bind", "127.0.0.1", "--directory", "ui"],
  { cwd: root, stdio: "ignore" },
);

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
let chrome;

try {
  await sleep(250);
  // Invoke the stable binary directly. /usr/local/bin/google-chrome pins a
  // shared profile and port, and a second launch never gets its own DevTools port.
  const chromeBin = process.env.CHROME_BIN || "/usr/bin/google-chrome-stable";
  chrome = spawn(chromeBin, [
    "--headless=new",
    "--no-sandbox",
    "--disable-gpu",
    "--disable-dev-shm-usage",
    "--remote-debugging-port=0",
    `--user-data-dir=${profile}`,
    "--window-size=1280,800",
    "about:blank",
  ], { stdio: "ignore" });

  let version;
  for (let i = 0; i < 50 && !version; i += 1) {
    try {
      const active = readFileSync(`${profile}/DevToolsActivePort`, "utf8").trim().split("\n");
      const debugPort = active[0];
      version = await fetch(`http://127.0.0.1:${debugPort}/json/version`).then((response) => response.json());
    } catch {
      await sleep(100);
    }
  }
  if (!version) throw new Error("Chrome did not open a debugging port");

  const ws = new WebSocket(version.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    ws.addEventListener("open", resolve, { once: true });
    ws.addEventListener("error", reject, { once: true });
  });

  let next = 1;
  const pending = new Map();
  const logs = [];
  ws.addEventListener("message", (event) => {
    const body = JSON.parse(event.data);
    if (body.method === "Runtime.consoleAPICalled") {
      logs.push(body.params.args.map((arg) => arg.value || arg.description || "").join(" "));
    }
    if (body.method === "Runtime.exceptionThrown") {
      logs.push(body.params.exceptionDetails.text || "exception");
    }
    if (body.id && pending.has(body.id)) {
      pending.get(body.id)(body);
      pending.delete(body.id);
    }
  });

  function cdp(method, params = {}, sessionId) {
    const id = next++;
    return new Promise((resolve, reject) => {
      pending.set(id, (body) => {
        if (body.error) reject(new Error(`${method}: ${JSON.stringify(body.error)}`));
        else resolve(body.result);
      });
      const message = { id, method, params };
      if (sessionId) message.sessionId = sessionId;
      ws.send(JSON.stringify(message));
    });
  }

  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  await cdp("Page.enable", {}, sessionId);
  await cdp("Runtime.enable", {}, sessionId);
  await cdp("Emulation.setEmulatedMedia", {
    features: [{ name: "prefers-color-scheme", value: "dark" }],
  }, sessionId);
  await cdp("Page.navigate", { url: `http://127.0.0.1:${port}/index.html?selftest=1` }, sessionId);

  let report = "";
  for (let i = 0; i < 50 && !report; i += 1) {
    await sleep(100);
    const result = await cdp("Runtime.evaluate", {
      expression: "document.body.dataset.selftest || ''",
      returnByValue: true,
    }, sessionId);
    report = result.result.value;
  }
  const background = await cdp("Runtime.evaluate", {
    expression: "getComputedStyle(document.body).backgroundColor",
    returnByValue: true,
  }, sessionId);
  const shot = await cdp("Page.captureScreenshot", { format: "png" }, sessionId);
  writeFileSync("/tmp/phext-notepad-selftest.png", Buffer.from(shot.data, "base64"));

  await cdp("Runtime.evaluate", {
    expression: "call('open_baseline')",
    awaitPromise: true,
  }, sessionId);
  for (const level of ["outer", "middle", "inner"]) {
    await cdp("Runtime.evaluate", {
      expression: `call('set_zoom', { level: '${level}' })`,
      awaitPromise: true,
    }, sessionId);
    await sleep(50);
    const levelShot = await cdp("Page.captureScreenshot", { format: "png" }, sessionId);
    writeFileSync(`/tmp/phext-notepad-${level}.png`, Buffer.from(levelShot.data, "base64"));
  }
  await cdp("Emulation.setDeviceMetricsOverride", {
    width: 720,
    height: 480,
    deviceScaleFactor: 1,
    mobile: false,
  }, sessionId);
  await sleep(50);
  const narrow = await cdp("Runtime.evaluate", {
    expression: `(() => {
      const form = document.getElementById('coord-form');
      const editor = document.getElementById('editor-pane');
      return JSON.stringify({
        formOverflow: form.scrollWidth > form.clientWidth + 1,
        form: Math.round(form.clientWidth),
        editor: Math.round(editor.getBoundingClientRect().width),
        tree: Math.round(document.getElementById('tree-pane').getBoundingClientRect().width),
      });
    })()`,
    returnByValue: true,
  }, sessionId);
  console.log("narrow", narrow.result.value);
  const narrowShot = await cdp("Page.captureScreenshot", { format: "png" }, sessionId);
  writeFileSync("/tmp/phext-notepad-narrow.png", Buffer.from(narrowShot.data, "base64"));
  const message = await cdp("Runtime.evaluate", {
    expression: "document.getElementById('message').textContent",
    returnByValue: true,
  }, sessionId);
  console.log(report);
  console.log("background", background.result.value);
  console.log("message", message.result.value);
  if (logs.length) console.log(logs.join("\n"));
  if (!report || report.startsWith("error") || report.startsWith("failed")) process.exitCode = 1;
  if (!report.includes("hidden true")) process.exitCode = 1;
  if (!report.includes("outer 11D")) process.exitCode = 1;
  if (!report.includes("middle 7D")) process.exitCode = 1;
  if (!report.includes("inner 4D")) process.exitCode = 1;
  if (!report.includes("dirty true")) process.exitCode = 1;
  ws.close();
} finally {
  if (chrome) chrome.kill("SIGKILL");
  server.kill();
  await sleep(150);
  rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 50 });
}

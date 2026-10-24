import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));
const port = 4173;
const server = spawn(
  "python3",
  ["-m", "http.server", String(port), "--bind", "127.0.0.1", "--directory", "ui"],
  { cwd: root, stdio: "ignore" },
);

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

try {
  await sleep(250);
  const chrome = spawn("google-chrome", [
    "--headless=new",
    "--no-sandbox",
    "--disable-gpu",
    "--remote-debugging-port=9222",
    "--window-size=1280,800",
    "about:blank",
  ], { stdio: "ignore" });

  let version;
  for (let i = 0; i < 40 && !version; i += 1) {
    try {
      version = await fetch("http://127.0.0.1:9222/json/version").then((response) => response.json());
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
  ws.addEventListener("message", (event) => {
    const body = JSON.parse(event.data);
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
  console.log(report);
  console.log("background", background.result.value);
  if (!report || report.startsWith("error") || report.startsWith("failed")) process.exitCode = 1;
  if (!report.includes("hidden true")) process.exitCode = 1;
  if (!report.includes("outer 11D")) process.exitCode = 1;
  if (!report.includes("middle 7D")) process.exitCode = 1;
  if (!report.includes("inner 4D")) process.exitCode = 1;
  if (!report.includes("dirty true")) process.exitCode = 1;
  ws.close();
  chrome.kill();
} finally {
  server.kill();
}

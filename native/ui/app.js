const $ = (id) => document.getElementById(id);

let state = null;
let preview = null;
let applying = false;
let editGen = 0;
let editQueued = null;
let editFlight = null;

function invoke(command, args) {
  const core = window.__TAURI__ && window.__TAURI__.core;
  if (!core || !core.invoke) {
    return previewInvoke(command, args || {});
  }
  return core.invoke(command, args || {});
}

async function call(command, args) {
  try {
    const view = await invoke(command, args);
    if (view) render(view);
    return view;
  } catch (err) {
    const text = err && err.message ? err.message : String(err);
    $("message").textContent = text;
    return null;
  }
}

function render(view) {
  state = view;
  applying = true;
  try {
    document.documentElement.dataset.theme = view.theme || "system";
    document.body.classList.toggle("tree-hidden", !view.tree_visible);
    document.body.classList.toggle("wrap", !!view.word_wrap);
    applySplitter(view.splitter);
    $("editor").wrap = view.word_wrap ? "soft" : "off";

    const title = `${view.dirty ? "• " : ""}${view.filename || "Untitled"} — Phext Notepad`;
    $("doc-title").textContent = title;
    document.title = title;

    renderArms(view);
    renderSteppers(view);
    renderZoom(view);
    renderCrumbs(view);
    $("outline-title").textContent = view.outline_title;
    renderTree(view.outline);
    renderRecent(view.recent || []);
    setChecked("tree", view.tree_visible);
    setChecked("wrap", view.word_wrap);
    document.querySelectorAll('[data-action="theme"]').forEach((button) => {
      button.setAttribute("aria-checked", button.dataset.theme === view.theme ? "true" : "false");
    });
    document.querySelectorAll('[data-action="zoom"]').forEach((button) => {
      button.setAttribute("aria-checked", button.dataset.level === view.zoom ? "true" : "false");
    });

    const editor = $("editor");
    if (view.origin !== "edit" && editor.value !== view.scroll) {
      editor.value = view.scroll;
      editor.setSelectionRange(0, 0);
    }
    $("summary").textContent = view.summary || "";
    $("message").textContent = view.message || "";
    updateCaret();
  } finally {
    applying = false;
  }
}

function renderArms(view) {
  const root = $("arms");
  const html = view.arms.map((arm, index) => {
    const slash = index === 0 ? "" : '<span class="slash">/</span>';
    return `${slash}<label class="arm${arm.active ? " active" : ""}" data-level="${arm.level}">
      <button type="button" class="sub" data-zoom="${arm.level}" title="${arm.title}">${arm.subspace}</button>
      <input data-arm="${arm.level}" aria-label="${arm.title}" value="${escapeAttr(arm.text)}" spellcheck="false" autocomplete="off">
    </label>`;
  }).join("");
  if (root.dataset.signature !== signature(view.arms)) {
    const focused = document.activeElement && document.activeElement.dataset && document.activeElement.dataset.arm;
    root.innerHTML = html;
    root.dataset.signature = signature(view.arms);
    if (focused) {
      const input = root.querySelector(`[data-arm="${focused}"]`);
      if (input) input.focus();
    }
  } else {
    view.arms.forEach((arm) => {
      const input = root.querySelector(`[data-arm="${arm.level}"]`);
      const label = input && input.closest(".arm");
      if (!input || document.activeElement === input) return;
      input.value = arm.text;
      label.classList.toggle("active", arm.active);
    });
  }
}

function renderSteppers(view) {
  const arm = view.arms.find((item) => item.active) || view.arms[view.arms.length - 1];
  $("steppers").innerHTML = arm.dimensions.map((dim) => `
    <div class="step">
      <span class="name">${dim.label}</span>
      <button type="button" data-nudge="${dim.name}" data-delta="-1" aria-label="Decrease ${dim.label}">−</button>
      <output>${dim.value}</output>
      <button type="button" data-nudge="${dim.name}" data-delta="1" aria-label="Increase ${dim.label}">+</button>
    </div>`).join("");
}

function renderZoom(view) {
  document.querySelectorAll("#zoom-switch button").forEach((button) => {
    button.classList.toggle("active", button.dataset.level === view.zoom);
  });
  $("zoom-in").disabled = !view.can_zoom_in;
  $("zoom-out").disabled = !view.can_zoom_out;
}

function renderCrumbs(view) {
  const crumbs = $("crumbs");
  if (!view.crumbs || view.crumbs.length === 0) {
    crumbs.innerHTML = "";
    return;
  }
  crumbs.innerHTML = view.crumbs.map((crumb) =>
    `<button type="button" class="crumb" data-zoom="${crumb.level}">${escapeText(crumb.label)}</button>`
  ).join("");
}

function renderTree(nodes) {
  const tree = $("tree");
  const scroll = tree.scrollTop;
  if (!nodes || nodes.length === 0) {
    tree.innerHTML = '<p class="empty">No scrolls in this subspace yet. Type in the editor to create one.</p>';
    return;
  }
  tree.innerHTML = `<ul class="branch" role="group">${nodes.map(renderNode).join("")}</ul>`;
  tree.scrollTop = scroll;
}

function renderNode(node) {
  const open = node.on_path && node.children && node.children.length > 0;
  const twisty = node.children && node.children.length
    ? `<button type="button" class="twisty" aria-expanded="${open ? "true" : "false"}" aria-label="Expand">${open ? "▾" : "▸"}</button>`
    : '<span class="twisty spacer"></span>';
  const kids = node.children && node.children.length
    ? `<ul class="branch" role="group"${open ? "" : " hidden"}>${node.children.map(renderNode).join("")}</ul>`
    : "";
  const preview = node.preview ? `<span class="preview">${escapeText(node.preview)}</span>` : "";
  return `<li role="treeitem" aria-expanded="${open ? "true" : "false"}">
    <div class="row${node.on_path ? " on-path" : ""}${node.current ? " current" : ""}">
      ${twisty}
      <button type="button" class="node" data-coord="${escapeAttr(node.coordinate)}">
        <span class="kind ${node.kind}">${escapeText(node.label)}</span>
        <span class="count">${node.scroll_count}</span>
        ${preview}
      </button>
    </div>
    ${kids}
  </li>`;
}

function renderRecent(paths) {
  const root = $("recent");
  if (!paths.length) {
    root.innerHTML = '<button type="button" disabled>No recent files</button>';
    return;
  }
  root.innerHTML = paths.map((path) =>
    `<button type="button" data-action="recent" data-path="${escapeAttr(path)}">${escapeText(path)}</button>`
  ).join("");
}

function setChecked(action, value) {
  const button = document.querySelector(`[data-action="${action}"]`);
  if (button) button.setAttribute("aria-checked", value ? "true" : "false");
}

function updateCaret() {
  const editor = $("editor");
  const until = editor.value.slice(0, editor.selectionStart);
  const lines = until.split("\n");
  $("caret").textContent = `Line: ${lines.length}, Column: ${lines[lines.length - 1].length + 1}`;
}

function applySplitter(width) {
  const workspace = $("workspace");
  const max = Math.max(120, workspace.clientWidth - 160 - 6);
  const next = Math.max(120, Math.min(max || width, width));
  document.documentElement.style.setProperty("--tree-width", `${next}px`);
  $("splitter").setAttribute("aria-valuenow", String(Math.round(next)));
}

function currentPrefs(overrides) {
  return Object.assign({
    treeVisible: state.tree_visible,
    splitter: state.splitter,
    theme: state.theme,
    wordWrap: state.word_wrap,
  }, overrides);
}

async function flushEdit() {
  if (!state) return;
  const text = $("editor").value;
  if (text === state.scroll && !editQueued && !editFlight) return;
  editQueued = text;
  await pushScroll();
}

async function pushScroll() {
  if (editFlight) return editFlight;
  editFlight = (async () => {
    while (editQueued !== null) {
      const text = editQueued;
      const gen = ++editGen;
      editQueued = null;
      const view = await invoke("set_scroll", { text });
      if (gen === editGen && editQueued === null) render(view);
      else if (gen === editGen) state = view;
    }
  })().finally(() => {
    editFlight = null;
  });
  return editFlight;
}

function scheduleJump() {
  if (applying || !state) return;
  const inputs = [...document.querySelectorAll("#arms input")];
  if (!inputs.every((input) => /^\d+\.\d+\.\d+$/.test(input.value.trim()))) return;
  const coordinate = inputs.map((input) => input.value.trim()).join("/");
  if (coordinate === state.coordinate) return;
  window.clearTimeout(scheduleJump.timer);
  scheduleJump.timer = window.setTimeout(() => {
    flushEdit().then(() => call("jump", { coordinate }));
  }, 180);
}

function askSave() {
  const dialog = $("confirm");
  dialog.showModal();
  return new Promise((resolve) => {
    dialog.addEventListener("close", () => resolve(dialog.returnValue || "cancel"), { once: true });
  });
}

async function maybeDiscard() {
  await flushEdit();
  if (!state || !state.dirty) return true;
  const choice = await askSave();
  if (choice === "cancel" || choice === "") return false;
  if (choice === "save") {
    const view = await call("save");
    return !!(view && view.path && !view.dirty);
  }
  await call("discard");
  return true;
}

function closeMenus() {
  document.querySelectorAll(".menu-pop").forEach((menu) => {
    menu.hidden = true;
  });
  document.querySelectorAll(".menu-root").forEach((button) => {
    button.setAttribute("aria-expanded", "false");
  });
}

function signature(arms) {
  return arms.map((arm) => `${arm.level}:${arm.text}:${arm.active}`).join("|");
}

function escapeText(value) {
  return String(value).replace(/[&<>]/g, (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;" }[ch]));
}

function escapeAttr(value) {
  return escapeText(value).replace(/"/g, "&quot;");
}

document.addEventListener("click", (event) => {
  const root = event.target.closest(".menu-root");
  if (root) {
    const menu = document.getElementById(root.getAttribute("aria-controls"));
    const open = menu.hidden;
    closeMenus();
    menu.hidden = !open;
    root.setAttribute("aria-expanded", open ? "true" : "false");
    return;
  }
  if (!event.target.closest(".menu-pop")) closeMenus();
});

document.body.addEventListener("click", async (event) => {
  const action = event.target.closest("[data-action]");
  const zoom = event.target.closest("[data-zoom]");
  const nudge = event.target.closest("[data-nudge]");
  const coord = event.target.closest("[data-coord]");
  const twisty = event.target.closest(".twisty");

  if (twisty) {
    const item = twisty.closest('[role="treeitem"]');
    const kids = item && item.querySelector(":scope > ul");
    if (!kids) return;
    const collapsed = kids.hasAttribute("hidden");
    if (collapsed) kids.removeAttribute("hidden");
    else kids.setAttribute("hidden", "");
    twisty.setAttribute("aria-expanded", collapsed ? "true" : "false");
    twisty.textContent = collapsed ? "▾" : "▸";
    return;
  }

  if (zoom && !event.target.closest("#zoom-switch")) {
    await flushEdit();
    await call("set_zoom", { level: zoom.dataset.zoom });
    return;
  }

  if (nudge) {
    await flushEdit();
    await call("nudge", { dimension: nudge.dataset.nudge, delta: Number(nudge.dataset.delta) });
    return;
  }

  if (coord && event.target.closest(".node")) {
    await flushEdit();
    await call("jump", { coordinate: coord.dataset.coord });
    if (event.detail === 2 && state && state.can_zoom_in) {
      await call("zoom_in");
      $("editor").focus();
    }
    return;
  }

  if (!action) return;
  closeMenus();
  const name = action.dataset.action;
  if (name === "new") {
    if (await maybeDiscard()) await call("new_document");
  } else if (name === "open") {
    if (await maybeDiscard()) await call("open_dialog");
  } else if (name === "baseline") {
    if (await maybeDiscard()) await call("open_baseline");
  } else if (name === "recent") {
    if (await maybeDiscard()) await call("open_path", { path: action.dataset.path });
  } else if (name === "save") {
    await flushEdit();
    await call("save");
  } else if (name === "save-as") {
    await flushEdit();
    await call("save_as");
  } else if (name === "exit") {
    await exitApp();
  } else if (name === "tree") {
    await call("set_prefs", currentPrefs({ treeVisible: !state.tree_visible }));
  } else if (name === "wrap") {
    await call("set_prefs", currentPrefs({ wordWrap: !state.word_wrap }));
  } else if (name === "theme") {
    await call("set_prefs", currentPrefs({ theme: action.dataset.theme }));
  } else if (name === "zoom") {
    await flushEdit();
    await call("set_zoom", { level: action.dataset.level });
  } else if (name === "zoom-in") {
    await flushEdit();
    await call("zoom_in");
  } else if (name === "zoom-out") {
    await flushEdit();
    await call("zoom_out");
  } else if (name === "about") {
    $("about").showModal();
  }
});

$("zoom-switch").addEventListener("click", async (event) => {
  const button = event.target.closest("button");
  if (!button) return;
  await flushEdit();
  await call("set_zoom", { level: button.dataset.level });
});
$("zoom-in").addEventListener("click", async () => {
  await flushEdit();
  await call("zoom_in");
});
$("zoom-out").addEventListener("click", async () => {
  await flushEdit();
  await call("zoom_out");
});

$("coord-form").addEventListener("submit", (event) => event.preventDefault());
$("coord-form").addEventListener("input", (event) => {
  if (event.target.matches("input")) scheduleJump();
});
$("coord-form").addEventListener("focusin", (event) => {
  const arm = event.target.dataset && event.target.dataset.arm;
  if (!arm || applying || !state || state.zoom === arm) return;
  call("set_zoom", { level: arm });
});

$("editor").addEventListener("input", () => {
  editQueued = $("editor").value;
  pushScroll().catch((err) => {
    $("message").textContent = err && err.message ? err.message : String(err);
  });
  updateCaret();
});
$("editor").addEventListener("keyup", updateCaret);
$("editor").addEventListener("click", updateCaret);
$("editor").addEventListener("select", updateCaret);

const splitter = $("splitter");
splitter.addEventListener("pointerdown", (event) => {
  if (!state || !state.tree_visible) return;
  splitter.setPointerCapture(event.pointerId);
  const move = (ev) => {
    const rect = $("workspace").getBoundingClientRect();
    let width = ev.clientX - rect.left;
    const max = rect.width - 160 - splitter.offsetWidth;
    width = Math.max(120, Math.min(max, width));
    document.documentElement.style.setProperty("--tree-width", `${width}px`);
    state.splitter = Math.round(width);
    splitter.setAttribute("aria-valuenow", String(state.splitter));
  };
  const up = () => {
    splitter.removeEventListener("pointermove", move);
    call("set_prefs", currentPrefs({ splitter: state.splitter }));
  };
  splitter.addEventListener("pointermove", move);
  splitter.addEventListener("pointerup", up, { once: true });
});
splitter.addEventListener("keydown", (event) => {
  if (!state || !state.tree_visible) return;
  const delta = event.key === "ArrowLeft" ? -16 : event.key === "ArrowRight" ? 16 : 0;
  if (!delta) return;
  event.preventDefault();
  const width = Math.max(120, state.splitter + delta);
  state.splitter = width;
  applySplitter(width);
  call("set_prefs", currentPrefs({ splitter: state.splitter }));
});

window.addEventListener("resize", () => {
  if (state && state.tree_visible) applySplitter(state.splitter);
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") closeMenus();
  const meta = event.ctrlKey || event.metaKey;
  if (meta && event.key.toLowerCase() === "s") {
    event.preventDefault();
    flushEdit().then(() => call(event.shiftKey ? "save_as" : "save"));
  } else if (meta && event.key.toLowerCase() === "o") {
    event.preventDefault();
    maybeDiscard().then((ok) => { if (ok) call("open_dialog"); });
  } else if (meta && event.key.toLowerCase() === "n") {
    event.preventDefault();
    maybeDiscard().then((ok) => { if (ok) call("new_document"); });
  } else if (event.altKey && !meta) {
    const levels = { "1": "outer", "2": "middle", "3": "inner" };
    if (levels[event.key]) {
      event.preventDefault();
      flushEdit().then(() => call("set_zoom", { level: levels[event.key] }));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      flushEdit().then(() => call("zoom_out"));
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      flushEdit().then(() => call("zoom_in"));
    }
  }
});

async function exitApp() {
  await flushEdit();
  if (state && state.dirty && !state.path) {
    const choice = await askSave();
    if (choice === "cancel" || choice === "") return;
    if (choice === "save") {
      const view = await call("save");
      if (!view || !view.path) return;
    } else {
      await call("discard");
    }
  }
  await invoke("close_window");
}

function listen(name, handler) {
  const events = window.__TAURI__ && window.__TAURI__.event;
  if (events && events.listen) events.listen(name, handler);
}

listen("prompt-save-on-close", async () => {
  await flushEdit();
  if (!state || !state.dirty || state.path) {
    await invoke("close_window");
    return;
  }
  const choice = await askSave();
  if (choice === "cancel" || choice === "") return;
  if (choice === "save") {
    const view = await call("save");
    if (!view || !view.path) return;
  } else {
    await call("discard");
  }
  await invoke("close_window");
});

call("snapshot");

if (new URLSearchParams(location.search).has("selftest")) {
  (async () => {
    const notes = [];
    const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    for (let i = 0; i < 50 && !state; i += 1) await sleep(40);
    if (!state) {
      document.body.dataset.selftest = "failed to load";
      return;
    }
    notes.push(`start ${state.zoom} ${state.outline_title} scrolls ${state.scroll_count}`);
    await call("set_zoom", { level: "outer" });
    notes.push(`outer ${state.outline_title} libs ${document.querySelectorAll(".kind.library").length}`);
    await call("set_zoom", { level: "middle" });
    notes.push(`middle ${state.outline_title} crumbs ${document.querySelectorAll(".crumb").length}`);
    await call("set_zoom", { level: "inner" });
    notes.push(`inner ${state.outline_title}`);
    await call("set_prefs", currentPrefs({ treeVisible: false, splitter: 280 }));
    const editorWidth = $("editor-pane").getBoundingClientRect().width;
    const workspaceWidth = $("workspace").getBoundingClientRect().width;
    notes.push(`hidden ${document.body.classList.contains("tree-hidden")} editor ${Math.round(editorWidth)} of ${Math.round(workspaceWidth)}`);
    await call("set_prefs", currentPrefs({ treeVisible: true, splitter: 360, theme: "dark" }));
    notes.push(`theme ${document.documentElement.dataset.theme} tree ${$("tree-pane").getBoundingClientRect().width}`);
    $("editor").value = "selftest scroll\nsecond";
    $("editor").setSelectionRange(16, 16);
    $("editor").dispatchEvent(new Event("input"));
    await flushEdit();
    updateCaret();
    notes.push(`dirty ${state.dirty} caret ${$("caret").textContent} text ${state.scroll.split("\n")[0]}`);
    await call("set_prefs", currentPrefs({ theme: "system", treeVisible: true }));
    document.body.dataset.selftest = notes.join(" | ");
  })().catch((err) => {
    document.body.dataset.selftest = `error ${err && err.message ? err.message : err}`;
  });
}

// Browser preview when the page is opened outside the Tauri shell.
// The fixture is the baseline phext rendered by the same Rust model.
async function previewInvoke(command, args) {
  if (!preview) {
    const response = await fetch("fixture.json");
    preview = { fixture: await response.json(), zoom: "inner", prefs: {}, scroll: null, dirty: false };
    preview.prefs = {
      tree_visible: true,
      splitter: 320,
      theme: "system",
      word_wrap: true,
    };
  }
  const fixture = preview.fixture;
  const base = structuredClone(fixture[preview.zoom] || fixture.inner);
  Object.assign(base, preview.prefs);
  if (preview.scroll !== null && preview.zoom === "inner") {
    base.scroll = preview.scroll;
    base.dirty = preview.dirty;
  }
  if (command === "snapshot") return base;
  if (command === "set_zoom") {
    preview.zoom = args.level;
    const next = structuredClone(fixture[args.level]);
    Object.assign(next, preview.prefs);
    next.origin = "navigate";
    return next;
  }
  if (command === "zoom_in" || command === "zoom_out") {
    const order = ["outer", "middle", "inner"];
    let index = order.indexOf(preview.zoom);
    index = command === "zoom_in" ? Math.min(2, index + 1) : Math.max(0, index - 1);
    preview.zoom = order[index];
    const next = structuredClone(fixture[preview.zoom]);
    Object.assign(next, preview.prefs);
    next.origin = "navigate";
    return next;
  }
  if (command === "set_prefs") {
    preview.prefs.tree_visible = args.treeVisible;
    preview.prefs.splitter = args.splitter;
    preview.prefs.theme = args.theme;
    preview.prefs.word_wrap = args.wordWrap;
    Object.assign(base, preview.prefs);
    base.origin = "prefs";
    return base;
  }
  if (command === "set_scroll") {
    preview.scroll = args.text;
    preview.dirty = true;
    base.scroll = args.text;
    base.dirty = true;
    base.origin = "edit";
    base.filename = base.filename || "baseline.phext";
    return base;
  }
  if (command === "jump") {
    const found = (fixture.scrolls || []).find((row) => row[0] === args.coordinate);
    if (found) base.scroll = found[1];
    base.coordinate = args.coordinate;
    base.origin = "navigate";
    base.message = "";
    return base;
  }
  if (command === "discard") {
    preview.dirty = false;
    base.dirty = false;
    base.origin = "prefs";
    return base;
  }
  if (command === "save" || command === "save_as" || command === "open_dialog" || command === "new_document" || command === "close_window") {
    base.message = "This preview has no file dialog. Run the native app to open and save.";
    base.origin = "prefs";
    return base;
  }
  if (command === "open_baseline") {
    preview.zoom = "inner";
    preview.scroll = null;
    preview.dirty = false;
    return structuredClone(fixture.inner);
  }
  return base;
}

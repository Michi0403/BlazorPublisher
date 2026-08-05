const floaters = ["✨", "🌸", "✦", "🐾", "🎀", "♡", "⋆", "🖋️", "📚", "🎨"];
const themeStorageKey = "publisherstudio-docs-theme";
const prefersReducedMotion = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const supportsFinePointer = () => window.matchMedia("(pointer: fine)").matches;

function readThemePreference() {
  try {
    const value = window.localStorage.getItem(themeStorageKey);
    return value === "light" || value === "dark" || value === "system" ? value : "system";
  }
  catch {
    return "system";
  }
}

function resolvedTheme(preference) {
  if (preference === "light" || preference === "dark") return preference;
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function applyTheme(preference = readThemePreference()) {
  const root = document.documentElement;
  if (!root) return;
  const theme = resolvedTheme(preference);
  root.setAttribute("data-bs-theme", theme);
  root.dataset.publisherstudioThemePreference = preference;

  const button = document.querySelector(".publisherstudio-theme-control");
  if (button instanceof HTMLButtonElement) {
    const icon = preference === "dark" ? "🌙" : preference === "light" ? "☀️" : "🌓";
    const label = preference === "dark" ? "Dark" : preference === "light" ? "Light" : "System";
    button.dataset.themePreference = preference;
    button.setAttribute("aria-label", `Documentation theme: ${label}. Activate to change it.`);
    button.title = `Documentation theme: ${label}`;
    const iconElement = button.querySelector(".publisherstudio-theme-icon");
    const labelElement = button.querySelector(".publisherstudio-theme-label");
    if (iconElement) iconElement.textContent = icon;
    if (labelElement) labelElement.textContent = label;
  }
}

function cycleTheme() {
  const current = readThemePreference();
  const next = current === "system" ? "dark" : current === "dark" ? "light" : "system";
  try {
    window.localStorage.setItem(themeStorageKey, next);
  }
  catch {
    // The selected theme remains active for this page when storage is unavailable.
  }
  applyTheme(next);
}

function hideBuiltInThemePickers() {
  const selectors = [
    "button[aria-label*='theme' i]",
    "button[title*='theme' i]",
    "button[data-bs-theme-value]",
    ".theme-picker",
    ".theme-toggle",
    ".theme-switcher"
  ];

  for (const selector of selectors) {
    for (const element of document.querySelectorAll(selector)) {
      if (!(element instanceof HTMLElement) || element.classList.contains("publisherstudio-theme-control")) continue;
      element.hidden = true;
      element.setAttribute("aria-hidden", "true");
      element.tabIndex = -1;
    }
  }
}

function ensureThemeControl() {
  if (document.querySelector(".publisherstudio-theme-control")) {
    applyTheme();
    return;
  }

  const host = document.querySelector(".navbar .navbar-nav, .navbar .buttons, .navbar .d-flex, .navbar");
  if (!host) return;

  const button = document.createElement("button");
  button.type = "button";
  button.className = "publisherstudio-theme-control";
  button.innerHTML = '<span class="publisherstudio-theme-icon" aria-hidden="true">🌓</span><span class="publisherstudio-theme-label">System</span>';
  button.addEventListener("click", cycleTheme);
  host.appendChild(button);
  applyTheme();
}

function decorateBrand() {
  const brand = document.querySelector(".navbar-brand");
  if (!brand || brand.dataset.publisherstudioDecorated === "true") return;

  for (const element of brand.querySelectorAll(":scope > img, :scope > svg, :scope > .logo, :scope > [class*='logo']")) {
    element.setAttribute("aria-hidden", "true");
    element.hidden = true;
  }
  for (const node of [...brand.childNodes]) {
    if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() === "D") node.remove();
  }

  const paw = document.createElement("span");
  paw.className = "publisherstudio-brand-paw";
  paw.setAttribute("aria-hidden", "true");
  paw.textContent = "🐾";
  brand.prepend(paw);
  brand.dataset.publisherstudioDecorated = "true";
}

function createKawaiiSky() {
  if (document.querySelector(".publisherstudio-kawaii-sky")) return;
  const sky = document.createElement("div");
  sky.className = "publisherstudio-kawaii-sky";
  sky.setAttribute("aria-hidden", "true");
  const count = window.matchMedia("(max-width: 767.98px)").matches ? 12 : 24;
  for (let index = 0; index < count; index += 1) {
    const item = document.createElement("span");
    item.className = "publisherstudio-kawaii-floater";
    item.textContent = floaters[index % floaters.length];
    item.style.setProperty("--publisherstudio-left", `${(index * 37 + 7) % 98}%`);
    item.style.setProperty("--publisherstudio-top", `${(index * 53 + 11) % 96}%`);
    item.style.setProperty("--publisherstudio-size", `${0.72 + ((index * 13) % 9) / 10}rem`);
    item.style.setProperty("--publisherstudio-opacity", `${0.10 + ((index * 17) % 18) / 100}`);
    item.style.setProperty("--publisherstudio-duration", `${13 + ((index * 19) % 15)}s`);
    item.style.setProperty("--publisherstudio-delay", `${-((index * 7) % 17)}s`);
    item.style.setProperty("--publisherstudio-rotate", `${(index * 29) % 42 - 21}deg`);
    sky.appendChild(item);
  }
  document.body.prepend(sky);
}

function addKawaiiClick(event) {
  const target = event.target instanceof Element ? event.target.closest("a, button, .nav-link") : null;
  if (!target || prefersReducedMotion()) return;
  const pop = document.createElement("span");
  pop.className = "publisherstudio-kawaii-pop";
  pop.setAttribute("aria-hidden", "true");
  pop.textContent = floaters[Math.floor(Math.random() * floaters.length)];
  pop.style.setProperty("--publisherstudio-pop-x", `${event.clientX}px`);
  pop.style.setProperty("--publisherstudio-pop-y", `${event.clientY}px`);
  document.body.appendChild(pop);
  window.setTimeout(() => pop.remove(), 950);
}

function ensureCursorCompanion() {
  if (!supportsFinePointer() || prefersReducedMotion() || document.querySelector(".publisherstudio-cursor-paw")) return;
  const paw = document.createElement("span");
  paw.className = "publisherstudio-cursor-paw";
  paw.setAttribute("aria-hidden", "true");
  paw.textContent = "🐾";
  document.body.appendChild(paw);
  document.addEventListener("pointermove", event => {
    paw.style.transform = `translate(${event.clientX}px, ${event.clientY}px)`;
  }, { passive: true });
}

function decorateDocumentation() {
  document.documentElement.classList.add("publisherstudio-kawaii-docs");
  applyTheme();
  hideBuiltInThemePickers();
  ensureThemeControl();
  decorateBrand();
  createKawaiiSky();
  ensureCursorCompanion();

  if (document.documentElement.dataset.publisherstudioKawaiiStarted !== "true") {
    document.documentElement.dataset.publisherstudioKawaiiStarted = "true";
    document.addEventListener("click", addKawaiiClick, { passive: true });
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener?.("change", () => {
      if (readThemePreference() === "system") applyTheme("system");
    });
  }
}

function startPublisherStudioDocumentation() {
  decorateDocumentation();
  window.requestAnimationFrame(decorateDocumentation);
  window.setTimeout(decorateDocumentation, 250);
}

export default {
  iconLinks: [
    {
      icon: "github",
      href: "https://github.com/Michi0403/BlazorPublisher",
      title: "PublisherStudio on GitHub"
    }
  ],
  start: startPublisherStudioDocumentation
};

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", startPublisherStudioDocumentation, { once: true });
}
else {
  startPublisherStudioDocumentation();
}

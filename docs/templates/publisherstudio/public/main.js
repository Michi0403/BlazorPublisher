// javascript-diagnostics: guarded

function reportDocumentationError(context, error) {
  try {
    const bridge = globalThis.publisherStudioJavaScriptDiagnostics;
    if (bridge?.report) bridge.report('publisherstudio-docs.' + context, error);
    else console.error('PublisherStudio documentation JavaScript error in ' + context + '.', error);
  } catch (reportError) {
    console.error('PublisherStudio documentation diagnostics failed.', reportError);
  }
}
const THEME_COOKIE = "publisherstudio-docs-theme";
const THEME_STORAGE = "publisherstudio-docs-theme";
const DOCFX_THEME_STORAGE = "theme";
const VALID_THEMES = new Set(["light", "dark", "auto"]);
const THEME_LABELS = {
  light: { label: "Light", icon: "☀️" },
  dark: { label: "Dark", icon: "🌙" },
  auto: { label: "Auto", icon: "◐" }
};
const floaters = ["✨", "✦", "🐾", "🌸", "♡", "⋆", "🎀"];
const pawTrailIcons = ["🐾", "🐾", "🐾", "ฅ^•ﻌ•^ฅ"];

const prefersReducedMotion = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const supportsFinePointer = () => window.matchMedia("(pointer: fine)").matches;

function readCookie(name) {
  try {
      const prefix = `${encodeURIComponent(name)}=`;
      for (const part of document.cookie.split(";")) {
        const value = part.trim();
        if (value.startsWith(prefix)) return decodeURIComponent(value.slice(prefix.length));
      }
      return null;
  } catch (error) {
    reportDocumentationError('readCookie', error);
    throw error;
  }
}

function readStoredTheme() {
  try {
      const cookieTheme = readCookie(THEME_COOKIE);
      if (VALID_THEMES.has(cookieTheme)) return cookieTheme;

      for (const key of [THEME_STORAGE, DOCFX_THEME_STORAGE]) {
        try {
          const value = window.localStorage.getItem(key);
          if (VALID_THEMES.has(value)) return value;
        }
        catch (error) {
        reportDocumentationError('recovered-operation', error);
          // Locked-down WebViews may deny storage. The cookie/system fallback still works.
        }
      }
      return "auto";
  } catch (error) {
    reportDocumentationError('readStoredTheme', error);
    throw error;
  }
}

function resolveTheme(preference) {
  try {
      if (preference === "dark" || preference === "light") return preference;
      return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  } catch (error) {
    reportDocumentationError('resolveTheme', error);
    throw error;
  }
}

function persistTheme(preference) {
  try {
      if (!VALID_THEMES.has(preference)) return;
      try {
        window.localStorage.setItem(THEME_STORAGE, preference);
        window.localStorage.setItem(DOCFX_THEME_STORAGE, preference);
      }
      catch (error) {
        reportDocumentationError('recovered-operation', error);
        // Cookie persistence is still attempted below.
      }

      const secure = window.location.protocol === "https:" ? "; Secure" : "";
      document.cookie = `${encodeURIComponent(THEME_COOKIE)}=${encodeURIComponent(preference)}; Max-Age=31536000; Path=/; SameSite=Lax${secure}`;
  } catch (error) {
    reportDocumentationError('persistTheme', error);
    throw error;
  }
}

function updateThemeControl(preference) {
  try {
      const normalized = VALID_THEMES.has(preference) ? preference : "auto";
      const current = THEME_LABELS[normalized];
      document.querySelectorAll("[data-publisherstudio-theme-control]").forEach(control => {
        if (!(control instanceof HTMLElement)) return;
        control.dataset.themePreference = normalized;
        const icon = control.querySelector("[data-publisherstudio-theme-current-icon]");
        const text = control.querySelector("[data-publisherstudio-theme-current-label]");
        if (icon) icon.textContent = current.icon;
        if (text) text.textContent = current.label;
        control.querySelectorAll("[data-publisherstudio-theme]").forEach(button => {
          const active = button.getAttribute("data-publisherstudio-theme") === normalized;
          button.classList.toggle("active", active);
          button.setAttribute("aria-checked", String(active));
        });
      });
  } catch (error) {
    reportDocumentationError('updateThemeControl', error);
    throw error;
  }
}

function applyTheme(preference, persist = false) {
  try {
      const normalized = VALID_THEMES.has(preference) ? preference : "auto";
      document.documentElement.dataset.publisherstudioThemePreference = normalized;
      document.documentElement.setAttribute("data-bs-theme", resolveTheme(normalized));
      updateThemeControl(normalized);
      if (persist) persistTheme(normalized);
  } catch (error) {
    reportDocumentationError('applyTheme', error);
    throw error;
  }
}

function createThemeControl() {
  try {
      const details = document.createElement("details");
      details.className = "publisherstudio-theme-control";
      details.setAttribute("data-publisherstudio-theme-control", "true");

      const summary = document.createElement("summary");
      summary.className = "btn border-0 publisherstudio-theme-control-toggle";
      summary.title = "Choose documentation theme";
      summary.setAttribute("aria-label", "Choose documentation theme");
      summary.innerHTML = '<span data-publisherstudio-theme-current-icon aria-hidden="true">◐</span><span class="visually-hidden">Theme: <span data-publisherstudio-theme-current-label>Auto</span></span>';
      details.appendChild(summary);

      const menu = document.createElement("div");
      menu.className = "publisherstudio-theme-control-menu";
      menu.setAttribute("role", "radiogroup");
      menu.setAttribute("aria-label", "Documentation theme");

      for (const preference of ["light", "dark", "auto"]) {
        const option = THEME_LABELS[preference];
        const button = document.createElement("button");
        button.type = "button";
        button.className = "publisherstudio-theme-control-option";
        button.setAttribute("data-publisherstudio-theme", preference);
        button.setAttribute("role", "radio");
        button.setAttribute("aria-checked", "false");
        button.innerHTML = `<span aria-hidden="true">${option.icon}</span><span>${option.label}</span>`;
        menu.appendChild(button);
      }
      details.appendChild(menu);
      return details;
  } catch (error) {
    reportDocumentationError('createThemeControl', error);
    throw error;
  }
}

function mountThemeControl() {
  try {
      const nativeToggle = document.querySelector(
        ".navbar .dropdown > a[title*='theme' i], .navbar .dropdown > button[title*='theme' i], .navbar .dropdown > [aria-label*='theme' i]"
      );
      const nativePicker = nativeToggle?.closest(".dropdown");
      let control = document.querySelector("[data-publisherstudio-theme-control]");

      if (nativePicker) {
        nativePicker.classList.add("publisherstudio-native-theme-picker");
        nativePicker.setAttribute("aria-hidden", "true");
        nativePicker.setAttribute("inert", "");
        if (control && control.nextElementSibling !== nativePicker) nativePicker.before(control);
      }

      if (!control) {
        const insertionParent = nativePicker?.parentElement || document.querySelector(".navbar .navbar-collapse, .navbar .container-xxl, .navbar");
        if (!insertionParent) return false;
        control = createThemeControl();
        if (nativePicker) nativePicker.before(control);
        else insertionParent.appendChild(control);
      }

      updateThemeControl(readStoredTheme());
      return true;
  } catch (error) {
    reportDocumentationError('mountThemeControl', error);
    throw error;
  }
}

function installThemePersistence() {
  try {
      if (document.documentElement.dataset.publisherstudioThemePersistence === "true") return;
      document.documentElement.dataset.publisherstudioThemePersistence = "true";

      const initial = readStoredTheme();
      applyTheme(initial, true);

      document.addEventListener("click", event => {
        const option = event.target instanceof Element
          ? event.target.closest("[data-publisherstudio-theme]")
          : null;
        const requested = option?.getAttribute("data-publisherstudio-theme");
        if (!VALID_THEMES.has(requested)) return;

        event.preventDefault();
        event.stopPropagation();
        applyTheme(requested, true);
        option.closest("details")?.removeAttribute("open");
      });

      document.addEventListener("click", event => {
        document.querySelectorAll("details[data-publisherstudio-theme-control][open]").forEach(control => {
          if (event.target instanceof Node && !control.contains(event.target)) control.removeAttribute("open");
        });
      });

      document.addEventListener("keydown", event => {
        if (event.key !== "Escape") return;
        document.querySelectorAll("details[data-publisherstudio-theme-control][open]").forEach(control => {
          control.removeAttribute("open");
          control.querySelector("summary")?.focus();
        });
      });

      window.addEventListener("storage", event => {
        if (event.key !== THEME_STORAGE && event.key !== DOCFX_THEME_STORAGE) return;
        const value = VALID_THEMES.has(event.newValue) ? event.newValue : readStoredTheme();
        applyTheme(value, true);
      });

      window.matchMedia("(prefers-color-scheme: dark)").addEventListener?.("change", () => {
        if (readStoredTheme() === "auto") applyTheme("auto", false);
      });
  } catch (error) {
    reportDocumentationError('installThemePersistence', error);
    throw error;
  }
}

function watchThemeControl() {
  try {
      if (document.documentElement.dataset.publisherstudioThemeControlWatch === "true") return;
      document.documentElement.dataset.publisherstudioThemeControlWatch = "true";
      let scheduled = false;
      const observer = new MutationObserver(() => {
        if (scheduled) return;
        scheduled = true;
        window.requestAnimationFrame(() => {
          scheduled = false;
          mountThemeControl();
        });
      });
      observer.observe(document.body, { childList: true, subtree: true });
      window.setTimeout(() => observer.disconnect(), 10000);
  } catch (error) {
    reportDocumentationError('watchThemeControl', error);
    throw error;
  }
}

function createKawaiiSky() {
  try {
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
        item.style.setProperty("--publisherstudio-size", `${0.7 + ((index * 13) % 8) / 10}rem`);
        item.style.setProperty("--publisherstudio-opacity", `${0.10 + ((index * 17) % 18) / 100}`);
        item.style.setProperty("--publisherstudio-duration", `${14 + ((index * 19) % 14)}s`);
        item.style.setProperty("--publisherstudio-delay", `${-((index * 7) % 17)}s`);
        item.style.setProperty("--publisherstudio-rotate", `${(index * 29) % 42 - 21}deg`);
        sky.appendChild(item);
      }
      document.body.prepend(sky);
  } catch (error) {
    reportDocumentationError('createKawaiiSky', error);
    throw error;
  }
}

function decorateBrand() {
  try {
      const brand = document.querySelector(".navbar-brand");
      if (!brand) return;
      brand.dataset.publisherstudioCatBrand = "true";
      const logo = brand.querySelector("img#logo, img[src*='logo.svg']");
      if (logo) {
        logo.hidden = false;
        logo.removeAttribute("aria-hidden");
        logo.setAttribute("alt", "PublisherStudio cat paw");
      }
      for (const node of [...brand.childNodes]) {
        if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() === "D") node.remove();
      }
  } catch (error) {
    reportDocumentationError('decorateBrand', error);
    throw error;
  }
}

function addKawaiiClick(event) {
  try {
      const target = event.target instanceof Element ? event.target.closest("a, button, .nav-link") : null;
      if (target && !prefersReducedMotion()) {
        const pop = document.createElement("span");
        pop.className = "publisherstudio-kawaii-pop";
        pop.setAttribute("aria-hidden", "true");
        pop.textContent = floaters[Math.floor(Math.random() * floaters.length)];
        pop.style.setProperty("--publisherstudio-pop-x", `${event.clientX}px`);
        pop.style.setProperty("--publisherstudio-pop-y", `${event.clientY}px`);
        document.body.appendChild(pop);
        window.setTimeout(() => pop.remove(), 1000);
      }

      if (!supportsFinePointer() || prefersReducedMotion()) return;
      const scratch = document.createElement("span");
      scratch.className = "publisherstudio-cat-scratch";
      scratch.setAttribute("aria-hidden", "true");
      scratch.style.left = `${event.clientX}px`;
      scratch.style.top = `${event.clientY}px`;
      document.body.appendChild(scratch);
      window.setTimeout(() => scratch.remove(), 850);
  } catch (error) {
    reportDocumentationError('addKawaiiClick', error);
    throw error;
  }
}

function ensureCursorCompanion() {
  try {
      if (!supportsFinePointer() || prefersReducedMotion() || document.querySelector(".publisherstudio-cursor-paw")) return;
      const paw = document.createElement("span");
      paw.className = "publisherstudio-cursor-paw";
      paw.setAttribute("aria-hidden", "true");
      paw.textContent = "🐾";
      document.body.appendChild(paw);

      let lastTrailTime = 0;
      document.addEventListener("pointermove", event => {
        paw.style.transform = `translate(${event.clientX}px, ${event.clientY}px)`;
        const now = Date.now();
        if (now - lastTrailTime < 95) return;
        lastTrailTime = now;
        const trail = document.createElement("span");
        trail.className = "publisherstudio-paw-trail";
        trail.setAttribute("aria-hidden", "true");
        trail.textContent = pawTrailIcons[Math.floor(Math.random() * pawTrailIcons.length)];
        trail.style.left = `${event.clientX}px`;
        trail.style.top = `${event.clientY}px`;
        trail.style.setProperty("--publisherstudio-trail-rotate", `${Math.round(Math.random() * 34 - 17)}deg`);
        document.body.appendChild(trail);
        window.setTimeout(() => trail.remove(), 1200);
      }, { passive: true });
  } catch (error) {
    reportDocumentationError('ensureCursorCompanion', error);
    throw error;
  }
}


async function ensureRootDocumentationRail() {
  try {
      const main = document.querySelector('body:not([data-search]) > main.container-xxl');
      if (!main || main.querySelector(':scope > .toc-offcanvas')) return;

      const content = main.querySelector(':scope > .content');
      const tocRelative = document.querySelector('meta[name="docfx:tocrel"]')?.getAttribute('content')?.trim();
      const navRelative = document.querySelector('meta[name="docfx:navrel"]')?.getAttribute('content')?.trim();
      if (!content || !tocRelative || !navRelative) return;

      let tocUrl;
      let navUrl;
      try {
        tocUrl = new URL(tocRelative, document.baseURI);
        navUrl = new URL(navRelative, document.baseURI);
      }
      catch (error) {
        reportDocumentationError('recovered-operation', error);
        return;
      }

      // DocFX omits the left rail on a landing page when the page TOC and the
      // navigation TOC are the same file. The desktop shell still reserves that
      // column, so reuse the authoritative TOC instead of leaving dead space.
      if (tocUrl.href !== navUrl.href) return;

      const shell = document.createElement('div');
      shell.className = 'toc-offcanvas publisherstudio-root-toc';
      shell.setAttribute('data-publisherstudio-root-toc', 'true');
      shell.innerHTML = `
        <div class="offcanvas-md offcanvas-start" tabindex="-1" id="tocOffcanvas" aria-labelledby="tocOffcanvasLabel">
          <div class="offcanvas-header">
            <h5 class="offcanvas-title" id="tocOffcanvasLabel">Table of Contents</h5>
            <button type="button" class="btn-close" data-bs-dismiss="offcanvas" data-bs-target="#tocOffcanvas" aria-label="Close"></button>
          </div>
          <div class="offcanvas-body">
            <nav class="toc" id="toc" aria-label="Documentation"></nav>
          </div>
        </div>`;
      main.insertBefore(shell, content);

      const target = shell.querySelector('nav.toc');
      try {
        const response = await fetch(tocUrl, { credentials: 'same-origin' });
        if (!response.ok) throw new Error(`TOC request failed with ${response.status}`);
        const parsed = new DOMParser().parseFromString(await response.text(), 'text/html');
        const list = parsed.querySelector('#sidetoggle .sidetoc .toc > ul, .sidetoc .toc > ul, #toc > ul');
        if (!target || !list) throw new Error('TOC markup was not found');

        target.replaceChildren(list.cloneNode(true));
        const currentUrl = new URL(window.location.href);
        currentUrl.hash = '';
        for (const link of target.querySelectorAll('a[href]')) {
          try {
            const linkUrl = new URL(link.getAttribute('href'), tocUrl);
            linkUrl.hash = '';
            link.href = linkUrl.href;
            const active = linkUrl.href === currentUrl.href;
            link.classList.toggle('active', active);
            link.parentElement?.classList.toggle('active', active);
            if (active) link.setAttribute('aria-current', 'page');
            else link.removeAttribute('aria-current');
          }
          catch (error) {
        reportDocumentationError('recovered-operation', error);
            // One malformed optional link must not take down the documentation shell.
          }
        }
      }
      catch (error) {
        shell.remove();
        console.warn('PublisherStudio documentation navigation could not be loaded.', error);
      }
  } catch (error) {
    reportDocumentationError('ensureRootDocumentationRail', error);
    throw error;
  }
}

function startKawaiiDocumentation() {
  try {
      document.documentElement.classList.add("publisherstudio-kawaii-docs");
      void ensureRootDocumentationRail();
      installThemePersistence();
      createKawaiiSky();
      decorateBrand();
      ensureCursorCompanion();

      if (document.documentElement.dataset.publisherstudioKawaiiStarted !== "true") {
        document.documentElement.dataset.publisherstudioKawaiiStarted = "true";
        document.addEventListener("click", addKawaiiClick, { passive: true });
      }

      mountThemeControl();
      watchThemeControl();
      window.requestAnimationFrame(() => {
        decorateBrand();
        mountThemeControl();
      });
      window.setTimeout(mountThemeControl, 250);
      window.setTimeout(mountThemeControl, 900);
      window.setTimeout(mountThemeControl, 2200);
  } catch (error) {
    reportDocumentationError('startKawaiiDocumentation', error);
    throw error;
  }
}

export default {
  iconLinks: [
    {
      icon: "github",
      href: "https://github.com/Michi0403/BlazorPublisher",
      title: "PublisherStudio on GitHub"
    }
  ],
  start: startKawaiiDocumentation
};

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", startKawaiiDocumentation, { once: true });
}
else {
  startKawaiiDocumentation();
}

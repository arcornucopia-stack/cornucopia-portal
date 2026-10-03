// Public model page behind every share from the app: m.html?id={modelKey}.
// No sign-in - it reads only cornucopia/shared_models/{modelKey}, the public
// copy the portal writes when a model is published (publishSharedModel).
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import { getDatabase, ref as dbRef, get } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-database.js";

// Fill these in when the store listings go live - the page switches from
// "coming soon" to store buttons with no app update needed.
const APP_STORE_URL = "";
const PLAY_STORE_URL = "";

// Must match AppLinks.Scheme in the app and the Android package name.
const APP_SCHEME = "cornucopiaar";
const ANDROID_PACKAGE = "com.cornucopia.v2";

const CTA_LABELS = {
  buy_now: "Buy Now", subscribe: "Subscribe", donate: "Donate",
  pre_order: "Pre-Order", join_waitlist: "Join Waitlist"
};

const $ = (id) => document.getElementById(id);
const isAndroid = /Android/i.test(navigator.userAgent);
const isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent)
  || (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);

const modelKey = new URLSearchParams(location.search).get("id") || "";

renderStores();
$("openApp").addEventListener("click", openInApp);

if (!/^[A-Za-z0-9_-]+$/.test(modelKey)) {
  showMessage("This link is incomplete", "It doesn't point to a product. Ask whoever shared it to send it again.");
} else {
  load();
}

async function load() {
  const config = window.CORNUCOPIA_FIREBASE_CONFIG;
  if (!config?.projectId) {
    showMessage("Something went wrong", "This page couldn't start. Please try again later.");
    return;
  }
  try {
    const db = getDatabase(initializeApp(config));
    const snap = await get(dbRef(db, `cornucopia/shared_models/${modelKey}`));
    if (!snap.exists() || !snap.val().name) {
      showMessage("This product isn't available", "It may have been removed by the brand that listed it. You can still get Cornucopia to discover other products in 3D.", true);
      return;
    }
    render(snap.val());
  } catch (err) {
    console.error(err);
    showMessage("Couldn't load this product", "Check your connection and refresh the page.");
  }
}

function render(m) {
  document.title = `${m.name} — See it in 3D on Cornucopia`;
  $("product").setAttribute("aria-busy", "false");

  $("name").textContent = m.name;
  if (m.businessName) {
    $("maker").textContent = m.businessName;
    $("maker").hidden = false;
  }
  $("desc").textContent = m.description || "";

  renderPrices(m);

  if (m.ctaType && m.ctaType !== "none" && /^https?:\/\//i.test(m.ctaLink || "")) {
    const cta = $("cta");
    cta.textContent = CTA_LABELS[m.ctaType] || "Learn more";
    cta.href = m.ctaLink;
    cta.hidden = false;
  }

  renderStage(m);
}

/** Price first; on a discount the brand value shows struck through next to
 * it, on BOGO a badge says so. Values arrive already formatted ("GH₵1,000"). */
function renderPrices(m) {
  const box = $("prices");
  box.innerHTML = "";
  const add = (el) => box.appendChild(el);
  const node = (tag, cls, text) => { const e = document.createElement(tag); e.className = cls; if (text) e.textContent = text; return e; };

  if (m.price) add(node("span", "price", m.price));
  if (m.brandValue && m.price && m.dealType === "discount") {
    const was = node("span", "was", "");
    was.append("Brand value ");
    const s = document.createElement("s");
    s.textContent = m.brandValue;
    was.append(s);
    add(was);
  } else if (m.brandValue) {
    const line = node("span", "value-line", "");
    line.append("Brand value ");
    const strong = document.createElement("strong");
    strong.textContent = m.brandValue;
    line.append(strong);
    add(line);
  }
  if (m.dealType === "discount" && m.price) add(node("span", "deal-badge", "Deal"));
  if (m.dealType === "bogo") add(node("span", "deal-badge", "Buy one, get one"));
}

/** Interactive 3D (with "View in your space" AR on phones that support it)
 * when the 3D file can load; otherwise the product picture. The 3D file
 * needs cross-origin access on the storage bucket - until that's set up,
 * model-viewer reports an error and the picture takes its place. */
function renderStage(m) {
  const stage = $("stage");
  const showPicture = () => {
    stage.innerHTML = "";
    if (m.thumbnailUrl) {
      const img = document.createElement("img");
      img.className = "poster";
      img.src = m.thumbnailUrl;
      img.alt = m.name;
      stage.appendChild(img);
      stage.appendChild(chip("Open in Cornucopia to see it in 3D"));
    } else {
      stage.innerHTML = `<div class="stage-empty"><img src="./brand/cornucopia-mark.svg" alt="" /></div>`;
    }
  };

  if (!m.glbUrl) { showPicture(); return; }

  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const mv = document.createElement("model-viewer");
  mv.setAttribute("src", m.glbUrl);
  if (m.thumbnailUrl) mv.setAttribute("poster", m.thumbnailUrl);
  mv.setAttribute("alt", `3D model of ${m.name}`);
  mv.setAttribute("camera-controls", "");
  mv.setAttribute("touch-action", "pan-y");
  mv.setAttribute("shadow-intensity", "1");
  mv.setAttribute("ar", "");
  mv.setAttribute("ar-modes", "webxr scene-viewer quick-look");
  if (!reduceMotion) mv.setAttribute("auto-rotate", "");

  const arButton = document.createElement("button");
  arButton.slot = "ar-button";
  arButton.className = "ar-btn";
  arButton.type = "button";
  arButton.textContent = "View in your space";
  mv.appendChild(arButton);

  mv.addEventListener("error", (e) => {
    console.warn("3D view unavailable, showing the picture instead:", e.detail);
    showPicture();
  });
  mv.addEventListener("load", () => stage.appendChild(chip("Drag to turn it around")), { once: true });

  stage.innerHTML = "";
  stage.appendChild(mv);
}

function chip(text) {
  const c = document.createElement("span");
  c.className = "stage-chip";
  c.textContent = text;
  return c;
}

/** Launches the app on this product. Android: an intent URL, which Chrome
 * turns into "open the app" when it's installed and otherwise falls back to
 * the Play Store (or back here). iOS/other: the custom scheme directly. If the
 * page is still in front a moment later, the app evidently isn't installed. */
function openInApp() {
  const key = encodeURIComponent(modelKey);
  if (isAndroid) {
    const fallback = PLAY_STORE_URL || `${location.origin}${location.pathname}${location.search}#get-app`;
    location.href = `intent://model/${key}#Intent;scheme=${APP_SCHEME};package=${ANDROID_PACKAGE};`
      + `S.browser_fallback_url=${encodeURIComponent(fallback)};end`;
  } else {
    location.href = `${APP_SCHEME}://model/${key}`;
  }
  setTimeout(() => {
    if (document.visibilityState === "visible") {
      $("installHint").hidden = false;
      $("get-app").scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
  }, 1600);
}

function renderStores() {
  const stores = [];
  if (APP_STORE_URL) stores.push({ label: "App Store", url: APP_STORE_URL, mine: isIOS });
  if (PLAY_STORE_URL) stores.push({ label: "Google Play", url: PLAY_STORE_URL, mine: isAndroid });
  stores.sort((a, b) => Number(b.mine) - Number(a.mine)); // this device's store first

  const box = $("stores");
  box.innerHTML = "";
  stores.forEach((s) => {
    const a = document.createElement("a");
    a.className = "btn btn-store";
    a.href = s.url;
    a.rel = "noopener";
    a.textContent = s.label;
    box.appendChild(a);
  });
  $("storesSoon").hidden = stores.length > 0;
}

function showMessage(title, body, keepAppCard = false) {
  $("product").hidden = !keepAppCard;
  if (keepAppCard) {
    // Keep only the get-the-app card under the message.
    $("stage").hidden = true;
    $("product").querySelector(".card:not(.app)").hidden = true;
    $("openApp").hidden = true; // nothing to open - just the get-the-app pitch
    $("product").setAttribute("aria-busy", "false");
  }
  $("messageTitle").textContent = title;
  $("messageBody").textContent = body;
  $("message").hidden = false;
  document.title = `${title} — Cornucopia`;
}

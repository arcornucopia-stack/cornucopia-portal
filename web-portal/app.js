import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import {
  getAuth,
  signInWithEmailAndPassword,
  onAuthStateChanged,
  signOut
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";
import {
  getDatabase,
  ref as dbRef,
  child,
  get,
  set,
  update,
  push
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-database.js";
import {
  getStorage,
  ref,
  uploadBytesResumable,
  getDownloadURL
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-storage.js";

const ROOT = "cornucopia";

const config = window.CORNUCOPIA_FIREBASE_CONFIG;
if (!config || !config.projectId) {
  alert("Missing Firebase config. Create web-portal/firebase-config.js first.");
  throw new Error("Missing Firebase config");
}

const app = initializeApp(config);
const auth = getAuth(app);
const db = getDatabase(app);
const storage = getStorage(app);

const emailjsConfig = window.CORNUCOPIA_EMAILJS_CONFIG;
const emailjsReady = !!(emailjsConfig?.publicKey && !emailjsConfig.publicKey.startsWith("YOUR_") && window.emailjs);
if (emailjsReady) {
  try { window.emailjs.init({ publicKey: emailjsConfig.publicKey }); }
  catch (err) { console.warn("[Cornucopia] EmailJS init failed:", err); }
}

// ── Version stamp – if you don't see this line in the Console after page load,
//    the browser is still serving old cached code. Hard-reload with Ctrl+Shift+R.
console.log("%c[Cornucopia] app.js v11 loaded ✓", "color:green;font-weight:bold;font-size:14px");

const authScreen = byId("authScreen");
const appScreen = byId("appScreen");
const emailInput = byId("emailInput");
const passwordInput = byId("passwordInput");
const loginButton = byId("loginButton");
const logoutButton = byId("logoutButton");
const authMessage = byId("authMessage");
const roleBadge = byId("roleBadge");

const businessNameInput = byId("businessNameInput");
const displayNameInput = byId("displayNameInput");
const questionInput = byId("questionInput");
const targetModeInput = byId("targetModeInput");
const targetUserIdsInput = byId("targetUserIdsInput");
const partnerSubscribersPanel = byId("partnerSubscribersPanel");
const partnerSubscribersList = byId("partnerSubscribersList");
const glbInput = byId("glbInput");
const uploadButton = byId("uploadButton");
const uploadProgress = byId("uploadProgress");
const uploadMessage = byId("uploadMessage");
const uploadSectionTitle = byId("uploadSectionTitle");
const mySubmissionsTitle = byId("mySubmissionsTitle");

const mySubmissionsBody = byId("mySubmissionsBody");
const uploadsNavButton = byId("uploadsNavButton");
const publishedModelsBody = byId("publishedModelsBody");
const dispatchModelSelect = byId("dispatchModelSelect");
const dispatchUsersList = byId("dispatchUsersList");
const refreshDispatchDataButton = byId("refreshDispatchDataButton");
const sendModelToUsersButton = byId("sendModelToUsersButton");
const dispatchMessage = byId("dispatchMessage");
const partnerApprovedModelSelect = byId("partnerApprovedModelSelect");
const partnerDeliveryUsersList = byId("partnerDeliveryUsersList");
const refreshPartnerDeliveryButton = byId("refreshPartnerDeliveryButton");
const sendPartnerModelButton = byId("sendPartnerModelButton");
const partnerDeliveryMessage = byId("partnerDeliveryMessage");
const partnerSelectForSubscribers = byId("partnerSelectForSubscribers");
const availableUsersForPartnerList = byId("availableUsersForPartnerList");
const refreshPartnerSubscribersButton = byId("refreshPartnerSubscribersButton");
const savePartnerSubscribersButton = byId("savePartnerSubscribersButton");
const partnerSubscribersMessage = byId("partnerSubscribersMessage");
const mailingListCsvInput = byId("mailingListCsvInput");
const mailingListUploadButton = byId("mailingListUploadButton");
const mailingListUploadMessage = byId("mailingListUploadMessage");
const mailingListCountText = byId("mailingListCountText");
const mailingListBody = byId("mailingListBody");
const MAILING_LIST_CAP = 1000;
let mailingListCache = {};

const categoryFilterSelect = byId("categoryFilterSelect");
const newCategoryInput = byId("newCategoryInput");
const addCategoryButton = byId("addCategoryButton");
const categoriesList = byId("categoriesList");
const categoriesMessage = byId("categoriesMessage");
let categoriesCache = {};
let currentCategoryFilter = "";

const totalUploads = byId("totalUploads");
const approvedUploads = byId("approvedUploads");
const openCount = byId("openCount");
const saveCount = byId("saveCount");
const cardTotalUploads = byId("cardTotalUploads");
const cardApprovedUploads = byId("cardApprovedUploads");
const filterAllSubmissions = byId("filterAllSubmissions");
const filterApprovedSubmissions = byId("filterApprovedSubmissions");
const filterPendingSubmissions = byId("filterPendingSubmissions");
const MAX_UPLOAD_BYTES = 100 * 1024 * 1024;

let currentUser = null;
let currentProfile = null;
let submissionsCache = [];
let currentSubmissionFilter = "all";
let currentDetailSubmissionId = null;

bindNavigation();
bindModals();

loginButton.addEventListener("click", async () => {
  authMessage.textContent = "Signing in...";
  try {
    await signInWithEmailAndPassword(auth, emailInput.value.trim(), passwordInput.value);
    authMessage.textContent = "";
  } catch (err) {
    authMessage.textContent = err.message;
  }
});

logoutButton.addEventListener("click", () => signOut(auth));
uploadButton.addEventListener("click", uploadModel);
refreshDispatchDataButton?.addEventListener("click", loadDispatchData);
sendModelToUsersButton?.addEventListener("click", sendModelToSelectedUsers);
refreshPartnerDeliveryButton?.addEventListener("click", loadPartnerDeliveryData);
sendPartnerModelButton?.addEventListener("click", sendApprovedPartnerModelToUsers);
refreshPartnerSubscribersButton?.addEventListener("click", loadPartnerSubscriptionAdminData);
savePartnerSubscribersButton?.addEventListener("click", savePartnerSubscribersMapping);
mailingListUploadButton?.addEventListener("click", handleMailingListUpload);
addCategoryButton?.addEventListener("click", addCategory);
categoryFilterSelect?.addEventListener("change", () => {
  currentCategoryFilter = categoryFilterSelect.value;
  renderSubmissionRows();
});
byId("modelPageSaveCategoryButton")?.addEventListener("click", saveModelPageCategory);
partnerSelectForSubscribers?.addEventListener("change", syncPartnerSubscribersSelection);
byId("cardTotalUploads")?.addEventListener("click", () => { setActiveScreen("uploads"); setSubmissionFilter("all"); });
byId("cardApprovedUploads")?.addEventListener("click", () => { setActiveScreen("uploads"); setSubmissionFilter("approved"); });
byId("cardOpenCount")?.addEventListener("click", () => { setActiveScreen("uploads"); setSubmissionFilter("all"); });
byId("cardSaveCount")?.addEventListener("click", () => { setActiveScreen("uploads"); setSubmissionFilter("all"); });
// Clicking Opens/Saves hint text is handled in the card — both go to uploads so user can click a model for analytics
filterAllSubmissions?.addEventListener("click", () => setSubmissionFilter("all"));
filterApprovedSubmissions?.addEventListener("click", () => setSubmissionFilter("approved"));
filterPendingSubmissions?.addEventListener("click", () => setSubmissionFilter("pending"));

targetModeInput?.addEventListener("change", () => {
  const isSpecific = targetModeInput.value === "specific_users";
  if (targetUserIdsInput) targetUserIdsInput.style.display = isSpecific ? "block" : "none";
});

document.addEventListener("keydown", (e) => {
  if (e.key !== "Escape") return;
  if (!byId("analyticsModal")?.classList.contains("hidden")) closeAnalytics();
  else if (!byId("detailsModal")?.classList.contains("hidden")) closeDetailsModal();
  else if (!byId("modelDetailModal")?.classList.contains("hidden")) closeModelDetail();
  else if (!byId("uploadModal")?.classList.contains("hidden")) closeUploadModal();
});

function bindModals() {
  // Upload modal – open
  byId("openUploadModal")?.addEventListener("click", () => {
    byId("uploadModal")?.classList.remove("hidden");
  });
  // Upload modal – Cancel and Done replace the old X button.
  // Pass force=true so they can close the modal even while step 2 is showing.
  byId("cancelUploadModal")?.addEventListener("click", () => closeUploadModal(true));
  byId("doneUploadModal")?.addEventListener("click", () => closeUploadModal(true));
  // Overlay click still closes
  byId("uploadModal")?.addEventListener("click", (e) => {
    if (e.target === byId("uploadModal")) closeUploadModal();
  });

  // Upload modal – step 2 question type toggle (MC options)
  byId("uploadQuestionTypeSelect")?.addEventListener("change", () => {
    const isMc = byId("uploadQuestionTypeSelect").value === "multiple_choice";
    byId("uploadMcOptionsContainer")?.classList.toggle("hidden", !isMc);
  });
  // Upload modal – add MC option row
  byId("uploadAddMcOption")?.addEventListener("click", () => {
    const list = byId("uploadMcOptionsList");
    if (!list) return;
    const inp = document.createElement("input");
    inp.className = "mc-option";
    inp.placeholder = `Option ${list.querySelectorAll(".mc-option").length + 1}`;
    list.appendChild(inp);
  });
  // Upload modal – add question to newly uploaded model
  byId("uploadAddQuestionButton")?.addEventListener("click", addQuestionToUploadModal);

  // Model detail modal
  byId("closeModelDetail")?.addEventListener("click", closeModelDetail);
  byId("modelDetailModal")?.addEventListener("click", (e) => {
    if (e.target === byId("modelDetailModal")) closeModelDetail();
  });

  // Multiple choice option toggle
  byId("questionTypeSelect")?.addEventListener("change", () => {
    const isMc = byId("questionTypeSelect").value === "multiple_choice";
    byId("mcOptionsContainer")?.classList.toggle("hidden", !isMc);
  });

  // Add MC option
  byId("addMcOption")?.addEventListener("click", () => {
    const list = byId("mcOptionsList");
    const inp = document.createElement("input");
    inp.className = "mc-option";
    inp.placeholder = `Option ${list.querySelectorAll(".mc-option").length + 1}`;
    list.appendChild(inp);
  });

  // Add question
  byId("addQuestionButton")?.addEventListener("click", addQuestionToModel);

  // Done button closes model detail
  byId("doneModelDetail")?.addEventListener("click", closeModelDetail);

  // Analytics modal
  byId("closeAnalytics")?.addEventListener("click", closeAnalytics);
  byId("analyticsModal")?.addEventListener("click", (e) => {
    if (e.target === byId("analyticsModal")) closeAnalytics();
  });

  // Details modal
  byId("closeDetails")?.addEventListener("click", closeDetailsModal);
  byId("detailsModal")?.addEventListener("click", (e) => {
    if (e.target === byId("detailsModal")) closeDetailsModal();
  });
  byId("addAttributeRow")?.addEventListener("click", addAttributeRow);
  byId("saveDetails")?.addEventListener("click", saveModelDetails);

  // Model page – back button
  byId("backToUploads")?.addEventListener("click", () => {
    setActiveScreen("uploads");
    _modelPageItem = null;
  });

  // Model page – add question (this is what the submissions table's "Edit"
  // button actually lands on, so a model with 0 questions at upload time can
  // still get one added later)
  byId("modelPageQuestionTypeSelect")?.addEventListener("change", () => {
    const isMc = byId("modelPageQuestionTypeSelect").value === "multiple_choice";
    byId("modelPageMcOptionsContainer")?.classList.toggle("hidden", !isMc);
  });
  byId("modelPageAddMcOption")?.addEventListener("click", () => {
    const list = byId("modelPageMcOptionsList");
    if (!list) return;
    const inp = document.createElement("input");
    inp.className = "mc-option";
    inp.placeholder = `Option ${list.querySelectorAll(".mc-option").length + 1}`;
    list.appendChild(inp);
  });
  byId("modelPageAddQuestionButton")?.addEventListener("click", addQuestionToModelPage);


}

function closeAnalytics() {
  byId("analyticsModal")?.classList.add("hidden");
}

async function renderQuestionAnalytics(item, globalYes, globalNo, opens, sent) {
  const section = byId("questionAnalyticsSection");
  const list = byId("questionAnalyticsList");
  if (!section || !list) return;

  // Load questions from submission
  const questionsSnap = await get(dbRef(db, `${ROOT}/submissions/${item.id}/questions`));
  if (!questionsSnap.exists()) { section.style.display = "none"; return; }

  const questions = Object.values(questionsSnap.val());
  if (!questions.length) { section.style.display = "none"; return; }

  section.style.display = "";
  list.innerHTML = "";

  const typeLabels = { yes_no: "Yes / No", multiple_choice: "Multiple Choice", rating: "Rating", open_text: "Open Text" };

  questions.forEach((q) => {
    const card = document.createElement("div");
    card.className = "q-analytics-card";
    card.innerHTML = `<div class="q-analytics-header">
      <span class="question-type-badge qtype-${q.type}">${typeLabels[q.type] || q.type}</span>
      <p class="q-analytics-text">${escapeHtml(q.text || "")}</p>
    </div>`;

    const body = document.createElement("div");
    body.className = "q-analytics-body";

    if (q.type === "yes_no") {
      // Use global yes/no since the app maps to a single question
      const total = globalYes + globalNo || 1;
      const yesPct = Math.round((globalYes / total) * 100);
      const noPct = Math.round((globalNo / total) * 100);
      body.innerHTML = `
        <div class="vote-bar-row" style="margin-top:8px">
          <span class="vote-label yes-label">${globalYes} Yes (${yesPct}%)</span>
          <div class="vote-bar-track">
            <div class="vote-bar-yes" style="width:${yesPct}%"></div>
            <div class="vote-bar-no" style="width:${noPct}%"></div>
          </div>
          <span class="vote-label no-label">${globalNo} No (${noPct}%)</span>
        </div>
        <p class="muted" style="font-size:12px;margin-top:6px">${opens} total response${opens !== 1 ? "s" : ""} out of ${sent || "?"} delivered</p>`;
    } else if (q.type === "rating") {
      body.innerHTML = `<p class="muted" style="font-size:13px;margin-top:6px">Rating responses tracked globally — see overall rating above.</p>`;
    } else if (q.type === "multiple_choice") {
      const opts = Array.isArray(q.options) ? q.options : [];
      body.innerHTML = `<p class="muted" style="font-size:13px;margin-top:6px">Per-option tracking coming soon. ${opts.length} option${opts.length !== 1 ? "s" : ""}: ${opts.map(o => escapeHtml(o)).join(", ")}</p>`;
    } else if (q.type === "open_text") {
      body.innerHTML = `<p class="muted" style="font-size:13px;margin-top:6px">Open text responses are not aggregated automatically.</p>`;
    }

    card.appendChild(body);
    list.appendChild(card);
  });
}

function closeDetailsModal() {
  byId("detailsModal")?.classList.add("hidden");
  _detailsSubmissionId = null;
}

function addSelectAll(listEl) {
  const checkboxes = listEl.querySelectorAll("input[type='checkbox']");
  if (!checkboxes.length) return;
  const wrapper = document.createElement("label");
  wrapper.className = "user-row select-all-row";
  wrapper.innerHTML = `<input type="checkbox" class="select-all-cb" /> <span><strong>Select all</strong></span>`;
  listEl.prepend(wrapper);
  const cb = wrapper.querySelector(".select-all-cb");
  cb.addEventListener("change", () => {
    listEl.querySelectorAll("input[type='checkbox']:not(.select-all-cb)").forEach((x) => { x.checked = cb.checked; });
  });
  checkboxes.forEach((x) => x.addEventListener("change", () => {
    const all = [...listEl.querySelectorAll("input[type='checkbox']:not(.select-all-cb)")];
    cb.checked = all.every((c) => c.checked);
    cb.indeterminate = !cb.checked && all.some((c) => c.checked);
  }));
}

// ─── Portal Notification System ─────────────────────────────────────────────

async function sendPortalNotification(toUid, message, type = "info", screen = "uploads", filter = null) {
  const ref = push(dbRef(db, `${ROOT}/portal_notifications/${toUid}`));
  const payload = { message, type, screen, createdAt: Date.now(), read: false };
  if (filter) payload.filter = filter;
  await set(ref, payload);
}

async function notifyAllAdmins(message, type = "info", screen = "uploads", filter = null) {
  try {
    const usersSnap = await get(dbRef(db, `${ROOT}/users`));
    if (!usersSnap.exists()) return;
    const promises = [];
    usersSnap.forEach(child => {
      const role = String(child.val()?.role || "").toLowerCase();
      if (role === "admin") promises.push(sendPortalNotification(child.key, message, type, screen, filter));
    });
    await Promise.all(promises);
  } catch (err) { console.warn("Could not notify admins:", err); }
}

let _notifDropdownOpen = false;
let _bellBound = false;

function bindNotificationBell() {
  if (_bellBound) return; // prevent duplicate binding on re-auth
  const bell = byId("bellBtn");
  const dropdown = byId("notifDropdown");
  if (!bell || !dropdown) return;
  _bellBound = true;

  bell.addEventListener("click", (e) => {
    e.stopPropagation();
    _notifDropdownOpen = !_notifDropdownOpen;
    dropdown.classList.toggle("hidden", !_notifDropdownOpen);
    if (_notifDropdownOpen) loadPortalNotifications();
  });

  document.addEventListener("click", (e) => {
    if (_notifDropdownOpen && !dropdown.contains(e.target) && e.target !== bell) {
      _notifDropdownOpen = false;
      dropdown.classList.add("hidden");
    }
  });

  byId("markAllRead")?.addEventListener("click", async () => {
    if (!currentUser) return;
    const snap = await get(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}`));
    if (snap.exists()) {
      const updates = {};
      snap.forEach(child => { updates[child.key + "/read"] = true; });
      await update(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}`), updates);
    }
    byId("notifBadge")?.classList.add("hidden");
    loadPortalNotifications();
  });
}

async function loadPortalNotifications() {
  const list = byId("notifDropdownList");
  const badge = byId("notifBadge");
  if (!currentUser) return;
  try {
    const snap = await get(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}`));
    const items = snap.exists()
      ? Object.entries(snap.val()).sort((a, b) => (b[1].createdAt || 0) - (a[1].createdAt || 0)).slice(0, 20)
      : [];
    const unread = items.filter(([, n]) => !n.read).length;

    if (badge) {
      badge.textContent = unread > 99 ? "99+" : String(unread);
      badge.classList.toggle("hidden", unread === 0);
    }

    if (!list) return;
    list.innerHTML = "";
    if (!items.length) {
      list.innerHTML = "<p class='muted' style='padding:16px'>No notifications yet.</p>";
      return;
    }

    items.forEach(([id, n]) => {
      const div = document.createElement("div");
      div.className = `notif-item${n.read ? "" : " notif-unread"}`;

      // All notifications link to the relevant screen
      const targetScreen = n.screen || "uploads";
      div.innerHTML = `
        <span class="notif-dot notif-dot-${n.type || "info"}"></span>
        <div class="notif-body">
          <p>${escapeHtml(n.message)}</p>
          <div class="notif-footer">
            <small class="muted">${formatTs(n.createdAt)}</small>
            <a class="notif-link">View →</a>
          </div>
        </div>
      `;

      // Click anywhere on the item: mark read + navigate
      div.addEventListener("click", async () => {
        await set(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}/${id}/read`), true);
        div.classList.remove("notif-unread");
        const cur = parseInt(badge?.textContent) || 0;
        if (badge && cur > 0) {
          const next = cur - 1;
          badge.textContent = String(next);
          badge.classList.toggle("hidden", next === 0);
        }
        _notifDropdownOpen = false;
        byId("notifDropdown")?.classList.add("hidden");
        setActiveScreen(targetScreen);
        if (targetScreen === "uploads") setSubmissionFilter(n.filter || "all");
      });

      list.appendChild(div);
    });
  } catch (err) {
    if (list) list.innerHTML = `<p class='muted' style='padding:16px'>Could not load: ${err.message}</p>`;
  }
}

function showToast(message, type = "success") {
  const toast = byId("toastNotification");
  if (!toast) return;
  toast.textContent = message;
  toast.className = `toast toast-${type}`;
  toast.classList.remove("hidden");
  setTimeout(() => toast.classList.add("hidden"), 5000);
}

/**
 * Close the upload modal.
 * @param {boolean} force  When true, close even if step 2 is showing (used by Done / Cancel).
 *                         When false (default), the close is blocked while step 2 is active so
 *                         that Escape / overlay clicks cannot dismiss the question-entry screen.
 */
function closeUploadModal(force = false) {
  // Block any non-explicit close while the user is on step 2 (questions screen).
  // Escape, overlay click, and any background code all arrive here with force=false.
  const step2 = byId("uploadStep2");
  if (!force && step2?.style.display === "grid") {
    console.log("[Cornucopia] closeUploadModal blocked – still on step 2");
    return;
  }
  console.trace("[Cornucopia] closeUploadModal executing (force=" + force + ")");
  byId("uploadModal")?.classList.add("hidden");
  if (uploadProgress) uploadProgress.value = 0;
  if (uploadMessage) uploadMessage.textContent = "";
  if (displayNameInput) displayNameInput.value = "";
  const descInput = byId("modelDescriptionInput");
  if (descInput) descInput.value = "";
  if (glbInput) glbInput.value = "";

  // Reset two-step state so the modal is fresh on next open
  const step1 = byId("uploadStep1");
  // step2 already declared above for the early-return guard — reuse it
  const wrapper = byId("uploadStepsWrapper");
  if (step1) { step1.style.cssText = ""; step1.classList.remove("upload-step-exiting"); }
  if (step2) { step2.style.cssText = "display:none"; step2.classList.remove("upload-step-entering"); }
  if (wrapper) { wrapper.style.height = ""; wrapper.classList.remove("is-transitioning"); }
  const titleEl = byId("uploadModalTitle");
  if (titleEl) titleEl.textContent = "New Upload";
  byId("doneUploadModal")?.classList.add("hidden");
  const qtInput = byId("uploadQuestionTextInput");
  if (qtInput) qtInput.value = "";
  const qMsg = byId("uploadQuestionMessage");
  if (qMsg) qMsg.textContent = "";
  byId("uploadMcOptionsContainer")?.classList.add("hidden");
  const typeSelect = byId("uploadQuestionTypeSelect");
  if (typeSelect) typeSelect.value = "yes_no";
  const mcList = byId("uploadMcOptionsList");
  if (mcList) mcList.innerHTML = '<input class="mc-option" placeholder="Option 1" /><input class="mc-option" placeholder="Option 2" />';
}

// ─── Upload modal two-step transition ────────────────────────────────────────

/**
 * After a successful upload, slide step 2 (question input) up into the modal
 * within the same paint cycle — no multi-phase delays that background work
 * could interrupt.
 */
function transitionUploadToQuestions(submissionId) {
  console.log("[Cornucopia] transitionUploadToQuestions called, submissionId =", submissionId);
  // Guarantee the modal is open no matter what
  byId("uploadModal")?.classList.remove("hidden");

  currentDetailSubmissionId = submissionId;

  const wrapper = byId("uploadStepsWrapper");
  const step1   = byId("uploadStep1");
  const step2   = byId("uploadStep2");
  if (!wrapper || !step1 || !step2) return;

  // Seed step 2 content while it is still invisible
  renderQuestionsInUploadModal({});
  const qtInput    = byId("uploadQuestionTextInput");
  const qMsg       = byId("uploadQuestionMessage");
  const typeSelect = byId("uploadQuestionTypeSelect");
  if (qtInput)    qtInput.value = "";
  if (qMsg)       qMsg.textContent = "";
  if (typeSelect) typeSelect.value = "yes_no";
  byId("uploadMcOptionsContainer")?.classList.add("hidden");
  byId("uploadModalTitle") && (byId("uploadModalTitle").textContent = "Add Questions");

  // ── Instant swap: hide step 1, place step 2 just below viewport ──
  step1.style.display  = "none";
  step2.style.display  = "grid";
  step2.style.opacity  = "0";
  step2.style.transform = "translateY(36px)";
  wrapper.classList.add("is-transitioning");

  // Force the browser to commit step 2's off-screen position before we
  // start the transition (a single reflow is all we need)
  void step2.offsetHeight;

  // Animate step 2 into its natural position in one smooth motion
  step2.style.transition = "transform 0.36s cubic-bezier(0.4,0,0.2,1), opacity 0.3s ease";
  step2.style.transform  = "translateY(0)";
  step2.style.opacity    = "1";

  // After the animation settles, strip the inline transition properties,
  // let the wrapper grow freely again (so adding several multiple-choice
  // options doesn't clip the Add Question button off the bottom), and
  // reveal the Done button
  setTimeout(() => {
    step2.style.transition = "";
    step2.style.transform  = "";
    step2.style.opacity    = "";
    wrapper.classList.remove("is-transitioning");
    byId("doneUploadModal")?.classList.remove("hidden");
  }, 400);
}

function renderQuestionsInUploadModal(questions) {
  const list = byId("uploadQuestionsList");
  if (!list) return;
  list.innerHTML = "";
  const entries = Object.entries(questions);
  if (!entries.length) {
    list.innerHTML = `<div class="empty-questions">No questions yet. Add one below to start collecting feedback.</div>`;
    return;
  }
  const typeLabels = { yes_no: "Yes / No", multiple_choice: "Multiple Choice", rating: "Rating (1 – 5)", open_text: "Open Text" };
  entries.forEach(([qId, q]) => {
    const card = document.createElement("div");
    card.className = "question-card";
    const optionsHtml = (q.type === "multiple_choice" && Array.isArray(q.options) && q.options.length)
      ? `<ul class="question-options">${q.options.map((o) => `<li>${escapeHtml(o)}</li>`).join("")}</ul>`
      : q.type === "rating"
        ? `<ul class="question-options"><li>1</li><li>2</li><li>3</li><li>4</li><li>5</li></ul>`
        : "";
    card.innerHTML = `
      <div class="question-card-body">
        <span class="question-type-badge qtype-${q.type}">${typeLabels[q.type] || q.type}</span>
        <p style="margin-top:6px">${escapeHtml(q.text || "")}</p>
        ${optionsHtml}
      </div>
      <button class="delete-question-btn" data-qid="${qId}" title="Remove question">✕</button>
    `;
    card.querySelector(".delete-question-btn").addEventListener("click", () => deleteQuestionFromUploadModal(qId));
    list.appendChild(card);
  });
}

async function addQuestionToUploadModal() {
  if (!currentDetailSubmissionId) return;
  const type    = byId("uploadQuestionTypeSelect")?.value;
  const text    = (byId("uploadQuestionTextInput")?.value || "").trim();
  const msgEl   = byId("uploadQuestionMessage");

  if (!text) { if (msgEl) msgEl.textContent = "Question text is required."; return; }

  let options = [];
  if (type === "multiple_choice") {
    options = [...(byId("uploadMcOptionsList")?.querySelectorAll(".mc-option") || [])]
      .map((el) => el.value.trim()).filter(Boolean);
    if (options.length < 2) { if (msgEl) msgEl.textContent = "Add at least 2 options."; return; }
  }

  const questionRef = push(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
  await set(questionRef, { type, text, options, createdAt: Date.now() });

  if (msgEl) msgEl.textContent = "";
  const textInput = byId("uploadQuestionTextInput");
  if (textInput) textInput.value = "";

  const snap = await get(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
  renderQuestionsInUploadModal(snap.exists() ? snap.val() : {});

  // Keep the submissions table question count in sync
  const cached = submissionsCache.find((x) => x.id === currentDetailSubmissionId);
  if (cached) { cached.questions = snap.exists() ? snap.val() : {}; renderSubmissionRows(); }
}

async function deleteQuestionFromUploadModal(questionId) {
  if (!currentDetailSubmissionId) return;
  try {
    await set(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions/${questionId}`), null);
    const snap = await get(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
    renderQuestionsInUploadModal(snap.exists() ? snap.val() : {});
    const cached = submissionsCache.find((x) => x.id === currentDetailSubmissionId);
    if (cached) { cached.questions = snap.exists() ? snap.val() : {}; renderSubmissionRows(); }
  } catch (err) {
    const msgEl = byId("uploadQuestionMessage");
    if (msgEl) msgEl.textContent = `Could not delete question: ${err.message || err}`;
  }
}

// ─────────────────────────────────────────────────────────────────────────────

function closeModelDetail() {
  byId("modelDetailModal")?.classList.add("hidden");
  currentDetailSubmissionId = null;
}
targetModeInput?.dispatchEvent(new Event("change"));

onAuthStateChanged(auth, async (user) => {
  currentUser = user;

  if (!user) {
    authScreen.classList.remove("hidden");
    appScreen.classList.add("hidden");
    document.querySelector(".shell")?.classList.add("auth-mode");
    currentProfile = null;
    return;
  }
  document.querySelector(".shell")?.classList.remove("auth-mode");
  bindNotificationBell();

  const profileSnap = await get(child(dbRef(db), `${ROOT}/users/${user.uid}`));
  if (!profileSnap.exists()) {
    authMessage.textContent = `No user role found. Create ${ROOT}/users/{uid} in Realtime Database.`;
    await signOut(auth);
    return;
  }

  currentProfile = profileSnap.val();
  const normalizedRole = normalizeRole(currentProfile.role);
  roleBadge.textContent = normalizedRole ? normalizedRole.toUpperCase() : "UNASSIGNED";
  authScreen.classList.add("hidden");
  appScreen.classList.remove("hidden");

  // Show company name in sidebar
  const companyName = currentProfile.businessName || currentProfile.name || "";
  const sidebarCompany = byId("sidebarCompany");
  if (sidebarCompany) {
    sidebarCompany.textContent = companyName;
    sidebarCompany.classList.toggle("hidden", !companyName);
  }

  // Show admin business column in submissions table
  const isAdmin = normalizedRole === "admin";
  document.querySelectorAll(".admin-only-th").forEach((th) => {
    // Must use "table-cell", not "" — the CSS default is display:none so
    // clearing the inline style would leave the CSS rule in effect.
    th.style.display = isAdmin ? "table-cell" : "none";
  });

  const uploadsLabel = isAdmin ? "Manage Uploads" : "My Uploads";
  if (uploadsNavButton) uploadsNavButton.textContent = uploadsLabel;
  const backToUploadsBtn = byId("backToUploads");
  if (backToUploadsBtn) backToUploadsBtn.textContent = `← ${uploadsLabel}`;
  setAdminVisibility(isAdmin);
  setUploadUIForRole(isAdmin);
  setSubmissionFilter("all");
  setActiveScreen("dashboard");
  await refreshAll();
});

async function refreshAll() {
  await Promise.allSettled([
    loadMySubmissions(),
    loadAnalytics(),
    loadPublishedModels(),
    loadDispatchData(),
    loadPartnerSubscriptionAdminData(),
    loadPartnerSubscribersForUpload(),
    loadPartnerDeliveryData(),
    loadPortalNotifications(),
    loadMailingList(),
    loadCategories()
  ]);
}

// ─── Thumbnail generation ────────────────────────────────────────────────────

/**
 * After a GLB upload completes, load it in the off-screen <model-viewer>,
 * capture a PNG via toBlob(), upload to Firebase Storage, and save the URL.
 * This runs fire-and-forget — failures are logged but never shown to the user.
 */
/**
 * glbFile — the original File object (already in memory).
 * Using URL.createObjectURL avoids Firebase Storage CORS entirely.
 */
async function generateAndUploadThumbnail(submissionId, businessId, glbFile) {
  console.log("[Cornucopia] 🖼 Thumbnail generation starting for", submissionId);
  const objectUrl = URL.createObjectURL(glbFile);
  try {
    // ① model-viewer element
    const mv = byId("thumbnailGenerator");
    if (!mv) { console.warn("[Cornucopia] #thumbnailGenerator element not found"); return; }
    console.log("[Cornucopia] 🖼 Step 1: waiting for model-viewer custom element...");

    // Wait up to 10 s for the CDN module to register the custom element
    await Promise.race([
      customElements.whenDefined("model-viewer"),
      new Promise((_, reject) => setTimeout(() => reject(new Error("model-viewer CE timeout")), 10000))
    ]);
    console.log("[Cornucopia] 🖼 Step 2: custom element ready, toBlob =", typeof mv.toBlob);

    if (typeof mv.toBlob !== "function") {
      console.warn("[Cornucopia] toBlob not a function — model-viewer version may not support it");
      return;
    }

    // ② Load GLB via local blob URL (no CORS, no auth token needed)
    // Scale the timeout with file size - a flat 30s was tuned for small test
    // files and timed out on a real 34MB upload. 30s floor, ~2s/MB, 120s cap
    // so a genuinely broken file still fails in reasonable time.
    const fileSizeMB = glbFile.size / (1024 * 1024);
    const loadTimeoutMs = Math.round(Math.max(30000, Math.min(120000, 15000 + fileSizeMB * 2000)));
    console.log(`[Cornucopia] 🖼 Step 3: loading GLB into model-viewer... (${fileSizeMB.toFixed(1)} MB, timeout ${loadTimeoutMs}ms)`);
    await new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        mv.removeEventListener("load", onLoad);
        mv.removeEventListener("error", onError);
        reject(new Error(`model-viewer GLB load timeout (${Math.round(loadTimeoutMs / 1000)}s, file was ${fileSizeMB.toFixed(1)} MB)`));
      }, loadTimeoutMs);
      const onLoad  = () => { clearTimeout(timeout); mv.removeEventListener("load", onLoad); mv.removeEventListener("error", onError); resolve(); };
      const onError = (e) => { clearTimeout(timeout); mv.removeEventListener("load", onLoad); mv.removeEventListener("error", onError); reject(new Error("model-viewer error: " + (e?.detail?.type || e))); };
      mv.addEventListener("load",  onLoad);
      mv.addEventListener("error", onError);
      mv.src = objectUrl;
    });
    console.log("[Cornucopia] 🖼 Step 4: GLB loaded, waiting 1.2 s for renderer to settle...");

    // ③ Let the WebGL renderer finish (textures, lighting)
    await new Promise((r) => setTimeout(r, 1200));

    // ④ Capture PNG
    console.log("[Cornucopia] 🖼 Step 5: calling toBlob()...");
    const blob = await mv.toBlob({ idealAspect: true });
    mv.src = ""; // free GPU memory
    if (!blob) throw new Error("toBlob() returned null");
    console.log("[Cornucopia] 🖼 Step 6: PNG blob size =", blob.size, "bytes");

    // ⑤ Upload PNG to Firebase Storage
    console.log("[Cornucopia] 🖼 Step 7: uploading PNG to Firebase Storage...");
    const thumbPath = `partner_uploads/${businessId}/${submissionId}/thumbnail.png`;
    const thumbStorageRef = ref(storage, thumbPath);
    const thumbTask = uploadBytesResumable(thumbStorageRef, blob, { contentType: "image/png" });
    await new Promise((resolve, reject) => thumbTask.on("state_changed", null, reject, resolve));
    const thumbUrl = await getDownloadURL(thumbStorageRef);
    console.log("[Cornucopia] ✓ Thumbnail saved:", thumbUrl);

    // ⑥ Save to submissions (portal reads from here — always allowed)
    await update(dbRef(db, `${ROOT}/submissions/${submissionId}`), {
      thumbnailUrl: thumbUrl,
      thumbnailError: null,
      thumbnailErrorAt: null
    });

    // ⑦ Live-update UI — do this before the optional models write so it
    //    always runs even if the DB rule below denies the secondary update
    const cached = submissionsCache.find((x) => x.id === submissionId);
    if (cached) { cached.thumbnailUrl = thumbUrl; renderSubmissionRows(); }
    if (currentDetailSubmissionId === submissionId) renderModelPageThumbnail(thumbUrl);

    // ⑧ Best-effort: mirror thumbnailUrl to models/{modelKey} for the Unity app.
    //    Wrapped in its own try-catch — partners may not have write access to
    //    models/ and that's fine; the portal uses submissions/ anyway.
    try {
      const subSnap = await get(dbRef(db, `${ROOT}/submissions/${submissionId}`));
      if (subSnap.exists() && subSnap.val().modelKey) {
        // Only update an existing record — update() on a missing path would
        // create a thumbnail-only stub that the apps render as a ghost product.
        const modelRef = dbRef(db, `${ROOT}/models/${subSnap.val().modelKey}`);
        const modelSnap = await get(modelRef);
        if (modelSnap.exists()) {
          await update(modelRef, { thumbnailUrl: thumbUrl });
          console.log("[Cornucopia] ✓ thumbnailUrl mirrored to models record");
        }
      }
    } catch (mirrorErr) {
      console.warn("[Cornucopia] Could not mirror thumbnailUrl to models/ (non-fatal):", mirrorErr.message);
    }

  } catch (err) {
    // Thumbnail generation is best-effort, but a silent console-only failure
    // let broken thumbnails go unnoticed for months. Surface it and record the
    // reason on the submission so it can be diagnosed after the fact.
    const reason = err?.message || String(err);
    console.error("[Cornucopia] ✗ Thumbnail generation failed:", reason);
    showToast(`Thumbnail could not be generated: ${reason}`, "error");
    try {
      await update(dbRef(db, `${ROOT}/submissions/${submissionId}`), {
        thumbnailError: reason,
        thumbnailErrorAt: Date.now()
      });
    } catch (_) { /* diagnostics only — never mask the original failure */ }
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
}

/** Show thumbnail image in the model page preview card, or fall back to placeholder. */
function renderModelPageThumbnail(url) {
  const img = byId("modelPageThumbnail");
  const placeholder = byId("modelPagePreviewPlaceholder");
  if (url && img) {
    img.src = url;
    img.classList.remove("hidden");
    if (placeholder) placeholder.style.display = "none";
  } else {
    if (img) img.classList.add("hidden");
    if (placeholder) placeholder.style.display = "";
  }
}

async function uploadModel() {
  if (!currentUser || !currentProfile) return;

  const file = glbInput.files?.[0];
  if (!file) {
    uploadMessage.textContent = "Please choose a .glb file before uploading.";
    return;
  }

  if (!file.name.toLowerCase().endsWith(".glb")) {
    uploadMessage.textContent = "Only .glb files are allowed.";
    return;
  }
  if (file.size > MAX_UPLOAD_BYTES) {
    uploadMessage.textContent = "File is too large. Max size is 100 MB.";
    return;
  }

  const businessId = currentProfile.businessId || currentUser.uid;
  const businessName = (currentProfile.businessName || currentProfile.name || businessId).trim();

  const isAdmin = (currentProfile.role || "").toLowerCase() === "admin";
  let targetMode = targetModeInput?.value || "all_users";
  let targetUserIds = parseTargetUserIds(targetUserIdsInput?.value || "");

  if (!isAdmin) {
    targetMode = "specific_users";
    targetUserIds = getCheckedPartnerSubscriberIds();
  } else if (targetMode === "specific_users" && targetUserIds.length === 0) {
    uploadMessage.textContent = "Specific users mode requires at least one user UID.";
    return;
  }

  uploadMessage.textContent = "Uploading...";
  uploadButton.disabled = true;

  const submissionRef = push(dbRef(db, `${ROOT}/submissions`));
  const submissionId = submissionRef.key;
  const baseName = stripGlbExtension(file.name);
  const modelKey = `${sanitizeKey(baseName)}_${String(Date.now()).slice(-6)}`;
  const storagePath = `partner_uploads/${businessId}/${submissionId}/${file.name}`;
  const storageRef = ref(storage, storagePath);

  const task = uploadBytesResumable(storageRef, file, { contentType: "model/gltf-binary" });
  task.on(
    "state_changed",
    (snapshot) => {
      const pct = Math.round((snapshot.bytesTransferred / snapshot.totalBytes) * 100);
      uploadProgress.value = pct;
    },
    (error) => {
      uploadButton.disabled = false;
      uploadMessage.textContent = `Upload failed: ${error.message}`;
    },
    async () => {
      try {
        await set(submissionRef, {
          submissionId,
          modelKey,
          businessId,
          businessName,
          uploaderUid: currentUser.uid,
          uploaderRole: currentProfile.role || "partner",
          fileName: file.name,
          displayName: (displayNameInput?.value || baseName).trim(),
          description: (byId("modelDescriptionInput")?.value || "").trim(),
          question: "",
          picPathh: sanitizeKey(baseName),
          storagePath,
          targetMode,
          targetUserIds,
          status: "pending",
          pushedToApp: false,
          pushedAt: null,
          pushedCount: 0,
          createdAt: Date.now(),
          approvedAt: null,
          rejectedAt: null,
          decisionBy: null
        });

        await upsertModelUploadTracker({
          modelKey,
          fileName: file.name,
          displayName: (displayNameInput.value || baseName).trim(),
          businessId,
          businessName,
          uploaderUid: currentUser.uid,
          uploaderRole: normalizeRole(currentProfile.role) || "partner",
          storagePath,
          status: "pending",
          approved: false,
          pushedToApp: false
        });

        if (normalizeRole(currentProfile.role) === "admin") {
          await updateSubmissionStatus(submissionId, "approved");
          await pushSubmissionToApp(submissionId, { silent: true });
        }

        const isAdminUpload = normalizeRole(currentProfile.role) === "admin";
        showToast(
          isAdminUpload
            ? "🎉 Model uploaded and published to the app!"
            : "🎉 Model uploaded successfully! Our team will review it shortly."
        );
        // Transition to step 2 immediately — before any data refresh so the
        // animation fires right away and nothing can close the modal first.
        transitionUploadToQuestions(submissionId);
        // Generate PNG thumbnail from the local File (no CORS, fire-and-forget)
        generateAndUploadThumbnail(submissionId, businessId, file);
        // Refresh data in the background (no await — runs while step 2 is shown)
        refreshAll();
        // Notify admins of new upload (partners only)
        if (!isAdminUpload) {
          const partnerName = currentProfile.businessName || currentProfile.name || businessId;
          notifyAllAdmins(`📦 ${partnerName} submitted a new model for review.`, "info", "uploads", "pending");
        }
      } catch (err) {
        uploadMessage.textContent = `Upload completed but publish step failed: ${err.message || err}`;
      } finally {
        uploadButton.disabled = false;
      }
    }
  );
}

async function loadMySubmissions() {
  submissionsCache = [];
  mySubmissionsBody.innerHTML = "";
  if (!currentUser || !currentProfile) return;
  try {
    const all = await getAllSubmissions();
    const businessId = currentProfile.businessId || currentUser.uid;
    const rows = normalizeRole(currentProfile.role) === "admin"
      ? all
      : all.filter((x) => x.businessId === businessId);

    rows.sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));

    let approved = 0;
    rows.forEach((item) => {
      if (item.status === "approved") approved += 1;
    });

    submissionsCache = rows;
    totalUploads.textContent = String(rows.length);
    approvedUploads.textContent = String(approved);
    renderSubmissionRows();
  } catch (err) {
    uploadMessage.textContent = `Could not load submissions: ${err.message || err}`;
  }
}

/**
 * Approve/Reject/Push to App now show up in two places (the merged Manage
 * Uploads table and the model detail page, per R-2.5) — both wire buttons
 * with data-id/data-action and call this shared handler rather than each
 * having their own copy of the confirm/loading/notify/refresh flow.
 */
function bindSubmissionActionButtons(container) {
  container.querySelectorAll("button[data-id][data-action]").forEach((btn) => {
    btn.onclick = () => runSubmissionAction(btn);
  });
}

async function runSubmissionAction(btn) {
  const id = btn.getAttribute("data-id");
  const action = btn.getAttribute("data-action");

  const confirmMessages = {
    approve: "Approve this submission and make it ready to push?",
    reject: "Reject this submission? This cannot be undone.",
    push: "Push this model to the app for users to see?"
  };
  const loadingLabels = { approve: "Approving…", reject: "Rejecting…", push: "Pushing to app…" };
  const successMessages = {
    approve: "✓ Submission approved.",
    reject: "✓ Submission rejected.",
    push: "🚀 Model pushed to the app!"
  };

  if (!confirm(confirmMessages[action])) return;

  const originalText = btn.textContent.trim();
  btn.textContent = loadingLabels[action];
  btn.disabled = true;
  const allBtns = btn.closest(".row-actions")?.querySelectorAll("button");
  allBtns?.forEach((b) => { b.disabled = true; });

  try {
    if (action === "approve") {
      await updateSubmissionStatus(id, "approved");
    } else if (action === "reject") {
      await updateSubmissionStatus(id, "rejected");
    } else if (action === "push") {
      await pushSubmissionToApp(id);
    }
    // Notify the partner who submitted
    try {
      const subSnap = await get(dbRef(db, `${ROOT}/submissions/${id}`));
      if (subSnap.exists()) {
        const sub = subSnap.val();
        const partnerUid = sub.uploaderUid;
        const modelName = sub.displayName || sub.fileName || "your model";
        const partnerMessages = {
          approve: `✅ "${modelName}" has been approved!`,
          reject: `❌ "${modelName}" was not approved this time.`,
          push: `🚀 "${modelName}" is now live in the app!`
        };
        if (partnerUid && partnerMessages[action]) {
          await sendPortalNotification(partnerUid, partnerMessages[action],
            action === "reject" ? "error" : "success");
        }
      }
    } catch (e) { console.warn("Could not notify partner:", e); }

    // Flash row before refresh — only applies when the button lives in a
    // table row; the model-page action card has no <tr> ancestor and just
    // skips straight to the refresh below.
    const row = btn.closest("tr");
    const flashClass = { approve: "row-flash-approved", reject: "row-flash-rejected", push: "row-flash-pushed" }[action];
    if (row && flashClass) {
      row.classList.add(flashClass);
      await new Promise((res) => setTimeout(res, action === "push" ? 1800 : 1400));
      row.classList.remove(flashClass);
    }
    showToast(successMessages[action]);
    await refreshAll();

    // If the model detail page is open on this exact submission, refresh it
    // in place so its status pill / stats / action-button states catch up.
    if (_modelPageItem?.id === id) {
      const updated = submissionsCache.find((s) => s.id === id);
      if (updated) await openModelPage(updated);
    }
  } catch (err) {
    showToast(`Action failed: ${err.message || err}`, "error");
    btn.textContent = originalText;
    allBtns?.forEach((b) => { b.disabled = false; });
  }
}

async function updateSubmissionStatus(id, status) {
  await update(dbRef(db, `${ROOT}/submissions/${id}`), {
    status,
    decisionBy: currentUser.uid,
    approvedAt: status === "approved" ? Date.now() : null,
    rejectedAt: status === "rejected" ? Date.now() : null
  });

  // Approval publishes the catalog record (see ensurePublishedModel) so the
  // partner can target-send it immediately. It does NOT assign it to any
  // user — that's still Push to App's job (broad) or the partner's own send
  // (targeted). Only admins can approve, and only admins may write
  // cornucopia/models per the RTDB rules, so this has to happen here rather
  // than in the partner-facing send actions themselves.
  if (status === "approved") {
    const submissionSnap = await get(dbRef(db, `${ROOT}/submissions/${id}`));
    if (submissionSnap.exists()) {
      await ensurePublishedModel(id, { id, ...submissionSnap.val() });
    }
  }

  const submissionSnap2 = await get(dbRef(db, `${ROOT}/submissions/${id}`));
  if (submissionSnap2.exists()) {
    const s = submissionSnap2.val();
    if (s?.modelKey) {
      await upsertModelUploadTracker({
        modelKey: s.modelKey,
        status,
        approved: status === "approved",
        decisionBy: currentUser.uid
      });
    }
  }
}

/** First question assigned to a submission, falling back to the default prompt. */
async function resolveSubmissionQuestion(submissionId, item) {
  let questionText = item.question || "";
  if (!questionText) {
    const questionsSnap = await get(dbRef(db, `${ROOT}/submissions/${submissionId}/questions`));
    if (questionsSnap.exists()) {
      const questions = Object.values(questionsSnap.val());
      const first = questions.find(q => q.type === "yes_no") || questions[0];
      if (first) questionText = first.text;
    }
  }
  return questionText || "Would you like this product?";
}

/** Creates or refreshes cornucopia/models/{modelKey} from a submission - the
 * one place that builds a real, complete catalog record (never a stub).
 * Used by approval (so partners can target-send right away) and by Push to
 * App (broad distribution). Returns the modelKey. */
async function ensurePublishedModel(submissionId, item) {
  const modelKey = item.modelKey || sanitizeKey(stripGlbExtension(item.fileName || `model_${submissionId}`));
  const modelRef = dbRef(db, `${ROOT}/models/${modelKey}`);
  const modelSnap = await get(modelRef);
  const existingModel = modelSnap.exists() ? modelSnap.val() : {};

  const mergedModel = {
    ...existingModel,
    name: item.displayName || stripGlbExtension(item.fileName || modelKey),
    modelNamee: modelKey,
    picPathh: item.picPathh || existingModel.picPathh || modelKey,
    description: item.description || existingModel.description || "",
    question: await resolveSubmissionQuestion(submissionId, item),
    // Lets the apps fetch the submission's full typed question set
    // (yes/no, multiple_choice, rating, open_text) instead of just the
    // flattened text string above.
    submissionId,
    storagePath: item.storagePath || "",
    businessName: item.businessName || existingModel.businessName || "",
    category: item.category || existingModel.category || null,
    categoryName: item.categoryName || existingModel.categoryName || null,
    // Newest-first ordering in the apps; keep the original date on re-push
    createdAt: existingModel.createdAt || Date.now(),
    data: {
      sent: toInt(existingModel?.data?.sent, 0),
      opens: toInt(existingModel?.data?.opens, 0),
      saved: toInt(existingModel?.data?.saved, 0),
      yes: toInt(existingModel?.data?.yes, 0),
      no: toInt(existingModel?.data?.no, 0),
      rating: String(existingModel?.data?.rating ?? "0.0")
    }
  };
  const thumbUrl = item.thumbnailUrl || existingModel.thumbnailUrl;
  if (thumbUrl) mergedModel.thumbnailUrl = thumbUrl;

  await set(modelRef, mergedModel);
  if (!item.modelKey) {
    await update(dbRef(db, `${ROOT}/submissions/${submissionId}`), { modelKey });
  }
  return modelKey;
}

async function pushSubmissionToApp(submissionId, options = {}) {
  const submissionRef = dbRef(db, `${ROOT}/submissions/${submissionId}`);
  const submissionSnap = await get(submissionRef);
  if (!submissionSnap.exists()) return;

  const item = { id: submissionId, ...submissionSnap.val() };
  if (item.status === "rejected") {
    alert("Rejected submissions cannot be pushed.");
    return;
  }

  if (item.status !== "approved") {
    await updateSubmissionStatus(submissionId, "approved");
  }

  const modelKey = await ensurePublishedModel(submissionId, item);

  let assigned = 0;
  let assignmentError = null;
  try {
    const userSnap = await get(dbRef(db, `${ROOT}/users`));
    const usersMap = userSnap.exists() ? userSnap.val() : {};
    const allUserIds = Object.keys(usersMap);

    let targetUserIds = [];
    if (item.targetMode === "specific_users") {
      targetUserIds = (item.targetUserIds || []).filter((uid) => allUserIds.includes(uid));
    } else {
      targetUserIds = allUserIds.filter((uid) => {
        const role = String(usersMap[uid]?.role || "").toLowerCase();
        return role !== "admin" && role !== "partner";
      });
      if (targetUserIds.length === 0) {
        assignmentError = "No end-users found to send to.";
      }
    }

    for (const uid of targetUserIds) {
      const userModelRef = dbRef(db, `${ROOT}/users/${uid}/models/${modelKey}`);
      const existing = await get(userModelRef);
      if (existing.exists()) continue;

      await set(userModelRef, {
        MName: modelKey,
        saved: false,
        Rating: "0.0",
        answer: "pending"
      });
      assigned += 1;
    }
  } catch (err) {
    assignmentError = err?.message || String(err);
  }

  await update(submissionRef, {
    status: "approved",
    pushedToApp: true,
    pushedAt: Date.now(),
    pushedCount: assigned,
    modelKey,
    decisionBy: currentUser.uid,
    assignmentError
  });

  // Update model data/sent count so analytics shows how many users received it
  if (modelKey && assigned > 0) {
    try {
      const modelDataRef = dbRef(db, `${ROOT}/models/${modelKey}/data`);
      const existing = await get(modelDataRef);
      const cur = existing.exists() ? existing.val() : {};
      await update(modelDataRef, { sent: toInt(cur.sent, 0) + assigned });
    } catch (e) { console.warn("Could not update sent count:", e); }
  }

  await upsertModelUploadTracker({
    modelKey,
    status: "approved",
    approved: true,
    pushedToApp: true,
    pushedCount: assigned,
    assignmentError: assignmentError || null,
    decisionBy: currentUser.uid
  });

  if (!options.silent) {
    if (assignmentError) {
      alert(`Model published to app list, but user assignment failed: ${assignmentError}. Use "Send Existing Model To Users".`);
    } else {
      alert(`Model pushed to app data. Assigned to ${assigned} users.`);
    }
  }
}

async function loadAnalytics() {
  if (!currentProfile || !currentUser) return;
  try {
    const isAdmin = normalizeRole(currentProfile.role) === "admin";
    const businessId = currentProfile.businessId || currentUser.uid;

    const all = await getAllSubmissions();
    const relevant = isAdmin ? all : all.filter((x) => x.businessId === businessId);
    const modelKeys = relevant.map((x) => x.modelKey).filter(Boolean);

    let opens = 0;
    let saves = 0;

    await Promise.all(
      modelKeys.map(async (key) => {
        const snap = await get(dbRef(db, `${ROOT}/models/${key}/data`));
        if (!snap.exists()) return;
        const d = snap.val();
        opens += toInt(d.yes, 0) + toInt(d.no, 0);
        saves += toInt(d.saved, 0);
      })
    );

    openCount.textContent = String(opens);
    saveCount.textContent = String(saves);
  } catch {
    openCount.textContent = "0";
    saveCount.textContent = "0";
  }
}

async function loadPublishedModels() {
  publishedModelsBody.innerHTML = "";
  if (!currentProfile || (currentProfile.role || "").toLowerCase() !== "admin") return;
  try {
    const snap = await get(dbRef(db, `${ROOT}/models`));
    const raw = snap.exists() ? snap.val() : {};
    const models = Object.entries(raw).map(([id, value]) => ({ id, ...value }));
    models.sort((a, b) => String(a.id).localeCompare(String(b.id)));

    models.forEach((m) => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${escapeHtml(m.id || "-")}</td>
        <td>${escapeHtml(m.name || "-")}</td>
        <td>${escapeHtml(m.question || "-")}</td>
      `;
      publishedModelsBody.appendChild(tr);
    });
  } catch (err) {
    dispatchMessage.textContent = `Could not read models list: ${err.message || err}`;
  }
}

async function loadDispatchData() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() !== "admin") return;
  if (dispatchMessage) dispatchMessage.textContent = "";

  try {
    const [modelsSnap, usersSnap] = await Promise.all([
      get(dbRef(db, `${ROOT}/models`)),
      get(dbRef(db, `${ROOT}/users`))
    ]);

    const models = modelsSnap.exists() ? modelsSnap.val() : {};
    const users = usersSnap.exists() ? usersSnap.val() : {};

    dispatchModelSelect.innerHTML = "<option value=\"\">Select model...</option>";
    Object.keys(models).sort().forEach((modelKey) => {
      const opt = document.createElement("option");
      opt.value = modelKey;
      opt.textContent = `${modelKey} (${models[modelKey]?.name || "no name"})`;
      dispatchModelSelect.appendChild(opt);
    });

    dispatchUsersList.innerHTML = "";
    Object.entries(users).forEach(([uid, value]) => {
      const role = String(value?.role || "").toLowerCase();
      if (role === "admin" || role === "partner") return;

      const name = value?.name || value?.businessName || uid;
      const row = document.createElement("label");
      row.className = "user-row";
      row.innerHTML = `<input type="checkbox" value="${uid}" /> <span>${escapeHtml(name)} <small>(${escapeHtml(uid)})</small></span>`;
      dispatchUsersList.appendChild(row);
    });

    if (!dispatchUsersList.children.length) {
      dispatchUsersList.innerHTML = "<div class='muted'>No end-users found (non-admin/non-partner).</div>";
    } else {
      addSelectAll(dispatchUsersList);
    }
  } catch (err) {
    dispatchMessage.textContent = `Could not load users/models for delivery: ${err.message || err}`;
  }
}

async function sendModelToSelectedUsers() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() !== "admin") return;

  const modelKey = dispatchModelSelect.value;
  if (!modelKey) {
    dispatchMessage.textContent = "Select a model first.";
    return;
  }

  const checked = [...dispatchUsersList.querySelectorAll("input[type='checkbox']:checked")];
  if (!checked.length) {
    dispatchMessage.textContent = "Select at least one user.";
    return;
  }

  let assigned = 0;
  try {
    for (const node of checked) {
      const uid = node.value;
      const userModelRef = dbRef(db, `${ROOT}/users/${uid}/models/${modelKey}`);
      const existing = await get(userModelRef);
      if (existing.exists()) continue;

      await set(userModelRef, {
        MName: modelKey,
        saved: false,
        Rating: "0.0",
        answer: "pending"
      });
      assigned += 1;
    }
    dispatchMessage.textContent = `Model sent to ${assigned} selected users.`;
    await refreshAll();
  } catch (err) {
    dispatchMessage.textContent = `Send failed: ${err.message || err}`;
  }
}

async function loadPartnerSubscribersForUpload() {
  if (!currentUser || !currentProfile) return;
  if ((currentProfile.role || "").toLowerCase() === "admin") return;

  partnerSubscribersList.innerHTML = "";

  try {
    const [subsSnap, usersSnap] = await Promise.all([
      get(dbRef(db, `${ROOT}/partners/${currentUser.uid}/subscribers`)),
      get(dbRef(db, `${ROOT}/users`))
    ]);

    const subscribers = subsSnap.exists() ? Object.keys(subsSnap.val()) : [];
    const users = usersSnap.exists() ? usersSnap.val() : {};

    subscribers.forEach((uid) => {
      const user = users[uid] || {};
      const name = user.name || user.email || user.businessName || uid;
      const row = document.createElement("label");
      row.className = "user-row";
      row.innerHTML = `<input type="checkbox" value="${uid}" /> <span>${escapeHtml(name)} <small>(${escapeHtml(uid)})</small></span>`;
      partnerSubscribersList.appendChild(row);
    });

    if (!subscribers.length) {
      partnerSubscribersList.innerHTML = "<div class='muted'>No subscribers mapped yet. Ask admin to assign subscribers to your partner account.</div>";
    }
  } catch (err) {
    partnerSubscribersList.innerHTML = `<div class='muted'>Could not load subscribers: ${escapeHtml(err.message || String(err))}</div>`;
  }
}

function getCheckedPartnerSubscriberIds() {
  return [...partnerSubscribersList.querySelectorAll("input[type='checkbox']:checked")].map((x) => x.value);
}

async function loadPartnerSubscriptionAdminData() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() !== "admin") return;
  partnerSubscribersMessage.textContent = "";

  try {
    const usersSnap = await get(dbRef(db, `${ROOT}/users`));
    const users = usersSnap.exists() ? usersSnap.val() : {};

    partnerSelectForSubscribers.innerHTML = "<option value=\"\">Select partner...</option>";
    availableUsersForPartnerList.innerHTML = "";

    Object.entries(users).forEach(([uid, value]) => {
      const role = String(value?.role || "").toLowerCase();
      if (role === "partner") {
        const opt = document.createElement("option");
        opt.value = uid;
        opt.textContent = `${value?.businessName || value?.name || uid} (${uid})`;
        partnerSelectForSubscribers.appendChild(opt);
      }
    });

    Object.entries(users).forEach(([uid, value]) => {
      const role = String(value?.role || "").toLowerCase();
      if (role === "admin" || role === "partner") return;
      const row = document.createElement("label");
      row.className = "user-row";
      row.innerHTML = `<input type="checkbox" value="${uid}" /> <span>${escapeHtml(value?.name || value?.email || uid)} <small>(${escapeHtml(uid)})</small></span>`;
      availableUsersForPartnerList.appendChild(row);
    });

    if (!availableUsersForPartnerList.children.length) {
      availableUsersForPartnerList.innerHTML = "<div class='muted'>No end-users found.</div>";
    }
  } catch (err) {
    partnerSubscribersMessage.textContent = `Could not load partner subscriber data: ${err.message || err}`;
  }
}

async function syncPartnerSubscribersSelection() {
  const partnerUid = partnerSelectForSubscribers.value;
  [...availableUsersForPartnerList.querySelectorAll("input[type='checkbox']")].forEach((x) => {
    x.checked = false;
  });

  if (!partnerUid) return;

  try {
    const subsSnap = await get(dbRef(db, `${ROOT}/partners/${partnerUid}/subscribers`));
    const subs = subsSnap.exists() ? subsSnap.val() : {};
    [...availableUsersForPartnerList.querySelectorAll("input[type='checkbox']")].forEach((x) => {
      x.checked = !!subs[x.value];
    });
  } catch (err) {
    partnerSubscribersMessage.textContent = `Could not load existing mapping: ${err.message || err}`;
  }
}

async function savePartnerSubscribersMapping() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() !== "admin") return;
  const partnerUid = partnerSelectForSubscribers.value;
  if (!partnerUid) {
    partnerSubscribersMessage.textContent = "Select a partner first.";
    return;
  }

  const selected = [...availableUsersForPartnerList.querySelectorAll("input[type='checkbox']:checked")].map((x) => x.value);
  const payload = {};
  selected.forEach((uid) => {
    payload[uid] = true;
  });

  try {
    await set(dbRef(db, `${ROOT}/partners/${partnerUid}/subscribers`), payload);
    partnerSubscribersMessage.textContent = `Saved ${selected.length} subscribers for partner.`;
    if (currentUser?.uid === partnerUid) {
      await loadPartnerSubscribersForUpload();
    }
  } catch (err) {
    partnerSubscribersMessage.textContent = `Save failed: ${err.message || err}`;
  }
}

// ─── Partner mailing list (Epic 1) ──────────────────────────────────────────

/** Minimal CSV splitter — handles quoted fields containing commas ("a, b"). */
function splitCsvLine(line) {
  const cells = [];
  let cur = "";
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (inQuotes) {
      if (ch === '"') {
        if (line[i + 1] === '"') { cur += '"'; i++; }
        else inQuotes = false;
      } else {
        cur += ch;
      }
    } else if (ch === '"') {
      inQuotes = true;
    } else if (ch === ",") {
      cells.push(cur);
      cur = "";
    } else {
      cur += ch;
    }
  }
  cells.push(cur);
  return cells.map((c) => c.trim());
}

/**
 * Requires a header row with an "email" column (a "name" column is optional)
 * rather than trying to guess a headerless layout — keeps the contract simple
 * and matches how Mailchimp/Google Contacts/Excel exports are shaped.
 */
function parseMailingListCsv(text) {
  const lines = text.split(/\r\n|\r|\n/).filter((l) => l.trim().length > 0);
  if (!lines.length) return { entries: [], invalidRows: 0, error: "The file is empty." };

  const header = splitCsvLine(lines[0]).map((h) => h.toLowerCase());
  const emailIdx = header.indexOf("email");
  if (emailIdx === -1) {
    return {
      entries: [],
      invalidRows: 0,
      error: 'CSV must have a header row with an "email" column (an optional "name" column is also supported).'
    };
  }
  const nameIdx = header.indexOf("name");

  const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  const entries = [];
  let invalidRows = 0;
  lines.slice(1).forEach((line) => {
    const cells = splitCsvLine(line);
    const email = (cells[emailIdx] || "").trim().toLowerCase();
    const name = nameIdx >= 0 ? (cells[nameIdx] || "").trim() : "";
    if (EMAIL_RE.test(email)) entries.push({ email, name });
    else if (cells.some((c) => c.trim().length)) invalidRows++;
  });
  return { entries, invalidRows, error: null };
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Consent gate (spam protocol): a new contact starts "pending" and only
 * counts as marketable once they click Opt In on optin.html. The link embeds
 * partner name/uid directly so that page never needs read access to the
 * mailing list — see database.rules.json for the matching write rule.
 */
async function sendMailingListWelcomeEmail(entry, entryId, partnerName) {
  if (!emailjsReady) {
    return { sent: false, reason: "EmailJS is not configured yet (see web-portal/README.md)." };
  }
  const base = location.href.replace(/[^/]*$/, "");
  const link = `${base}optin.html?p=${encodeURIComponent(currentUser.uid)}&e=${encodeURIComponent(entryId)}&n=${encodeURIComponent(partnerName)}`;
  try {
    await window.emailjs.send(emailjsConfig.serviceId, emailjsConfig.templateId, {
      to_email: entry.email,
      to_name: entry.name || entry.email,
      partner_name: partnerName,
      optin_link: link
    });
    return { sent: true };
  } catch (err) {
    return { sent: false, reason: err?.text || err?.message || String(err) };
  }
}

async function loadMailingList() {
  if (!currentUser || normalizeRole(currentProfile?.role) !== "partner") return;
  if (!mailingListBody) return;

  try {
    const snap = await get(dbRef(db, `${ROOT}/partners/${currentUser.uid}/mailingList`));
    mailingListCache = snap.exists() ? snap.val() : {};
    renderMailingListRows();
    updateMailingListCount();
  } catch (err) {
    if (mailingListUploadMessage) mailingListUploadMessage.textContent = `Could not load mailing list: ${err.message || err}`;
  }
}

function updateMailingListCount() {
  if (!mailingListCountText) return;
  const count = Object.keys(mailingListCache).length;
  mailingListCountText.textContent = `${count.toLocaleString()} / ${MAILING_LIST_CAP.toLocaleString()} contacts used`;
}

function renderMailingListRows() {
  if (!mailingListBody) return;
  mailingListBody.innerHTML = "";

  const rows = Object.entries(mailingListCache).sort((a, b) => (b[1].uploadedAt || 0) - (a[1].uploadedAt || 0));
  rows.forEach(([, entry]) => {
    const status = entry.status || "pending";
    const pillClass = status === "opted_in" ? "status-approved" : status === "declined" ? "status-rejected" : "status-pending";
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(entry.email || "")}</td>
      <td>${escapeHtml(entry.name || "-")}</td>
      <td><span class="status-pill ${pillClass}">${escapeHtml(status.replace("_", " "))}</span></td>
      <td>${formatTs(entry.uploadedAt)}</td>
    `;
    mailingListBody.appendChild(tr);
  });

  if (!rows.length) {
    mailingListBody.innerHTML = "<tr><td colspan=\"4\" class=\"muted\">No contacts uploaded yet.</td></tr>";
  }
}

async function handleMailingListUpload() {
  if (!currentUser || normalizeRole(currentProfile?.role) !== "partner") return;
  const file = mailingListCsvInput?.files?.[0];
  if (!file) {
    mailingListUploadMessage.textContent = "Choose a CSV file first.";
    return;
  }

  mailingListUploadButton.disabled = true;
  mailingListUploadMessage.textContent = "Reading file...";

  try {
    const text = await file.text();
    const { entries, invalidRows, error } = parseMailingListCsv(text);
    if (error) {
      mailingListUploadMessage.textContent = error;
      return;
    }
    if (!entries.length) {
      mailingListUploadMessage.textContent = "No valid email addresses found in that file.";
      return;
    }

    const existingEmails = new Set(Object.values(mailingListCache).map((e) => (e.email || "").toLowerCase()));
    const seenInFile = new Set();
    const newEntries = [];
    entries.forEach((entry) => {
      if (existingEmails.has(entry.email) || seenInFile.has(entry.email)) return;
      seenInFile.add(entry.email);
      newEntries.push(entry);
    });
    const duplicateCount = entries.length - newEntries.length;

    // Gate (R-1.4): reject the whole upload rather than partially applying it,
    // so a partner never ends up with a silently truncated list.
    const existingCount = Object.keys(mailingListCache).length;
    if (existingCount >= MAILING_LIST_CAP) {
      mailingListUploadMessage.textContent = `You've reached the ${MAILING_LIST_CAP.toLocaleString()}-contact free limit. Contact us to upgrade before uploading more.`;
      return;
    }
    if (existingCount + newEntries.length > MAILING_LIST_CAP) {
      const remaining = MAILING_LIST_CAP - existingCount;
      mailingListUploadMessage.textContent = `That file has ${newEntries.length} new contact(s), but you only have ${remaining} slot(s) left on the free plan (${MAILING_LIST_CAP.toLocaleString()} cap). Trim the file or contact us to upgrade.`;
      return;
    }
    if (!newEntries.length) {
      mailingListUploadMessage.textContent = `No new contacts to add (${duplicateCount} already on your list).`;
      return;
    }

    mailingListUploadMessage.textContent = `Saving ${newEntries.length} contact(s)...`;
    const partnerName = currentProfile.businessName || currentProfile.name || "Cornucopia";
    const listRef = dbRef(db, `${ROOT}/partners/${currentUser.uid}/mailingList`);
    const updates = {};
    const toEmail = [];
    newEntries.forEach((entry) => {
      const entryId = push(listRef).key;
      const record = { email: entry.email, name: entry.name || "", status: "pending", uploadedAt: Date.now() };
      updates[entryId] = record;
      toEmail.push({ id: entryId, ...record });
    });
    await update(listRef, updates);
    mailingListCache = { ...mailingListCache, ...updates };
    renderMailingListRows();
    updateMailingListCount();

    mailingListUploadMessage.textContent = `Saved ${newEntries.length} contact(s). Sending welcome emails...`;
    let sentCount = 0;
    let firstEmailError = null;
    for (const entry of toEmail) {
      const result = await sendMailingListWelcomeEmail(entry, entry.id, partnerName);
      if (result.sent) sentCount++;
      else if (!firstEmailError) firstEmailError = result.reason;
      await sleep(300); // keep well under EmailJS's per-second rate limit
    }

    const dupText = duplicateCount ? ` (${duplicateCount} duplicate${duplicateCount === 1 ? "" : "s"} skipped)` : "";
    const invalidText = invalidRows ? ` ${invalidRows} row(s) had no valid email and were ignored.` : "";
    mailingListUploadMessage.textContent = sentCount === toEmail.length
      ? `Added ${newEntries.length} contact(s)${dupText} and sent ${sentCount} welcome email(s).${invalidText}`
      : `Added ${newEntries.length} contact(s)${dupText}, but only ${sentCount}/${toEmail.length} welcome emails sent. ${firstEmailError || ""}${invalidText}`;

    mailingListCsvInput.value = "";
  } catch (err) {
    mailingListUploadMessage.textContent = `Upload failed: ${err.message || err}`;
  } finally {
    mailingListUploadButton.disabled = false;
  }
}

// ─── Category management (Epic 4, portal-only per R-4.1) ───────────────────
// The app has no catalog-browse screen today (users only ever see models
// explicitly assigned to them), so there's nowhere for an "empty category"
// to show up on the mobile side (R-4.2) — categories are admin/portal
// organization for now. The category filter dropdown only lists categories
// that currently have at least one model, which is the portal-side
// equivalent of that rule.

async function loadCategories() {
  if (normalizeRole(currentProfile?.role) !== "admin") return;
  try {
    const snap = await get(dbRef(db, `${ROOT}/categories`));
    categoriesCache = snap.exists() ? snap.val() : {};
    renderCategoriesList();
    renderCategoryFilterOptions();
    if (_modelPageItem) renderModelPageCategory(_modelPageItem);
  } catch (err) {
    if (categoriesMessage) categoriesMessage.textContent = `Could not load categories: ${err.message || err}`;
  }
}

async function addCategory() {
  const name = (newCategoryInput?.value || "").trim();
  if (!name) {
    if (categoriesMessage) categoriesMessage.textContent = "Enter a category name.";
    return;
  }
  const exists = Object.values(categoriesCache).some((c) => (c.name || "").toLowerCase() === name.toLowerCase());
  if (exists) {
    if (categoriesMessage) categoriesMessage.textContent = "That category already exists.";
    return;
  }
  try {
    const ref = push(dbRef(db, `${ROOT}/categories`));
    await set(ref, { name, createdAt: Date.now() });
    if (newCategoryInput) newCategoryInput.value = "";
    if (categoriesMessage) categoriesMessage.textContent = `Added "${name}".`;
    await loadCategories();
  } catch (err) {
    if (categoriesMessage) categoriesMessage.textContent = `Could not add category: ${err.message || err}`;
  }
}

async function deleteCategory(categoryId, name) {
  const count = submissionsCache.filter((s) => s.category === categoryId).length;
  const confirmMsg = count > 0
    ? `Delete "${name}"? ${count} model(s) using it will become uncategorized.`
    : `Delete "${name}"?`;
  if (!confirm(confirmMsg)) return;

  try {
    const updates = {};
    submissionsCache.forEach((s) => {
      if (s.category === categoryId) {
        updates[`${ROOT}/submissions/${s.id}/category`] = null;
        updates[`${ROOT}/submissions/${s.id}/categoryName`] = null;
        if (s.modelKey) {
          updates[`${ROOT}/models/${s.modelKey}/category`] = null;
          updates[`${ROOT}/models/${s.modelKey}/categoryName`] = null;
        }
      }
    });
    updates[`${ROOT}/categories/${categoryId}`] = null;
    await update(dbRef(db), updates);
    if (categoriesMessage) categoriesMessage.textContent = `Deleted "${name}".`;
    await Promise.all([loadCategories(), loadMySubmissions()]);
  } catch (err) {
    if (categoriesMessage) categoriesMessage.textContent = `Could not delete category: ${err.message || err}`;
  }
}

function renderCategoriesList() {
  if (!categoriesList) return;
  const entries = Object.entries(categoriesCache).sort((a, b) => (a[1].name || "").localeCompare(b[1].name || ""));
  categoriesList.innerHTML = "";
  if (!entries.length) {
    categoriesList.innerHTML = "<div class='muted'>No categories yet.</div>";
    return;
  }
  entries.forEach(([id, cat]) => {
    const count = submissionsCache.filter((s) => s.category === id).length;
    const row = document.createElement("div");
    row.className = "user-row";
    row.innerHTML = `
      <span style="flex:1">${escapeHtml(cat.name)} <small class="muted">(${count} model${count === 1 ? "" : "s"})</small></span>
      <button class="danger" type="button" style="padding:4px 10px;font-size:12px">Delete</button>
    `;
    row.querySelector("button").addEventListener("click", () => deleteCategory(id, cat.name));
    categoriesList.appendChild(row);
  });
}

function renderCategoryFilterOptions() {
  if (!categoryFilterSelect) return;
  const nonEmpty = Object.entries(categoriesCache)
    .filter(([id]) => submissionsCache.some((s) => s.category === id))
    .sort((a, b) => (a[1].name || "").localeCompare(b[1].name || ""));

  const previous = categoryFilterSelect.value;
  categoryFilterSelect.innerHTML = '<option value="">All categories</option>'
    + nonEmpty.map(([id, cat]) => `<option value="${id}">${escapeHtml(cat.name)}</option>`).join("");

  if (nonEmpty.some(([id]) => id === previous)) {
    categoryFilterSelect.value = previous;
  } else if (previous) {
    categoryFilterSelect.value = "";
    currentCategoryFilter = "";
  }
}

/** Category assignment on the model detail page — the one place a submission
 * actually gets tagged; the categories card above only manages the list. */
function renderModelPageCategory(item) {
  if (normalizeRole(currentProfile?.role) !== "admin") return;
  const select = byId("modelPageCategorySelect");
  const msg = byId("modelPageCategoryMessage");
  if (!select) return;

  const options = Object.entries(categoriesCache).sort((a, b) => (a[1].name || "").localeCompare(b[1].name || ""));
  select.innerHTML = '<option value="">Uncategorized</option>'
    + options.map(([id, cat]) => `<option value="${id}">${escapeHtml(cat.name)}</option>`).join("");
  select.value = item.category || "";
  if (msg) msg.textContent = "";
}

async function saveModelPageCategory() {
  const item = _modelPageItem;
  if (!item?.id) return;
  const select = byId("modelPageCategorySelect");
  const msg = byId("modelPageCategoryMessage");
  const categoryId = select?.value || "";
  const categoryName = categoryId ? (categoriesCache[categoryId]?.name || "") : "";

  if (msg) msg.textContent = "Saving...";
  try {
    await update(dbRef(db, `${ROOT}/submissions/${item.id}`), {
      category: categoryId || null,
      categoryName: categoryName || null
    });
    if (item.modelKey) {
      const modelSnap = await get(dbRef(db, `${ROOT}/models/${item.modelKey}`));
      if (modelSnap.exists()) {
        await update(dbRef(db, `${ROOT}/models/${item.modelKey}`), {
          category: categoryId || null,
          categoryName: categoryName || null
        });
      }
    }
    const cached = submissionsCache.find((s) => s.id === item.id);
    if (cached) { cached.category = categoryId || null; cached.categoryName = categoryName || null; }
    if (msg) msg.textContent = "Saved.";
    renderCategoriesList();
    renderCategoryFilterOptions();
    renderSubmissionRows();
  } catch (err) {
    if (msg) msg.textContent = `Could not save: ${err.message || err}`;
  }
}

async function loadPartnerDeliveryData() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() === "admin") return;
  if (partnerDeliveryMessage) partnerDeliveryMessage.textContent = "";

  try {
    const [submissionsSnap, usersSnap] = await Promise.all([
      get(dbRef(db, `${ROOT}/submissions`)),
      get(dbRef(db, `${ROOT}/users`))
    ]);

    const submissions = submissionsSnap.exists() ? submissionsSnap.val() : {};
    const users = usersSnap.exists() ? usersSnap.val() : {};
    const businessId = currentProfile.businessId || currentUser.uid;

    const approved = Object.values(submissions)
      .filter((s) => s && s.businessId === businessId && s.status === "approved" && s.modelKey)
      .sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));

    partnerApprovedModelSelect.innerHTML = "<option value=\"\">Select approved model...</option>";
    const seen = new Set();
    approved.forEach((s) => {
      if (seen.has(s.modelKey)) return;
      seen.add(s.modelKey);
      const opt = document.createElement("option");
      opt.value = s.modelKey;
      opt.textContent = `${s.displayName || s.fileName || s.modelKey} (${s.modelKey})`;
      partnerApprovedModelSelect.appendChild(opt);
    });

    partnerDeliveryUsersList.innerHTML = "";
    Object.entries(users).forEach(([uid, value]) => {
      const role = String(value?.role || "").toLowerCase();
      if (role === "admin" || role === "partner") return;
      const row = document.createElement("label");
      row.className = "user-row";
      row.innerHTML = `<input type="checkbox" value="${uid}" /> <span>${escapeHtml(value?.name || value?.email || value?.businessName || uid)} <small>(${escapeHtml(uid)})</small></span>`;
      partnerDeliveryUsersList.appendChild(row);
    });

    if (!partnerDeliveryUsersList.children.length) {
      partnerDeliveryUsersList.innerHTML = "<div class='muted'>No users available yet.</div>";
    } else {
      addSelectAll(partnerDeliveryUsersList);
    }
  } catch (err) {
    partnerDeliveryMessage.textContent = `Could not load partner delivery data: ${err.message || err}`;
  }
}

async function sendApprovedPartnerModelToUsers() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() === "admin") return;
  const modelKey = partnerApprovedModelSelect.value;
  if (!modelKey) {
    partnerDeliveryMessage.textContent = "Select an approved model first.";
    return;
  }

  const selected = [...partnerDeliveryUsersList.querySelectorAll("input[type='checkbox']:checked")].map((x) => x.value);
  if (!selected.length) {
    partnerDeliveryMessage.textContent = "Select at least one user.";
    return;
  }

  let assigned = 0;
  try {
    // The model must already be in the app catalog (i.e. pushed by an admin)
    const modelSnap = await get(dbRef(db, `${ROOT}/models/${modelKey}`));
    if (!modelSnap.exists()) {
      partnerDeliveryMessage.textContent = "This model hasn't been pushed to the app yet — ask an admin to push it first.";
      return;
    }

    for (const uid of selected) {
      const userModelRef = dbRef(db, `${ROOT}/users/${uid}/models/${modelKey}`);
      const existing = await get(userModelRef);
      if (existing.exists()) continue;
      await set(userModelRef, { MName: modelKey, saved: false, Rating: "0.0", answer: "pending" });
      assigned += 1;
    }
    // Update data/sent count so analytics reflects delivery
    if (assigned > 0) {
      try {
        const modelDataRef = dbRef(db, `${ROOT}/models/${modelKey}/data`);
        const existing = await get(modelDataRef);
        const cur = existing.exists() ? existing.val() : {};
        await update(modelDataRef, { sent: toInt(cur.sent, 0) + assigned });
      } catch (e) { console.warn("Could not update sent count:", e); }
    }
    partnerDeliveryMessage.textContent = `Sent model to ${assigned} users.`;
    showToast(`Model sent to ${assigned} user${assigned !== 1 ? "s" : ""}.`);
    await refreshAll();
  } catch (err) {
    partnerDeliveryMessage.textContent = `Send failed: ${err.message || err}`;
  }
}

async function getAllSubmissions() {
  const snap = await get(dbRef(db, `${ROOT}/submissions`));
  const raw = snap.exists() ? snap.val() : {};
  return Object.entries(raw).map(([id, value]) => ({ id, ...value }));
}

let _detailsSubmissionId = null;

function openModelDetailsEditor(item) {
  if (!item?.id) return;
  _detailsSubmissionId = item.id;
  const title = item.displayName || item.fileName || "Model";
  const titleEl = byId("detailsTitle");
  if (titleEl) titleEl.textContent = `${title} — Details`;
  const tbody = byId("attributesBody");
  if (!tbody) return;
  tbody.innerHTML = "";
  const existing = item.attributes || {};
  const entries = typeof existing === "object" ? Object.entries(existing) : [];
  if (entries.length) {
    entries.forEach(([attr, val]) => addAttributeRow(attr, val));
  } else {
    addAttributeRow("", "");
  }
  byId("detailsModal")?.classList.remove("hidden");
}

function addAttributeRow(attr = "", val = "") {
  const tbody = byId("attributesBody");
  if (!tbody) return;
  const tr = document.createElement("tr");
  tr.innerHTML = `
    <td><input class="attr-input attr-key" placeholder="e.g. Colour" value="${escapeHtml(String(attr))}" /></td>
    <td><input class="attr-input attr-val" placeholder="e.g. Red" value="${escapeHtml(String(val))}" /></td>
    <td><button class="delete-attr-btn" type="button" title="Remove row">✕</button></td>
  `;
  tr.querySelector(".delete-attr-btn").addEventListener("click", () => tr.remove());
  tbody.appendChild(tr);
}

async function saveModelDetails() {
  if (!_detailsSubmissionId) return;
  const tbody = byId("attributesBody");
  const saveBtn = byId("saveDetails");
  if (!tbody) return;
  const rows = [...tbody.querySelectorAll("tr")];
  const attributes = {};
  rows.forEach((row) => {
    const key = row.querySelector(".attr-key")?.value.trim();
    const val = row.querySelector(".attr-val")?.value.trim();
    if (key) attributes[key] = val || "";
  });
  if (saveBtn) { saveBtn.textContent = "Saving…"; saveBtn.disabled = true; }
  try {
    await set(dbRef(db, `${ROOT}/submissions/${_detailsSubmissionId}/attributes`), attributes);
    const cached = submissionsCache.find((x) => x.id === _detailsSubmissionId);
    if (cached) cached.attributes = attributes;
    showToast("✓ Details saved.");
    closeDetailsModal();
  } catch (err) {
    showToast(`Could not save: ${err.message || err}`, "error");
  } finally {
    if (saveBtn) { saveBtn.textContent = "Save Details"; saveBtn.disabled = false; }
  }
}

async function openModelAnalytics(item) {
  if (!item?.id) return;
  const title = item.displayName || item.fileName || "Model";
  const titleEl = byId("analyticsTitle");
  const metaEl = byId("analyticsMeta");
  const loadingEl = byId("analyticsLoading");
  const contentEl = byId("analyticsContent");
  if (titleEl) titleEl.textContent = title;
  if (metaEl) metaEl.textContent = `Status: ${statusLabel(item)} · Uploaded ${formatTs(item.createdAt)}`;
  if (loadingEl) loadingEl.classList.remove("hidden");
  if (contentEl) contentEl.classList.add("hidden");
  byId("analyticsModal")?.classList.remove("hidden");

  try {
    const modelKey = item.modelKey;
    let data = { sent: 0, saved: 0, yes: 0, no: 0, rating: "0.0" };
    if (modelKey) {
      const snap = await get(dbRef(db, `${ROOT}/models/${modelKey}/data`));
      if (snap.exists()) data = { ...data, ...snap.val() };
    }

    // Fall back to pushedCount from submission if model data/sent not yet set
    const sent = toInt(data.sent, 0) || toInt(item.pushedCount, 0);
    const saved = toInt(data.saved, 0);
    const yes = toInt(data.yes, 0);
    const no = toInt(data.no, 0);
    const opens = yes + no;
    const rating = parseFloat(data.rating) || 0;
    const responseRate = sent > 0 ? Math.round((opens / sent) * 100) : 0;
    const saveRate = opens > 0 ? Math.round((saved / opens) * 100) : 0;
    const total = yes + no || 1;

    const s = (id, val) => { const el = byId(id); if (el) el.textContent = val; };
    s("statSent", sent || "—");
    s("statOpens", opens);
    s("statSaves", saved);
    s("statResponseRate", sent > 0 ? `${responseRate}%` : "—");
    s("statConversionRate", opens > 0 ? `${saveRate}%` : "—");
    s("statYesLabel", `${yes} Yes`);
    s("statNoLabel", `${no} No`);
    s("statRating", rating.toFixed(1));

    const yesBar = byId("voteBarYes");
    const noBar = byId("voteBarNo");
    if (yesBar) yesBar.style.width = `${Math.round((yes / total) * 100)}%`;
    if (noBar) noBar.style.width = `${Math.round((no / total) * 100)}%`;

    const stars = "★".repeat(Math.round(rating)) + "☆".repeat(5 - Math.round(rating));
    s("statStars", stars);

    // Load questions and their response data
    renderQuestionAnalytics(item, yes, no, opens, sent);

    if (loadingEl) loadingEl.classList.add("hidden");
    if (contentEl) contentEl.classList.remove("hidden");
  } catch (err) {
    if (loadingEl) loadingEl.textContent = `Could not load analytics: ${err.message || err}`;
  }
}

function openModelDetail(item) {
  if (!item?.id) return;
  // Close any other open modal first
  closeUploadModal();
  currentDetailSubmissionId = item.id;
  const title = item.displayName || item.fileName || "Model";
  const titleEl = byId("modelDetailTitle");
  const metaEl = byId("modelDetailMeta");
  const textEl = byId("questionTextInput");
  const msgEl = byId("questionMessage");
  const typeEl = byId("questionTypeSelect");
  if (titleEl) titleEl.textContent = title;
  if (metaEl) metaEl.textContent = `Status: ${statusLabel(item)} · Uploaded ${formatTs(item.createdAt)}`;
  renderQuestions(item.questions || {});
  if (textEl) textEl.value = "";
  if (msgEl) msgEl.textContent = "";
  if (typeEl) typeEl.value = "yes_no";
  byId("mcOptionsContainer")?.classList.add("hidden");
  byId("modelDetailModal")?.classList.remove("hidden");
}

function renderQuestions(questions) {
  const list = byId("questionsList");
  if (!list) return;
  list.innerHTML = "";
  const entries = Object.entries(questions);
  if (entries.length === 0) {
    list.innerHTML = `<div class="empty-questions">No questions yet. Add one below to start collecting feedback.</div>`;
    return;
  }
  entries.forEach(([qId, q]) => {
    const card = document.createElement("div");
    card.className = "question-card";
    const typeLabels = { yes_no: "Yes / No", multiple_choice: "Multiple Choice", rating: "Rating", open_text: "Open Text" };
    const optionsHtml = (q.type === "multiple_choice" && Array.isArray(q.options) && q.options.length)
      ? `<ul class="question-options">${q.options.map((o) => `<li>${escapeHtml(o)}</li>`).join("")}</ul>`
      : q.type === "rating" ? `<ul class="question-options"><li>1</li><li>2</li><li>3</li><li>4</li><li>5</li></ul>` : "";
    card.innerHTML = `
      <div class="question-card-body">
        <span class="question-type-badge qtype-${q.type}">${typeLabels[q.type] || q.type}</span>
        <p style="margin-top:6px">${escapeHtml(q.text || "")}</p>
        ${optionsHtml}
      </div>
      <button class="delete-question-btn" data-qid="${qId}" title="Remove question">✕</button>
    `;
    card.querySelector(".delete-question-btn").addEventListener("click", () => deleteQuestion(qId));
    list.appendChild(card);
  });
}

async function addQuestionToModel() {
  if (!currentDetailSubmissionId) return;
  const type = byId("questionTypeSelect")?.value;
  const text = (byId("questionTextInput")?.value || "").trim();
  const msgEl = byId("questionMessage");

  if (!text) { if (msgEl) msgEl.textContent = "Question text is required."; return; }

  let options = [];
  if (type === "multiple_choice") {
    options = [...(byId("mcOptionsList")?.querySelectorAll(".mc-option") || [])]
      .map((el) => el.value.trim())
      .filter(Boolean);
    if (options.length < 2) { if (msgEl) msgEl.textContent = "Add at least 2 options."; return; }
  }

  const questionRef = push(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
  await set(questionRef, { type, text, options, createdAt: Date.now() });

  if (msgEl) msgEl.textContent = "";
  byId("questionTextInput").value = "";

  // Refresh questions in modal
  const snap = await get(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
  renderQuestions(snap.exists() ? snap.val() : {});

  // Update cache so question count reflects without full reload
  const cached = submissionsCache.find((x) => x.id === currentDetailSubmissionId);
  if (cached) {
    cached.questions = snap.exists() ? snap.val() : {};
    renderSubmissionRows();
  }
}

async function deleteQuestion(questionId) {
  if (!currentDetailSubmissionId) return;
  try {
    await set(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions/${questionId}`), null);
    const snap = await get(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
    renderQuestions(snap.exists() ? snap.val() : {});
    const cached = submissionsCache.find((x) => x.id === currentDetailSubmissionId);
    if (cached) {
      cached.questions = snap.exists() ? snap.val() : {};
      renderSubmissionRows();
    }
  } catch (err) {
    const msgEl = byId("questionMessage");
    if (msgEl) msgEl.textContent = `Could not delete question: ${err.message || err}`;
  }
}

function targetLabel(item) {
  if (item.targetMode === "specific_users") {
    const count = Array.isArray(item.targetUserIds) ? item.targetUserIds.length : 0;
    return `Specific (${count})`;
  }
  return "All users";
}

function statusLabel(item) {
  const base = item.status || "pending";
  if (item.pushedToApp) {
    return `${base} / pushed (${toInt(item.pushedCount, 0)})`;
  }
  return base;
}

function parseTargetUserIds(value) {
  return String(value || "")
    .split(/[\s,\n\r]+/)
    .map((x) => x.trim())
    .filter(Boolean);
}

function sanitizeKey(value) {
  return String(value || "")
    .toLowerCase()
    .replace(/\.glb$/i, "")
    .replace(/[^a-z0-9_\-]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .slice(0, 50) || "model";
}

function stripGlbExtension(value) {
  return String(value || "").replace(/\.glb$/i, "");
}

function toInt(value, fallback) {
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function bindNavigation() {
  const navButtons = [...document.querySelectorAll(".nav-btn[data-screen]")];
  navButtons.forEach((btn) => {
    btn.addEventListener("click", () => {
      const target = btn.getAttribute("data-screen");
      setActiveScreen(target);
    });
  });
}

function setActiveScreen(screenId) {
  const navButtons = [...document.querySelectorAll(".nav-btn[data-screen]")];
  const screens = [...document.querySelectorAll(".screen")];
  const targetButton = navButtons.find((btn) => btn.getAttribute("data-screen") === screenId);
  const targetScreen = screens.find((screen) => screen.id === screenId);

  navButtons.forEach((btn) => btn.classList.remove("active"));
  screens.forEach((screen) => screen.classList.remove("active"));

  if (targetButton) {
    targetButton.classList.add("active");
  }
  if (targetScreen) {
    targetScreen.classList.add("active");
  }

  const label = targetButton ? targetButton.textContent.trim().replace(/\d+\+?$/, "").trim() : "Dashboard";
  byId("welcomeText").textContent = label;
}

function setSubmissionFilter(filter) {
  currentSubmissionFilter = filter;
  updateSubmissionFilterUI();
  renderSubmissionRows();
}

function updateSubmissionFilterUI() {
  const map = {
    all: filterAllSubmissions,
    approved: filterApprovedSubmissions,
    pending: filterPendingSubmissions
  };
  [filterAllSubmissions, filterApprovedSubmissions, filterPendingSubmissions].forEach((btn) => {
    btn?.classList.remove("active");
  });
  map[currentSubmissionFilter]?.classList.add("active");
}

function renderSubmissionRows() {
  if (!mySubmissionsBody) return;
  mySubmissionsBody.innerHTML = "";
  let rows = submissionsCache;
  if (currentSubmissionFilter === "approved") {
    rows = rows.filter((item) => item.status === "approved");
  } else if (currentSubmissionFilter === "pending") {
    rows = rows.filter((item) => item.status === "pending");
  }
  if (currentCategoryFilter) {
    rows = rows.filter((item) => item.category === currentCategoryFilter);
  }

  const isAdmin = normalizeRole(currentProfile?.role) === "admin";

  rows.forEach((item) => {
    const qCount = item.questions ? Object.keys(item.questions).length : 0;
    const qBadgeClass = qCount > 0 ? "q-count-badge has-questions" : "q-count-badge";
    const qLabel = qCount > 0 ? `${qCount} question${qCount !== 1 ? "s" : ""}` : "0 questions";
    const displayName = item.displayName || item.fileName || "-";

    const thumbHtml = item.thumbnailUrl
      ? `<img src="${escapeHtml(item.thumbnailUrl)}" class="row-thumbnail" alt="thumbnail">`
      : `<div class="row-thumbnail-placeholder"></div>`;

    const approved = item.status === "approved";
    const rejected = item.status === "rejected";
    const pushed = !!item.pushedToApp;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>
        <div style="display:flex;align-items:center;gap:10px">
          ${thumbHtml}
          <div style="display:flex;align-items:flex-start;gap:8px;flex-wrap:wrap;flex:1">
            <button class="model-name-link" data-analytics="true">
              <strong>${escapeHtml(displayName)}</strong><br>
              <span class="muted" style="font-size:12px">${escapeHtml(item.fileName || "")}</span>
            </button>
            <button class="model-details-btn" type="button" title="Edit model details">Details</button>
          </div>
        </div>
      </td>
      ${isAdmin ? `<td>${escapeHtml(item.businessName || item.businessId || "-")}</td>` : ""}
      ${isAdmin ? `<td>${escapeHtml(targetLabel(item))}</td>` : ""}
      <td><span class="status-pill status-${item.status || "pending"}">${escapeHtml(statusLabel(item))}</span></td>
      <td>
        <div class="q-cell">
          <span class="${qBadgeClass}">${qLabel}</span>
          <button class="manage-questions-btn" type="button">✏ Edit</button>
        </div>
      </td>
      <td>${formatTs(item.createdAt)}</td>
      ${isAdmin ? `
      <td>
        <div class="row-actions">
          <button class="secondary" data-id="${item.id}" data-action="approve" ${approved || rejected ? "disabled" : ""}>Approve</button>
          <button class="danger" data-id="${item.id}" data-action="reject" ${approved || rejected ? "disabled" : ""}>Reject</button>
          <button class="success" data-id="${item.id}" data-action="push" ${!approved || pushed ? "disabled" : ""}>${pushed ? "Pushed ✓" : "Push to App"}</button>
        </div>
      </td>` : ""}
    `;
    tr.querySelector(".manage-questions-btn").addEventListener("click", () => openModelPage(item));
    tr.querySelector("[data-analytics]").addEventListener("click", () => openModelPage(item));
    tr.querySelector(".model-details-btn").addEventListener("click", () => openModelDetailsEditor(item));
    mySubmissionsBody.appendChild(tr);
  });

  if (isAdmin) {
    bindSubmissionActionButtons(mySubmissionsBody);
    renderCategoriesList();
    renderCategoryFilterOptions();
  }
}

// ─── Model Page (full-screen detail view) ────────────────────────────────────

let _modelPageItem = null;

async function openModelPage(item) {
  if (!item?.id) return;
  closeUploadModal();
  _modelPageItem = item;
  currentDetailSubmissionId = item.id;

  const title = item.displayName || item.fileName || "Model";
  const titleEl = byId("modelPageTitle");
  const metaEl  = byId("modelPageMeta");
  if (titleEl) titleEl.textContent = title;
  if (metaEl)  metaEl.textContent  = `Status: ${statusLabel(item)} · Uploaded ${formatTs(item.createdAt)}`;
  const descEl = byId("modelPageDescription");
  if (descEl) descEl.textContent = item.description || "No description provided.";
  renderModelPageAttributes(item.attributes || {});

  // Fresh add-question form on every visit, not whatever was left typed
  // in from a previous model's page.
  const mpQMsg = byId("modelPageQuestionMessage");
  if (mpQMsg) mpQMsg.textContent = "";
  const mpQText = byId("modelPageQuestionTextInput");
  if (mpQText) mpQText.value = "";
  const mpQType = byId("modelPageQuestionTypeSelect");
  if (mpQType) mpQType.value = "yes_no";
  byId("modelPageMcOptionsContainer")?.classList.add("hidden");
  const mpMcList = byId("modelPageMcOptionsList");
  if (mpMcList) mpMcList.innerHTML = '<input class="mc-option" placeholder="Option 1" /><input class="mc-option" placeholder="Option 2" />';

  byId("modelPageLoading")?.classList.remove("hidden");
  byId("modelPageContent")?.classList.add("hidden");

  setActiveScreen("modelPage");
  // Override the topbar title that setActiveScreen sets
  const wt = byId("welcomeText");
  if (wt) wt.textContent = title;

  try {
    const modelKey = item.modelKey;
    let data = { sent: 0, saved: 0, yes: 0, no: 0, opens: 0, rating: "0.0" };
    if (modelKey) {
      const snap = await get(dbRef(db, `${ROOT}/models/${modelKey}/data`));
      if (snap.exists()) data = { ...data, ...snap.val() };
    }

    const sent   = toInt(data.sent, 0) || toInt(item.pushedCount, 0);
    const saved  = toInt(data.saved, 0);
    const yes    = toInt(data.yes, 0);
    const no     = toInt(data.no, 0);
    // A genuine "package opened" counter, independent of yes/no - a model whose
    // question is multiple_choice/rating/open_text (or has no question at all)
    // still has an Opens count instead of being stuck at 0 forever.
    const opens  = toInt(data.opens, 0);
    const rating = parseFloat(data.rating) || 0;
    const responseRate = sent  > 0 ? Math.round((opens / sent)  * 100) : 0;
    const saveRate     = opens > 0 ? Math.round((saved / opens) * 100) : 0;
    const stars  = "★".repeat(Math.round(rating)) + "☆".repeat(5 - Math.round(rating));

    // Render stats row
    const statsRow = byId("modelPageStatsRow");
    if (statsRow) {
      const stats = [
        { value: sent  || "—",                          label: "Sent To"       },
        { value: opens,                                  label: "Opens"         },
        { value: saved,                                  label: "Saves"         },
        { value: opens > 0 ? `${saveRate}%`     : "—",  label: "Save Rate"     },
        { value: opens,                                  label: "Response"      },
        { value: sent  > 0 ? `${responseRate}%` : "—",  label: "Response Rate" },
        { value: rating.toFixed(1),                      label: "Rating"        },
        { value: stars,                                  label: "/ 5.0"         },
      ];
      statsRow.innerHTML = stats.map((s) => `
        <div class="mp-stat">
          <span class="mp-stat-value">${escapeHtml(String(s.value))}</span>
          <span class="mp-stat-label">${escapeHtml(s.label)}</span>
        </div>`).join("");
    }

    await renderModelPageQuestions(item, yes, no, opens, sent);
    await renderModelPageDistribution(item);

    // Show thumbnail or placeholder
    renderModelPageThumbnail(item.thumbnailUrl || null);

    renderModelPageActions(item);
    renderModelPageCategory(item);

    byId("modelPageLoading")?.classList.add("hidden");
    byId("modelPageContent")?.classList.remove("hidden");
  } catch (err) {
    const el = byId("modelPageLoading");
    if (el) el.textContent = `Could not load: ${err.message || err}`;
  }
}

/** Approve/Reject/Push to App, directly on the model detail page (R-2.5) —
 * mirrors the same three buttons in the Manage Uploads table, sharing the
 * exact same handler (runSubmissionAction) so both stay in sync. */
function renderModelPageActions(item) {
  if (normalizeRole(currentProfile?.role) !== "admin") return;
  const card = byId("modelPageActionsCard");
  const approveBtn = byId("modelPageApproveButton");
  const rejectBtn = byId("modelPageRejectButton");
  const pushBtn = byId("modelPagePushButton");
  if (!card || !approveBtn || !rejectBtn || !pushBtn) return;

  const approved = item.status === "approved";
  const rejected = item.status === "rejected";
  const pushed = !!item.pushedToApp;

  [approveBtn, rejectBtn, pushBtn].forEach((btn) => { btn.dataset.id = item.id; });
  approveBtn.dataset.action = "approve";
  rejectBtn.dataset.action = "reject";
  pushBtn.dataset.action = "push";

  approveBtn.disabled = approved || rejected;
  rejectBtn.disabled = approved || rejected;
  pushBtn.disabled = !approved || pushed;
  pushBtn.textContent = pushed ? "Pushed ✓" : "Push to App";

  bindSubmissionActionButtons(card);
}

/** Renders the free-form key/value attributes set via the "Details" modal
 * (My Uploads' Details button, saveModelDetails) - that modal writes to
 * submissions/{id}/attributes but nothing previously read it back on this
 * page, so anything added there was invisible after saving. */
function renderModelPageAttributes(attributes) {
  const card = byId("modelPageAttributesCard");
  const list = byId("modelPageAttributesList");
  if (!list) return;

  const entries = Object.entries(attributes || {}).filter(([key]) => key);
  if (!entries.length) {
    if (card) card.style.display = "none";
    return;
  }
  if (card) card.style.display = "";

  list.innerHTML = entries.map(([key, val]) => `
    <div class="attr-row">
      <span class="attr-row-label">${escapeHtml(key)}</span>
      <span class="attr-row-value">${escapeHtml(String(val))}</span>
    </div>`).join("");
}

async function renderModelPageQuestions(item, yes, no, opens, sent) {
  const list = byId("modelPageQuestionsList");
  if (!list) return;

  const snap = await get(dbRef(db, `${ROOT}/submissions/${item.id}/questions`));
  const entries = snap.exists() ? Object.entries(snap.val()) : [];

  list.innerHTML = "";

  if (!entries.length) {
    list.innerHTML = `<div class="empty-questions">No questions yet.</div>`;
    return;
  }

  const typeLabels = {
    yes_no: "Yes / No", multiple_choice: "Multiple Choice",
    rating: "Rating (1–5)", open_text: "Open Text"
  };

  const total  = yes + no || 1;
  const yesPct = Math.round((yes / total) * 100);
  const noPct  = Math.round((no  / total) * 100);

  entries.forEach(([qId, q]) => {
    let analyticsHtml = "";
    if (q.type === "yes_no") {
      analyticsHtml = `
        <div class="vote-bar-row" style="margin-top:8px">
          <span class="vote-label yes-label">${yes} Yes (${yesPct}%)</span>
          <div class="vote-bar-track">
            <div class="vote-bar-yes" style="width:${yesPct}%"></div>
            <div class="vote-bar-no"  style="width:${noPct}%"></div>
          </div>
          <span class="vote-label no-label">${no} No (${noPct}%)</span>
        </div>
        <p class="muted" style="font-size:12px;margin-top:6px">
          ${yes + no} total response${(yes + no) !== 1 ? "s" : ""} out of ${sent || "?"} delivered
        </p>`;
    } else if (q.type === "rating") {
      analyticsHtml = `<p class="muted" style="font-size:13px;margin-top:6px">Rating tracked globally — see Rating stat above.</p>`;
    } else if (q.type === "multiple_choice") {
      const opts = Array.isArray(q.options) ? q.options : [];
      analyticsHtml = `<p class="muted" style="font-size:13px;margin-top:6px">${opts.length} option${opts.length !== 1 ? "s" : ""}: ${opts.map((o) => escapeHtml(o)).join(", ")}</p>`;
    } else {
      analyticsHtml = `<p class="muted" style="font-size:13px;margin-top:6px">Open text responses are collected in-app.</p>`;
    }

    const card = document.createElement("div");
    card.className = "q-analytics-card";
    card.innerHTML = `
      <div class="q-analytics-header" style="display:flex;align-items:flex-start;gap:10px;flex-wrap:wrap">
        <span class="question-type-badge qtype-${q.type}">${typeLabels[q.type] || q.type}</span>
        <p class="q-analytics-text" style="flex:1;margin:0">${escapeHtml(q.text || "")}</p>
        <button class="secondary mp-del-q-btn" data-qid="${qId}" type="button"
                style="font-size:12px;padding:4px 10px;flex-shrink:0">✕ Remove</button>
      </div>
      <div class="q-analytics-body">${analyticsHtml}</div>
    `;

    card.querySelector(".mp-del-q-btn").addEventListener("click", async () => {
      if (!currentDetailSubmissionId) return;
      try {
        await set(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions/${qId}`), null);
        const updated = await get(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
        // Refresh page questions list
        await renderModelPageQuestions(item, yes, no, opens, sent);
        // Keep the cache in sync
        const cached = submissionsCache.find((x) => x.id === currentDetailSubmissionId);
        if (cached) { cached.questions = updated.exists() ? updated.val() : {}; renderSubmissionRows(); }
      } catch (err) { showToast(`Delete failed: ${err.message || err}`, "error"); }
    });

    list.appendChild(card);
  });
}

/** Re-derives the yes/no/opens/sent stats renderModelPageQuestions needs and
 * re-renders the list - used after adding a question, when those values
 * aren't already sitting in local scope from the initial openModelPage load. */
async function refreshModelPageQuestions() {
  if (!_modelPageItem) return;
  const modelKey = _modelPageItem.modelKey;
  let data = { yes: 0, no: 0, opens: 0, sent: 0 };
  if (modelKey) {
    const snap = await get(dbRef(db, `${ROOT}/models/${modelKey}/data`));
    if (snap.exists()) data = { ...data, ...snap.val() };
  }
  const sent = toInt(data.sent, 0) || toInt(_modelPageItem.pushedCount, 0);
  await renderModelPageQuestions(_modelPageItem, toInt(data.yes, 0), toInt(data.no, 0), toInt(data.opens, 0), sent);
}

async function addQuestionToModelPage() {
  if (!currentDetailSubmissionId || !_modelPageItem) return;
  const type  = byId("modelPageQuestionTypeSelect")?.value;
  const text  = (byId("modelPageQuestionTextInput")?.value || "").trim();
  const msgEl = byId("modelPageQuestionMessage");

  if (!text) { if (msgEl) msgEl.textContent = "Question text is required."; return; }

  let options = [];
  if (type === "multiple_choice") {
    options = [...(byId("modelPageMcOptionsList")?.querySelectorAll(".mc-option") || [])]
      .map((el) => el.value.trim()).filter(Boolean);
    if (options.length < 2) { if (msgEl) msgEl.textContent = "Add at least 2 options."; return; }
  }

  try {
    const questionRef = push(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
    await set(questionRef, { type, text, options, createdAt: Date.now() });

    if (msgEl) msgEl.textContent = "";
    const textInput = byId("modelPageQuestionTextInput");
    if (textInput) textInput.value = "";
    const mcList = byId("modelPageMcOptionsList");
    if (mcList) mcList.innerHTML = '<input class="mc-option" placeholder="Option 1" /><input class="mc-option" placeholder="Option 2" />';
    byId("modelPageMcOptionsContainer")?.classList.add("hidden");
    const typeSelect = byId("modelPageQuestionTypeSelect");
    if (typeSelect) typeSelect.value = "yes_no";

    await refreshModelPageQuestions();

    const snap = await get(dbRef(db, `${ROOT}/submissions/${currentDetailSubmissionId}/questions`));
    const cached = submissionsCache.find((x) => x.id === currentDetailSubmissionId);
    if (cached) { cached.questions = snap.exists() ? snap.val() : {}; renderSubmissionRows(); }
  } catch (err) {
    if (msgEl) msgEl.textContent = `Could not add question: ${err.message || err}`;
  }
}

async function renderModelPageDistribution(item) {
  const countEl   = byId("modelPageDistCount");
  const listEl    = byId("modelPageUsersList");
  const msgEl     = byId("modelPageSendMessage");
  const sendBtn   = byId("modelPageSendButton");
  const actionsEl = byId("modelPageSendActions");
  if (!listEl) return;

  const isAdmin  = normalizeRole(currentProfile?.role) === "admin";
  const modelKey = item.modelKey;

  // Partners can only send if the model is approved
  if (!isAdmin && item.status !== "approved") {
    listEl.innerHTML = `<p class="muted">Model must be approved before it can be sent to users.</p>`;
    if (actionsEl) actionsEl.style.display = "none";
    return;
  }

  if (!modelKey) {
    listEl.innerHTML = `<p class="muted">Model has not been published to the app yet.</p>`;
    if (actionsEl) actionsEl.style.display = "none";
    return;
  }

  // modelKey is set on every submission at upload time, long before it's ever
  // pushed - so it being present here doesn't mean cornucopia/models/{modelKey}
  // exists yet. Sending without this check silently creates a name-less,
  // storagePath-less stub via the sent-count update() below, which the apps
  // then render as a broken "3D model unavailable" entry.
  const catalogSnap = await get(dbRef(db, `${ROOT}/models/${modelKey}`));
  if (!catalogSnap.exists()) {
    listEl.innerHTML = `<p class="muted">This model hasn't been pushed to the app yet — push it from Manage Uploads first.</p>`;
    if (actionsEl) actionsEl.style.display = "none";
    return;
  }

  listEl.innerHTML = `<p class="muted" style="padding:8px 0">Loading users…</p>`;

  try {
    const [usersSnap, subsSnap] = await Promise.all([
      get(dbRef(db, `${ROOT}/users`)),
      isAdmin
        ? Promise.resolve(null)
        : get(dbRef(db, `${ROOT}/partners/${currentUser.uid}/subscribers`))
    ]);

    const allUsers     = usersSnap.exists() ? usersSnap.val() : {};
    const subscribers  = (!isAdmin && subsSnap?.exists()) ? Object.keys(subsSnap.val()) : null;

    // Find which users already have this model
    const distUsers = new Set();
    const checks = Object.keys(allUsers).map((uid) =>
      get(dbRef(db, `${ROOT}/users/${uid}/models/${modelKey}`))
        .then((s) => { if (s.exists()) distUsers.add(uid); })
        .catch(() => {})
    );
    await Promise.all(checks);

    if (countEl) countEl.textContent = distUsers.size;

    // Build eligible user list
    const eligible = Object.entries(allUsers).filter(([uid, u]) => {
      const role = String(u?.role || "").toLowerCase();
      if (role === "admin" || role === "partner") return false;
      if (subscribers !== null && !subscribers.includes(uid)) return false;
      return true;
    });

    listEl.innerHTML = "";

    if (!eligible.length) {
      listEl.innerHTML = `<p class="muted">No eligible users found.</p>`;
      if (actionsEl) actionsEl.style.display = "none";
      return;
    }

    if (actionsEl) actionsEl.style.display = "";

    // Select-all row
    const saLabel = document.createElement("label");
    saLabel.className = "user-row select-all-row";
    saLabel.innerHTML = `<input type="checkbox" class="mp-sa-cb" /> <span><strong>Select all</strong></span>`;
    listEl.appendChild(saLabel);

    eligible.forEach(([uid, u]) => {
      const alreadySent = distUsers.has(uid);
      const name = u?.name || u?.email || u?.businessName || uid;
      const label = document.createElement("label");
      label.className = `user-row${alreadySent ? " user-row-sent" : ""}`;
      label.innerHTML = `
        <input type="checkbox" value="${escapeHtml(uid)}"
               ${alreadySent ? 'checked disabled title="Already sent"' : ""} />
        <span>${escapeHtml(name)} <small>(${escapeHtml(uid)})</small></span>
        ${alreadySent ? '<span class="dist-sent-badge">✓ sent</span>' : ""}`;
      listEl.appendChild(label);
    });

    // Wire select-all
    const saCb = listEl.querySelector(".mp-sa-cb");
    const cbs  = () => [...listEl.querySelectorAll("input[type='checkbox']:not(.mp-sa-cb):not(:disabled)")];
    if (saCb) {
      saCb.addEventListener("change", () => cbs().forEach((c) => { c.checked = saCb.checked; }));
    }

    // Wire send button
    if (sendBtn) {
      sendBtn.onclick = async () => {
        const selected = cbs().filter((c) => c.checked).map((c) => c.value);
        if (!selected.length) { if (msgEl) msgEl.textContent = "Select at least one user."; return; }

        sendBtn.disabled = true;
        sendBtn.textContent = "Sending…";
        if (msgEl) msgEl.textContent = "";
        let assigned = 0;

        try {
          for (const uid of selected) {
            const ref = dbRef(db, `${ROOT}/users/${uid}/models/${modelKey}`);
            if ((await get(ref)).exists()) continue;
            await set(ref, { MName: modelKey, saved: false, Rating: "0.0", answer: "pending" });
            assigned += 1;
          }

          // Update sent count in model data
          if (assigned > 0) {
            try {
              const mdr = dbRef(db, `${ROOT}/models/${modelKey}/data`);
              const cur = (await get(mdr)).val() || {};
              await update(mdr, { sent: toInt(cur.sent, 0) + assigned });
            } catch (e) { console.warn("Could not update sent count:", e); }
          }

          showToast(`✓ Model sent to ${assigned} user${assigned !== 1 ? "s" : ""}.`);
          if (msgEl) msgEl.textContent = `Sent to ${assigned} user${assigned !== 1 ? "s" : ""}.`;

          // Refresh distribution panel with updated counts
          await renderModelPageDistribution(item);

          // Sync cache
          const cached = submissionsCache.find((x) => x.id === item.id);
          if (cached) { cached.pushedCount = toInt(cached.pushedCount, 0) + assigned; renderSubmissionRows(); }
        } catch (err) {
          if (msgEl) msgEl.textContent = `Send failed: ${err.message || err}`;
        } finally {
          sendBtn.disabled = false;
          sendBtn.textContent = "Send to Selected";
        }
      };
    }
  } catch (err) {
    listEl.innerHTML = `<p class="muted">Could not load users: ${err.message || err}</p>`;
  }
}


function setAdminVisibility(isAdmin) {
  document.querySelectorAll(".admin-only").forEach((el) => {
    el.style.display = isAdmin ? "" : "none";
  });
  document.querySelectorAll(".partner-metric").forEach((el) => {
    el.style.display = isAdmin ? "none" : "";
  });
}

function setUploadUIForRole(isAdmin) {
  if (isAdmin) {
    if (uploadButton) uploadButton.textContent = "Upload and Publish";
    document.querySelectorAll(".partner-only").forEach((el) => {
      el.style.display = "none";
    });
    if (targetModeInput) { targetModeInput.style.display = ""; targetModeInput.value = "all_users"; targetModeInput?.dispatchEvent(new Event("change")); }
    if (targetUserIdsInput) targetUserIdsInput.style.display = "";
    if (partnerSubscribersPanel) partnerSubscribersPanel.style.display = "none";
  } else {
    if (uploadButton) uploadButton.textContent = "Upload For Review";
    document.querySelectorAll(".partner-only").forEach((el) => {
      el.style.display = "";
    });
    if (partnerSubscribersPanel) partnerSubscribersPanel.style.display = "";
    if (targetModeInput) { targetModeInput.value = "specific_users"; targetModeInput.style.display = "none"; }
    if (targetUserIdsInput) targetUserIdsInput.style.display = "none";
    if (partnerDeliveryMessage) partnerDeliveryMessage.textContent = "";
  }
}

async function upsertModelUploadTracker(data) {
  if (!data?.modelKey) return;
  const trackerRef = dbRef(db, `${ROOT}/models_uploads/${data.modelKey}`);
  const existingSnap = await get(trackerRef);
  const existing = existingSnap.exists() ? existingSnap.val() : {};
  await update(trackerRef, {
    ...existing,
    ...data,
    updatedAt: Date.now()
  });
}

function normalizeRole(role) {
  const r = String(role || "").trim().toLowerCase();
  if (r === "admin") return "admin";
  if (r === "partner") return "partner";
  return r || "";
}

function formatTs(ts) {
  if (!ts) return "-";
  return new Date(ts).toLocaleString();
}

function byId(id) {
  return document.getElementById(id);
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

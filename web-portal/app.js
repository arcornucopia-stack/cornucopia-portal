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
  uploadBytesResumable
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
const pendingBody = byId("pendingBody");
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
  // Upload modal
  byId("openUploadModal")?.addEventListener("click", () => {
    byId("uploadModal")?.classList.remove("hidden");
  });
  byId("closeUploadModal")?.addEventListener("click", closeUploadModal);
  byId("uploadModal")?.addEventListener("click", (e) => {
    if (e.target === byId("uploadModal")) closeUploadModal();
  });

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
}

function closeAnalytics() {
  byId("analyticsModal")?.classList.add("hidden");
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

async function sendPortalNotification(toUid, message, type = "info") {
  const ref = push(dbRef(db, `${ROOT}/portal_notifications/${toUid}`));
  await set(ref, { message, type, createdAt: Date.now(), read: false });
}

async function notifyAllAdmins(message, type = "info") {
  try {
    const usersSnap = await get(dbRef(db, `${ROOT}/users`));
    if (!usersSnap.exists()) return;
    const promises = [];
    usersSnap.forEach(child => {
      const role = String(child.val()?.role || "").toLowerCase();
      if (role === "admin") promises.push(sendPortalNotification(child.key, message, type));
    });
    await Promise.all(promises);
  } catch (err) { console.warn("Could not notify admins:", err); }
}

async function loadPortalNotifications() {
  const list = byId("notifList");
  const badge = byId("notifBadge");
  if (!list || !currentUser) return;
  try {
    const snap = await get(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}`));
    const items = snap.exists() ? Object.entries(snap.val()).sort((a, b) => (b[1].createdAt || 0) - (a[1].createdAt || 0)) : [];
    const unread = items.filter(([, n]) => !n.read).length;

    if (badge) {
      badge.textContent = unread > 99 ? "99+" : String(unread);
      badge.classList.toggle("hidden", unread === 0);
    }

    list.innerHTML = "";
    if (!items.length) {
      list.innerHTML = "<p class='muted'>No notifications yet.</p>";
      return;
    }
    items.forEach(([id, n]) => {
      const div = document.createElement("div");
      div.className = `notif-item${n.read ? "" : " notif-unread"}`;
      div.innerHTML = `
        <span class="notif-dot notif-dot-${n.type || "info"}"></span>
        <div class="notif-body">
          <p>${escapeHtml(n.message)}</p>
          <small class="muted">${formatTs(n.createdAt)}</small>
        </div>
      `;
      div.addEventListener("click", () => {
        set(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}/${id}/read`), true);
        div.classList.remove("notif-unread");
        if (badge) {
          const cur = parseInt(badge.textContent) || 0;
          if (cur > 0) {
            const next = cur - 1;
            badge.textContent = String(next);
            badge.classList.toggle("hidden", next === 0);
          }
        }
      });
      list.appendChild(div);
    });

    // Mark all read after viewing
    if (unread > 0 && byId("notifications")?.classList.contains("active")) {
      items.filter(([, n]) => !n.read).forEach(([id]) => {
        set(dbRef(db, `${ROOT}/portal_notifications/${currentUser.uid}/${id}/read`), true);
      });
    }
  } catch (err) { list.innerHTML = `<p class='muted'>Could not load notifications: ${err.message}</p>`; }
}

function showToast(message, type = "success") {
  const toast = byId("toastNotification");
  if (!toast) return;
  toast.textContent = message;
  toast.className = `toast toast-${type}`;
  toast.classList.remove("hidden");
  setTimeout(() => toast.classList.add("hidden"), 5000);
}

function closeUploadModal() {
  byId("uploadModal")?.classList.add("hidden");
  if (uploadProgress) uploadProgress.value = 0;
  if (uploadMessage) uploadMessage.textContent = "";
  if (displayNameInput) displayNameInput.value = "";
  const descInput = byId("modelDescriptionInput");
  if (descInput) descInput.value = "";
  if (glbInput) glbInput.value = "";
}

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
    th.style.display = isAdmin ? "" : "none";
  });

  setAdminVisibility(isAdmin);
  setUploadUIForRole(isAdmin);
  setSubmissionFilter("all");
  setActiveScreen("dashboard");
  await refreshAll();
});

async function refreshAll() {
  await Promise.allSettled([
    loadMySubmissions(),
    loadApprovalQueue(),
    loadAnalytics(),
    loadPublishedModels(),
    loadDispatchData(),
    loadPartnerSubscriptionAdminData(),
    loadPartnerSubscribersForUpload(),
    loadPartnerDeliveryData(),
    loadPortalNotifications()
  ]);
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

        closeUploadModal();
        const isAdminUpload = normalizeRole(currentProfile.role) === "admin";
        showToast(
          isAdminUpload
            ? "🎉 Model uploaded and published to the app!"
            : "🎉 Model uploaded successfully! Our team will review it shortly."
        );
        // Notify admins of new upload (partners only)
        if (!isAdminUpload) {
          const partnerName = currentProfile.businessName || currentProfile.name || businessId;
          await notifyAllAdmins(`📦 ${partnerName} submitted a new model for review.`, "info");
        }
        await refreshAll();
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

async function loadApprovalQueue() {
  pendingBody.innerHTML = "";
  if (!currentProfile || normalizeRole(currentProfile.role) !== "admin") return;
  try {
    const all = await getAllSubmissions();
    all.sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));

    all.forEach((item) => {
      const approved = item.status === "approved";
      const rejected = item.status === "rejected";
      const pushed = !!item.pushedToApp;

      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${escapeHtml(item.businessName || item.businessId || "-")}</td>
        <td>${escapeHtml(item.displayName || item.fileName || "-")}</td>
        <td>${escapeHtml(targetLabel(item))}</td>
        <td><span class="status-pill status-${item.status || "pending"}">${escapeHtml(statusLabel(item))}</span></td>
        <td>${formatTs(item.createdAt)}</td>
        <td>
          <div class="row-actions">
            <button class="secondary" data-id="${item.id}" data-action="approve"
              ${approved || rejected ? "disabled" : ""}>Approve</button>
            <button class="danger" data-id="${item.id}" data-action="reject"
              ${approved || rejected ? "disabled" : ""}>Reject</button>
            <button class="success" data-id="${item.id}" data-action="push"
              ${!approved || pushed ? "disabled" : ""}>
              ${pushed ? "Pushed ✓" : "Push to App"}
            </button>
          </div>
        </td>
      `;
      pendingBody.appendChild(tr);
    });

    pendingBody.querySelectorAll("button[data-id]").forEach((btn) => {
      btn.addEventListener("click", async () => {
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

          // Flash row before refresh
          const row = btn.closest("tr");
          const flashClass = { approve: "row-flash-approved", reject: "row-flash-rejected", push: "row-flash-pushed" }[action];
          if (row && flashClass) {
            row.classList.add(flashClass);
            await new Promise((res) => setTimeout(res, action === "push" ? 1800 : 1400));
            row.classList.remove(flashClass);
          }
          showToast(successMessages[action]);
          await refreshAll();
        } catch (err) {
          showToast(`Action failed: ${err.message || err}`, "error");
          btn.textContent = originalText;
          allBtns?.forEach((b) => { b.disabled = false; });
        }
      });
    });
  } catch (err) {
    uploadMessage.textContent = `Could not load approval queue: ${err.message || err}`;
  }
}

async function updateSubmissionStatus(id, status) {
  await update(dbRef(db, `${ROOT}/submissions/${id}`), {
    status,
    decisionBy: currentUser.uid,
    approvedAt: status === "approved" ? Date.now() : null,
    rejectedAt: status === "rejected" ? Date.now() : null
  });

  if (status === "approved") {
    await ensureModelRecordFromSubmission(id);
  }

  const submissionSnap = await get(dbRef(db, `${ROOT}/submissions/${id}`));
  if (submissionSnap.exists()) {
    const s = submissionSnap.val();
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

async function ensureModelRecordFromSubmission(submissionId) {
  const submissionSnap = await get(dbRef(db, `${ROOT}/submissions/${submissionId}`));
  if (!submissionSnap.exists()) return;

  const item = submissionSnap.val();
  const modelKey = item.modelKey || sanitizeKey(stripGlbExtension(item.fileName || `model_${submissionId}`));
  const modelRef = dbRef(db, `${ROOT}/models/${modelKey}`);
  const modelSnap = await get(modelRef);
  if (modelSnap.exists()) return;

  // Read the first question assigned to this submission
  let questionText = item.question || "";
  if (!questionText) {
    const questionsSnap = await get(dbRef(db, `${ROOT}/submissions/${submissionId}/questions`));
    if (questionsSnap.exists()) {
      const questions = Object.values(questionsSnap.val());
      const first = questions.find(q => q.type === "yes_no") || questions[0];
      if (first) questionText = first.text;
    }
  }
  if (!questionText) questionText = "Would you like this product?";

  await set(modelRef, {
    name: item.displayName || stripGlbExtension(item.fileName || modelKey),
    modelNamee: modelKey,
    picPathh: item.picPathh || modelKey,
    question: questionText,
    storagePath: item.storagePath || "",
    data: { sent: 0, saved: 0, yes: 0, no: 0, rating: "0.0" }
  });

  await update(dbRef(db, `${ROOT}/submissions/${submissionId}`), { modelKey });
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

  const modelKey = item.modelKey || sanitizeKey(stripGlbExtension(item.fileName || `model_${submissionId}`));
  const modelRef = dbRef(db, `${ROOT}/models/${modelKey}`);
  const modelSnap = await get(modelRef);
  const existingModel = modelSnap.exists() ? modelSnap.val() : {};

  const mergedModel = {
    ...existingModel,
    name: item.displayName || stripGlbExtension(item.fileName || modelKey),
    modelNamee: modelKey,
    picPathh: item.picPathh || existingModel.picPathh || modelKey,
    question: item.question || existingModel.question || "Would you like this product?",
    storagePath: item.storagePath,
    data: {
      sent: toInt(existingModel?.data?.sent, 0),
      saved: toInt(existingModel?.data?.saved, 0),
      yes: toInt(existingModel?.data?.yes, 0),
      no: toInt(existingModel?.data?.no, 0),
      rating: String(existingModel?.data?.rating ?? "0.0")
    }
  };

  await set(modelRef, mergedModel);

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
  dispatchMessage.textContent = "";

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

async function loadPartnerDeliveryData() {
  if (!currentProfile || (currentProfile.role || "").toLowerCase() === "admin") return;
  partnerDeliveryMessage.textContent = "";

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
    for (const uid of selected) {
      const userModelRef = dbRef(db, `${ROOT}/users/${uid}/models/${modelKey}`);
      const existing = await get(userModelRef);
      if (existing.exists()) continue;
      await set(userModelRef, { MName: modelKey, saved: false, Rating: "0.0", answer: "pending" });
      assigned += 1;
    }
    partnerDeliveryMessage.textContent = `Sent model to ${assigned} users.`;
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

    const sent = toInt(data.sent, 0);
    const saved = toInt(data.saved, 0);
    const yes = toInt(data.yes, 0);
    const no = toInt(data.no, 0);
    const opens = yes + no;
    const rating = parseFloat(data.rating) || 0;
    const responseRate = sent > 0 ? Math.round((opens / sent) * 100) : 0;
    const total = yes + no || 1;

    const s = (id, val) => { const el = byId(id); if (el) el.textContent = val; };
    s("statSent", sent);
    s("statOpens", opens);
    s("statSaves", saved);
    s("statResponseRate", `${responseRate}%`);
    s("statYesLabel", `${yes} Yes`);
    s("statNoLabel", `${no} No`);
    s("statRating", rating.toFixed(1));

    const yesBar = byId("voteBarYes");
    const noBar = byId("voteBarNo");
    if (yesBar) yesBar.style.width = `${Math.round((yes / total) * 100)}%`;
    if (noBar) noBar.style.width = `${Math.round((no / total) * 100)}%`;

    const stars = "★".repeat(Math.round(rating)) + "☆".repeat(5 - Math.round(rating));
    s("statStars", stars);

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

  if (screenId === "notifications") loadPortalNotifications();
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

  const isAdmin = normalizeRole(currentProfile?.role) === "admin";

  rows.forEach((item) => {
    const qCount = item.questions ? Object.keys(item.questions).length : 0;
    const qBadgeClass = qCount > 0 ? "q-count-badge has-questions" : "q-count-badge";
    const qLabel = qCount > 0 ? `${qCount} question${qCount !== 1 ? "s" : ""}` : "0 questions";
    const displayName = item.displayName || item.fileName || "-";

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>
        <div style="display:flex;align-items:flex-start;gap:8px;flex-wrap:wrap">
          <button class="model-name-link" data-analytics="true">
            <strong>${escapeHtml(displayName)}</strong><br>
            <span class="muted" style="font-size:12px">${escapeHtml(item.fileName || "")}</span>
          </button>
          <button class="model-details-btn" type="button" title="Edit model details">Details</button>
        </div>
      </td>
      ${isAdmin ? `<td>${escapeHtml(item.businessName || item.businessId || "-")}</td>` : ""}
      <td><span class="status-pill status-${item.status || "pending"}">${escapeHtml(statusLabel(item))}</span></td>
      <td>
        <div class="q-cell">
          <span class="${qBadgeClass}">${qLabel}</span>
          <button class="manage-questions-btn" type="button">✏ Edit</button>
        </div>
      </td>
      <td>${formatTs(item.createdAt)}</td>
    `;
    tr.querySelector(".manage-questions-btn").addEventListener("click", () => openModelDetail(item));
    tr.querySelector("[data-analytics]").addEventListener("click", () => openModelAnalytics(item));
    tr.querySelector(".model-details-btn").addEventListener("click", () => openModelDetailsEditor(item));
    mySubmissionsBody.appendChild(tr);
  });
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

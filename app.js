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
const glbInput = byId("glbInput");
const uploadButton = byId("uploadButton");
const uploadProgress = byId("uploadProgress");
const uploadMessage = byId("uploadMessage");

const mySubmissionsBody = byId("mySubmissionsBody");
const pendingBody = byId("pendingBody");

const totalUploads = byId("totalUploads");
const approvedUploads = byId("approvedUploads");
const openCount = byId("openCount");
const saveCount = byId("saveCount");

let currentUser = null;
let currentProfile = null;

bindNavigation();

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

targetModeInput.addEventListener("change", () => {
  targetUserIdsInput.style.display = targetModeInput.value === "specific_users" ? "block" : "none";
});
targetModeInput.dispatchEvent(new Event("change"));

onAuthStateChanged(auth, async (user) => {
  currentUser = user;

  if (!user) {
    authScreen.classList.remove("hidden");
    appScreen.classList.add("hidden");
    currentProfile = null;
    return;
  }

  const profileSnap = await get(child(dbRef(db), `${ROOT}/users/${user.uid}`));
  if (!profileSnap.exists()) {
    authMessage.textContent = `No user role found. Create ${ROOT}/users/{uid} in Realtime Database.`;
    await signOut(auth);
    return;
  }

  currentProfile = profileSnap.val();
  roleBadge.textContent = currentProfile.role || "partner";
  authScreen.classList.add("hidden");
  appScreen.classList.remove("hidden");

  setAdminVisibility(currentProfile.role === "admin");
  await refreshAll();
});

async function refreshAll() {
  await Promise.all([
    loadMySubmissions(),
    loadApprovalQueue(),
    loadAnalytics()
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

  const businessId = currentProfile.businessId || currentUser.uid;
  const businessName = (businessNameInput.value || currentProfile.businessName || "").trim();
  if (!businessName) {
    uploadMessage.textContent = "Business name is required.";
    return;
  }

  const targetMode = targetModeInput.value;
  const targetUserIds = parseTargetUserIds(targetUserIdsInput.value);
  if (targetMode === "specific_users" && targetUserIds.length === 0) {
    uploadMessage.textContent = "Specific users mode requires at least one user UID.";
    return;
  }

  uploadMessage.textContent = "Uploading...";

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
      uploadMessage.textContent = error.message;
    },
    async () => {
      await set(submissionRef, {
        submissionId,
        modelKey,
        businessId,
        businessName,
        uploaderUid: currentUser.uid,
        uploaderRole: currentProfile.role || "partner",
        fileName: file.name,
        displayName: (displayNameInput.value || baseName).trim(),
        question: (questionInput.value || "Would you like this product?").trim(),
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

      if ((currentProfile.role || "").toLowerCase() === "admin") {
        await updateSubmissionStatus(submissionId, "approved");
        await pushSubmissionToApp(submissionId, { silent: true });
      }

      uploadProgress.value = 0;
      glbInput.value = "";
      displayNameInput.value = "";
      questionInput.value = "";
      targetUserIdsInput.value = "";
      uploadMessage.textContent = (currentProfile.role || "").toLowerCase() === "admin"
        ? "Uploaded and pushed to app."
        : "Uploaded. Submission is waiting for admin approval/push.";
      await refreshAll();
    }
  );
}

async function loadMySubmissions() {
  mySubmissionsBody.innerHTML = "";
  if (!currentUser || !currentProfile) return;

  const all = await getAllSubmissions();
  const businessId = currentProfile.businessId || currentUser.uid;
  const rows = currentProfile.role === "admin"
    ? all
    : all.filter((x) => x.businessId === businessId);

  rows.sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));

  let approved = 0;
  rows.forEach((item) => {
    if (item.status === "approved") approved += 1;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(item.fileName || "-")}</td>
      <td>${escapeHtml(targetLabel(item))}</td>
      <td><span class="status-pill status-${item.status || "pending"}">${escapeHtml(statusLabel(item))}</span></td>
      <td>${formatTs(item.createdAt)}</td>
    `;
    mySubmissionsBody.appendChild(tr);
  });

  totalUploads.textContent = String(rows.length);
  approvedUploads.textContent = String(approved);
}

async function loadApprovalQueue() {
  pendingBody.innerHTML = "";
  if (!currentProfile || currentProfile.role !== "admin") return;

  const all = await getAllSubmissions();
  all.sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));

  all.forEach((item) => {
    const canPush = item.status !== "rejected" && !item.pushedToApp;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(item.businessName || item.businessId || "-")}</td>
      <td>${escapeHtml(item.fileName || "-")}</td>
      <td>${escapeHtml(targetLabel(item))}</td>
      <td><span class="status-pill status-${item.status || "pending"}">${escapeHtml(statusLabel(item))}</span></td>
      <td>${formatTs(item.createdAt)}</td>
      <td>
        <div class="row-actions">
          <button class="secondary" data-id="${item.id}" data-action="approve">Approve</button>
          <button class="danger" data-id="${item.id}" data-action="reject">Reject</button>
          ${canPush ? `<button class="success" data-id="${item.id}" data-action="push">Push to App</button>` : ""}
        </div>
      </td>
    `;
    pendingBody.appendChild(tr);
  });

  pendingBody.querySelectorAll("button[data-id]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      const action = btn.getAttribute("data-action");

      if (action === "approve") {
        await updateSubmissionStatus(id, "approved");
      } else if (action === "reject") {
        await updateSubmissionStatus(id, "rejected");
      } else if (action === "push") {
        await pushSubmissionToApp(id);
      }

      await refreshAll();
    });
  });
}

async function updateSubmissionStatus(id, status) {
  await update(dbRef(db, `${ROOT}/submissions/${id}`), {
    status,
    decisionBy: currentUser.uid,
    approvedAt: status === "approved" ? Date.now() : null,
    rejectedAt: status === "rejected" ? Date.now() : null
  });
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
      targetUserIds = allUserIds;
    }
  }

  let assigned = 0;
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

  await update(submissionRef, {
    status: "approved",
    pushedToApp: true,
    pushedAt: Date.now(),
    pushedCount: assigned,
    modelKey,
    decisionBy: currentUser.uid
  });

  if (!options.silent) {
    alert(`Model pushed to app data. Assigned to ${assigned} users.`);
  }
}

async function loadAnalytics() {
  if (!currentProfile || !currentUser) return;

  const businessId = currentProfile.businessId || currentUser.uid;
  const snap = await get(dbRef(db, `${ROOT}/events`));
  const raw = snap.exists() ? snap.val() : {};

  let opens = 0;
  let saves = 0;

  Object.values(raw).forEach((event) => {
    if (!event || event.businessId !== businessId) return;
    if (event.eventType === "open") opens += 1;
    if (event.eventType === "save") saves += 1;
  });

  openCount.textContent = String(opens);
  saveCount.textContent = String(saves);
}

async function getAllSubmissions() {
  const snap = await get(dbRef(db, `${ROOT}/submissions`));
  const raw = snap.exists() ? snap.val() : {};
  return Object.entries(raw).map(([id, value]) => ({ id, ...value }));
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
      navButtons.forEach((x) => x.classList.remove("active"));
      btn.classList.add("active");

      const target = btn.getAttribute("data-screen");
      [...document.querySelectorAll(".screen")].forEach((screen) => {
        screen.classList.toggle("active", screen.id === target);
      });

      byId("welcomeText").textContent = btn.textContent;
    });
  });
}

function setAdminVisibility(isAdmin) {
  document.querySelectorAll(".admin-only").forEach((el) => {
    el.style.display = isAdmin ? "" : "none";
  });
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

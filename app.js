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
  } catch (err) {
    authMessage.textContent = err.message;
  }
});

logoutButton.addEventListener("click", () => signOut(auth));
uploadButton.addEventListener("click", uploadModel);

onAuthStateChanged(auth, async (user) => {
  currentUser = user;

  if (!user) {
    authScreen.classList.remove("hidden");
    appScreen.classList.add("hidden");
    currentProfile = null;
    return;
  }

  const profileSnap = await get(child(dbRef(db), `users/${user.uid}`));
  if (!profileSnap.exists()) {
    authMessage.textContent = "No user role found. Create users/{uid} in Realtime Database.";
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
    loadPendingApprovals(),
    loadAnalytics()
  ]);
}

async function uploadModel() {
  if (!currentUser || !currentProfile) return;

  const file = glbInput.files?.[0];
  if (!file) {
    uploadMessage.textContent = "Choose a .glb file first.";
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

  uploadMessage.textContent = "Uploading...";

  const submissionRef = push(dbRef(db, "submissions"));
  const submissionId = submissionRef.key;
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
        businessId,
        businessName,
        uploaderUid: currentUser.uid,
        fileName: file.name,
        storagePath,
        status: "pending",
        createdAt: Date.now(),
        approvedAt: null,
        rejectedAt: null,
        decisionBy: null
      });

      uploadProgress.value = 0;
      glbInput.value = "";
      uploadMessage.textContent = "Uploaded. Waiting for admin approval.";
      await refreshAll();
    }
  );
}

async function loadMySubmissions() {
  mySubmissionsBody.innerHTML = "";
  if (!currentUser || !currentProfile) return;

  const snap = await get(dbRef(db, "submissions"));
  const raw = snap.exists() ? snap.val() : {};
  const all = Object.entries(raw).map(([id, value]) => ({ id, ...value }));

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
      <td><span class="status-pill status-${item.status || "pending"}">${item.status || "pending"}</span></td>
      <td>${formatTs(item.createdAt)}</td>
    `;
    mySubmissionsBody.appendChild(tr);
  });

  totalUploads.textContent = String(rows.length);
  approvedUploads.textContent = String(approved);
}

async function loadPendingApprovals() {
  pendingBody.innerHTML = "";
  if (!currentProfile || currentProfile.role !== "admin") return;

  const snap = await get(dbRef(db, "submissions"));
  const raw = snap.exists() ? snap.val() : {};
  const pending = Object.entries(raw)
    .map(([id, value]) => ({ id, ...value }))
    .filter((x) => x.status === "pending")
    .sort((a, b) => (a.createdAt || 0) - (b.createdAt || 0));

  pending.forEach((item) => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(item.businessName || item.businessId || "-")}</td>
      <td>${escapeHtml(item.fileName || "-")}</td>
      <td>${formatTs(item.createdAt)}</td>
      <td>
        <div class="row-actions">
          <button data-id="${item.id}" data-action="approve">Approve</button>
          <button data-id="${item.id}" data-action="reject">Reject</button>
        </div>
      </td>
    `;
    pendingBody.appendChild(tr);
  });

  pendingBody.querySelectorAll("button[data-id]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      const action = btn.getAttribute("data-action");

      await update(dbRef(db, `submissions/${id}`), {
        status: action === "approve" ? "approved" : "rejected",
        decisionBy: currentUser.uid,
        approvedAt: action === "approve" ? Date.now() : null,
        rejectedAt: action === "reject" ? Date.now() : null
      });

      await refreshAll();
    });
  });
}

async function loadAnalytics() {
  if (!currentProfile || !currentUser) return;

  const businessId = currentProfile.businessId || currentUser.uid;
  const snap = await get(dbRef(db, "events"));
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

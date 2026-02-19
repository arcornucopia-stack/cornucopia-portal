import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import {
  getAuth,
  signInWithEmailAndPassword,
  onAuthStateChanged,
  signOut
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-auth.js";
import {
  getFirestore,
  doc,
  getDoc,
  addDoc,
  collection,
  serverTimestamp,
  query,
  where,
  getDocs,
  orderBy,
  updateDoc
} from "https://www.gstatic.com/firebasejs/10.12.5/firebase-firestore.js";
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
const db = getFirestore(app);
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

  const userDoc = await getDoc(doc(db, "users", user.uid));
  if (!userDoc.exists()) {
    authMessage.textContent = "No user role found. Create users/{uid} in Firestore.";
    await signOut(auth);
    return;
  }

  currentProfile = userDoc.data();
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

  const businessName = (businessNameInput.value || currentProfile.businessName || "").trim();
  if (!businessName) {
    uploadMessage.textContent = "Business name is required.";
    return;
  }

  uploadMessage.textContent = "Uploading...";
  const submissionId = crypto.randomUUID();
  const storagePath = `partner_uploads/${currentProfile.businessId || currentUser.uid}/${submissionId}/${file.name}`;
  const storageRef = ref(storage, storagePath);
  const task = uploadBytesResumable(storageRef, file, { contentType: "model/gltf-binary" });

  task.on("state_changed", (snapshot) => {
    const pct = Math.round((snapshot.bytesTransferred / snapshot.totalBytes) * 100);
    uploadProgress.value = pct;
  }, (error) => {
    uploadMessage.textContent = error.message;
  }, async () => {
    await addDoc(collection(db, "submissions"), {
      submissionId,
      businessId: currentProfile.businessId || currentUser.uid,
      businessName,
      uploaderUid: currentUser.uid,
      fileName: file.name,
      storagePath,
      status: "pending",
      createdAt: serverTimestamp()
    });

    uploadProgress.value = 0;
    glbInput.value = "";
    uploadMessage.textContent = "Uploaded. Waiting for admin approval.";
    await refreshAll();
  });
}

async function loadMySubmissions() {
  if (!currentUser || !currentProfile) return;
  mySubmissionsBody.innerHTML = "";

  let q;
  if (currentProfile.role === "admin") {
    q = query(collection(db, "submissions"), orderBy("createdAt", "desc"));
  } else {
    q = query(
      collection(db, "submissions"),
      where("businessId", "==", currentProfile.businessId || currentUser.uid),
      orderBy("createdAt", "desc")
    );
  }

  const snap = await getDocs(q);
  let rows = 0;
  let approved = 0;
  snap.forEach((d) => {
    const item = d.data();
    rows += 1;
    if (item.status === "approved") approved += 1;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(item.fileName || "-")}</td>
      <td><span class="status-pill status-${item.status || "pending"}">${item.status || "pending"}</span></td>
      <td>${formatTs(item.createdAt)}</td>
    `;
    mySubmissionsBody.appendChild(tr);
  });

  totalUploads.textContent = String(rows);
  approvedUploads.textContent = String(approved);
}

async function loadPendingApprovals() {
  pendingBody.innerHTML = "";
  if (!currentProfile || currentProfile.role !== "admin") return;

  const q = query(collection(db, "submissions"), where("status", "==", "pending"), orderBy("createdAt", "asc"));
  const snap = await getDocs(q);

  snap.forEach((d) => {
    const item = d.data();
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(item.businessName || item.businessId || "-")}</td>
      <td>${escapeHtml(item.fileName || "-")}</td>
      <td>${formatTs(item.createdAt)}</td>
      <td>
        <div class="row-actions">
          <button data-id="${d.id}" data-action="approve">Approve</button>
          <button data-id="${d.id}" data-action="reject">Reject</button>
        </div>
      </td>
    `;
    pendingBody.appendChild(tr);
  });

  pendingBody.querySelectorAll("button[data-id]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = btn.getAttribute("data-id");
      const action = btn.getAttribute("data-action");
      await updateDoc(doc(db, "submissions", id), {
        status: action === "approve" ? "approved" : "rejected",
        decisionBy: currentUser.uid,
        approvedAt: action === "approve" ? serverTimestamp() : null,
        rejectedAt: action === "reject" ? serverTimestamp() : null
      });
      await refreshAll();
    });
  });
}

async function loadAnalytics() {
  if (!currentProfile) return;
  const businessId = currentProfile.businessId || currentUser?.uid;

  const opensQuery = query(
    collection(db, "events"),
    where("businessId", "==", businessId),
    where("eventType", "==", "open")
  );

  const savesQuery = query(
    collection(db, "events"),
    where("businessId", "==", businessId),
    where("eventType", "==", "save")
  );

  const [opensSnap, savesSnap] = await Promise.all([getDocs(opensQuery), getDocs(savesQuery)]);
  openCount.textContent = String(opensSnap.size);
  saveCount.textContent = String(savesSnap.size);
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
  if (!ts?.toDate) return "-";
  return ts.toDate().toLocaleString();
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

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
const MAX_UPLOAD_BYTES = 100 * 1024 * 1024;

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
refreshDispatchDataButton?.addEventListener("click", loadDispatchData);
sendModelToUsersButton?.addEventListener("click", sendModelToSelectedUsers);
refreshPartnerDeliveryButton?.addEventListener("click", loadPartnerDeliveryData);
sendPartnerModelButton?.addEventListener("click", sendApprovedPartnerModelToUsers);
refreshPartnerSubscribersButton?.addEventListener("click", loadPartnerSubscriptionAdminData);
savePartnerSubscribersButton?.addEventListener("click", savePartnerSubscribersMapping);
partnerSelectForSubscribers?.addEventListener("change", syncPartnerSubscribersSelection);

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
  const normalizedRole = normalizeRole(currentProfile.role);
  roleBadge.textContent = normalizedRole ? normalizedRole.toUpperCase() : "UNASSIGNED";
  authScreen.classList.add("hidden");
  appScreen.classList.remove("hidden");

  const isAdmin = normalizedRole === "admin";
  setAdminVisibility(isAdmin);
  setUploadUIForRole(isAdmin);
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
    loadPartnerDeliveryData()
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
  const businessName = (businessNameInput.value || currentProfile.businessName || "").trim();
  if (!businessName) {
    uploadMessage.textContent = "Business name is required.";
    return;
  }

  const isAdmin = (currentProfile.role || "").toLowerCase() === "admin";
  let targetMode = targetModeInput.value;
  let targetUserIds = parseTargetUserIds(targetUserIdsInput.value);

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

        uploadProgress.value = 0;
        glbInput.value = "";
        displayNameInput.value = "";
        questionInput.value = "";
        targetUserIdsInput.value = "";
        uploadMessage.textContent = normalizeRole(currentProfile.role) === "admin"
          ? "Uploaded and pushed to app."
          : "Uploaded. Submission is waiting for admin approval. You can send to users after approval.";
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
  mySubmissionsBody.innerHTML = "";
  if (!currentUser || !currentProfile) return;
  try {
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
  } catch (err) {
    uploadMessage.textContent = `Could not load submissions: ${err.message || err}`;
  }
}

async function loadApprovalQueue() {
  pendingBody.innerHTML = "";
  if (!currentProfile || currentProfile.role !== "admin") return;
  try {
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

  await set(modelRef, {
    name: item.displayName || stripGlbExtension(item.fileName || modelKey),
    modelNamee: modelKey,
    picPathh: item.picPathh || modelKey,
    question: item.question || "Would you like this product?",
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
        targetUserIds = allUserIds;
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

function setUploadUIForRole(isAdmin) {
  if (isAdmin) {
    uploadSectionTitle.textContent = "Upload .glb model (Publish to app)";
    mySubmissionsTitle.textContent = "Admin uploads";
    uploadButton.textContent = "Upload and Publish";
    document.querySelectorAll(".partner-only").forEach((el) => {
      el.style.display = "none";
    });
    targetModeInput.style.display = "";
    targetUserIdsInput.style.display = "";
    partnerSubscribersPanel.style.display = "none";
    targetModeInput.value = "all_users";
    targetModeInput.dispatchEvent(new Event("change"));
    uploadMessage.textContent = "Admin uploads publish directly to app users.";
  } else {
    uploadSectionTitle.textContent = "Upload .glb model (Submit for review)";
    mySubmissionsTitle.textContent = "My submissions";
    uploadButton.textContent = "Upload For Review";
    document.querySelectorAll(".partner-only").forEach((el) => {
      el.style.display = "";
    });
    partnerSubscribersPanel.style.display = "";
    targetModeInput.value = "specific_users";
    targetModeInput.style.display = "none";
    targetUserIdsInput.style.display = "none";
    partnerDeliveryMessage.textContent = "";
    uploadMessage.textContent = "Partner uploads require admin approval before push.";
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

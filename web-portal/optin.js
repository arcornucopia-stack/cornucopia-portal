import { initializeApp } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-app.js";
import { getDatabase, ref as dbRef, update } from "https://www.gstatic.com/firebasejs/10.12.5/firebase-database.js";

const ROOT = "cornucopia";

const optinMessage = document.getElementById("optinMessage");
const optInButton = document.getElementById("optInButton");
const declineButton = document.getElementById("declineButton");
const partnerNameSpan = document.getElementById("partnerNameSpan");

const config = window.CORNUCOPIA_FIREBASE_CONFIG;
if (!config || !config.projectId) {
  optinMessage.textContent = "This page is misconfigured. Please contact the sender of your email.";
  optInButton.disabled = true;
  declineButton.disabled = true;
  throw new Error("Missing Firebase config");
}

const app = initializeApp(config);
const db = getDatabase(app);

const params = new URLSearchParams(location.search);
const partnerUid = params.get("p");
const entryId = params.get("e");
const partnerName = params.get("n");

if (partnerName) partnerNameSpan.textContent = partnerName;

if (!partnerUid || !entryId) {
  optinMessage.textContent = "This link is missing information and can't be used. Please contact the sender of your email.";
  optInButton.disabled = true;
  declineButton.disabled = true;
} else {
  optInButton.addEventListener("click", () => respond("opted_in", "You're opted in — thanks! You can close this page."));
  declineButton.addEventListener("click", () => respond("declined", "You've been opted out. You can close this page."));
}

async function respond(status, successMessage) {
  optInButton.disabled = true;
  declineButton.disabled = true;
  optinMessage.textContent = "Saving your choice...";
  try {
    await update(dbRef(db, `${ROOT}/partners/${partnerUid}/mailingList/${entryId}`), {
      status,
      respondedAt: Date.now()
    });
    optinMessage.textContent = successMessage;
  } catch (err) {
    optinMessage.textContent = `Something went wrong: ${err.message || err}. Please try again.`;
    optInButton.disabled = false;
    declineButton.disabled = false;
  }
}

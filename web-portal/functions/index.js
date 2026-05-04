/**
 * Optional Cloud Functions template.
 * Use callable functions so client cannot self-approve uploads.
 * (Realtime Database version)
 */

const functions = require("firebase-functions");
const admin = require("firebase-admin");

admin.initializeApp();

exports.approveSubmission = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Sign-in required");
  }

  const uid = context.auth.uid;
  const userSnap = await admin.database().ref(`users/${uid}`).once("value");
  const user = userSnap.val();

  if (!user || user.role !== "admin") {
    throw new functions.https.HttpsError("permission-denied", "Admin role required");
  }

  const submissionId = data?.submissionId;
  if (!submissionId) {
    throw new functions.https.HttpsError("invalid-argument", "submissionId is required");
  }

  await admin.database().ref(`submissions/${submissionId}`).update({
    status: "approved",
    decisionBy: uid,
    approvedAt: Date.now(),
    rejectedAt: null
  });

  return { ok: true };
});

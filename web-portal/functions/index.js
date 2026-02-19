/**
 * Optional Cloud Functions template.
 * Use callable functions so client cannot self-approve uploads.
 */

const functions = require("firebase-functions");
const admin = require("firebase-admin");

admin.initializeApp();

exports.approveSubmission = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Sign-in required");
  }

  const uid = context.auth.uid;
  const userDoc = await admin.firestore().collection("users").doc(uid).get();
  if (!userDoc.exists || userDoc.data().role !== "admin") {
    throw new functions.https.HttpsError("permission-denied", "Admin role required");
  }

  const submissionId = data?.submissionId;
  if (!submissionId) {
    throw new functions.https.HttpsError("invalid-argument", "submissionId is required");
  }

  await admin.firestore().collection("submissions").doc(submissionId).update({
    status: "approved",
    decisionBy: uid,
    approvedAt: admin.firestore.FieldValue.serverTimestamp()
  });

  return { ok: true };
});

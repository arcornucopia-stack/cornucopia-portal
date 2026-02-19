# Web Portal GitHub Pages Deploy

This repository includes an Actions workflow at `.github/workflows/deploy-web-portal.yml`.

## How it deploys
- Trigger: push to `main` or `master` when `web-portal/**` changes
- Source deployed: `web-portal/`
- Target: GitHub Pages environment

## One-time GitHub setup
1. Push this repo to GitHub.
2. In GitHub repo: `Settings -> Pages`.
3. Under Build and deployment, set Source to `GitHub Actions`.
4. Wait for workflow run "Deploy Web Portal to GitHub Pages".
5. Open the Pages URL shown in the workflow output.

## Firebase auth domain
Add your Pages domain in Firebase Console:
- `Authentication -> Settings -> Authorized domains`
- Add: `<your-username>.github.io`

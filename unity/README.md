# Unity Placeholder

The actual Unity project files live at the repository root (e.g., `Assets/`, `ProjectSettings/`).

This folder exists to host documentation and future Unity CI/CD configs.

## WebGL Drop-in Guide
1) Build Unity for WebGL with compression set to Brotli or Gzip and enable "Decompression Fallback".
2) Copy the entire `Build/` folder output into `/web/public/unity/Build/`.
3) Visit `http://localhost:3000/play` to load it.
4) The web page will send the selected level to `GameManager.OnLevelSelected(int)` once the build is ready.

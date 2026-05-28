# Agent Instructions — MR Motifs

Unity collection of focused Mixed Reality reference patterns ("motifs") for recurring MR mechanics — currently Passthrough Transitioning, Shared Activities, Instant Content Placement & Depth Effects, and Colocated Experiences.

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official requirements, per-motif notes, and additional-requirements sections for Shared Activities / Instant Content Placement / Colocated Experiences
- `ProjectSettings/ProjectVersion.txt` — Unity editor version
- `Packages/manifest.json` — Unity package versions (Meta XR Core / Interaction / Platform, MRUK, Avatars, OpenXR, Unity OpenXR Meta)
- `LICENSE` — license terms

## Quest / Horizon-specific notes

- The bootstrap scene is `Assets/MRMotifs/MRMotifsHome.unity` — every motif is reachable from its in-scene `Menu Panel`. Toggle the menu with the controller start/menu button or the menu hand-gesture.
- After any project setting change, run `Meta > Tools > Update AndroidManifest.xml` before building to the device, or runtime permissions / features will be stale.
- The Depth API and MRUK Space Sharing API require the **Unity OpenXR Meta** plugin extension installed alongside the base OpenXR plugin.
- The Networked Avatar Building Block silently requires the **Avatars SDK Samples** to be imported via Package Manager — missing samples cause runtime errors that look like network bugs.
- Spatial anchor sharing in this project is **group-based** (as of Meta XR v71); do not regress to user-based sharing APIs when editing colocation code.
- `Assets/MRMotifs/PassthroughTransitioning/Shaders/PassthroughFader.shader` requires `Cull Off` and Render Queue `Transparent-1 (2999)` — do not change these without re-verifying that the in-sphere fade does not z-fight.

# Meta Quest tooling

This is a Meta Quest / Horizon OS sample. The bespoke intro above is the source of truth for what this project is and how it's built — use it (and the files it points at) instead of restating facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: <https://github.com/meta-quest/agentic-tools>. Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.

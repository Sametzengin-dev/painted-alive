# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Users/samet/OneDrive/Desktop/PaintedAlive/PaintedAlive`
- Last analyzed: 2026-09-16
- Last analyzed commit: `846eaa0aa9022da93eaf344694f17b2dabd1c6d0`
- Target: Windows x86_64 / Steam friend test; production Steam deployment is out of scope.

## Confirmed Environment

- Unity version: 6000.3.19f1 (7689f4515d75)
- Render pipeline: Universal Render Pipeline 17.3.0
- Input system: Unity Input System 1.19.0 through project input-reader components
- Target platforms: Windows Standalone is the M56 target; installed editor also has Web, UWP, and Windows Server modules.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Networking | FishNet 4.7.2 resolved from the requested 4.7.3 tag | Confirmed | `Packages/manifest.json`, resolved `package.json` |
| Steam API | Steamworks.NET 2025.164.1 | Confirmed | `Packages/manifest.json`, `Packages/packages-lock.json` |
| Steam transport | FishySteamworks 4.1.1 embedded with a local runtime asmdef because upstream UPM source has no asmdef | Confirmed | `Packages/com.firstgeargames.fishysteamworks/` |
| Rendering | URP 17.3.0 | Confirmed | `Packages/manifest.json` |
| Input | Unity Input System 1.19.0 | Confirmed | `Packages/manifest.json`, `FigureInputReader.cs` |
| Tests | Unity Test Framework 1.6.0 installed; no first-party test assembly found | Confirmed | package manifest and asset search |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/_Project/Code/Runtime` | Main first-party gameplay/runtime code | Confirmed | `PaintedAlive.Runtime.asmdef` and representative systems |
| `Assets/_Project/Code/Editor` | Setup, diagnosis, and milestone editor tools | Confirmed | editor scripts and menu items |
| `Assets/_Project/Code/Runtime/Network/M56` | Friend-test networking runtime | Confirmed | M56 broadcasts, session, Steam bootstrap |
| `Assets/_Project/Code/Editor/Network/M56` | Idempotent setup, diagnostic, Windows build | Confirmed | M56 editor tools |
| `Assets/Scenes/P00_PaintLab.unity` | Enabled startup/gameplay scene | Confirmed | `ProjectSettings/EditorBuildSettings.asset` |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `PaintedAlive.Runtime` | First-party runtime/gameplay and M56 session | Input System, Splines, Mathematics, uGUI, FishNet.Runtime, Steamworks.NET, FishySteamworks.Runtime | One broad runtime assembly |
| `FishNet.Runtime` | FishNet networking runtime | GameKit dependencies, Mathematics | Git package assembly |
| `com.rlabrecque.steamworks.net` | Steamworks.NET wrapper | Native Steam libraries | Git package assembly |
| `FishySteamworks.Runtime` | FishNet Steam P2P/relay transport | FishNet.Runtime, Steamworks.NET | Local embedded package assembly |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/P00_PaintLab.unity`
- Likely startup scene: `P00_PaintLab`
- Scene loading flow: single enabled build scene; M56 network root persists through FishNet NetworkManager settings.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Gameplay | MonoBehaviour-based feature systems with explicit serialized references | Confirmed | Figure, Painter, match, Palimpsest, Living Gallery code |
| Roles | `InkPainterRoleAuthority` owns local camera/input role application; network bridge overrides local F1/F2 decisions while connected | Confirmed | role authority and M56 bridge/session |
| Networking | Listen server, server-owned complementary roles, FishNet broadcasts, FishySteamworks P2P | Confirmed | M56 session and setup |
| World actions | Map-agnostic `IPainterWorldAction` plus map-specific adapters | Confirmed | Painter world-action contracts and adapters |
| Reset | Existing match controllers and map reset interfaces remain authoritative locally; M56 triggers/reset-replicates from server | Confirmed | M56 session and environment state hubs |

## Coding Conventions

- Namespace style: `PaintedAlive.<Domain>`.
- Serialized fields: private fields with `[SerializeField]`, mostly lower camel case.
- Async: coroutines for gameplay telegraphs/transitions; no broad task framework detected.
- Comments/docs: XML summaries for public contracts and non-obvious gameplay constraints.

## Testing And Validation

- EditMode tests: no first-party test assembly found.
- PlayMode tests: no first-party test assembly found.
- CI/build validation: Unity 6000.3.19f1 batch-mode compile, M56 diagnostic/setup commands, Windows friend-test build command, and development `-m56-smoke-host` player check.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity CLI/editor batch execution | available | local Unity CLI and 6000.3.19f1 editor |
| Unity Editor MCP bridge | unavailable | no provider in package/config scan; `unity status` returned no instances |
| Native Unity UI automation | unavailable in this session | Computer Use exposed browser surfaces only |
| Console/log inspection | available | batch-mode log files |
| Build Settings inspection | available | serialized settings and Editor APIs |
| Scene/asset mutation | available | project files and Editor setup commands |
| Test runner | available, no first-party suites | Test Framework package |

## Important Constraints

- Do not commit or push.
- Do not modify production Steam deployment/settings; development AppID 480 only.
- Preserve existing gameplay and scene assets; do not replace FigureMotor or local prototype behavior.
- Real two-account/two-PC Steam acceptance cannot be inferred from local compilation or a host-only smoke test.
- The working tree contains extensive user changes unrelated to M56; preserve them.

## Unknowns And Confidence

- Real Steam relay connection between two separate accounts/PCs remains runtime-test required.
- Cross-peer behavior of every authored world action requires the two-PC test matrix.
- No production matchmaking, host migration, rollback, or anti-cheat is part of M56.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/_Project/Code/Runtime/PaintedAlive.Runtime.asmdef`
- M56 runtime/editor sources and prior bootstrap templates
- FishNet, Steamworks.NET, and FishySteamworks resolved sources
- Figure role/movement/input code
- Painter world-action, Palimpsest, Living Gallery, and reset code
- `Assets/Scenes/P00_PaintLab.unity`

<!-- unity-onboarding:generated:end -->

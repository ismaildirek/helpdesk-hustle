# Unity Project Health Report

## Post-audit remediation - 2026-08-10

- Removed automatic reload/play-mode triggers from all 16 Editor tools that
  can save scenes. Their explicit `Tools` menu commands remain available.
- Kept only the presentation-library asset installer automatic; it does not
  open or save scenes.
- Added a regression test that rejects `InitializeOnLoad` or
  `DidReloadScripts` on any scene-saving Editor tool.
- Added non-repeating DAY events: Network Trouble, Boss Inspection and Coffee
  Boost. They affect only newly assigned task duration and future score/anger
  results, and announce themselves in `YeniOfis` or `katlar`.
- Validation after remediation: **7 EditMode tests passed**, Unity compilation
  reported no C# errors or runtime exceptions, and the generated solution
  build completed with 0 errors and 0 first-party C# warnings.

## Current update — 2026-08-10

> Validation status: **Ready with limitations for Editor integration; not yet release-ready**
>
> Analyzed commit: `ffc1680` with extensive documented local changes
>
> Target: Android portrait/mobile

### Implemented milestone

- Added authoritative run progression: score, remaining-time bonus, combo,
  completed/failed counts, best score, best survival time and PlayerPrefs
  persistence.
- Added weekly pressure: task timers decrease by 7% per week to a safe 58%
  floor. Selected mini-games also gain conservative difficulty variants.
- Added a runtime office HUD and a loss summary with RETRY and MAIN MENU.
- Every third consecutive success can reduce boss anger by one step.
- Added common English mini-game instruction cards, arrival fades and
  success/failure feedback.
- Added low-volume generated office ambience without adding a new imported
  audio asset.
- Added three EditMode regression tests for scoring, combo bonus and combo
  reset behavior.

### Validation evidence

- Unity: `6000.3.20f1`.
- Unity Test Framework EditMode filter `GameProgressionSessionTests`:
  **3 passed, 0 failed, 0 skipped**.
- Test result:
  `C:\Users\admin\AppData\Local\Temp\Staj_Projesi1-CodexValidation\editmode-results.xml`.
- Unity test log: no C# errors, no NullReference/MissingReference exceptions,
  and the test runner exited with code 0. Two transient licensing handshake
  messages recovered and did not block compilation or tests.
- Generated solution build: **0 errors**. After explicit defaults were added
  to Inspector-populated fields, first-party CS0649 warning noise was removed;
  remaining warnings originate from MCP assembly version conflicts.
- All 14 enabled build scenes exist. The disabled legacy `ESkiAnaOfis` scene
  remains excluded.
- No missing-script sentinel or duplicate asset GUID was found in Assets.
- Android Build Support, SDK, NDK and OpenJDK directories exist. Portrait-only,
  safe-area-protected, ARMv7+ARM64 and IL2CPP settings remain configured.

### Current priority findings

| ID | Severity | Confidence | Finding | Next action |
|---|---|---|---|---|
| UH-007 | Medium | Confirmed | Runtime UI, scene transitions and difficulty variants compiled and their scoring rules passed EditMode tests, but their visual/touch behavior was not exercised on a phone. | Run the manual phone smoke checklist before balancing. |
| UH-008 | Medium | Confirmed | No APK/AAB or target-device profiler capture was produced in this milestone. | Produce a development APK, install it, then check memory, frame time, pause/resume and safe areas. |
| UH-009 | Medium | Confirmed | Git reports 49 untracked paths, 11 modified paths and 2 deleted paths. The backup reduces immediate risk, but the working project is not reproducibly captured in version control. | Review and commit the working Unity project before the next large content pass. |
| UH-004 | Medium | Confirmed | `com.coplaydev.unity-mcp` still tracks Git branch `main`; IDE builds report `System.Net.Http` and `System.IO.Compression` conflicts through `MCPForUnity.Editor.dll`. | Pin the dependency to the tested lock hash/tag in a separate maintenance change. |
| UH-010 | Low | Confirmed | Runtime code remains in the monolithic predefined assembly, and presentation/progression files have grown large (`SurvivalTimeDisplay.cs`, `MiniGameJuice.cs`, `ProceduralGameAudio.cs`). | Split them by responsibility and add runtime/editor asmdefs after the phone milestone. |
| UH-006 | Low | Confirmed | Android identity remains `com.DefaultCompany.Staj_Projesi1`, version `1.0`/code `1`. | Choose final company/product identifiers before store submission. |

### Healthy areas

- Scoring is awarded only after `TaskMissionSession` validates a real launched
  task, preventing duplicate score from direct mini-game scene launches.
- Failure, combo and boss-anger state have single authoritative owners and
  reset through Unity subsystem registration/new-game paths.
- Persistent data stores only simple best-record values and has safe defaults.
- New overlays use 1080x1920 Canvas scaling, unscaled animation time and do not
  block gameplay raycasts.
- Used music (`Forest`, `Happy`) is configured for streaming, background load
  and no preload. Other large WAV files are currently unreferenced content and
  are a repository-size concern rather than a confirmed player-build cost.
- Build scene paths, Unicode scene names and runtime task routes remain valid.

### Required manual smoke test

1. Start from `Giris_Ekran`, press Play and finish the intro.
2. Confirm the SCORE/COMBO/TASKS HUD fits a portrait phone safe area.
3. Complete three tracked tasks; verify points, combo and boss calming.
4. Abandon and expire tasks; verify combo resets and anger rises once.
5. Reach six failures; verify SHIFT OVER, records, RETRY and MAIN MENU.
6. Enter every mini-game once; verify its instruction card does not cover the
   back icon or required interaction.
7. Test mute/pause, app background/resume and several scene reloads.

### Limitations

This update proves Unity Editor compilation, deterministic progression rules,
scene configuration and static serialization health. It does **not** prove
visual layout, touch ergonomics, Android player build success, device
performance, memory use or long-session balance. Those require the phone smoke
test and a development APK.

---

## Historical baseline — 2026-08-07

> Last analyzed: 2026-08-07
> Commit: `ffc1680` with documented local changes
> Scope: Android/mobile release-readiness health check and targeted remediation
> Project root: `C:\Users\admin\Staj_Projesi1`

## Overall assessment

The project is conditionally ready for a phone Play test. The generated C# solution compiles with no errors, all 14 enabled build scenes exist, Android support is installed, ARMv7 and ARM64 are enabled, IL2CPP is selected, touch input is implemented, and the broken first-party scene references found by static inspection were cleaned up. A target APK and an on-device runtime pass were not completed because the project was already open in another Unity Editor process; the user will perform the phone Play test.

## Coverage

### Checked

- Unity version, package manifest and package lock
- Android Player Settings, orientation, safe area, scripting backend and CPU architectures
- build-scene list and scene-file existence
- generated C# solution compilation
- first-party runtime and editor-script samples
- scene and asset GUID integrity across enabled build scenes
- missing-script sentinels
- touch and mouse input paths
- URP mobile-facing settings
- large texture and audio import settings
- current Git working-tree baseline

### Not checked

- Unity Play Mode runtime scenarios
- Android Gradle/APK build and installation
- target-device pause/resume, notch layout, memory and frame-time measurements
- visual correctness of every scene
- automated EditMode or PlayMode tests, because the project has no first-party test suite

## Priority findings

| ID | Severity | Confidence | Domain | Finding |
|---|---|---|---|---|
| UH-001 | High | Confirmed | Editor reliability | `YeniOfisAnimationBuilder` loaded boss sprites from the wrong folder and raised an exception after script reload. Fixed. |
| UH-002 | Medium | Confirmed | Serialization | Four enabled scenes contained stale asset GUIDs; three unused missing SpriteRenderer references were cleared and one deleted repair-part entry was removed. Fixed. |
| UH-003 | Medium | Confirmed | Mobile UI | A portrait 1080x1920 project allowed all rotations and rendered under Android cutouts without a safe-area layout system. Rotation is now portrait-only and rendering outside the safe area is disabled. Fixed. |
| UH-004 | Medium | Likely | Packages | `com.coplaydev.unity-mcp` tracks Git branch `main`; this is not reproducible across fresh installs. The generated solution also reports two assembly-version conflict warnings originating from the MCP editor assembly. |
| UH-005 | Medium | Confirmed | Validation | No first-party EditMode or PlayMode tests were found. Core scene transitions and mini-games rely on manual validation. |
| UH-006 | Low | Confirmed | Android release metadata | The Android identifier is still `com.DefaultCompany.Staj_Projesi1`. It is adequate for local testing but should be replaced before store distribution. |

## Detailed findings

### UH-001 — Broken automatic animation generation

- Evidence: `Assets/Scripts/Editor/YeniOfisAnimationBuilder.cs`; Editor log exception for missing `Assets/Art/OfisYeni/boss_sprite_1.png`.
- Root cause: boss images live under `Assets/Art/OfisYeni/patron/`.
- Remediation: corrected the asset path used by `GetMainSprite(int)`.
- Validation: every sprite required by the two boss sequences exists at the corrected path; the generated C# editor assembly compiles. Unity Editor execution remains to be confirmed after the active Editor imports the change.

### UH-002 — Stale serialized asset references

- Evidence: stale GUIDs in `Dosya_Yükle.unity`, `bozukkasa.unity`, `katlar.unity`, and `kasa_parça.unity` did not resolve from Assets or installed packages.
- Impact: missing visuals in the hierarchy, Inspector missing-reference state, and noisy release validation.
- Remediation: cleared three already-unassigned legacy SpriteRenderer references and removed the deleted entry from `repairableSprites`; five valid repair sprites remain.
- Validation: none of the four stale GUIDs remains and no enabled scene contains a missing-script sentinel.

### UH-003 — Portrait and safe-area mismatch

- Evidence: `ProjectSettings/ProjectSettings.asset` used 1080x1920 while portrait, upside-down portrait, and both landscape rotations were enabled; `androidRenderOutsideSafeArea` was enabled without a project safe-area component.
- Impact: rotation could expose unintended framing and edge controls could sit beneath a notch or system inset.
- Remediation: allow portrait only and keep rendering inside Android's safe area.
- Validation: static Player Settings inspection passed; verify framing on the target phone.

### UH-004 — Floating MCP package and compilation warnings

- Evidence: `Packages/manifest.json` and `Packages/packages-lock.json` resolve `com.coplaydev.unity-mcp` from `#main`; `dotnet build` reports `System.Net.Http` and `System.IO.Compression` version conflicts through `MCPForUnity.Editor.dll`.
- Impact: a future fresh package restore can resolve different code, and IDE builds remain warning-noisy.
- Recommendation: pin the MCP package to a tested commit or tagged release. Do this separately from the phone smoke test.

### UH-005 — No automated tests

- Evidence: no first-party test `.cs` or test assembly definition was found.
- Impact: scene flow and touch regressions can only be detected manually.
- Recommendation: add a small EditMode suite for task routing/session state and a PlayMode smoke suite for the entrance-to-office flow.

### UH-006 — Default Android identity

- Evidence: `applicationIdentifier.Android` is `com.DefaultCompany.Staj_Projesi1` and version code is 1.
- Impact: unsuitable long-term package ownership/versioning for store distribution.
- Recommendation: choose the final reverse-domain identifier before publishing. Do not change it immediately if an installed local build must keep its app data.

## Healthy areas

- Unity 6000.3.20f1 and URP 17.3.0 are aligned.
- Android support modules, SDK, NDK, JDK and Android Player files are installed.
- Android uses IL2CPP Release and ARM64 only.
- All 14 enabled build scenes exist; the intended entrance scene is first.
- Runtime interactions consistently include `Touchscreen` input with mouse fallback.
- No duplicate asset GUIDs or missing-script sentinels were found.
- Referenced background music is configured for streaming and background loading.
- C# solution compilation completed with 0 errors.

## Recommended remediation order

1. Let Unity finish importing, confirm the Console has no new errors, then run the entrance scene and each mini-game once.
2. Perform the phone Play test in portrait, including notch/safe-area, touch, pause/resume, scene return, and audio checks.
3. Before distribution, pin the Unity MCP Git dependency, choose a final Android application identifier, and add basic automated smoke tests.

## Validation baseline

- Commit: `ffc1680`
- Working tree: already contained extensive user-authored changes and untracked Unity assets before this audit.
- Unity: `6000.3.20f1`
- Build scenes: 14 enabled, 1 disabled legacy scene
- Compilation: `dotnet build Staj_Projesi1.sln --no-restore --verbosity minimal` — passed, 0 errors, 2 MCP-related warnings
- Unity batch validation: blocked because Unity process 28268 already had the project open
- Android artifact: not produced

## Limitations

Static inspection and generated-solution compilation do not prove Unity Player compilation or runtime behavior. The active Unity process prevented a clean batch-mode import and Android build. No device profiler or phone screenshots were available, so performance, memory, visual layout, lifecycle behavior and touch hit areas remain device-test responsibilities.

## Team notes

Manual notes may be added here.

## Android release readiness update — 2026-08-10

This section supersedes the older validation baseline above.

- Unity batch import and Android release validation now pass.
- C# compilation passes with 0 errors; 13/13 EditMode tests pass.
- A clean IL2CPP/ARM64 AAB and a subsequent incremental AAB both succeeded.
- Final AAB: `Builds/Android/HelpdeskHustle-validation.aab`, 158,337,630 bytes (about 151.0 MiB).
- Bundletool universal APK estimate: 156,298,394 bytes (about 149.1 MiB).
- Target API 36, minimum API 25, OpenGL ES 3, ETC2, portrait-only, safe-area rendering enabled, version 1.0.0 (code 1).
- The final manifest does not request INTERNET or sensitive runtime permissions.
- Android back navigation and mobile lifecycle pause/resume handling were added.
- Authentication, Timeline, Visual Scripting, and unreferenced TMP example content were removed.
- A launcher icon, release validator, AAB builder, and public native symbol output were added.

Remaining publication blockers are the permanent package identifier, release upload keystore, local proof of the music asset license, store listing/policy assets, and real-device QA. The full evidence and release sequence are documented in `Docs/Release/AndroidReleaseReadiness.md`.

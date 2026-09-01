# Android Release Readiness — 2026-08-10

## Status

The project now produces a valid Android App Bundle, but publication is blocked until the owner chooses the permanent package identifier, configures the upload keystore, and preserves the music license evidence.

## Verified baseline

- Unity 6000.3.20f1; 14 enabled scenes with `Giris_Ekran` first.
- Android API 36 target, API 25 minimum, ARM64, IL2CPP Release, OpenGL ES 3, ETC2, portrait-only.
- App Bundle enabled; version name 1.0.0 and version code 1.
- Android back button and application pause/focus handling are installed at runtime.
- Rendering outside the Android safe area is disabled.
- C# build: 0 errors. Unity EditMode tests: 13/13 passed.
- Final validation AAB: `Builds/Android/HelpdeskHustle-validation.aab`.
- AAB size: 158,337,630 bytes (about 151.0 MiB).
- Bundletool universal APK estimate: 156,298,394 bytes (about 149.1 MiB).
- Final manifest has no `android.permission.INTERNET` and requests no sensitive runtime permission.
- Public native symbol archive is generated beside the AAB.

## Applied release changes

- Added an automated Android release settings/validation/build tool.
- Added a project launcher icon and assigned it to Android.
- Removed unused Authentication, Timeline, and Visual Scripting runtime packages.
- Removed unused TextMesh Pro Examples & Extras after verifying no external references.
- Added an Android Gradle manifest postprocessor that removes the unused INTERNET permission.
- Added a clean AAB command and a faster incremental AAB command.

## Publication blockers

1. Replace `com.DefaultCompany.Staj_Projesi1` with the permanent reverse-domain package ID. It cannot be changed after Play publication without creating a new app.
2. Create and securely back up a release upload keystore; the validation AAB currently uses a local debug key.
3. Preserve the Asset Store purchase/license proof for Casual & Relaxing Game Music, product 262740.
4. Complete real-device QA on at least one low/mid Android device and one notched device.
5. Produce the Play listing assets: 512×512 store icon, 1024×500 feature graphic, screenshots, short/long description, privacy policy URL, content rating, and Data safety answers.

## Remaining risks and recommendations

- Textures account for 317.8 MiB (96.2%) of uncompressed user assets. The AAB is below the Play base-module limit but close enough that the next optimization target should be large PNG import sizes and Android texture overrides. Do visual QA before lowering resolution.
- The launcher icon is valid but currently uses Unity's legacy icon path. Add a dedicated transparent adaptive foreground and flat background before the public release.
- The MCP editor package tracks Git branch `main` and produces two IDE-only assembly-version warnings. Pin it to a tested commit before the final release branch.
- The generated AAB is a validation artifact, not the upload candidate, until the permanent identity and keystore are configured.

## Recommended final sequence

1. Owner supplies studio/developer name and confirms final game title.
2. Set permanent package ID and create upload keystore.
3. Run two-device smoke/performance test and fix any safe-area/touch issues.
4. Optimize the largest textures only if device download or memory is too high.
5. Build signed release AAB, upload native symbols, and use Play Console internal testing.
6. Complete store listing, policy forms, and closed testing requirements.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Lucid Dream Companion is a **.NET MAUI Blazor Hybrid** app (single project, `LucidDreamCompanion/`) for dream journaling, lucidity training, and running overnight audio cues. UI is Blazor/Razor inside a `BlazorWebView`; platform integration (alarms, audio, speech) is native Android. The phased build history and detailed design rationale live in [docs/DEVELOPMENT_NOTES.md](docs/DEVELOPMENT_NOTES.md) and [LuciditySpec.md](LuciditySpec.md) — the spec is the source of truth when it disagrees with the design handoff.

## Build & run

```
dotnet build -f net10.0-android                  # Android — the primary, fully-featured target
dotnet build -f net10.0-windows10.0.19041.0      # Windows — desktop dev/testing of the journal UI only
```

- Targets `net10.0-android` (primary) and `net10.0-windows` (Windows only). iOS/MacCatalyst are intentionally trimmed.
- `Nullable` and `TreatWarningsAsErrors` are **on**. `EF1001` and `CS1591` are the only exceptions (see `.csproj`). New code must build warning-clean.
- There is **no test project**. Verification is manual on-device — Android Doze/OEM battery behavior cannot be reproduced on emulators, so overnight cue/alarm changes must be tested on real hardware (the OnePlus 13R is the reference device).
- Open `LucidDreamCompanion.sln` in Visual Studio 2022 with the .NET MAUI workload for the IDE flow.

## Architecture

**Service layer.** All business logic lives in `Services/` behind interfaces, registered as **singletons** in `MauiProgram.cs`. Razor components inject these — keep logic in services, not components. Services that need the DB take an `IDbContextFactory<LucidDbContext>` and create a short-lived context per operation (`await using var db = await _dbFactory.CreateDbContextAsync()`) — do **not** hold a long-lived `DbContext`, since services are singletons.

**Cross-platform partial services.** `AudioService` and `SpeechToTextService` are `partial class`es: the real Android implementation lives in `Platforms/Android/Services/` (e.g. `AndroidAudioPlayer.cs`, `AndroidSpeechToText.cs`) guarded by `#if ANDROID`, and non-Android targets get harmless stubs so the Windows build still runs. Follow this pattern for any new native capability rather than `#if`-splattering the call sites.

**Data layer.** EF Core 10 + SQLite at `FileSystem.AppDataDirectory/lucid.db`. The schema is **`EnsureCreated()`, not migrations** (see `MauiProgram.InitializeDatabaseAsync`). When you add or change a column on an existing entity you must *also* add an idempotent patch in `MauiProgram.ApplyAdditiveSchemaAsync` (use the `EnsureColumnAsync` helper for additive columns, or recreate a table that has never held shipped rows). New `DbSet`s and relationships go in `LucidDbContext.OnModelCreating`. Seeders in `Data/Seed/` run on every launch and must be idempotent; ordering matters (`TechniqueTemplatesSeeder` resolves audio assets created by `BundledAudioSeeder`).

**Overnight cue scheduling (the core differentiator).** `CueSchedulerService` arms one Android `AlarmManager.SetExactAndAllowWhileIdle` (RTC_WAKEUP) alarm per cue fire-time plus a stop-alarm. Each alarm's `PendingIntent` request code is the `ScheduledCueLog.Id` for exact cancellation. `CueAlarmReceiver` wakes `CueForegroundService` (type `mediaPlayback`) to play audio even from a killed process; `CueBootReceiver` re-arms after reboot. Three trigger modes: ClockBased, Interval, and SleepRelative (offset from a live `SleepSession.StartedAt`). Activating a profile cancels the previously-active profile's alarms; profiles are otherwise independent. **Technique-template profiles (`IsTechniqueTemplate = true`) are locked** — never edit them in place; the UI offers "Duplicate as editable profile" instead.

**Notifications.** `Plugin.LocalNotification` (v14 API under `Plugin.LocalNotification.Core.Models.*`). Three channels created in `MauiProgram`: `journal_reminders`, `reality_checks`, `cues_active`. Taps flow `NotificationActionTapped` → `NotificationRouter` (singleton) → `MainLayout` navigates to the pending route. Reschedule-on-launch in `MauiProgram` covers reboots and daily renewal.

**UI structure.** `Components/Pages/` holds routable pages (routes are defined via `@page` directives, mirroring the folder names). `Components/Shared/` holds reusable widgets. Bottom nav is Home / Journal / Sleep / Stats / Settings — Cues and Techniques live *under* the Sleep tab. Design tokens are copied verbatim from the design handoff into `wwwroot/tokens.css`; prefer token variables over hardcoded colors.

## Conventions

- Stats, streaks, and lucidity-rate logic all live in `StatsService` and are computed from `DreamEntry.DreamDate` in **local** time (midnight-based streaks with a one-day grace). Don't recompute these inline in components.
- Bundled audio WAVs are *generated* by `Resources/Raw/Audio/_generate.ps1`; that script and `CREDITS.md` are explicitly excluded from `MauiAsset` so they don't ship in the APK.
- New Android permissions go in `Platforms/Android/AndroidManifest.xml`. Exact-alarm and battery-optimization flows deep-link to system settings on first cue activation.

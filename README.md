# Lucidity — Lucid Dream Companion

An offline Android app for people learning to lucid dream. It combines a dream journal, daytime reality-check reminders and **overnight audio cues timed to your sleep**, so the whole training routine lives in one place.

Built with **.NET MAUI Blazor Hybrid** (.NET 10, C#, Razor, SQLite). Android is the primary target; a Windows build exists for quick UI testing.

> Think "power-user journal", not "breathe in, breathe out". It's a training tool, not a meditation app.

---

## What it's for

Most lucid-dreaming techniques (WBTB, MILD, FILD, WILD, SSILD) depend on three habits:

1. **Remembering and writing down dreams** every morning, so you start noticing *dream signs*.
2. **Doing reality checks** during the day, so the habit carries over into dreams.
3. **Being nudged at the right moment of the night**, e.g. a soft bell or a voice cue during late-night REM, or an alarm for Wake-Back-To-Bed.

Lucidity covers all three. Your phone schedules the cues reliably overnight (exact Android alarms, survives Doze and reboots), and everything you log feeds into streaks and lucidity stats.

## Features

**Dream journal**
- Entries with title, body, date of the night, sleep quality and a 5-step lucidity scale (*None → Faint → Brief → Sustained → Full control*)
- Tags (lucid, nightmare, recurring, dream sign, technique tags…), with your own colours
- Search and filters (minimum lucidity, tags), autosave while typing
- **Voice dictation**: tap the mic and speak your recall right after waking (English, Czech or device default)

**Reality checks and reminders**
- Randomised daytime reality-check notifications (different times each day); tapping one logs the check
- Morning "log your dream" reminder, plus a quick-capture notification when you end a sleep session

**Overnight audio cues**
- **Cue profiles**: build several setups ("FILD night", "WBTB experiment"…) and switch between them in two taps
- Three trigger modes: **clock time** (e.g. 04:30), **interval** (every N minutes within a window) and **sleep-relative** (e.g. 4 h 30 min after you pressed *Start Sleep*)
- Per-cue volume, fade-in, loop count and vibration pattern
- WBTB alarms that ring until you dismiss them
- Audio library: bundled sounds (soft bell, Tibetan bell, pink/brown noise, theta-beat tone), **import your own audio files** or **record voice memos** in the app

**Sleep sessions**
- *Start Sleep* → live elapsed timer, upcoming cues, optional "aim for tonight" note
- End with a quality rating and notes, then *End & log dreams*; dreams are linked to that night automatically
- Session history with the full cue log (what fired, when)
- Small brain teasers (Sudoku, Mines, Math) for staying awake during a WBTB wake-up

**Technique templates**
- Built-in guides and ready-made cue presets for **WBTB, MILD, FILD, WILD and SSILD**
- Templates are locked; *Duplicate as editable profile* to customise one

**Stats and streaks**
- Current and longest journaling streak, with milestone celebrations
- Lucidity rate (30 days, 90 days, all time), 90-day calendar heatmap, trend line
- Top tags, technique comparison (which profile produces the most lucid dreams), reality-check completion

**Your data stays yours**
- Everything is stored locally in SQLite on the device. No account, no server, no analytics.
- Export a full **JSON backup** (and import it back, merge or replace) or a **Markdown archive** (one file per dream, organised by year/month)
- Delete-all and database compaction in *Settings → Data*

## How to use it

A typical routine:

1. **First launch.** The onboarding asks for notification and microphone permission, lets you pick a starter technique (MILD is recommended) and sets your morning reminder time.
2. **During the day.** When a reality-check notification arrives, actually do the check (look at your hands, try to breathe through a pinched nose…) and tap it.
3. **At bedtime.** Open the **Sleep** tab, choose a cue profile (or a technique template) and press **Start Sleep**. You can lock the phone; cues fire on their own.
4. **During the night.** Cues play at their scheduled times. If a WBTB alarm wakes you, dismiss it in the app; the brain teasers help you stay awake for a few minutes before going back to sleep.
5. **In the morning.** Tap **End & log dreams**, then type or dictate everything you remember and set the lucidity level and tags.
6. **Over time.** Check **Stats** to see your streak, lucidity rate and which technique works best for you.

> **Important for reliable overnight cues:** on first activation the app asks you to allow *exact alarms* and to exclude it from *battery optimisation*. Do both. Some phone makers (OnePlus/OxygenOS, Xiaomi/MIUI, Samsung) kill background apps aggressively otherwise. Emulators don't reproduce this, so test overnight on a real phone.

## Build and run

**Requirements**
- .NET 10 SDK with the **.NET MAUI** workload (`dotnet workload install maui`)
- Android SDK; a device or emulator running **Android 8.0 (API 26)** or newer
- Optional: Visual Studio 2022+ with the .NET MAUI workload

**From the command line**

```bash
# Android (primary, full feature set)
dotnet build LucidDreamCompanion/LucidDreamCompanion.csproj -f net10.0-android

# Build and deploy to a connected Android device / running emulator
dotnet build LucidDreamCompanion/LucidDreamCompanion.csproj -t:Run -f net10.0-android

# Windows (journal UI only, for quick desktop testing)
dotnet build LucidDreamCompanion/LucidDreamCompanion.csproj -f net10.0-windows10.0.19041.0
```

**From Visual Studio:** open `LucidDreamCompanion.sln`, choose an Android device as the target and press Run.

The database is created automatically on first launch (`lucid.db` in the app's data folder). There is no setup, configuration or API key.

> On Windows, audio playback, alarms and speech recognition are stubbed out. Only the journal and the rest of the UI work there.

## Project structure

```
LucidDreamCompanion/
├── Components/
│   ├── Pages/          # Routable screens: Journal, Sleep, Cue, Technique, Stats, Settings, Onboarding, Games
│   ├── Shared/         # Reusable widgets (lucidity eye picker, tag picker, voice text area, toast…)
│   └── Layout/         # Main layout + bottom navigation
├── Services/           # All business logic behind interfaces (journal, cues, scheduler, stats, export…)
├── Data/               # EF Core entities, DbContext, idempotent seeders (tags, sounds, techniques)
├── Platforms/Android/  # Native Android: alarm & boot receivers, foreground service, audio player, speech-to-text
├── Resources/Raw/Audio # Bundled WAV cues (synthesised by _generate.ps1, see CREDITS.md)
└── wwwroot/            # CSS design tokens, app styles, JS interop
design/lucidity/        # Original HTML/JSX design prototype the UI is based on
docs/                   # Development notes
LuciditySpec.md         # Full implementation spec (data model, phases, acceptance criteria)
```

## Documentation

- [LuciditySpec.md](LuciditySpec.md): the full specification (data model, 9 build phases, Android caveats)
- [docs/DEVELOPMENT_NOTES.md](docs/DEVELOPMENT_NOTES.md): phase-by-phase implementation notes and design decisions
- [CLAUDE.md](CLAUDE.md): architecture overview and conventions for working on the code
- [design/lucidity/](design/lucidity/): the clickable design prototype

## Status

All 9 planned phases are implemented (journal, notifications and reality checks, audio library, cue scheduling, sleep sessions, voice journaling, technique templates, stats, export and polish). It's a personal project at version 0.1 and isn't published on Google Play. Build it yourself and sideload it.

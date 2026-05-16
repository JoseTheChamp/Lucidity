// data.js — sample dream entries + meta. Globally available.

const TAGS = [
  { name: "lucid",            color: "#5EEAD4" },
  { name: "recurring",        color: "#A78BFA" },
  { name: "false awakening",  color: "#6B8AFF" },
  { name: "nightmare",        color: "#F87171" },
  { name: "WBTB",             color: "#A78BFA" },
  { name: "MILD",             color: "#6B8AFF" },
  { name: "FILD",             color: "#6B8AFF" },
  { name: "dream sign",       color: "#5EEAD4" },
  { name: "flying",           color: "#6B8AFF" },
  { name: "water",            color: "#6B8AFF" },
  { name: "childhood home",   color: "#A78BFA" },
];

const tagBy = (name) => TAGS.find((t) => t.name === name) || { name, color: "#8B92A8" };

const ENTRIES = [
  {
    id: 9,
    title: "Static between channels",
    body: "I was watching an old CRT, then realized the static was forming faces. Tried a nose-pinch reality check and it worked — I could breathe through it. The room dimmed. I tried to spin to stabilize but woke up almost immediately.",
    dreamDate: "2026-05-14",
    createdAt: "2026-05-15T06:42:00Z",
    lucidity: 2,
    sleepQuality: 4,
    tags: ["lucid", "dream sign", "MILD"],
    sessionId: 41,
    wordCount: 184,
  },
  {
    id: 8,
    title: "The library that kept growing",
    body: "Stacks extending forever, kept finding the same blue book. Recurring.",
    dreamDate: "2026-05-13",
    createdAt: "2026-05-14T06:50:00Z",
    lucidity: 1,
    sleepQuality: 3,
    tags: ["recurring", "dream sign"],
    sessionId: 40,
    wordCount: 92,
  },
  {
    id: 7,
    title: "Flight over the lake",
    body: "Sustained lucidity for what felt like 4-5 minutes. Could see my hands clearly — six fingers on the left. Flew low over the water near the cabin. The cue 'are you dreaming?' fired and I remembered it WAS the cue, then woke up grinning.",
    dreamDate: "2026-05-11",
    createdAt: "2026-05-12T07:10:00Z",
    lucidity: 3,
    sleepQuality: 5,
    tags: ["lucid", "flying", "water", "WBTB"],
    sessionId: 38,
    wordCount: 312,
  },
  {
    id: 6,
    title: "False wake-up — the third one this week",
    body: "Got up, brushed my teeth, started making coffee, then noticed the clock read 3:47:62. False awakening.",
    dreamDate: "2026-05-10",
    createdAt: "2026-05-11T06:32:00Z",
    lucidity: 1,
    sleepQuality: 2,
    tags: ["false awakening", "recurring"],
    sessionId: 37,
    wordCount: 121,
  },
  {
    id: 5,
    title: "Driving with no road",
    body: "On a highway that kept dissolving into ocean. Not quite lucid but I knew something was off.",
    dreamDate: "2026-05-09",
    createdAt: "2026-05-10T07:00:00Z",
    lucidity: 0,
    sleepQuality: 3,
    tags: ["water"],
    sessionId: 36,
    wordCount: 65,
  },
  {
    id: 4,
    title: "Conversation with someone I haven't seen in years",
    body: "Old friend from school. Felt vivid. Trying to identify the dream-sign — every recurring person might be a trigger.",
    dreamDate: "2026-05-08",
    createdAt: "2026-05-09T06:55:00Z",
    lucidity: 0,
    sleepQuality: 4,
    tags: ["recurring", "dream sign"],
    sessionId: 35,
    wordCount: 88,
  },
  {
    id: 3,
    title: "Caught the moment of falling asleep",
    body: "Watched hypnagogia for a long time. Geometric patterns then a tunnel. Tried WILD transition but lost it.",
    dreamDate: "2026-05-07",
    createdAt: "2026-05-08T07:11:00Z",
    lucidity: 2,
    sleepQuality: 5,
    tags: ["lucid", "WBTB"],
    sessionId: 34,
    wordCount: 156,
  },
  {
    id: 2,
    title: "Childhood kitchen, again",
    body: "Same kitchen as last week. The fridge full of mismatched fruit.",
    dreamDate: "2026-05-06",
    createdAt: "2026-05-07T06:48:00Z",
    lucidity: 0,
    sleepQuality: 3,
    tags: ["recurring", "childhood home"],
    sessionId: 33,
    wordCount: 47,
  },
];

const LUCIDITY_LABELS = ["None", "Faint", "Brief", "Sustained", "Full"];
const LUCIDITY_LABELS_LONG = [
  "No lucidity",
  "Faint awareness",
  "Brief lucidity",
  "Sustained lucidity",
  "Full control",
];

const ACTIVE_PROFILE = {
  name: "MILD + WBTB",
  techniqueKey: "MILD",
  cueCount: 3,
  nextCue: { label: "Bell — soft", inMin: 247, asset: "bell-soft.ogg" },
};

const STREAK = {
  current: 14,
  longest: 23,
  lucidityRate30d: 0.31,
};

const LAST_NIGHT = {
  date: "Night of May 14",
  dreamCount: 2,
  lucidCount: 1,
  cuesFired: 3,
  cuesScheduled: 4,
  sleepDurationMin: 7 * 60 + 24,
  sleepQuality: 4,
};

function relativeDate(iso) {
  const d = new Date(iso + "T00:00:00");
  const now = new Date("2026-05-15T08:00:00");
  const days = Math.round((now - d) / 86400000);
  if (days === 0) return "Tonight";
  if (days === 1) return "Last night";
  if (days < 7) return `${days} nights ago`;
  if (days < 14) return "last week";
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}
function shortDate(iso) {
  const d = new Date(iso + "T00:00:00");
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}
function fmtClock(min) {
  const h = Math.floor(min / 60);
  const m = min % 60;
  return `${h}h ${String(m).padStart(2, "0")}m`;
}

// ─────────────────────────────────────────────────────────────
// Cue profiles + cues + audio assets
// ─────────────────────────────────────────────────────────────
const AUDIO_ASSETS = [
  { id: 1, name: "Bell — soft",            source: "bundled", durationMs: 4200,  family: "bell" },
  { id: 2, name: "Bell — Tibetan bowl",    source: "bundled", durationMs: 9800,  family: "bell" },
  { id: 3, name: "Voice — are you dreaming?", source: "bundled", durationMs: 1900, family: "voice" },
  { id: 4, name: "Pink noise",             source: "bundled", durationMs: 60000, family: "noise" },
  { id: 5, name: "Brown noise",            source: "bundled", durationMs: 60000, family: "noise" },
  { id: 6, name: "Binaural — theta",       source: "bundled", durationMs: 60000, family: "binaural" },
  { id: 7, name: "Voice memo · self-affirm",   source: "recording", durationMs: 8400,  family: "voice" },
  { id: 8, name: "rain-loop.mp3",          source: "imported", durationMs: 120000, family: "noise" },
];
const audioById = (id) => AUDIO_ASSETS.find((a) => a.id === id);

const VIBRATION_PRESETS = [
  { id: "none",   label: "None",   pattern: null },
  { id: "pulse",  label: "Pulse",  pattern: "0,200,100,200" },
  { id: "long",   label: "Long",   pattern: "0,800" },
  { id: "double", label: "Double", pattern: "0,150,80,150,80,150" },
];

// Trigger modes: 'sleep' (SleepRelative), 'clock' (ClockBased), 'interval' (Interval)
const PROFILES = [
  {
    id: 1,
    name: "MILD + WBTB",
    description: "Personal recipe — wake at 4:30, gentle prompts through final REM windows.",
    isTemplate: false,
    techniqueKey: null,
    active: true,
    cues: [
      { id: 11, label: "Wake for WBTB",      enabled: true,  trigger: "sleep", offsetMin: 270, audioId: 2, volume: 55, fadeSec: 8,  loops: 1, vibration: "long" },
      { id: 12, label: "Are you dreaming?",  enabled: true,  trigger: "sleep", offsetMin: 330, audioId: 3, volume: 25, fadeSec: 5,  loops: 2, vibration: "pulse" },
      { id: 13, label: "Soft bell — final",  enabled: true,  trigger: "sleep", offsetMin: 405, audioId: 1, volume: 30, fadeSec: 5,  loops: 1, vibration: "none" },
    ],
  },
  {
    id: 2,
    name: "WBTB",
    description: "Wake after ~5h, stay up 20–30 min, return to bed with intent.",
    isTemplate: true,
    techniqueKey: "WBTB",
    active: false,
    cues: [
      { id: 21, label: "Wake for WBTB",      enabled: true, trigger: "sleep", offsetMin: 300, audioId: 2, volume: 50, fadeSec: 8, loops: 1, vibration: "long" },
    ],
  },
  {
    id: 3,
    name: "MILD",
    description: "Mnemonic induction — no audio cues, just intention and journaling.",
    isTemplate: true,
    techniqueKey: "MILD",
    active: false,
    cues: [],
  },
  {
    id: 4,
    name: "FILD",
    description: "Finger-induced lucid dream — micro-movements after a brief wake.",
    isTemplate: true,
    techniqueKey: "FILD",
    active: false,
    cues: [
      { id: 41, label: "Move your fingers",  enabled: true, trigger: "sleep", offsetMin: 270, audioId: 3, volume: 30, fadeSec: 5, loops: 1, vibration: "pulse" },
      { id: 42, label: "Reality check ping", enabled: true, trigger: "clock", clockTime: "05:10", audioId: 1, volume: 25, fadeSec: 4, loops: 1, vibration: "none" },
    ],
  },
  {
    id: 5,
    name: "WILD",
    description: "Wake-initiated — pink noise during the REM peak window.",
    isTemplate: true,
    techniqueKey: "WILD",
    active: false,
    cues: [
      { id: 51, label: "Pink noise blanket", enabled: true, trigger: "sleep", offsetMin: 360, audioId: 4, volume: 20, fadeSec: 30, loops: 20, vibration: "none" },
    ],
  },
  {
    id: 6,
    name: "SSILD",
    description: "Senses-initiated — soft bell to cue the awareness cycle.",
    isTemplate: true,
    techniqueKey: "SSILD",
    active: false,
    cues: [
      { id: 61, label: "Awareness cycle",    enabled: true, trigger: "sleep", offsetMin: 270, audioId: 1, volume: 30, fadeSec: 5, loops: 1, vibration: "none" },
    ],
  },
  {
    id: 7,
    name: "Sandbox",
    description: "Experimentation — interval pings through the second half of the night.",
    isTemplate: false,
    techniqueKey: null,
    active: false,
    cues: [
      { id: 71, label: "Interval reminder",  enabled: true,  trigger: "interval", intervalStart: "04:00", intervalEnd: "07:00", everyMin: 30, audioId: 3, volume: 22, fadeSec: 4, loops: 1, vibration: "pulse" },
      { id: 72, label: "Wakeful nudge",      enabled: false, trigger: "clock",    clockTime: "06:45", audioId: 2, volume: 40, fadeSec: 6, loops: 1, vibration: "double" },
    ],
  },
];

function fmtOffset(min) {
  const h = Math.floor(min / 60);
  const m = min % 60;
  if (h === 0) return `${m}m`;
  if (m === 0) return `${h}h`;
  return `${h}h ${String(m).padStart(2, "0")}m`;
}

function describeCueTrigger(c) {
  if (c.trigger === "sleep")    return `T+${fmtOffset(c.offsetMin)} after sleep`;
  if (c.trigger === "clock")    return `at ${c.clockTime}`;
  if (c.trigger === "interval") return `every ${c.everyMin}m · ${c.intervalStart}–${c.intervalEnd}`;
  return "";
}

Object.assign(window, {
  TAGS, tagBy, ENTRIES,
  LUCIDITY_LABELS, LUCIDITY_LABELS_LONG,
  ACTIVE_PROFILE, STREAK, LAST_NIGHT,
  relativeDate, shortDate, fmtClock,
  AUDIO_ASSETS, audioById, VIBRATION_PRESETS, PROFILES, fmtOffset, describeCueTrigger,
});

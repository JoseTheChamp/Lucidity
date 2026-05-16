// Icon set — Lucide-style thin-line, stroke=1.5, sized via prop.
// All icons accept {size, color, strokeWidth, className, style}.

const _icon = (path, opts = {}) => function Icon({ size = 22, color = "currentColor", strokeWidth = 1.5, className, style }) {
  return (
    <svg
      width={size} height={size} viewBox="0 0 24 24"
      fill="none" stroke={color} strokeWidth={strokeWidth}
      strokeLinecap="round" strokeLinejoin="round"
      className={className} style={style}
      {...opts}
    >
      {path}
    </svg>
  );
};

const IconHome = _icon(<>
  <path d="M3 11.5 12 4l9 7.5" />
  <path d="M5 10v9a1 1 0 0 0 1 1h4v-6h4v6h4a1 1 0 0 0 1-1v-9" />
</>);

const IconBook = _icon(<>
  <path d="M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3V4z" />
  <path d="M5 17a3 3 0 0 1 3-3h11" />
</>);

const IconMoon = _icon(<>
  <path d="M21 12.8A8 8 0 1 1 11.2 3a6.5 6.5 0 0 0 9.8 9.8z" />
</>);

const IconWaves = _icon(<>
  <path d="M2 8c2 0 2-1.5 4-1.5S8 8 10 8s2-1.5 4-1.5S16 8 18 8s2-1.5 4-1.5" />
  <path d="M2 14c2 0 2-1.5 4-1.5S8 14 10 14s2-1.5 4-1.5S16 14 18 14s2-1.5 4-1.5" />
  <path d="M2 20c2 0 2-1.5 4-1.5S8 20 10 20s2-1.5 4-1.5S16 20 18 20s2-1.5 4-1.5" />
</>);

const IconChart = _icon(<>
  <path d="M3 3v18h18" />
  <path d="M7 14l3-3 4 4 5-6" />
</>);

const IconSettings = _icon(<>
  <circle cx="12" cy="12" r="3" />
  <path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1A1.7 1.7 0 0 0 9 19.4a1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1A1.7 1.7 0 0 0 4.6 9a1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1A1.7 1.7 0 0 0 15 4.6a1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z" />
</>);

const IconPlus = _icon(<>
  <path d="M12 5v14M5 12h14" />
</>);

const IconSearch = _icon(<>
  <circle cx="11" cy="11" r="7" />
  <path d="m21 21-4.3-4.3" />
</>);

const IconFilter = _icon(<>
  <path d="M3 5h18l-7 8v6l-4 2v-8z" />
</>);

const IconMic = _icon(<>
  <rect x="9" y="2" width="6" height="13" rx="3" />
  <path d="M5 11a7 7 0 0 0 14 0" />
  <path d="M12 18v4" />
  <path d="M9 22h6" />
</>);

const IconFlame = _icon(<>
  <path d="M12 2s5 4 5 9a5 5 0 0 1-10 0c0-2 1-3 2-4-1 4 3 4 3 1 0-2-2-3 0-6z" />
</>);

const IconBell = _icon(<>
  <path d="M6 8a6 6 0 1 1 12 0c0 7 3 8 3 8H3s3-1 3-8" />
  <path d="M10 21a2 2 0 0 0 4 0" />
</>);

const IconPlay = _icon(<>
  <path d="M6 4l14 8-14 8V4z" />
</>);

const IconPause = _icon(<>
  <rect x="6" y="4" width="4" height="16" rx="1" />
  <rect x="14" y="4" width="4" height="16" rx="1" />
</>);

const IconChevronRight = _icon(<>
  <path d="m9 6 6 6-6 6" />
</>);

const IconChevronLeft = _icon(<>
  <path d="m15 6-6 6 6 6" />
</>);

const IconCheck = _icon(<>
  <path d="M5 12l5 5L20 7" />
</>);

const IconClose = _icon(<>
  <path d="M6 6l12 12M18 6 6 18" />
</>);

const IconArrowRight = _icon(<>
  <path d="M5 12h14M13 5l7 7-7 7" />
</>);

const IconClock = _icon(<>
  <circle cx="12" cy="12" r="9" />
  <path d="M12 7v5l3 2" />
</>);

const IconTag = _icon(<>
  <path d="M20 12.5 12.5 20a2 2 0 0 1-2.8 0L3 13.3V4h9.3l7.7 7.7a2 2 0 0 1 0 2.8z" />
  <circle cx="8" cy="9" r="1.2" fill="currentColor" stroke="none" />
</>);

const IconSparkles = _icon(<>
  <path d="M12 3l1.7 4.3L18 9l-4.3 1.7L12 15l-1.7-4.3L6 9l4.3-1.7z" />
  <path d="M19 14l.8 2 2 .8-2 .8-.8 2-.8-2-2-.8 2-.8z" />
</>);

const IconDots = _icon(<>
  <circle cx="5" cy="12" r="1" fill="currentColor" />
  <circle cx="12" cy="12" r="1" fill="currentColor" />
  <circle cx="19" cy="12" r="1" fill="currentColor" />
</>);

const IconStar = _icon(<>
  <path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z" />
</>);

// ─────────────────────────────────────────────────────────────
// LucidityEye — 5-state opening eye glyph.
// level: 0 (None) → 1 (Faint) → 2 (Brief) → 3 (Sustained) → 4 (Full)
// As level rises: eyelid opens, pupil enlarges, glow appears.
// ─────────────────────────────────────────────────────────────
function LucidityEye({ level = 0, size = 22, color, strokeWidth = 1.5, glow = true }) {
  const c = color || (level >= 3 ? 'var(--lucid)' : level >= 1 ? 'var(--accent)' : 'var(--text-ter)');
  // Eye lid opening height: 0 = closed (line), 4 = full open
  // Use a clip ellipse that scales vertically by level.
  const open = [0.06, 0.28, 0.5, 0.75, 1][level] ?? 0; // fraction of vertical eye height
  // Pupil radius grows with level.
  const pupilR = [0, 1.4, 2.0, 2.6, 3.2][level] ?? 0;

  // SVG: 24x24 viewBox, eye centered
  // Eye outline: horizontal lens shape using path; we draw two arcs meeting at points 4,12 and 20,12.
  // Top arc curve height varies with `open`. Bottom mirrors.
  const cx = 12, cy = 12;
  const halfW = 8;
  const lidY = 5 * open; // top lid raises this much above 12; mirror below
  const topArc = `M ${cx - halfW},${cy} Q ${cx},${cy - lidY * 1.6} ${cx + halfW},${cy}`;
  const botArc = `M ${cx - halfW},${cy} Q ${cx},${cy + lidY * 1.6} ${cx + halfW},${cy}`;

  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
      stroke={c} strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round">
      {glow && level >= 3 && (
        <defs>
          <radialGradient id={`eye-glow-${level}`}>
            <stop offset="0%" stopColor={level >= 3 ? '#5EEAD4' : '#6B8AFF'} stopOpacity="0.45" />
            <stop offset="100%" stopColor="#5EEAD4" stopOpacity="0" />
          </radialGradient>
        </defs>
      )}
      {glow && level >= 3 && (
        <circle cx={cx} cy={cy} r="11" fill={`url(#eye-glow-${level})`} stroke="none" />
      )}
      {/* Eye lens */}
      <path d={topArc} />
      <path d={botArc} />
      {/* Pupil */}
      {level >= 1 && (
        <circle cx={cx} cy={cy} r={pupilR} fill={c} stroke="none" />
      )}
      {/* Iris ring for sustained+ */}
      {level >= 2 && (
        <circle cx={cx} cy={cy} r={pupilR + 1.5} stroke={c} strokeWidth="0.8" opacity="0.5" />
      )}
    </svg>
  );
}

Object.assign(window, {
  IconHome, IconBook, IconMoon, IconWaves, IconChart, IconSettings,
  IconPlus, IconSearch, IconFilter, IconMic, IconFlame, IconBell,
  IconPlay, IconPause, IconChevronRight, IconChevronLeft, IconCheck,
  IconClose, IconArrowRight, IconClock, IconTag, IconSparkles, IconDots, IconStar,
  LucidityEye,
});

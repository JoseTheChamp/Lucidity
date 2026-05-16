// editor.jsx — Entry Editor / Entry Detail combined
// Modes: 'new' | 'edit' | 'view' (but view is what we show by default when opening an existing one — has edit button)

function EditorScreen({ entryId, isNew, onBack }) {
  const original = entryId ? ENTRIES.find((e) => e.id === entryId) : null;

  const [title, setTitle] = React.useState(original?.title || "");
  const [body, setBody] = React.useState(original?.body || "");
  const [lucidity, setLucidity] = React.useState(original?.lucidity ?? 0);
  const [sleepQuality, setSleepQuality] = React.useState(original?.sleepQuality ?? 3);
  const [tags, setTags] = React.useState(original?.tags || []);
  const [dreamDate, setDreamDate] = React.useState(original?.dreamDate || "2026-05-14");
  const [savedAt, setSavedAt] = React.useState("Just now");
  const [recording, setRecording] = React.useState(false);
  const [tagPickerOpen, setTagPickerOpen] = React.useState(false);

  // Mock autosave indicator
  const dirtyTimer = React.useRef(null);
  function markDirty() {
    setSavedAt("Saving…");
    clearTimeout(dirtyTimer.current);
    dirtyTimer.current = setTimeout(() => setSavedAt("Saved"), 500);
  }

  // Toggle a tag
  function toggleTag(name) {
    markDirty();
    setTags((cur) => (cur.includes(name) ? cur.filter((t) => t !== name) : [...cur, name]));
  }

  const wc = body.trim() ? body.trim().split(/\s+/).length : 0;

  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", overflowX: "hidden", display: "flex", flexDirection: "column" }}>
      <EditorHeader
        onBack={onBack}
        savedAt={savedAt}
        wc={wc}
        isNew={isNew}
      />

      <div style={{ padding: "8px var(--pad-x) 24px", display: "flex", flexDirection: "column", gap: 18 }}>
        {/* Date + session pill */}
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <DateChip date={dreamDate} />
          {original?.sessionId && (
            <span className="pill" style={{ color: "var(--lavender)", borderColor: "rgba(167,139,250,0.3)" }}>
              <IconMoon size={10} color="var(--lavender)" /> Session #{original.sessionId}
            </span>
          )}
        </div>

        {/* Title */}
        <div>
          <input
            className="input-bare"
            value={title}
            onChange={(e) => { setTitle(e.target.value); markDirty(); }}
            placeholder="Title this dream…"
            style={{
              fontSize: 22,
              fontWeight: 500,
              letterSpacing: -0.4,
              lineHeight: 1.25,
              color: "var(--text-pri)",
            }}
          />
        </div>

        {/* Body + mic */}
        <BodyField
          value={body}
          onChange={(v) => { setBody(v); markDirty(); }}
          recording={recording}
          onToggleRecord={() => setRecording((r) => !r)}
        />

        {/* Lucidity */}
        <Section title="LUCIDITY">
          <LucidityPicker value={lucidity} onChange={(v) => { setLucidity(v); markDirty(); }} />
        </Section>

        {/* Sleep quality */}
        <Section title="SLEEP QUALITY">
          <SleepQuality value={sleepQuality} onChange={(v) => { setSleepQuality(v); markDirty(); }} />
        </Section>

        {/* Tags */}
        <Section title="TAGS" trailing={
          <button className="btn btn-ghost" onClick={() => setTagPickerOpen((v) => !v)} style={{ padding: "4px 6px", fontSize: 11, color: "var(--accent)", fontFamily: "var(--font-mono)" }}>
            {tagPickerOpen ? "DONE" : "EDIT"}
          </button>
        }>
          <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
            {tags.length === 0 && !tagPickerOpen && (
              <span style={{ fontSize: 13, color: "var(--text-ter)" }}>No tags yet</span>
            )}
            {!tagPickerOpen && tags.map((t) => <TagChip key={t} name={t} />)}
            {tagPickerOpen && TAGS.map((t) => (
              <TagChip
                key={t.name}
                name={t.name}
                active={tags.includes(t.name)}
                onClick={() => toggleTag(t.name)}
              />
            ))}
          </div>
        </Section>

        {/* Danger / meta */}
        <div style={{
          marginTop: 8, padding: "12px 14px",
          border: "1px solid var(--border)", borderRadius: 12,
          display: "flex", alignItems: "center", justifyContent: "space-between",
        }}>
          <div>
            <div className="label-meta">CREATED</div>
            <div className="mono" style={{ marginTop: 3, fontSize: 12, color: "var(--text-sec)" }}>
              {original ? new Date(original.createdAt).toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" }) : "Just now"}
            </div>
          </div>
          <button className="btn btn-ghost" style={{ color: "var(--warn)", fontSize: 12 }}>
            Delete
          </button>
        </div>
      </div>
    </div>
  );
}

function EditorHeader({ onBack, savedAt, wc, isNew }) {
  return (
    <div style={{
      padding: "14px var(--pad-x) 10px",
      display: "flex", alignItems: "center", justifyContent: "space-between",
      borderBottom: "1px solid var(--border)",
      background: "rgba(10,14,26,0.7)",
      backdropFilter: "blur(10px)",
      WebkitBackdropFilter: "blur(10px)",
    }}>
      <button onClick={onBack} className="btn btn-ghost" style={{ padding: 8, marginLeft: -8 }} aria-label="Back">
        <IconChevronLeft size={22} color="var(--text-sec)" />
      </button>
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <div style={{
          display: "flex", alignItems: "center", gap: 6,
          fontFamily: "var(--font-mono)", fontSize: 11,
          color: savedAt === "Saving…" ? "var(--lavender)" : "var(--text-ter)",
          letterSpacing: 0.5, textTransform: "uppercase",
        }}>
          {savedAt === "Saving…" ? (
            <span style={{ width: 6, height: 6, borderRadius: "50%", background: "var(--lavender)", animation: "breathe 1.2s ease-in-out infinite" }} />
          ) : (
            <IconCheck size={11} color="var(--text-ter)" />
          )}
          {savedAt}
        </div>
        <div style={{ width: 1, height: 12, background: "var(--border-2)" }} />
        <span className="mono tabular" style={{ fontSize: 11, color: "var(--text-ter)" }}>
          {wc}w
        </span>
      </div>
    </div>
  );
}

function DateChip({ date }) {
  return (
    <span className="pill" style={{ color: "var(--text-pri)", borderColor: "var(--border-2)" }}>
      <IconClock size={10} color="var(--text-sec)" /> Night of {shortDate(date)}
    </span>
  );
}

function Section({ title, trailing, children }) {
  return (
    <div>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10, padding: "0 2px" }}>
        <div className="label-meta">{title}</div>
        {trailing}
      </div>
      {children}
    </div>
  );
}

function BodyField({ value, onChange, recording, onToggleRecord }) {
  const textareaRef = React.useRef(null);

  // Auto-grow textarea
  React.useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = Math.max(140, el.scrollHeight) + "px";
  }, [value]);

  return (
    <div style={{
      position: "relative",
      background: "rgba(10,14,26,0.5)",
      border: "1px solid var(--border)",
      borderRadius: 14,
      padding: "14px 14px 56px",
      transition: "border-color 200ms ease",
      ...(recording ? { borderColor: "rgba(248,113,113,0.4)" } : {}),
    }}>
      <textarea
        ref={textareaRef}
        className="input-bare"
        placeholder="Describe the dream while it's fresh. Sensory details, colors, characters, anything that felt off…"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        style={{
          fontSize: 15,
          lineHeight: 1.55,
          minHeight: 140,
        }}
      />

      {/* Recording overlay waveform */}
      {recording && <Waveform />}

      {/* Mic button (bottom-right) */}
      <button
        onClick={onToggleRecord}
        style={{
          position: "absolute",
          bottom: 12, right: 12,
          width: 38, height: 38, borderRadius: "50%",
          background: recording ? "rgba(248,113,113,0.18)" : "rgba(255,255,255,0.04)",
          border: `1px solid ${recording ? "rgba(248,113,113,0.5)" : "var(--border-2)"}`,
          color: recording ? "var(--warn)" : "var(--text-sec)",
          display: "flex", alignItems: "center", justifyContent: "center",
          cursor: "pointer",
          animation: recording ? "mic-pulse 1.6s ease-in-out infinite" : "none",
          transition: "all 160ms ease",
        }}
        aria-label={recording ? "Stop recording" : "Start voice journaling"}
      >
        <IconMic size={18} color="currentColor" />
      </button>
    </div>
  );
}

function Waveform() {
  const bars = 28;
  return (
    <div style={{
      position: "absolute",
      bottom: 18, left: 14, right: 60,
      height: 24,
      display: "flex", alignItems: "center", gap: 2,
      pointerEvents: "none",
    }}>
      {Array.from({ length: bars }).map((_, i) => {
        const h = 4 + Math.abs(Math.sin(i * 0.7 + Date.now() * 0.0001)) * 18;
        return (
          <div
            key={i}
            style={{
              width: 2,
              height: h,
              background: "var(--warn)",
              opacity: 0.5 + Math.random() * 0.5,
              borderRadius: 1,
              animation: `breathe ${0.6 + (i % 5) * 0.2}s ease-in-out infinite`,
            }}
          />
        );
      })}
    </div>
  );
}

function LucidityPicker({ value, onChange }) {
  return (
    <div>
      <div style={{ display: "flex", gap: 6 }}>
        {[0, 1, 2, 3, 4].map((lv) => {
          const active = value === lv;
          return (
            <button
              key={lv}
              onClick={() => onChange(lv)}
              style={{
                flex: 1,
                background: active
                  ? (lv >= 3 ? "var(--lucid-soft)" : "var(--accent-soft)")
                  : "rgba(255,255,255,0.02)",
                border: `1px solid ${active ? (lv >= 3 ? "var(--lucid)" : "var(--accent)") : "var(--border)"}`,
                borderRadius: 12,
                padding: "12px 4px 10px",
                display: "flex", flexDirection: "column", alignItems: "center", gap: 6,
                cursor: "pointer",
                transition: "all 140ms ease",
              }}
            >
              <LucidityEye level={lv} size={22} />
              <span className="mono" style={{ fontSize: 9, letterSpacing: 0.5, color: active ? (lv >= 3 ? "var(--lucid)" : "var(--accent)") : "var(--text-ter)" }}>
                {LUCIDITY_LABELS[lv].toUpperCase()}
              </span>
            </button>
          );
        })}
      </div>
      <div style={{
        marginTop: 10, padding: "10px 12px",
        borderRadius: 10,
        background: "rgba(255,255,255,0.02)",
        border: "1px solid var(--border)",
        fontSize: 12.5, color: "var(--text-sec)", lineHeight: 1.45,
      }}>
        {LUCIDITY_DESCRIPTIONS[value]}
      </div>
    </div>
  );
}

const LUCIDITY_DESCRIPTIONS = [
  "No awareness — you were a character in the dream.",
  "Faint awareness — something felt off but you didn't pursue it.",
  "Brief lucidity — you knew, but lost it within seconds.",
  "Sustained lucidity — held awareness, perhaps explored.",
  "Full control — manipulated the dream environment with intent.",
];

function SleepQuality({ value, onChange }) {
  return (
    <div style={{
      display: "flex", alignItems: "center", justifyContent: "space-between",
      padding: "12px 14px",
      background: "rgba(255,255,255,0.02)",
      border: "1px solid var(--border)",
      borderRadius: 12,
    }}>
      <div style={{ display: "flex", gap: 6 }}>
        {[1, 2, 3, 4, 5].map((s) => (
          <button
            key={s}
            onClick={() => onChange(s)}
            style={{
              background: "transparent",
              border: "none",
              padding: 4,
              cursor: "pointer",
              color: s <= value ? "var(--accent)" : "var(--text-quat)",
            }}
            aria-label={`Sleep quality ${s}`}
          >
            <IconStar size={20} color="currentColor" style={{ fill: s <= value ? "var(--accent)" : "transparent" }} />
          </button>
        ))}
      </div>
      <span className="mono" style={{ fontSize: 12, color: "var(--text-sec)" }}>
        {["—", "Poor", "Restless", "OK", "Good", "Deep"][value]}
      </span>
    </div>
  );
}

Object.assign(window, { EditorScreen });

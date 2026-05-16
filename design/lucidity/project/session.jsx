// session.jsx — Active Sleep Session (ambient screen)

function SessionScreen({ onEnd, onBack, sessionStartedAt }) {
  // Tick the elapsed time every second.
  const [now, setNow] = React.useState(Date.now());
  React.useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  const elapsedMs = Math.max(0, now - sessionStartedAt);
  const elapsedSec = Math.floor(elapsedMs / 1000);
  const hh = Math.floor(elapsedSec / 3600);
  const mm = Math.floor((elapsedSec % 3600) / 60);
  const ss = elapsedSec % 60;

  // Mock cue schedule (offsets in seconds from session start)
  const cues = [
    { id: 1, label: "Soft bell",        offset: 60,    asset: "bell-soft.ogg" },
    { id: 2, label: "Are you dreaming?", offset: 270,   asset: "voice-aud.ogg" },
    { id: 3, label: "Tibetan bowl",     offset: 18000, asset: "bell-tibetan.ogg" },
  ];
  const nextCue = cues.find((c) => c.offset > elapsedSec) || null;
  const nextInSec = nextCue ? nextCue.offset - elapsedSec : null;

  // Confirm end
  const [confirmEnd, setConfirmEnd] = React.useState(false);

  return (
    <div className="screen-enter" style={{
      flex: 1, position: "relative", overflow: "hidden",
      background: "linear-gradient(180deg, #060814 0%, #02030a 80%, #000 100%)",
    }}>
      {/* Star field */}
      <StarField />
      {/* Breathing aura */}
      <div style={{
        position: "absolute",
        top: "32%", left: "50%",
        transform: "translate(-50%, -50%)",
        width: 360, height: 360,
        borderRadius: "50%",
        background: "radial-gradient(circle, rgba(167,139,250,0.18) 0%, rgba(107,138,255,0.08) 40%, transparent 70%)",
        animation: "breathe 6s ease-in-out infinite",
        pointerEvents: "none",
      }} />

      <div style={{
        position: "relative",
        zIndex: 5,
        padding: "20px 24px",
        height: "100%",
        display: "flex", flexDirection: "column",
      }}>
        {/* Top: back + profile name */}
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <button
            onClick={onBack}
            className="btn btn-ghost"
            style={{ padding: 8, marginLeft: -8, color: "var(--text-sec)" }}
            aria-label="Back"
          >
            <IconChevronLeft size={22} color="currentColor" />
          </button>
          <div style={{
            width: 8, height: 8, borderRadius: "50%",
            background: "var(--lavender)",
            boxShadow: "0 0 12px var(--lavender)",
            animation: "breathe 2.4s ease-in-out infinite",
          }} />
          <span className="mono" style={{ fontSize: 11, color: "var(--text-sec)", letterSpacing: 0.8, textTransform: "uppercase" }}>
            MONITORING · MILD + WBTB
          </span>
        </div>

        {/* Center: elapsed */}
        <div style={{
          flex: 1,
          display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center",
          paddingBottom: 60,
        }}>
          <div className="label-meta" style={{ marginBottom: 14, textAlign: "center" }}>SLEEPING SINCE 03:42</div>
          <div className="mono tabular" style={{
            fontSize: 64, fontWeight: 200, color: "var(--text-pri)",
            letterSpacing: -2, lineHeight: 1,
            textShadow: "0 0 40px rgba(107,138,255,0.25)",
          }}>
            {String(hh).padStart(2, "0")}:{String(mm).padStart(2, "0")}
            <span style={{ color: "var(--text-ter)", fontSize: 48 }}>:{String(ss).padStart(2, "0")}</span>
          </div>
          <div style={{ marginTop: 18, display: "flex", alignItems: "center", gap: 8, color: "var(--text-sec)" }}>
            <IconMoon size={14} color="var(--text-sec)" />
            <span className="mono" style={{ fontSize: 12, letterSpacing: 0.5 }}>elapsed</span>
          </div>
        </div>

        {/* Bottom: next cue + actions */}
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {nextCue ? (
            <NextCueCard cue={nextCue} secondsRemaining={nextInSec} />
          ) : (
            <div style={{
              padding: "14px 16px", borderRadius: 14,
              background: "rgba(19,24,41,0.6)",
              border: "1px solid var(--border)",
              textAlign: "center",
              color: "var(--text-sec)",
              fontSize: 13,
            }}>
              All cues fired. Sleep well.
            </div>
          )}
          <CueTimeline cues={cues} elapsedSec={elapsedSec} />
          <button
            onClick={() => setConfirmEnd(true)}
            className="btn"
            style={{
              padding: "14px 18px",
              borderRadius: 14,
              background: "rgba(248,113,113,0.06)",
              borderColor: "rgba(248,113,113,0.2)",
              color: "var(--warn)",
            }}
          >
            End sleep session
          </button>
        </div>
      </div>

      {confirmEnd && <EndConfirm onCancel={() => setConfirmEnd(false)} onConfirm={onEnd} />}
    </div>
  );
}

function NextCueCard({ cue, secondsRemaining }) {
  const m = Math.floor(secondsRemaining / 60);
  const s = secondsRemaining % 60;
  const h = Math.floor(m / 60);
  const display =
    h > 0 ? `${h}h ${m % 60}m ${String(s).padStart(2, "0")}s` :
    m > 0 ? `${m}m ${String(s).padStart(2, "0")}s` :
            `${s}s`;
  return (
    <div style={{
      padding: "14px 16px",
      borderRadius: 14,
      background: "linear-gradient(180deg, rgba(107,138,255,0.10), rgba(19,24,41,0.7))",
      border: "1px solid rgba(107,138,255,0.25)",
      display: "flex", alignItems: "center", gap: 12,
    }}>
      <div style={{
        width: 38, height: 38, borderRadius: 10,
        background: "var(--accent-soft)",
        border: "1px solid rgba(107,138,255,0.3)",
        display: "flex", alignItems: "center", justifyContent: "center",
        animation: "breathe 3.6s ease-in-out infinite",
      }}>
        <IconWaves size={18} color="var(--accent)" />
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div className="label-meta">NEXT CUE</div>
        <div style={{ marginTop: 3, fontSize: 14, fontWeight: 500, color: "var(--text-pri)" }}>
          {cue.label}
        </div>
      </div>
      <div style={{ textAlign: "right" }}>
        <div className="mono tabular" style={{ fontSize: 18, color: "var(--accent)", letterSpacing: -0.5 }}>
          {display}
        </div>
        <div className="mono" style={{ fontSize: 10, color: "var(--text-ter)", letterSpacing: 0.5, textTransform: "uppercase", marginTop: 2 }}>
          remaining
        </div>
      </div>
    </div>
  );
}

function CueTimeline({ cues, elapsedSec }) {
  // Total span = max cue offset + 30 min buffer.
  const total = Math.max(...cues.map((c) => c.offset)) + 1800;
  return (
    <div style={{ padding: "8px 4px" }}>
      <div style={{ position: "relative", height: 28 }}>
        {/* Track */}
        <div style={{
          position: "absolute", top: 13, left: 0, right: 0, height: 2,
          background: "var(--border)",
          borderRadius: 1,
        }} />
        {/* Progress */}
        <div style={{
          position: "absolute", top: 13, left: 0,
          width: `${Math.min(100, (elapsedSec / total) * 100)}%`,
          height: 2,
          background: "linear-gradient(90deg, var(--lavender), var(--accent))",
          borderRadius: 1,
          boxShadow: "0 0 8px var(--accent-glow)",
        }} />
        {/* "Now" marker */}
        <div style={{
          position: "absolute",
          top: 8,
          left: `${Math.min(100, (elapsedSec / total) * 100)}%`,
          width: 12, height: 12, borderRadius: "50%",
          background: "var(--accent)",
          transform: "translateX(-50%)",
          boxShadow: "0 0 0 4px rgba(107,138,255,0.18), 0 0 12px var(--accent-glow)",
        }} />
        {/* Cue ticks */}
        {cues.map((c) => {
          const fired = elapsedSec >= c.offset;
          return (
            <div
              key={c.id}
              style={{
                position: "absolute",
                top: 10, left: `${(c.offset / total) * 100}%`,
                transform: "translateX(-50%)",
                width: 8, height: 8, borderRadius: "50%",
                background: fired ? "var(--lucid)" : "var(--surface-2)",
                border: `1.5px solid ${fired ? "var(--lucid)" : "var(--border-2)"}`,
              }}
              title={c.label}
            />
          );
        })}
      </div>
      <div style={{ display: "flex", justifyContent: "space-between", marginTop: 4 }}>
        <span className="mono" style={{ fontSize: 10, color: "var(--text-ter)", letterSpacing: 0.5 }}>NOW</span>
        <span className="mono" style={{ fontSize: 10, color: "var(--text-ter)", letterSpacing: 0.5 }}>
          {cues.length} CUES SCHEDULED
        </span>
      </div>
    </div>
  );
}

function StarField() {
  // Pre-compute deterministic star positions so they don't reshuffle.
  const stars = React.useMemo(() => {
    const arr = [];
    let seed = 17;
    function rnd() { seed = (seed * 9301 + 49297) % 233280; return seed / 233280; }
    for (let i = 0; i < 60; i++) {
      arr.push({
        x: rnd() * 100,
        y: rnd() * 100,
        size: 0.8 + rnd() * 1.4,
        delay: rnd() * 4,
        dur: 2.5 + rnd() * 3.5,
        hue: rnd() > 0.85 ? "var(--lavender)" : rnd() > 0.7 ? "var(--accent)" : "#E8ECF4",
      });
    }
    return arr;
  }, []);
  return (
    <div style={{ position: "absolute", inset: 0, pointerEvents: "none", zIndex: 1 }}>
      {stars.map((s, i) => (
        <div
          key={i}
          style={{
            position: "absolute",
            left: `${s.x}%`,
            top: `${s.y}%`,
            width: s.size,
            height: s.size,
            borderRadius: "50%",
            background: s.hue,
            opacity: 0.6,
            boxShadow: s.size > 1.5 ? `0 0 ${s.size * 3}px ${s.hue}` : "none",
            animation: `twinkle ${s.dur}s ease-in-out ${s.delay}s infinite`,
          }}
        />
      ))}
    </div>
  );
}

function EndConfirm({ onCancel, onConfirm }) {
  return (
    <div style={{
      position: "absolute", inset: 0, zIndex: 100,
      background: "rgba(0,0,0,0.7)",
      backdropFilter: "blur(14px)",
      WebkitBackdropFilter: "blur(14px)",
      display: "flex", alignItems: "flex-end", justifyContent: "center",
      padding: 20, paddingBottom: 36,
    }}>
      <div style={{
        width: "100%",
        background: "linear-gradient(180deg, #1a2038 0%, #131829 100%)",
        border: "1px solid var(--border-2)",
        borderRadius: 20,
        padding: 22,
        display: "flex", flexDirection: "column", gap: 16,
      }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 500, marginBottom: 4 }}>End sleep session?</div>
          <div style={{ fontSize: 13, color: "var(--text-sec)", lineHeight: 1.5 }}>
            You'll be prompted for sleep quality and can log dreams immediately.
          </div>
        </div>
        <div style={{ display: "flex", gap: 10 }}>
          <button onClick={onCancel} className="btn" style={{ flex: 1 }}>
            Keep monitoring
          </button>
          <button onClick={onConfirm} className="btn btn-primary" style={{ flex: 1, background: "var(--warn)", color: "#1a0708" }}>
            End session
          </button>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { SessionScreen, SessionIndicator });

// ─────────────────────────────────────────────────────────────
// SessionIndicator — slim "monitoring" bar shown above every screen
// when a session is active (except the session screen itself).
// ─────────────────────────────────────────────────────────────
function SessionIndicator({ sessionStartedAt, onResume }) {
  const [now, setNow] = React.useState(Date.now());
  React.useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const elapsed = Math.max(0, Math.floor((now - sessionStartedAt) / 1000));
  const hh = Math.floor(elapsed / 3600);
  const mm = Math.floor((elapsed % 3600) / 60);
  const ss = elapsed % 60;
  const time = `${String(hh).padStart(2, "0")}:${String(mm).padStart(2, "0")}:${String(ss).padStart(2, "0")}`;
  return (
    <button
      onClick={onResume}
      style={{
        display: "flex",
        alignItems: "center",
        gap: 10,
        width: "100%",
        padding: "8px 16px",
        background: "linear-gradient(180deg, rgba(167,139,250,0.16), rgba(107,138,255,0.08))",
        borderBottom: "1px solid rgba(167,139,250,0.25)",
        borderTop: "none",
        borderLeft: "none",
        borderRight: "none",
        cursor: "pointer",
        color: "var(--text-pri)",
        fontFamily: "var(--font-sans)",
        textAlign: "left",
        position: "relative",
        overflow: "hidden",
      }}
    >
      {/* Soft scanning shimmer */}
      <div style={{
        position: "absolute",
        inset: 0,
        background: "radial-gradient(120% 100% at 0% 50%, rgba(167,139,250,0.12), transparent 60%)",
        pointerEvents: "none",
      }} />
      <div style={{
        width: 8, height: 8, borderRadius: "50%",
        background: "var(--lavender)",
        boxShadow: "0 0 12px var(--lavender)",
        animation: "breathe 2.4s ease-in-out infinite",
        flexShrink: 0,
        position: "relative",
        zIndex: 1,
      }} />
      <div style={{ display: "flex", alignItems: "baseline", gap: 8, flex: 1, minWidth: 0, position: "relative", zIndex: 1 }}>
        <span className="mono" style={{
          fontSize: 10,
          letterSpacing: 0.8,
          textTransform: "uppercase",
          color: "var(--lavender)",
        }}>
          MONITORING
        </span>
        <span className="mono tabular" style={{ fontSize: 12, color: "var(--text-pri)" }}>
          {time}
        </span>
        <span className="mono" style={{ fontSize: 10, color: "var(--text-sec)", letterSpacing: 0.5, overflow: "hidden", whiteSpace: "nowrap", textOverflow: "ellipsis" }}>
          · MILD + WBTB
        </span>
      </div>
      <IconChevronRight size={14} color="var(--lavender)" style={{ position: "relative", zIndex: 1 }} />
    </button>
  );
}

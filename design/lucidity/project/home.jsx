// home.jsx — Dashboard

function HomeScreen({ onNav, onStartSleep, onNewEntry, sessionActive }) {
  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", overflowX: "hidden" }}>
      <HomeHeader />
      <div style={{ padding: "8px var(--pad-x) 24px", display: "flex", flexDirection: "column", gap: "var(--gap)" }}>
        <StreakCard onTapStats={() => onNav("stats")} />
        <LastNightCard onOpen={() => onNav("journal")} />
        <ActiveProfileCard
          sessionActive={sessionActive}
          onStart={onStartSleep}
          onResume={() => onNav("session")}
          onManage={() => onNav("cues")}
          onOpenActive={() => onNav("profile", { profileId: PROFILES.find((p) => p.active)?.id || 1 })}
        />
        <RecentEntries onOpen={(id) => onNav("editor", { entryId: id })} onSeeAll={() => onNav("journal")} />
        <RealityCheckHint />
        <div style={{ height: 8 }} />
      </div>
    </div>
  );
}

function HomeHeader() {
  const greeting = "Good morning, Josef";
  return (
    <div style={{ padding: "18px var(--pad-x) 12px", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
      <div>
        <div className="label-meta" style={{ marginBottom: 6 }}>FRI · MAY 15 · 03:42</div>
        <div style={{ fontSize: 22, fontWeight: 500, letterSpacing: -0.3, lineHeight: 1.1 }}>{greeting}</div>
      </div>
      <button className="btn btn-ghost" style={{ padding: 10, borderRadius: 12 }} aria-label="Notifications">
        <IconBell size={20} color="var(--text-sec)" />
      </button>
    </div>
  );
}

function StreakCard({ onTapStats }) {
  return (
    <div className="card" onClick={onTapStats} style={{ cursor: "pointer", position: "relative", overflow: "hidden" }}>
      {/* Aura */}
      <div style={{
        position: "absolute", right: -40, top: -40, width: 180, height: 180,
        background: "radial-gradient(circle, var(--accent-soft) 0%, transparent 70%)",
        pointerEvents: "none",
      }} />
      <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 12 }}>
        <div>
          <div className="label-meta">CURRENT STREAK</div>
          <div style={{ display: "flex", alignItems: "baseline", gap: 8, marginTop: 6 }}>
            <span className="mono tabular" style={{ fontSize: "var(--fs-hero)", fontWeight: 300, letterSpacing: -1, color: "var(--text-pri)", lineHeight: 1 }}>
              {STREAK.current}
            </span>
            <span style={{ fontSize: 14, color: "var(--text-sec)" }}>nights</span>
          </div>
        </div>
        <IconChevronRight size={18} color="var(--text-ter)" />
      </div>
      <Sparkline />
      <div style={{ display: "flex", justifyContent: "space-between", marginTop: 12, fontSize: 12 }}>
        <div>
          <div className="label-meta">LONGEST</div>
          <div className="mono tabular" style={{ marginTop: 4, fontSize: 14, color: "var(--text-pri)" }}>{STREAK.longest} nights</div>
        </div>
        <div style={{ textAlign: "right" }}>
          <div className="label-meta">LUCIDITY · 30D</div>
          <div className="mono tabular" style={{ marginTop: 4, fontSize: 14, color: "var(--lucid)" }}>{Math.round(STREAK.lucidityRate30d * 100)}%</div>
        </div>
      </div>
    </div>
  );
}

function Sparkline() {
  // 14 nights of fictional lucidity scores 0–4
  const data = [0, 1, 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 1];
  const w = 320, h = 36;
  const step = w / (data.length - 1);
  const max = 4;
  const points = data.map((v, i) => `${i * step},${h - (v / max) * h}`).join(" ");
  return (
    <div style={{ width: "100%", overflow: "hidden", marginTop: 6 }}>
      <svg viewBox={`0 0 ${w} ${h}`} width="100%" height={h} preserveAspectRatio="none">
        <defs>
          <linearGradient id="spark-fill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--accent)" stopOpacity="0.25" />
            <stop offset="100%" stopColor="var(--accent)" stopOpacity="0" />
          </linearGradient>
        </defs>
        <polyline points={`0,${h} ${points} ${w},${h}`} fill="url(#spark-fill)" stroke="none" />
        <polyline points={points} fill="none" stroke="var(--accent)" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
        {data.map((v, i) =>
          v >= 2 ? (
            <circle key={i} cx={i * step} cy={h - (v / max) * h} r="1.8" fill="var(--lucid)" />
          ) : null
        )}
      </svg>
    </div>
  );
}

function LastNightCard({ onOpen }) {
  return (
    <div className="card" onClick={onOpen} style={{ cursor: "pointer" }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 14 }}>
        <div>
          <div className="label-meta">LAST NIGHT</div>
          <div style={{ marginTop: 4, fontSize: "var(--fs-title)", fontWeight: 500 }}>{LAST_NIGHT.date}</div>
        </div>
        <IconChevronRight size={18} color="var(--text-ter)" />
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 12 }}>
        <Stat label="DREAMS"   value={LAST_NIGHT.dreamCount} />
        <Stat label="LUCID"    value={LAST_NIGHT.lucidCount} accent="var(--lucid)" />
        <Stat label="SLEEP"    value={fmtClock(LAST_NIGHT.sleepDurationMin)} small />
      </div>
      <div className="divider" style={{ margin: "14px 0 12px" }} />
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <IconWaves size={16} color="var(--accent)" />
          <span className="mono" style={{ fontSize: 12, color: "var(--text-sec)" }}>
            {LAST_NIGHT.cuesFired}/{LAST_NIGHT.cuesScheduled} cues fired
          </span>
        </div>
        <div style={{ display: "flex", gap: 3 }}>
          {[1, 2, 3, 4, 5].map((s) => (
            <IconStar
              key={s}
              size={12}
              color={s <= LAST_NIGHT.sleepQuality ? "var(--text-sec)" : "var(--text-quat)"}
              style={{ fill: s <= LAST_NIGHT.sleepQuality ? "var(--text-sec)" : "transparent" }}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

function Stat({ label, value, accent, small }) {
  return (
    <div>
      <div className="label-meta">{label}</div>
      <div className="mono tabular" style={{
        marginTop: 4,
        fontSize: small ? 16 : 22,
        fontWeight: 400,
        color: accent || "var(--text-pri)",
        letterSpacing: -0.5,
      }}>
        {value}
      </div>
    </div>
  );
}

function ActiveProfileCard({ sessionActive, onStart, onResume, onManage, onOpenActive }) {
  return (
    <div
      className="card"
      style={{
        position: "relative",
        overflow: "hidden",
        background: sessionActive
          ? "linear-gradient(180deg, rgba(167,139,250,0.10), rgba(19,24,41,0.85))"
          : undefined,
        borderColor: sessionActive ? "rgba(167,139,250,0.3)" : "var(--border)",
      }}
    >
      <div
        onClick={onOpenActive}
        style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 14, cursor: "pointer" }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{
            width: 36, height: 36, borderRadius: 10,
            background: "var(--accent-soft)",
            border: "1px solid var(--border-2)",
            display: "flex", alignItems: "center", justifyContent: "center",
          }}>
            <IconMoon size={18} color="var(--accent)" />
          </div>
          <div>
            <div className="label-meta">ACTIVE PROFILE</div>
            <div style={{ marginTop: 3, fontSize: "var(--fs-title)", fontWeight: 500 }}>{ACTIVE_PROFILE.name}</div>
          </div>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span className="pill">{ACTIVE_PROFILE.cueCount} cues</span>
          <IconChevronRight size={14} color="var(--text-ter)" />
        </div>
      </div>

      {sessionActive ? (
        <SessionActiveBanner onResume={onResume} />
      ) : (
        <>
          <div style={{ display: "flex", alignItems: "center", gap: 10, padding: "10px 12px", border: "1px dashed var(--border-2)", borderRadius: 12, marginBottom: 14 }}>
            <IconClock size={14} color="var(--text-sec)" />
            <span className="mono" style={{ fontSize: 12, color: "var(--text-sec)" }}>
              Next cue · {ACTIVE_PROFILE.nextCue.label}
            </span>
            <span className="mono tabular" style={{ marginLeft: "auto", fontSize: 12, color: "var(--text-ter)" }}>
              T+{Math.floor(ACTIVE_PROFILE.nextCue.inMin / 60)}h {ACTIVE_PROFILE.nextCue.inMin % 60}m
            </span>
          </div>
          <div style={{ display: "flex", gap: 8 }}>
            <button className="btn btn-primary" onClick={onStart} style={{ flex: 1, padding: "14px 18px" }}>
              <IconMoon size={18} color="#0A0E1A" /> Start Sleep
            </button>
            <button className="btn" onClick={onManage} style={{ padding: "14px 14px" }} aria-label="Manage cues">
              <IconWaves size={18} color="var(--text-sec)" />
            </button>
          </div>
        </>
      )}
    </div>
  );
}

function SessionActiveBanner({ onResume }) {
  return (
    <div onClick={onResume} style={{ cursor: "pointer" }}>
      <div style={{
        display: "flex", alignItems: "center", gap: 12,
        padding: "12px 14px",
        background: "rgba(167,139,250,0.08)",
        border: "1px solid rgba(167,139,250,0.25)",
        borderRadius: 12,
      }}>
        <div style={{
          width: 10, height: 10, borderRadius: "50%",
          background: "var(--lavender)",
          boxShadow: "0 0 12px var(--lavender)",
          animation: "breathe 2.4s ease-in-out infinite",
        }} />
        <div style={{ flex: 1 }}>
          <div style={{ fontSize: 13, fontWeight: 500, color: "var(--text-pri)" }}>Session in progress</div>
          <div className="mono tabular" style={{ fontSize: 11, color: "var(--text-sec)", marginTop: 2 }}>
            00:00:14 · monitoring cues
          </div>
        </div>
        <IconChevronRight size={16} color="var(--lavender)" />
      </div>
    </div>
  );
}

function RecentEntries({ onOpen, onSeeAll }) {
  const recent = ENTRIES.slice(0, 3);
  return (
    <div>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10, padding: "0 2px" }}>
        <div className="label-meta">RECENT DREAMS</div>
        <button className="btn btn-ghost" onClick={onSeeAll} style={{ padding: "4px 8px", fontSize: 12, color: "var(--text-sec)" }}>
          All <IconArrowRight size={12} color="currentColor" />
        </button>
      </div>
      <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
        {recent.map((e) => (
          <EntryRow key={e.id} entry={e} compact onClick={() => onOpen(e.id)} />
        ))}
      </div>
    </div>
  );
}

function EntryRow({ entry, compact, onClick }) {
  return (
    <div
      onClick={onClick}
      style={{
        display: "flex",
        alignItems: "stretch",
        gap: 12,
        padding: compact ? "12px 14px" : "14px 16px",
        background: "rgba(26,32,56,0.35)",
        border: "1px solid var(--border)",
        borderRadius: 12,
        cursor: "pointer",
        transition: "background 120ms ease, border-color 120ms ease",
      }}
    >
      <div style={{ display: "flex", flexDirection: "column", alignItems: "center", paddingTop: 2 }}>
        <LucidityEye level={entry.lucidity} size={20} />
        <div className="mono" style={{ fontSize: 10, color: "var(--text-ter)", marginTop: 6, letterSpacing: 0.5 }}>
          {shortDate(entry.dreamDate).toUpperCase()}
        </div>
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{
          fontSize: 14, fontWeight: 500, color: "var(--text-pri)",
          whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis",
          marginBottom: 4,
        }}>
          {entry.title}
        </div>
        <div style={{
          fontSize: 12.5, color: "var(--text-sec)", lineHeight: 1.45,
          display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical",
          overflow: "hidden",
        }}>
          {entry.body}
        </div>
        {!compact && entry.tags.length > 0 && (
          <div style={{ display: "flex", flexWrap: "wrap", gap: 4, marginTop: 8 }}>
            {entry.tags.slice(0, 3).map((t) => (
              <TagChip key={t} name={t} small />
            ))}
            {entry.tags.length > 3 && (
              <span className="mono" style={{ fontSize: 11, color: "var(--text-ter)", alignSelf: "center" }}>
                +{entry.tags.length - 3}
              </span>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function TagChip({ name, small, active, onClick }) {
  const t = tagBy(name);
  return (
    <span
      className="chip"
      onClick={onClick}
      style={{
        padding: small ? "3px 8px" : "5px 10px",
        fontSize: small ? 11 : 12,
        borderColor: active ? t.color : "var(--border-2)",
        background: active ? `${t.color}1A` : "rgba(255,255,255,0.03)",
        color: active ? t.color : "var(--text-sec)",
        cursor: onClick ? "pointer" : "default",
      }}
    >
      <span className="dot" style={{ background: t.color }} />
      {name}
    </span>
  );
}

function RealityCheckHint() {
  return (
    <div style={{
      display: "flex", alignItems: "center", gap: 12, padding: "12px 14px",
      border: "1px dashed var(--border-2)",
      borderRadius: 12,
      background: "transparent",
    }}>
      <IconSparkles size={16} color="var(--lavender)" />
      <div style={{ flex: 1, fontSize: 12.5, color: "var(--text-sec)", lineHeight: 1.4 }}>
        2 reality checks pending today
      </div>
      <span className="pill" style={{ borderColor: "rgba(167,139,250,0.35)", color: "var(--lavender)" }}>3/5</span>
    </div>
  );
}

Object.assign(window, { HomeScreen, EntryRow, TagChip });

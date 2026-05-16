// placeholders.jsx — stub screens for Stats and Settings so nav doesn't dead-end.

function StatsScreen() {
  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", padding: "18px var(--pad-x) 24px" }}>
      <div className="label-meta">STATS</div>
      <div style={{ fontSize: 28, fontWeight: 400, letterSpacing: -0.6, marginTop: 4 }}>Progress</div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginTop: 18 }}>
        <BigStat label="TOTAL DREAMS" value="186" />
        <BigStat label="TOTAL LUCID"  value="34" accent="var(--lucid)" />
        <BigStat label="LUCIDITY RATE" value="18.3%" />
        <BigStat label="BEST TECHNIQUE" value="WBTB" small />
      </div>

      <div style={{ marginTop: 18 }}>
        <div className="label-meta" style={{ marginBottom: 10 }}>90-DAY HEATMAP</div>
        <Heatmap />
      </div>

      <div style={{ marginTop: 22 }}>
        <div className="label-meta" style={{ marginBottom: 10 }}>TOP TAGS</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {[
            { name: "recurring",       count: 28, pct: 0.78 },
            { name: "lucid",           count: 22, pct: 0.61 },
            { name: "flying",          count: 14, pct: 0.39 },
            { name: "false awakening", count: 11, pct: 0.31 },
            { name: "dream sign",      count: 9,  pct: 0.25 },
          ].map((t) => (
            <TagBar key={t.name} {...t} />
          ))}
        </div>
      </div>
    </div>
  );
}

function BigStat({ label, value, accent, small }) {
  return (
    <div className="card" style={{ padding: 16 }}>
      <div className="label-meta">{label}</div>
      <div className="mono tabular" style={{
        marginTop: 8,
        fontSize: small ? 22 : 30,
        letterSpacing: -0.5,
        color: accent || "var(--text-pri)",
        fontWeight: 300,
      }}>{value}</div>
    </div>
  );
}

function Heatmap() {
  const cols = 13, rows = 7;
  // Generate deterministic pseudo-random data
  const cells = [];
  let s = 23;
  function rnd() { s = (s * 9301 + 49297) % 233280; return s / 233280; }
  for (let i = 0; i < cols * rows; i++) {
    const r = rnd();
    cells.push(r < 0.3 ? 0 : r < 0.55 ? 1 : r < 0.78 ? 2 : r < 0.92 ? 3 : 4);
  }
  const colorFor = (lv) => {
    if (lv === 0) return "rgba(255,255,255,0.04)";
    if (lv === 1) return "rgba(107,138,255,0.18)";
    if (lv === 2) return "rgba(107,138,255,0.4)";
    if (lv === 3) return "rgba(94,234,212,0.55)";
    return "rgba(94,234,212,0.85)";
  };
  return (
    <div>
      <div style={{ display: "grid", gridTemplateColumns: `repeat(${cols}, 1fr)`, gap: 4 }}>
        {cells.map((lv, i) => (
          <div key={i} style={{
            aspectRatio: "1 / 1",
            borderRadius: 3,
            background: colorFor(lv),
            border: "1px solid rgba(255,255,255,0.03)",
          }} />
        ))}
      </div>
      <div style={{ marginTop: 10, display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <span className="mono" style={{ fontSize: 10, color: "var(--text-ter)" }}>13 weeks ago</span>
        <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
          <span className="mono" style={{ fontSize: 9, color: "var(--text-ter)", marginRight: 4 }}>LESS</span>
          {[0, 1, 2, 3, 4].map((lv) => (
            <div key={lv} style={{
              width: 10, height: 10, borderRadius: 2, background: colorFor(lv),
              border: "1px solid rgba(255,255,255,0.04)",
            }} />
          ))}
          <span className="mono" style={{ fontSize: 9, color: "var(--text-ter)", marginLeft: 4 }}>LUCID</span>
        </div>
        <span className="mono" style={{ fontSize: 10, color: "var(--text-ter)" }}>today</span>
      </div>
    </div>
  );
}

function TagBar({ name, count, pct }) {
  return (
    <div style={{
      display: "flex", alignItems: "center", gap: 12,
      padding: "10px 12px",
      background: "rgba(26,32,56,0.35)",
      border: "1px solid var(--border)",
      borderRadius: 10,
    }}>
      <TagChip name={name} small />
      <div style={{ flex: 1, height: 4, borderRadius: 2, background: "rgba(255,255,255,0.04)", overflow: "hidden" }}>
        <div style={{ width: `${pct * 100}%`, height: "100%", background: tagBy(name).color, opacity: 0.7 }} />
      </div>
      <span className="mono tabular" style={{ fontSize: 12, color: "var(--text-sec)", minWidth: 22, textAlign: "right" }}>
        {count}
      </span>
    </div>
  );
}

function SettingsScreen() {
  const items = [
    { group: "Journal", rows: [
      { label: "Reminder · morning", value: "06:30" },
      { label: "Reminder · backup",  value: "08:00", subtle: true },
      { label: "Default locale",     value: "en-US · cs-CZ" },
    ]},
    { group: "Reality checks", rows: [
      { label: "Enabled",            toggle: true, value: true },
      { label: "Pings per day",      value: "5" },
      { label: "Active hours",       value: "09:00 – 22:00" },
    ]},
    { group: "Audio cues", rows: [
      { label: "Active profile",     value: "MILD + WBTB" },
      { label: "Output route",       value: "Auto · prefer wired" },
      { label: "Battery exemption",  value: "Granted", success: true },
    ]},
    { group: "Data", rows: [
      { label: "Export…",            chevron: true },
      { label: "Import from JSON",   chevron: true },
      { label: "Clear all data",     danger: true },
    ]},
  ];
  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", padding: "18px var(--pad-x) 24px" }}>
      <div className="label-meta">SETTINGS</div>
      <div style={{ fontSize: 28, fontWeight: 400, letterSpacing: -0.6, marginTop: 4 }}>Configuration</div>
      <div style={{ marginTop: 18, display: "flex", flexDirection: "column", gap: 18 }}>
        {items.map((g) => (
          <div key={g.group}>
            <div className="label-meta" style={{ marginBottom: 8 }}>{g.group.toUpperCase()}</div>
            <div className="card" style={{ padding: 0, overflow: "hidden" }}>
              {g.rows.map((r, i) => (
                <div key={r.label} style={{
                  display: "flex", alignItems: "center", gap: 12,
                  padding: "14px 16px",
                  borderTop: i === 0 ? "none" : "1px solid var(--border)",
                  cursor: "pointer",
                }}>
                  <div style={{ flex: 1, fontSize: 14, color: r.danger ? "var(--warn)" : "var(--text-pri)" }}>
                    {r.label}
                  </div>
                  {r.toggle ? (
                    <Toggle on={r.value} />
                  ) : r.chevron ? (
                    <IconChevronRight size={16} color="var(--text-ter)" />
                  ) : (
                    <span className="mono" style={{
                      fontSize: 12,
                      color: r.success ? "var(--lucid)" : r.subtle ? "var(--text-ter)" : "var(--text-sec)",
                    }}>
                      {r.value}
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
        <div style={{ textAlign: "center", padding: "8px 0 12px" }}>
          <div className="mono" style={{ fontSize: 11, color: "var(--text-ter)", letterSpacing: 0.6 }}>
            LUCID DREAM COMPANION · v0.4.1
          </div>
        </div>
      </div>
    </div>
  );
}

function Toggle({ on }) {
  return (
    <div style={{
      width: 38, height: 22, borderRadius: 11,
      background: on ? "var(--accent)" : "var(--surface-2)",
      border: `1px solid ${on ? "transparent" : "var(--border-2)"}`,
      position: "relative",
      transition: "all 160ms ease",
    }}>
      <div style={{
        position: "absolute",
        top: 2, left: on ? 18 : 2,
        width: 16, height: 16, borderRadius: "50%",
        background: on ? "#0A0E1A" : "var(--text-sec)",
        transition: "left 160ms ease",
      }} />
    </div>
  );
}

Object.assign(window, { StatsScreen, SettingsScreen });

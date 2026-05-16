// journal.jsx — JournalList screen with filters, search, grouping

function JournalScreen({ onOpen, onNew }) {
  const [search, setSearch] = React.useState("");
  const [activeTag, setActiveTag] = React.useState(null);
  const [minLucidity, setMinLucidity] = React.useState(0);
  const [filtersOpen, setFiltersOpen] = React.useState(false);

  const filtered = React.useMemo(() => {
    return ENTRIES.filter((e) => {
      if (e.lucidity < minLucidity) return false;
      if (activeTag && !e.tags.includes(activeTag)) return false;
      if (search) {
        const q = search.toLowerCase();
        if (!e.title.toLowerCase().includes(q) && !e.body.toLowerCase().includes(q))
          return false;
      }
      return true;
    });
  }, [search, activeTag, minLucidity]);

  // Group by month
  const groups = React.useMemo(() => {
    const map = {};
    filtered.forEach((e) => {
      const d = new Date(e.dreamDate + "T00:00:00");
      const key = d.toLocaleDateString("en-US", { month: "long", year: "numeric" });
      map[key] = map[key] || [];
      map[key].push(e);
    });
    return Object.entries(map);
  }, [filtered]);

  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", overflowX: "hidden", display: "flex", flexDirection: "column" }}>
      <JournalHeader onNew={onNew} count={ENTRIES.length} />
      <JournalSearchBar
        value={search}
        onChange={setSearch}
        onToggleFilters={() => setFiltersOpen((v) => !v)}
        filtersOpen={filtersOpen}
        activeFilterCount={(activeTag ? 1 : 0) + (minLucidity > 0 ? 1 : 0)}
      />
      {filtersOpen && (
        <JournalFilters
          activeTag={activeTag}
          setActiveTag={setActiveTag}
          minLucidity={minLucidity}
          setMinLucidity={setMinLucidity}
        />
      )}

      <div style={{ padding: "8px var(--pad-x) 24px", display: "flex", flexDirection: "column", gap: 18 }}>
        {groups.length === 0 ? (
          <EmptyState onNew={onNew} />
        ) : (
          groups.map(([month, items]) => (
            <div key={month}>
              <div style={{
                display: "flex", alignItems: "center", gap: 12, margin: "4px 2px 10px",
              }}>
                <div className="label-meta">{month.toUpperCase()}</div>
                <div style={{ flex: 1, height: 1, background: "var(--border)" }} />
                <div className="mono" style={{ fontSize: 11, color: "var(--text-ter)" }}>
                  {items.length} {items.length === 1 ? "entry" : "entries"}
                </div>
              </div>
              <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                {items.map((e) => (
                  <EntryRow key={e.id} entry={e} onClick={() => onOpen(e.id)} />
                ))}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

function JournalHeader({ onNew, count }) {
  return (
    <div style={{
      padding: "18px var(--pad-x) 12px",
      display: "flex", alignItems: "center", justifyContent: "space-between",
    }}>
      <div>
        <div className="label-meta">JOURNAL</div>
        <div style={{ fontSize: 28, fontWeight: 400, letterSpacing: -0.6, marginTop: 4, display: "flex", alignItems: "baseline", gap: 10 }}>
          Dreams
          <span className="mono tabular" style={{ fontSize: 14, color: "var(--text-ter)", fontWeight: 400 }}>
            {String(count).padStart(3, "0")}
          </span>
        </div>
      </div>
      <button
        onClick={onNew}
        className="btn btn-primary"
        style={{ padding: "10px 14px", borderRadius: 12 }}
      >
        <IconPlus size={16} color="#0A0E1A" /> New
      </button>
    </div>
  );
}

function JournalSearchBar({ value, onChange, onToggleFilters, filtersOpen, activeFilterCount }) {
  return (
    <div style={{ padding: "0 var(--pad-x) 12px", display: "flex", gap: 10 }}>
      <div style={{
        flex: 1,
        display: "flex", alignItems: "center", gap: 10,
        background: "rgba(19,24,41,0.7)",
        border: "1px solid var(--border)",
        borderRadius: 12,
        padding: "10px 14px",
      }}>
        <IconSearch size={16} color="var(--text-ter)" />
        <input
          className="input-bare"
          placeholder="Search dreams…"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          style={{ fontSize: 14 }}
        />
      </div>
      <button
        onClick={onToggleFilters}
        className="btn"
        style={{
          padding: "0 14px",
          borderRadius: 12,
          minWidth: 48,
          position: "relative",
          background: filtersOpen ? "var(--accent-soft)" : "rgba(19,24,41,0.7)",
          borderColor: filtersOpen ? "var(--accent)" : "var(--border)",
          color: filtersOpen ? "var(--accent)" : "var(--text-sec)",
        }}
        aria-label="Filters"
      >
        <IconFilter size={16} color="currentColor" />
        {activeFilterCount > 0 && (
          <span style={{
            position: "absolute",
            top: -4, right: -4,
            minWidth: 16, height: 16,
            padding: "0 4px",
            borderRadius: 8,
            background: "var(--accent)",
            color: "#0A0E1A",
            fontSize: 10, fontFamily: "var(--font-mono)",
            display: "flex", alignItems: "center", justifyContent: "center",
            fontWeight: 600,
          }}>{activeFilterCount}</span>
        )}
      </button>
    </div>
  );
}

function JournalFilters({ activeTag, setActiveTag, minLucidity, setMinLucidity }) {
  const commonTags = ["lucid", "recurring", "false awakening", "WBTB", "MILD", "flying", "water", "dream sign"];
  return (
    <div style={{
      padding: "8px var(--pad-x) 16px",
      borderBottom: "1px solid var(--border)",
      background: "rgba(10,14,26,0.5)",
      display: "flex", flexDirection: "column", gap: 14,
    }}>
      <div>
        <div className="label-meta" style={{ marginBottom: 8 }}>MIN LUCIDITY</div>
        <div style={{ display: "flex", gap: 6 }}>
          {[0, 1, 2, 3, 4].map((lv) => {
            const active = minLucidity === lv;
            return (
              <button
                key={lv}
                onClick={() => setMinLucidity(lv)}
                style={{
                  flex: 1,
                  background: active ? "var(--accent-soft)" : "rgba(255,255,255,0.02)",
                  border: `1px solid ${active ? "var(--accent)" : "var(--border)"}`,
                  borderRadius: 10,
                  padding: "8px 4px",
                  display: "flex", flexDirection: "column", alignItems: "center", gap: 4,
                  cursor: "pointer",
                  color: active ? "var(--accent)" : "var(--text-sec)",
                  transition: "all 120ms ease",
                }}
              >
                <LucidityEye level={lv} size={18} color={active ? "var(--accent)" : "var(--text-ter)"} />
                <span className="mono" style={{ fontSize: 9, letterSpacing: 0.5 }}>{lv === 0 ? "ANY" : "≥" + lv}</span>
              </button>
            );
          })}
        </div>
      </div>
      <div>
        <div className="label-meta" style={{ marginBottom: 8 }}>TAG</div>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
          {commonTags.map((t) => (
            <TagChip
              key={t}
              name={t}
              active={activeTag === t}
              onClick={() => setActiveTag(activeTag === t ? null : t)}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

function EmptyState({ onNew }) {
  return (
    <div style={{ padding: "60px 12px", textAlign: "center" }}>
      <div style={{ display: "inline-flex", padding: 18, borderRadius: "50%", background: "var(--accent-soft)", marginBottom: 16 }}>
        <IconMoon size={28} color="var(--accent)" />
      </div>
      <div style={{ fontSize: 17, fontWeight: 500, marginBottom: 8 }}>No dreams match those filters</div>
      <div style={{ fontSize: 13, color: "var(--text-sec)", lineHeight: 1.5, marginBottom: 18, maxWidth: 280, margin: "0 auto 18px" }}>
        Keep a pen and paper by your bed — or log here within 30 seconds of waking.
      </div>
      <button onClick={onNew} className="btn btn-primary">
        <IconPlus size={16} color="#0A0E1A" /> New entry
      </button>
    </div>
  );
}

Object.assign(window, { JournalScreen });

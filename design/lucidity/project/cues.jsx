// cues.jsx — Cue Profiles list, Profile detail (cue list), single Cue Editor

// ─────────────────────────────────────────────────────────────
// 1) Cue Profiles list
// ─────────────────────────────────────────────────────────────
function CueProfilesScreen({ onOpenProfile, onNewProfile, onBack, onStartSleep }) {
  const userProfiles = PROFILES.filter((p) => !p.isTemplate);
  const templates = PROFILES.filter((p) => p.isTemplate);
  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", overflowX: "hidden" }}>
      <CuesHeader onBack={onBack} onNewProfile={onNewProfile} />
      <div style={{ padding: "8px var(--pad-x) 28px", display: "flex", flexDirection: "column", gap: 22 }}>
        <Section label="ACTIVE TONIGHT">
          <ActiveProfileBanner profile={PROFILES.find((p) => p.active)} onTap={onOpenProfile} onStartSleep={onStartSleep} />
        </Section>

        <Section label="MY PROFILES" trailing={
          <button className="btn btn-ghost" onClick={onNewProfile} style={{ padding: "4px 6px", fontSize: 11, color: "var(--accent)", fontFamily: "var(--font-mono)" }}>
            + NEW
          </button>
        }>
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {userProfiles.map((p) => (
              <ProfileRow key={p.id} profile={p} onClick={() => onOpenProfile(p.id)} />
            ))}
          </div>
        </Section>

        <Section label="TECHNIQUE TEMPLATES">
          <div style={{ fontSize: 12, color: "var(--text-ter)", lineHeight: 1.5, marginBottom: 10 }}>
            Locked recipes from established techniques. Duplicate to edit.
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {templates.map((p) => (
              <ProfileRow key={p.id} profile={p} onClick={() => onOpenProfile(p.id)} />
            ))}
          </div>
        </Section>
      </div>
    </div>
  );
}

function CuesHeader({ onBack, onNewProfile }) {
  return (
    <div style={{
      padding: "14px var(--pad-x) 12px",
      display: "flex", alignItems: "center", justifyContent: "space-between",
    }}>
      <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
        <button onClick={onBack} className="btn btn-ghost" style={{ padding: 8, marginLeft: -8 }} aria-label="Back">
          <IconChevronLeft size={22} color="var(--text-sec)" />
        </button>
        <div>
          <div className="label-meta">CUES</div>
          <div style={{ fontSize: 22, fontWeight: 500, letterSpacing: -0.3, marginTop: 2 }}>Profiles</div>
        </div>
      </div>
      <button onClick={onNewProfile} className="btn btn-primary" style={{ padding: "10px 14px", borderRadius: 12 }}>
        <IconPlus size={16} color="#0A0E1A" /> New
      </button>
    </div>
  );
}

function Section({ label, trailing, children }) {
  return (
    <div>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10, padding: "0 2px" }}>
        <div className="label-meta">{label}</div>
        {trailing}
      </div>
      {children}
    </div>
  );
}

function ActiveProfileBanner({ profile, onTap, onStartSleep }) {
  if (!profile) return null;
  return (
    <div
      className="card"
      style={{
        background: "linear-gradient(180deg, rgba(107,138,255,0.12), rgba(19,24,41,0.85))",
        borderColor: "rgba(107,138,255,0.3)",
        position: "relative",
        overflow: "hidden",
      }}
    >
      <div style={{
        position: "absolute", right: -30, top: -30, width: 150, height: 150,
        background: "radial-gradient(circle, var(--accent-soft) 0%, transparent 70%)",
        pointerEvents: "none",
      }} />
      <div
        onClick={() => onTap(profile.id)}
        style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 12, cursor: "pointer" }}
      >
        <div style={{
          width: 44, height: 44, borderRadius: 12,
          background: "var(--accent-soft)",
          border: "1px solid rgba(107,138,255,0.3)",
          display: "flex", alignItems: "center", justifyContent: "center",
          animation: "breathe 4s ease-in-out infinite",
        }}>
          <IconMoon size={22} color="var(--accent)" />
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="label-meta">ACTIVE TONIGHT</div>
          <div style={{ marginTop: 3, fontSize: 17, fontWeight: 500 }}>{profile.name}</div>
        </div>
        <IconChevronRight size={18} color="var(--text-ter)" />
      </div>
      <div style={{ display: "flex", gap: 14, padding: "12px 0", borderTop: "1px solid var(--border)", borderBottom: "1px solid var(--border)" }}>
        <Metric label="CUES" value={profile.cues.filter((c) => c.enabled).length} />
        <Metric label="FIRST" value={firstCueLabel(profile)} small />
        <Metric label="WINDOW" value={cueWindowLabel(profile)} small />
      </div>
      {onStartSleep && (
        <button className="btn btn-primary" onClick={onStartSleep} style={{ width: "100%", padding: "12px 18px", marginTop: 12 }}>
          <IconMoon size={16} color="#0A0E1A" /> Start sleep with this profile
        </button>
      )}
    </div>
  );
}

function Metric({ label, value, small }) {
  return (
    <div style={{ flex: 1 }}>
      <div className="label-meta">{label}</div>
      <div className="mono tabular" style={{ marginTop: 4, fontSize: small ? 13 : 18, color: "var(--text-pri)", letterSpacing: -0.3 }}>
        {value}
      </div>
    </div>
  );
}

function firstCueLabel(profile) {
  const sleeps = profile.cues.filter((c) => c.trigger === "sleep" && c.enabled).sort((a, b) => a.offsetMin - b.offsetMin);
  if (sleeps[0]) return `T+${fmtOffset(sleeps[0].offsetMin)}`;
  const clocks = profile.cues.filter((c) => c.trigger === "clock" && c.enabled);
  if (clocks[0]) return clocks[0].clockTime;
  return "—";
}
function cueWindowLabel(profile) {
  const sleeps = profile.cues.filter((c) => c.enabled && c.trigger === "sleep").map((c) => c.offsetMin);
  if (sleeps.length === 0) return "—";
  const min = Math.min(...sleeps), max = Math.max(...sleeps);
  return `${fmtOffset(min)} – ${fmtOffset(max)}`;
}

function ProfileRow({ profile, onClick }) {
  const enabledCount = profile.cues.filter((c) => c.enabled).length;
  return (
    <div
      onClick={onClick}
      style={{
        display: "flex",
        alignItems: "center",
        gap: 12,
        padding: "14px 16px",
        background: profile.active ? "rgba(107,138,255,0.06)" : "rgba(26,32,56,0.35)",
        border: `1px solid ${profile.active ? "rgba(107,138,255,0.3)" : "var(--border)"}`,
        borderRadius: 12,
        cursor: "pointer",
      }}
    >
      <div style={{
        width: 36, height: 36, borderRadius: 10,
        background: profile.active ? "var(--accent-soft)" : "rgba(255,255,255,0.04)",
        border: "1px solid var(--border-2)",
        display: "flex", alignItems: "center", justifyContent: "center",
        color: profile.active ? "var(--accent)" : "var(--text-sec)",
      }}>
        {profile.isTemplate ? <IconSparkles size={16} color="currentColor" /> : <IconWaves size={16} color="currentColor" />}
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ fontSize: 15, fontWeight: 500, color: "var(--text-pri)" }}>{profile.name}</span>
          {profile.isTemplate && <span className="pill" style={{ padding: "2px 6px", fontSize: 9 }}>TEMPLATE</span>}
          {profile.active && <span className="pill" style={{ padding: "2px 6px", fontSize: 9, color: "var(--accent)", borderColor: "rgba(107,138,255,0.4)" }}>ACTIVE</span>}
        </div>
        <div style={{ fontSize: 12, color: "var(--text-sec)", marginTop: 3, lineHeight: 1.4, display: "-webkit-box", WebkitLineClamp: 1, WebkitBoxOrient: "vertical", overflow: "hidden" }}>
          {profile.description}
        </div>
      </div>
      <div style={{ textAlign: "right" }}>
        <div className="mono tabular" style={{ fontSize: 14, color: "var(--text-pri)" }}>{enabledCount}</div>
        <div className="mono" style={{ fontSize: 9, color: "var(--text-ter)", letterSpacing: 0.5, marginTop: 2 }}>CUES</div>
      </div>
      <IconChevronRight size={14} color="var(--text-ter)" />
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// 2) Profile detail (list of cues within a profile)
// ─────────────────────────────────────────────────────────────
function ProfileDetailScreen({ profileId, onBack, onEditCue, onAddCue }) {
  const profile = PROFILES.find((p) => p.id === profileId) || PROFILES[0];
  const [activated, setActivated] = React.useState(profile.active);
  const [cueEnabled, setCueEnabled] = React.useState(
    Object.fromEntries(profile.cues.map((c) => [c.id, c.enabled]))
  );
  const sorted = [...profile.cues].sort((a, b) => {
    const order = { sleep: 0, clock: 1, interval: 2 };
    if (a.trigger !== b.trigger) return order[a.trigger] - order[b.trigger];
    if (a.trigger === "sleep") return a.offsetMin - b.offsetMin;
    if (a.trigger === "clock") return a.clockTime.localeCompare(b.clockTime);
    return 0;
  });

  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", overflowX: "hidden" }}>
      <ProfileDetailHeader profile={profile} onBack={onBack} />

      <div style={{ padding: "8px var(--pad-x) 28px", display: "flex", flexDirection: "column", gap: 18 }}>
        {/* Description */}
        <div style={{ fontSize: 13.5, color: "var(--text-sec)", lineHeight: 1.55 }}>
          {profile.description}
        </div>

        {/* Activate toggle */}
        <div className="card" style={{ display: "flex", alignItems: "center", gap: 12, padding: "14px 16px" }}>
          <div style={{
            width: 36, height: 36, borderRadius: 10,
            background: activated ? "rgba(167,139,250,0.16)" : "rgba(255,255,255,0.04)",
            border: "1px solid var(--border-2)",
            display: "flex", alignItems: "center", justifyContent: "center",
          }}>
            <IconMoon size={16} color={activated ? "var(--lavender)" : "var(--text-sec)"} />
          </div>
          <div style={{ flex: 1 }}>
            <div style={{ fontSize: 14, fontWeight: 500 }}>Activate tonight</div>
            <div style={{ fontSize: 11.5, color: "var(--text-sec)", marginTop: 2 }}>
              {activated ? "Will schedule cues when sleep starts" : "Not scheduled"}
            </div>
          </div>
          <Toggle on={activated} onChange={() => setActivated((v) => !v)} />
        </div>

        {/* Cue timeline preview */}
        {sorted.some((c) => c.trigger === "sleep") && (
          <CueOffsetMap cues={sorted.filter((c) => c.trigger === "sleep")} />
        )}

        {/* Cues list */}
        <div>
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10, padding: "0 2px" }}>
            <div className="label-meta">CUES · {profile.cues.length}</div>
            {!profile.isTemplate && (
              <button className="btn btn-ghost" onClick={onAddCue} style={{ padding: "4px 6px", fontSize: 11, color: "var(--accent)", fontFamily: "var(--font-mono)" }}>
                + ADD CUE
              </button>
            )}
          </div>
          {sorted.length === 0 ? (
            <div style={{
              padding: "28px 18px", textAlign: "center",
              border: "1px dashed var(--border-2)", borderRadius: 12,
              fontSize: 13, color: "var(--text-sec)", lineHeight: 1.5,
            }}>
              No audio cues — this technique relies on intention and journaling.
            </div>
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
              {sorted.map((c) => (
                <CueRow
                  key={c.id}
                  cue={c}
                  enabled={cueEnabled[c.id]}
                  onToggle={() => setCueEnabled((s) => ({ ...s, [c.id]: !s[c.id] }))}
                  onTap={() => !profile.isTemplate && onEditCue(c.id)}
                  locked={profile.isTemplate}
                />
              ))}
            </div>
          )}
        </div>

        {/* Bottom actions for templates */}
        {profile.isTemplate && (
          <button className="btn" style={{ padding: "14px 18px", width: "100%" }}>
            Duplicate as editable profile
          </button>
        )}

        {!profile.isTemplate && (
          <div style={{ display: "flex", flexDirection: "column", gap: 8, marginTop: 6 }}>
            <button onClick={onAddCue} className="btn btn-primary" style={{ width: "100%", padding: "14px 18px" }}>
              <IconPlus size={16} color="#0A0E1A" /> Add cue
            </button>
            <button className="btn btn-ghost" style={{ color: "var(--warn)", fontSize: 12 }}>
              Delete profile
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function ProfileDetailHeader({ profile, onBack }) {
  return (
    <div style={{
      padding: "14px var(--pad-x) 8px",
      display: "flex", alignItems: "center", gap: 10,
    }}>
      <button onClick={onBack} className="btn btn-ghost" style={{ padding: 8, marginLeft: -8 }} aria-label="Back">
        <IconChevronLeft size={22} color="var(--text-sec)" />
      </button>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div className="label-meta">{profile.isTemplate ? `TEMPLATE · ${profile.techniqueKey}` : "MY PROFILE"}</div>
        <div style={{ fontSize: 22, fontWeight: 500, letterSpacing: -0.3, marginTop: 2, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
          {profile.name}
        </div>
      </div>
      <button className="btn btn-ghost" style={{ padding: 8 }} aria-label="More">
        <IconDots size={18} color="var(--text-sec)" />
      </button>
    </div>
  );
}

function CueOffsetMap({ cues }) {
  // Visualize sleep-relative cues on a 0–8h sleep window.
  const totalMin = 480; // 8h
  return (
    <div className="card" style={{ padding: 14 }}>
      <div className="label-meta" style={{ marginBottom: 12 }}>SLEEP-RELATIVE TIMELINE · 0 – 8h</div>
      <div style={{ position: "relative", height: 32 }}>
        <div style={{ position: "absolute", top: 15, left: 0, right: 0, height: 2, background: "var(--border)", borderRadius: 1 }} />
        {/* Hour gridlines */}
        {[0, 1, 2, 3, 4, 5, 6, 7, 8].map((h) => (
          <div key={h} style={{
            position: "absolute", top: 12, left: `${(h * 60 / totalMin) * 100}%`,
            width: 1, height: 8, background: "var(--border-2)",
          }} />
        ))}
        {/* Cue markers */}
        {cues.map((c) => (
          <div
            key={c.id}
            style={{
              position: "absolute",
              top: 10, left: `${(c.offsetMin / totalMin) * 100}%`,
              transform: "translateX(-50%)",
              width: 12, height: 12, borderRadius: "50%",
              background: c.enabled ? "var(--accent)" : "var(--surface-2)",
              border: `1.5px solid ${c.enabled ? "var(--accent)" : "var(--border-2)"}`,
              boxShadow: c.enabled ? "0 0 8px var(--accent-glow)" : "none",
            }}
            title={c.label}
          />
        ))}
      </div>
      <div style={{ display: "flex", justifyContent: "space-between", marginTop: 6 }}>
        {[0, 2, 4, 6, 8].map((h) => (
          <span key={h} className="mono" style={{ fontSize: 9, color: "var(--text-ter)", letterSpacing: 0.5 }}>{h}h</span>
        ))}
      </div>
    </div>
  );
}

function CueRow({ cue, enabled, onToggle, onTap, locked }) {
  const audio = audioById(cue.audioId);
  const vib = VIBRATION_PRESETS.find((v) => v.id === cue.vibration);
  const familyIcon = audio?.family === "voice" ? IconMic : audio?.family === "noise" ? IconWaves : IconBell;
  const FamilyIcon = familyIcon;
  return (
    <div style={{
      display: "flex", alignItems: "stretch", gap: 12,
      padding: "12px 14px",
      background: "rgba(26,32,56,0.35)",
      border: "1px solid var(--border)",
      borderRadius: 12,
      opacity: enabled ? 1 : 0.55,
      cursor: locked ? "default" : "pointer",
    }} onClick={(e) => {
      if (e.target.closest("[data-no-row-tap]")) return;
      onTap();
    }}>
      <div style={{
        width: 36, height: 36, borderRadius: 10,
        background: enabled ? "var(--accent-soft)" : "rgba(255,255,255,0.04)",
        border: "1px solid var(--border-2)",
        display: "flex", alignItems: "center", justifyContent: "center",
        flexShrink: 0,
      }}>
        <FamilyIcon size={16} color={enabled ? "var(--accent)" : "var(--text-ter)"} />
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ fontSize: 14, fontWeight: 500, color: "var(--text-pri)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
            {cue.label}
          </span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 8, marginTop: 4, flexWrap: "wrap" }}>
          <span className="mono tabular" style={{ fontSize: 11, color: "var(--accent)", letterSpacing: 0.3 }}>
            {describeCueTrigger(cue)}
          </span>
          <span className="mono" style={{ fontSize: 11, color: "var(--text-ter)" }}>·</span>
          <span className="mono" style={{ fontSize: 11, color: "var(--text-sec)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", maxWidth: 130 }}>
            {audio?.name}
          </span>
          <span className="mono" style={{ fontSize: 11, color: "var(--text-ter)" }}>·</span>
          <span className="mono tabular" style={{ fontSize: 11, color: "var(--text-sec)" }}>
            {cue.volume}%
          </span>
          {vib && vib.id !== "none" && (
            <>
              <span className="mono" style={{ fontSize: 11, color: "var(--text-ter)" }}>·</span>
              <span className="mono" style={{ fontSize: 11, color: "var(--lavender)" }}>vib·{vib.label.toLowerCase()}</span>
            </>
          )}
        </div>
      </div>
      <div data-no-row-tap style={{ display: "flex", alignItems: "center" }}>
        <Toggle on={enabled} onChange={onToggle} disabled={locked} />
      </div>
    </div>
  );
}

// Wrap Toggle to allow onChange + disabled (Toggle in placeholders is decorative; reimplement here interactive)
function Toggle({ on, onChange, disabled }) {
  return (
    <div
      onClick={() => !disabled && onChange && onChange()}
      style={{
        width: 38, height: 22, borderRadius: 11,
        background: on ? "var(--accent)" : "var(--surface-2)",
        border: `1px solid ${on ? "transparent" : "var(--border-2)"}`,
        position: "relative",
        transition: "all 160ms ease",
        cursor: disabled ? "not-allowed" : "pointer",
        opacity: disabled ? 0.5 : 1,
      }}
    >
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

// ─────────────────────────────────────────────────────────────
// 3) Single Cue Editor
// ─────────────────────────────────────────────────────────────
function CueEditorScreen({ profileId, cueId, isNew, onBack }) {
  const profile = PROFILES.find((p) => p.id === profileId);
  const existing = cueId ? profile?.cues.find((c) => c.id === cueId) : null;

  const [label, setLabel] = React.useState(existing?.label || "");
  const [trigger, setTrigger] = React.useState(existing?.trigger || "sleep");
  const [offsetMin, setOffsetMin] = React.useState(existing?.offsetMin ?? 300);
  const [clockTime, setClockTime] = React.useState(existing?.clockTime || "05:00");
  const [intervalStart, setIntervalStart] = React.useState(existing?.intervalStart || "04:00");
  const [intervalEnd, setIntervalEnd] = React.useState(existing?.intervalEnd || "07:00");
  const [everyMin, setEveryMin] = React.useState(existing?.everyMin ?? 30);
  const [audioId, setAudioId] = React.useState(existing?.audioId ?? 1);
  const [volume, setVolume] = React.useState(existing?.volume ?? 30);
  const [fadeSec, setFadeSec] = React.useState(existing?.fadeSec ?? 5);
  const [loops, setLoops] = React.useState(existing?.loops ?? 1);
  const [vibration, setVibration] = React.useState(existing?.vibration || "none");
  const [enabled, setEnabled] = React.useState(existing?.enabled ?? true);
  const [audioPickerOpen, setAudioPickerOpen] = React.useState(false);
  const [previewPlaying, setPreviewPlaying] = React.useState(false);

  const audio = audioById(audioId);

  return (
    <div className="screen-scroll screen-enter" style={{ flex: 1, overflowY: "auto", overflowX: "hidden" }}>
      <CueEditorHeader onBack={onBack} isNew={isNew} />

      <div style={{ padding: "8px var(--pad-x) 28px", display: "flex", flexDirection: "column", gap: 22 }}>
        {/* Label */}
        <FieldGroup label="LABEL">
          <input
            className="input-bare"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            placeholder="e.g. Are you dreaming?"
            style={{
              fontSize: 17, fontWeight: 500,
              padding: "12px 14px",
              background: "rgba(10,14,26,0.5)",
              border: "1px solid var(--border)",
              borderRadius: 12,
            }}
          />
        </FieldGroup>

        {/* Enabled */}
        <RowItem
          title="Enabled"
          subtitle={enabled ? "Will fire on activation" : "Will not fire"}
          trailing={<Toggle on={enabled} onChange={() => setEnabled((v) => !v)} />}
        />

        {/* Trigger mode */}
        <FieldGroup label="TRIGGER">
          <SegControl
            value={trigger}
            onChange={setTrigger}
            options={[
              { value: "sleep",    label: "Sleep-relative" },
              { value: "clock",    label: "Clock" },
              { value: "interval", label: "Interval" },
            ]}
          />
          <div style={{ marginTop: 12 }}>
            {trigger === "sleep" && (
              <SleepRelativeFields offsetMin={offsetMin} setOffsetMin={setOffsetMin} />
            )}
            {trigger === "clock" && (
              <ClockField time={clockTime} onChange={setClockTime} />
            )}
            {trigger === "interval" && (
              <IntervalFields
                start={intervalStart} setStart={setIntervalStart}
                end={intervalEnd}     setEnd={setIntervalEnd}
                every={everyMin}      setEvery={setEveryMin}
              />
            )}
          </div>
        </FieldGroup>

        {/* Audio */}
        <FieldGroup label="AUDIO">
          <AudioPickerRow
            audio={audio}
            previewPlaying={previewPlaying}
            onTogglePreview={() => setPreviewPlaying((v) => !v)}
            onChange={() => setAudioPickerOpen(true)}
          />
        </FieldGroup>

        {/* Volume */}
        <FieldGroup label="VOLUME" suffix={<span className="mono tabular" style={{ fontSize: 12, color: "var(--text-pri)" }}>{volume}%</span>}>
          <Slider value={volume} onChange={setVolume} min={0} max={100} />
          <div style={{ display: "flex", justifyContent: "space-between", marginTop: 4 }}>
            <Sub>silent</Sub>
            <Sub>loud</Sub>
          </div>
        </FieldGroup>

        {/* Fade-in */}
        <FieldGroup label="FADE-IN" suffix={<span className="mono tabular" style={{ fontSize: 12, color: "var(--text-pri)" }}>{fadeSec}s</span>}>
          <Slider value={fadeSec} onChange={setFadeSec} min={0} max={30} />
        </FieldGroup>

        {/* Loop count */}
        <FieldGroup label="LOOPS">
          <Stepper value={loops} onChange={setLoops} min={1} max={20} />
        </FieldGroup>

        {/* Vibration */}
        <FieldGroup label="VIBRATION">
          <div style={{ display: "flex", gap: 6 }}>
            {VIBRATION_PRESETS.map((v) => (
              <button
                key={v.id}
                onClick={() => setVibration(v.id)}
                style={{
                  flex: 1,
                  background: vibration === v.id ? "var(--accent-soft)" : "rgba(255,255,255,0.02)",
                  border: `1px solid ${vibration === v.id ? "var(--accent)" : "var(--border)"}`,
                  color: vibration === v.id ? "var(--accent)" : "var(--text-sec)",
                  borderRadius: 10,
                  padding: "10px 4px",
                  cursor: "pointer",
                  display: "flex", flexDirection: "column", alignItems: "center", gap: 6,
                }}
              >
                <VibPatternViz pattern={v.pattern} active={vibration === v.id} />
                <span className="mono" style={{ fontSize: 10, letterSpacing: 0.5 }}>{v.label.toUpperCase()}</span>
              </button>
            ))}
          </div>
        </FieldGroup>

        {/* Delete */}
        {!isNew && (
          <button className="btn btn-ghost" style={{ color: "var(--warn)", fontSize: 13, marginTop: 8 }}>
            Delete cue
          </button>
        )}
      </div>

      {audioPickerOpen && (
        <AudioPickerSheet
          currentId={audioId}
          onPick={(id) => { setAudioId(id); setAudioPickerOpen(false); }}
          onClose={() => setAudioPickerOpen(false)}
        />
      )}
    </div>
  );
}

function CueEditorHeader({ onBack, isNew }) {
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
      <div style={{ fontSize: 13, fontWeight: 500, color: "var(--text-pri)" }}>
        {isNew ? "New cue" : "Edit cue"}
      </div>
      <button onClick={onBack} className="btn btn-ghost" style={{ padding: "6px 10px", color: "var(--accent)", fontSize: 13, fontWeight: 500 }}>
        Done
      </button>
    </div>
  );
}

function FieldGroup({ label, suffix, children }) {
  return (
    <div>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10 }}>
        <div className="label-meta">{label}</div>
        {suffix}
      </div>
      {children}
    </div>
  );
}

function RowItem({ title, subtitle, trailing }) {
  return (
    <div className="card" style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 14px" }}>
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: 14, fontWeight: 500 }}>{title}</div>
        {subtitle && <div style={{ fontSize: 11.5, color: "var(--text-sec)", marginTop: 2 }}>{subtitle}</div>}
      </div>
      {trailing}
    </div>
  );
}

function Sub({ children }) {
  return <span className="mono" style={{ fontSize: 10, color: "var(--text-ter)", letterSpacing: 0.5, textTransform: "uppercase" }}>{children}</span>;
}

function SegControl({ value, onChange, options }) {
  return (
    <div style={{
      display: "grid",
      gridTemplateColumns: `repeat(${options.length}, 1fr)`,
      gap: 4,
      padding: 4,
      background: "rgba(10,14,26,0.7)",
      border: "1px solid var(--border)",
      borderRadius: 12,
    }}>
      {options.map((o) => {
        const active = value === o.value;
        return (
          <button
            key={o.value}
            onClick={() => onChange(o.value)}
            style={{
              background: active ? "var(--accent-soft)" : "transparent",
              border: `1px solid ${active ? "rgba(107,138,255,0.35)" : "transparent"}`,
              color: active ? "var(--accent)" : "var(--text-sec)",
              borderRadius: 8,
              padding: "8px 6px",
              fontSize: 12,
              fontWeight: active ? 500 : 400,
              fontFamily: "var(--font-sans)",
              cursor: "pointer",
              transition: "all 140ms ease",
            }}
          >
            {o.label}
          </button>
        );
      })}
    </div>
  );
}

function SleepRelativeFields({ offsetMin, setOffsetMin }) {
  const h = Math.floor(offsetMin / 60);
  const m = offsetMin % 60;
  return (
    <div className="card" style={{ padding: 14 }}>
      <div style={{ display: "flex", alignItems: "baseline", justifyContent: "center", gap: 16, marginBottom: 12 }}>
        <NumberWheel value={h} onChange={(v) => setOffsetMin(v * 60 + m)} max={9} suffix="h" />
        <NumberWheel value={m} onChange={(v) => setOffsetMin(h * 60 + v)} max={59} suffix="m" step={5} />
      </div>
      <div style={{ fontSize: 12, color: "var(--text-sec)", textAlign: "center", lineHeight: 1.5 }}>
        Fires <span className="mono" style={{ color: "var(--accent)" }}>{fmtOffset(offsetMin)}</span> after Sleep starts.
        Typical REM windows: <span className="mono">4h30 – 7h00</span>.
      </div>
    </div>
  );
}

function NumberWheel({ value, onChange, max, suffix, step = 1 }) {
  function inc() { onChange(Math.min(max, value + step)); }
  function dec() { onChange(Math.max(0, value - step)); }
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
      <button onClick={inc} style={{
        background: "transparent", border: "none", color: "var(--text-ter)",
        cursor: "pointer", padding: 4,
      }}>
        <svg width="16" height="10" viewBox="0 0 16 10"><path d="M2 8L8 2L14 8" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/></svg>
      </button>
      <div className="mono tabular" style={{
        fontSize: 38, fontWeight: 300, letterSpacing: -1, color: "var(--text-pri)",
        lineHeight: 1, minWidth: 60, textAlign: "center",
      }}>
        {String(value).padStart(2, "0")}
        <span style={{ fontSize: 16, color: "var(--text-ter)", marginLeft: 2 }}>{suffix}</span>
      </div>
      <button onClick={dec} style={{
        background: "transparent", border: "none", color: "var(--text-ter)",
        cursor: "pointer", padding: 4,
      }}>
        <svg width="16" height="10" viewBox="0 0 16 10"><path d="M2 2L8 8L14 2" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round"/></svg>
      </button>
    </div>
  );
}

function ClockField({ time, onChange }) {
  return (
    <div className="card" style={{ padding: 14, display: "flex", alignItems: "center", gap: 12 }}>
      <IconClock size={20} color="var(--accent)" />
      <input
        type="time"
        value={time}
        onChange={(e) => onChange(e.target.value)}
        style={{
          flex: 1,
          background: "transparent",
          border: "none",
          color: "var(--text-pri)",
          fontFamily: "var(--font-mono)",
          fontSize: 24,
          fontWeight: 300,
          letterSpacing: -0.5,
          outline: "none",
          colorScheme: "dark",
        }}
      />
      <span className="mono" style={{ fontSize: 11, color: "var(--text-ter)", letterSpacing: 0.5 }}>LOCAL</span>
    </div>
  );
}

function IntervalFields({ start, setStart, end, setEnd, every, setEvery }) {
  return (
    <div className="card" style={{ padding: 14, display: "flex", flexDirection: "column", gap: 12 }}>
      <div style={{ display: "flex", gap: 10 }}>
        <div style={{ flex: 1 }}>
          <div className="label-meta" style={{ marginBottom: 6 }}>FROM</div>
          <input type="time" value={start} onChange={(e) => setStart(e.target.value)} style={timeInputStyle} />
        </div>
        <div style={{ flex: 1 }}>
          <div className="label-meta" style={{ marginBottom: 6 }}>UNTIL</div>
          <input type="time" value={end} onChange={(e) => setEnd(e.target.value)} style={timeInputStyle} />
        </div>
      </div>
      <div>
        <div className="label-meta" style={{ marginBottom: 6 }}>EVERY</div>
        <div style={{ display: "flex", gap: 6 }}>
          {[10, 15, 20, 30, 45, 60].map((m) => (
            <button
              key={m}
              onClick={() => setEvery(m)}
              style={{
                flex: 1,
                padding: "8px 4px",
                background: every === m ? "var(--accent-soft)" : "rgba(255,255,255,0.02)",
                border: `1px solid ${every === m ? "var(--accent)" : "var(--border)"}`,
                color: every === m ? "var(--accent)" : "var(--text-sec)",
                borderRadius: 8,
                fontFamily: "var(--font-mono)",
                fontSize: 12,
                cursor: "pointer",
              }}
            >
              {m}m
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

const timeInputStyle = {
  width: "100%",
  background: "rgba(255,255,255,0.02)",
  border: "1px solid var(--border)",
  borderRadius: 10,
  padding: "10px 12px",
  color: "var(--text-pri)",
  fontFamily: "var(--font-mono)",
  fontSize: 18,
  outline: "none",
  colorScheme: "dark",
};

function AudioPickerRow({ audio, previewPlaying, onTogglePreview, onChange }) {
  return (
    <div className="card" style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 14px" }}>
      <button
        onClick={(e) => { e.stopPropagation(); onTogglePreview(); }}
        style={{
          width: 40, height: 40, borderRadius: "50%",
          background: previewPlaying ? "var(--accent)" : "var(--accent-soft)",
          border: previewPlaying ? "none" : "1px solid rgba(107,138,255,0.3)",
          color: previewPlaying ? "#0A0E1A" : "var(--accent)",
          display: "flex", alignItems: "center", justifyContent: "center",
          cursor: "pointer",
          flexShrink: 0,
          animation: previewPlaying ? "pulse-ring 1.5s infinite" : "none",
        }}
        aria-label={previewPlaying ? "Stop preview" : "Play preview"}
      >
        {previewPlaying ? <IconPause size={16} color="currentColor" /> : <IconPlay size={16} color="currentColor" style={{ marginLeft: 2 }} />}
      </button>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: 14, fontWeight: 500, color: "var(--text-pri)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
          {audio.name}
        </div>
        <div className="mono" style={{ fontSize: 11, color: "var(--text-sec)", marginTop: 3 }}>
          {audio.source.toUpperCase()} · {(audio.durationMs / 1000).toFixed(1)}s
        </div>
        {previewPlaying && <PreviewWaveform />}
      </div>
      <button onClick={onChange} className="btn btn-ghost" style={{ padding: "6px 10px", color: "var(--accent)", fontSize: 12, fontFamily: "var(--font-mono)", letterSpacing: 0.5 }}>
        CHANGE
      </button>
    </div>
  );
}

function PreviewWaveform() {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 2, marginTop: 6, height: 12 }}>
      {Array.from({ length: 24 }).map((_, i) => (
        <div
          key={i}
          style={{
            width: 2,
            height: 3 + Math.abs(Math.sin(i * 0.6)) * 9,
            background: "var(--accent)",
            opacity: 0.4 + Math.random() * 0.5,
            borderRadius: 1,
            animation: `breathe ${0.6 + (i % 5) * 0.2}s ease-in-out infinite`,
          }}
        />
      ))}
    </div>
  );
}

function Slider({ value, onChange, min = 0, max = 100 }) {
  return (
    <div style={{ position: "relative", padding: "12px 0" }}>
      <input
        type="range"
        min={min}
        max={max}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        style={{
          width: "100%",
          appearance: "none",
          WebkitAppearance: "none",
          background: "transparent",
          height: 24,
          cursor: "pointer",
        }}
        className="lucid-slider"
      />
      <style>{`
        .lucid-slider::-webkit-slider-runnable-track {
          height: 4px;
          background: linear-gradient(to right, var(--accent) 0%, var(--accent) ${((value - min) / (max - min)) * 100}%, var(--surface-2) ${((value - min) / (max - min)) * 100}%, var(--surface-2) 100%);
          border-radius: 2px;
        }
        .lucid-slider::-webkit-slider-thumb {
          appearance: none;
          -webkit-appearance: none;
          width: 18px;
          height: 18px;
          border-radius: 50%;
          background: var(--text-pri);
          margin-top: -7px;
          border: 2px solid var(--accent);
          box-shadow: 0 0 12px var(--accent-glow);
          cursor: pointer;
        }
        .lucid-slider::-moz-range-track {
          height: 4px;
          background: var(--surface-2);
          border-radius: 2px;
        }
        .lucid-slider::-moz-range-progress {
          height: 4px;
          background: var(--accent);
          border-radius: 2px;
        }
        .lucid-slider::-moz-range-thumb {
          width: 18px;
          height: 18px;
          border-radius: 50%;
          background: var(--text-pri);
          border: 2px solid var(--accent);
          cursor: pointer;
        }
      `}</style>
    </div>
  );
}

function Stepper({ value, onChange, min = 0, max = 99 }) {
  return (
    <div className="card" style={{ display: "flex", alignItems: "center", padding: "8px 8px" }}>
      <button
        onClick={() => onChange(Math.max(min, value - 1))}
        style={stepBtnStyle}
        aria-label="Decrement"
      >−</button>
      <div className="mono tabular" style={{ flex: 1, textAlign: "center", fontSize: 22, fontWeight: 300, color: "var(--text-pri)", letterSpacing: -0.5 }}>
        ×{value}
      </div>
      <button
        onClick={() => onChange(Math.min(max, value + 1))}
        style={stepBtnStyle}
        aria-label="Increment"
      >+</button>
    </div>
  );
}

const stepBtnStyle = {
  width: 38, height: 38, borderRadius: 10,
  background: "rgba(255,255,255,0.04)",
  border: "1px solid var(--border-2)",
  color: "var(--text-pri)",
  cursor: "pointer",
  fontSize: 22,
  fontWeight: 300,
  fontFamily: "var(--font-sans)",
  lineHeight: 1,
};

function VibPatternViz({ pattern, active }) {
  if (!pattern) {
    return <div style={{ height: 12, display: "flex", alignItems: "center" }}>
      <div style={{ width: 18, height: 1.5, background: active ? "var(--accent)" : "var(--text-ter)" }} />
    </div>;
  }
  const segs = pattern.split(",").map(Number);
  const total = segs.reduce((a, b) => a + b, 0);
  return (
    <div style={{ height: 12, display: "flex", alignItems: "center", gap: 1, width: 36 }}>
      {segs.map((ms, i) => {
        const isPulse = i % 2 === 1; // 0=delay, 1=pulse, 2=delay…
        const w = Math.max(1, (ms / total) * 36);
        return (
          <div
            key={i}
            style={{
              width: w,
              height: isPulse ? 10 : 1.5,
              background: isPulse ? (active ? "var(--accent)" : "var(--text-sec)") : "transparent",
              borderRadius: 1,
            }}
          />
        );
      })}
    </div>
  );
}

function AudioPickerSheet({ currentId, onPick, onClose }) {
  const groups = [
    { label: "BUNDLED",      items: AUDIO_ASSETS.filter((a) => a.source === "bundled") },
    { label: "MY RECORDINGS", items: AUDIO_ASSETS.filter((a) => a.source === "recording") },
    { label: "IMPORTED",     items: AUDIO_ASSETS.filter((a) => a.source === "imported") },
  ];
  return (
    <div style={{
      position: "absolute", inset: 0, zIndex: 100,
      background: "rgba(0,0,0,0.7)",
      backdropFilter: "blur(14px)",
      WebkitBackdropFilter: "blur(14px)",
      display: "flex", alignItems: "flex-end", justifyContent: "center",
      padding: 12, paddingBottom: 28,
    }}>
      <div style={{
        width: "100%",
        maxHeight: "78%",
        background: "linear-gradient(180deg, #1a2038 0%, #131829 100%)",
        border: "1px solid var(--border-2)",
        borderRadius: 20,
        padding: 18,
        display: "flex", flexDirection: "column", gap: 14,
        overflow: "hidden",
      }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div>
            <div className="label-meta">PICK AUDIO</div>
            <div style={{ fontSize: 17, fontWeight: 500, marginTop: 4 }}>Audio library</div>
          </div>
          <button onClick={onClose} className="btn btn-ghost" style={{ padding: 8 }}>
            <IconClose size={18} color="var(--text-sec)" />
          </button>
        </div>
        <div style={{ flex: 1, overflowY: "auto", display: "flex", flexDirection: "column", gap: 16 }}>
          {groups.map((g) => g.items.length > 0 && (
            <div key={g.label}>
              <div className="label-meta" style={{ marginBottom: 6 }}>{g.label}</div>
              <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                {g.items.map((a) => (
                  <AudioPickRow key={a.id} asset={a} selected={a.id === currentId} onPick={() => onPick(a.id)} />
                ))}
              </div>
            </div>
          ))}
          <div className="divider" />
          <button className="btn" style={{ padding: "12px 14px" }}>
            <IconPlus size={14} color="currentColor" /> Import file
          </button>
          <button className="btn" style={{ padding: "12px 14px" }}>
            <IconMic size={14} color="currentColor" /> Record new
          </button>
        </div>
      </div>
    </div>
  );
}

function AudioPickRow({ asset, selected, onPick }) {
  const FamilyIcon = asset.family === "voice" ? IconMic : asset.family === "noise" ? IconWaves : IconBell;
  return (
    <div
      onClick={onPick}
      style={{
        display: "flex", alignItems: "center", gap: 12,
        padding: "10px 12px",
        background: selected ? "var(--accent-soft)" : "transparent",
        border: `1px solid ${selected ? "rgba(107,138,255,0.35)" : "transparent"}`,
        borderRadius: 10,
        cursor: "pointer",
      }}
    >
      <FamilyIcon size={16} color={selected ? "var(--accent)" : "var(--text-sec)"} />
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontSize: 13.5, fontWeight: selected ? 500 : 400, color: "var(--text-pri)" }}>
          {asset.name}
        </div>
        <div className="mono" style={{ fontSize: 11, color: "var(--text-ter)", marginTop: 2 }}>
          {(asset.durationMs / 1000).toFixed(1)}s
        </div>
      </div>
      {selected && <IconCheck size={16} color="var(--accent)" />}
      {!selected && (
        <button
          onClick={(e) => e.stopPropagation()}
          style={{
            width: 30, height: 30, borderRadius: "50%",
            background: "rgba(255,255,255,0.04)", border: "1px solid var(--border-2)",
            color: "var(--text-sec)", cursor: "pointer",
            display: "flex", alignItems: "center", justifyContent: "center",
          }}
          aria-label="Preview"
        >
          <IconPlay size={11} color="currentColor" style={{ marginLeft: 1 }} />
        </button>
      )}
    </div>
  );
}

Object.assign(window, { CueProfilesScreen, ProfileDetailScreen, CueEditorScreen });

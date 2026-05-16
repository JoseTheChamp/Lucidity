// PhoneFrame.jsx — Custom Android (OnePlus-style) bezel with center hole-punch.
// Dark themed for Lucid Dream Companion.

function PhoneFrame({ children, width = 396, height = 856, time = "3:42", batteryPct = 47 }) {
  return (
    <div
      style={{
        width: width + 16,
        height: height + 16,
        borderRadius: 52,
        padding: 8,
        background: "linear-gradient(180deg, #1a1d28 0%, #0c0e15 100%)",
        boxShadow:
          "0 0 0 1px rgba(255,255,255,0.04) inset, " +
          "0 40px 120px -20px rgba(0,0,0,0.7), " +
          "0 0 0 1px rgba(0,0,0,0.6)",
      }}
    >
      <div
        style={{
          width,
          height,
          borderRadius: 44,
          overflow: "hidden",
          background: "#000",
          position: "relative",
          boxShadow:
            "0 0 0 2px #050608 inset",
        }}
      >
        {/* App background */}
        <div className="app-bg" />

        {/* Status bar */}
        <PhoneStatusBar time={time} batteryPct={batteryPct} />

        {/* Center hole-punch camera (OnePlus-style) */}
        <div
          style={{
            position: "absolute",
            left: "50%",
            top: 12,
            transform: "translateX(-50%)",
            width: 11,
            height: 11,
            borderRadius: "50%",
            background: "#000",
            boxShadow:
              "inset 0 0 0 1px rgba(60,68,90,0.55), 0 0 0 2px rgba(0,0,0,0.7)",
            zIndex: 50,
          }}
        >
          <div
            style={{
              position: "absolute",
              inset: 2,
              borderRadius: "50%",
              background:
                "radial-gradient(circle at 30% 30%, #2a3349 0%, #0b0e18 70%, #000 100%)",
            }}
          />
        </div>

        {/* Content layer */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            paddingTop: 40,
            paddingBottom: 28,
            display: "flex",
            flexDirection: "column",
          }}
        >
          {children}
        </div>

        {/* Gesture nav indicator */}
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            bottom: 8,
            display: "flex",
            justifyContent: "center",
            zIndex: 40,
          }}
        >
          <div
            style={{
              width: 124,
              height: 4,
              borderRadius: 2,
              background: "rgba(232,236,244,0.35)",
            }}
          />
        </div>
      </div>
    </div>
  );
}

function PhoneStatusBar({ time, batteryPct }) {
  return (
    <div
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        right: 0,
        height: 40,
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "0 24px",
        color: "#E8ECF4",
        fontFamily: "var(--font-mono)",
        fontSize: 13,
        zIndex: 30,
        fontFeatureSettings: '"tnum" 1',
      }}
    >
      <span style={{ fontWeight: 500, letterSpacing: 0.2 }}>{time}</span>
      <div style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 11 }}>
        {/* Signal triangle */}
        <svg width="14" height="14" viewBox="0 0 16 16">
          <path d="M2 13.5L13.5 2v11.5z" fill="#E8ECF4" />
        </svg>
        {/* Wifi */}
        <svg width="14" height="14" viewBox="0 0 16 16">
          <path d="M8 13.3a1.1 1.1 0 1 1 0-2.2 1.1 1.1 0 0 1 0 2.2zm0-4.4c1.5 0 2.9.6 4 1.6l-1.4 1.4a3.7 3.7 0 0 0-5.2 0L4 10.5A5.8 5.8 0 0 1 8 8.9zm0-3.5c2.6 0 4.9.9 6.8 2.5l-1.4 1.4a8 8 0 0 0-10.8 0L1.2 7.5A10 10 0 0 1 8 5z" fill="#E8ECF4" />
        </svg>
        {/* Battery */}
        <div style={{ display: "flex", alignItems: "center", gap: 3, marginLeft: 2 }}>
          <span style={{ opacity: 0.85 }}>{batteryPct}</span>
          <div
            style={{
              width: 22,
              height: 11,
              borderRadius: 3,
              border: "1px solid #E8ECF4",
              position: "relative",
              padding: 1.5,
            }}
          >
            <div
              style={{
                width: `${batteryPct}%`,
                height: "100%",
                background: "#E8ECF4",
                borderRadius: 1,
              }}
            />
            <div
              style={{
                position: "absolute",
                right: -3,
                top: 2.5,
                width: 2,
                height: 4,
                borderRadius: 1,
                background: "#E8ECF4",
              }}
            />
          </div>
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────
// BottomNav — 5 destinations, active state with thin accent line above
// ─────────────────────────────────────────────────────────────
function BottomNav({ current, onChange }) {
  const tabs = [
    { id: "home", label: "Home", Icon: IconHome },
    { id: "journal", label: "Journal", Icon: IconBook },
    { id: "sleep", label: "Sleep", Icon: IconMoon },
    { id: "stats", label: "Stats", Icon: IconChart },
    { id: "settings", label: "Settings", Icon: IconSettings },
  ];
  return (
    <div
      style={{
        position: "relative",
        background: "linear-gradient(180deg, rgba(10,14,26,0.6), rgba(5,8,16,0.95))",
        borderTop: "1px solid var(--border)",
        backdropFilter: "blur(20px)",
        WebkitBackdropFilter: "blur(20px)",
        padding: "10px 8px 18px",
        display: "flex",
        justifyContent: "space-around",
        alignItems: "stretch",
      }}
    >
      {tabs.map((t) => {
        const active = t.id === current;
        return (
          <button
            key={t.id}
            onClick={() => onChange(t.id)}
            style={{
              flex: 1,
              background: "transparent",
              border: "none",
              padding: "8px 4px 4px",
              cursor: "pointer",
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              gap: 4,
              color: active ? "var(--accent)" : "var(--text-ter)",
              position: "relative",
              transition: "color 160ms ease",
            }}
          >
            {active && (
              <div
                style={{
                  position: "absolute",
                  top: 0,
                  width: 28,
                  height: 2,
                  borderRadius: 2,
                  background: "var(--accent)",
                  boxShadow: "0 0 10px var(--accent-glow)",
                }}
              />
            )}
            <t.Icon size={22} color="currentColor" strokeWidth={1.6} />
            <span
              style={{
                fontSize: 10,
                fontFamily: "var(--font-mono)",
                letterSpacing: 0.6,
                textTransform: "uppercase",
              }}
            >
              {t.label}
            </span>
          </button>
        );
      })}
    </div>
  );
}

Object.assign(window, { PhoneFrame, BottomNav });

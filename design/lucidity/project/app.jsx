// app.jsx — root app: navigation, phone frame composition.

function App() {

  // ─── Navigation ────────────────────────────────────────────
  // We keep a stack so back works for editor.
  const [stack, setStack] = React.useState([{ screen: "home" }]);
  const top = stack[stack.length - 1];

  function push(screen, params = {}) {
    setStack((s) => [...s, { screen, ...params }]);
  }
  function replaceRoot(screen) {
    setStack([{ screen }]);
  }
  function pop() {
    setStack((s) => (s.length > 1 ? s.slice(0, -1) : s));
  }
  function navTo(screen, params) {
    // Bottom-nav navigation resets to that root
    if (["home", "journal", "stats", "settings"].includes(screen)) {
      replaceRoot(screen);
      return;
    }
    if (screen === "editor") return push("editor", params);
    if (screen === "session") return push("session");
    if (screen === "cues") return push("cues");
    if (screen === "profile") return push("profile", params);
    if (screen === "cue") return push("cue", params);
    push(screen, params);
  }

  // Session state
  const [sessionStartedAt, setSessionStartedAt] = React.useState(null);

  function startSleep() {
    setSessionStartedAt(Date.now() - 14_000); // start 14s back so we see motion immediately
    push("session");
  }
  function endSleep() {
    setSessionStartedAt(null);
    pop();
  }

  // Determine which bottom-nav tab is highlighted
  const navTabFor = (screen) => {
    if (screen === "home") return "home";
    if (screen === "journal" || screen === "editor") return "journal";
    if (screen === "session" || screen === "cues" || screen === "profile" || screen === "cue") return "sleep";
    if (screen === "stats") return "stats";
    if (screen === "settings") return "settings";
    return "home";
  };

  // Render the current screen
  let screenEl;
  if (top.screen === "home") {
    screenEl = (
      <HomeScreen
        onNav={navTo}
        onStartSleep={startSleep}
        onNewEntry={() => push("editor", { isNew: true })}
        sessionActive={!!sessionStartedAt}
      />
    );
  } else if (top.screen === "journal") {
    screenEl = (
      <JournalScreen
        onOpen={(id) => push("editor", { entryId: id })}
        onNew={() => push("editor", { isNew: true })}
      />
    );
  } else if (top.screen === "editor") {
    screenEl = (
      <EditorScreen
        entryId={top.entryId}
        isNew={top.isNew}
        onBack={pop}
      />
    );
  } else if (top.screen === "session") {
    screenEl = (
      <SessionScreen
        sessionStartedAt={sessionStartedAt || Date.now()}
        onEnd={endSleep}
        onBack={pop}
      />
    );
  } else if (top.screen === "stats") {
    screenEl = <StatsScreen />;
  } else if (top.screen === "settings") {
    screenEl = <SettingsScreen />;
  } else if (top.screen === "cues") {
    screenEl = (
      <CueProfilesScreen
        onBack={pop}
        onOpenProfile={(id) => push("profile", { profileId: id })}
        onNewProfile={() => push("profile", { profileId: 7, isNew: true })}
        onStartSleep={startSleep}
      />
    );
  } else if (top.screen === "profile") {
    screenEl = (
      <ProfileDetailScreen
        profileId={top.profileId}
        onBack={pop}
        onEditCue={(cid) => push("cue", { profileId: top.profileId, cueId: cid })}
        onAddCue={() => push("cue", { profileId: top.profileId, isNew: true })}
      />
    );
  } else if (top.screen === "cue") {
    screenEl = (
      <CueEditorScreen
        profileId={top.profileId}
        cueId={top.cueId}
        isNew={top.isNew}
        onBack={pop}
      />
    );
  }

  // Bottom nav stays visible during sessions so the user can navigate; only editor/cue editor go full-screen.
  const showBottomNav = !["editor", "cue"].includes(top.screen);

  return (
    <div style={{
      minHeight: "100vh",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      padding: "32px 16px",
      background: "radial-gradient(80% 60% at 50% 0%, #0c1226 0%, #04060d 60%, #000 100%)",
    }}>
      <PhoneFrame>
        {sessionStartedAt && top.screen !== "session" && (
          <SessionIndicator
            sessionStartedAt={sessionStartedAt}
            onResume={() => push("session")}
          />
        )}
        <div style={{ flex: 1, display: "flex", flexDirection: "column", minHeight: 0 }} key={top.screen + (top.entryId || "") + (top.isNew ? "n" : "")}>
          {screenEl}
        </div>
        {showBottomNav && (
          <BottomNav
            current={navTabFor(top.screen)}
            onChange={(id) => {
              if (id === "sleep") {
                if (sessionStartedAt) push("session");
                else push("cues");
                return;
              }
              navTo(id);
            }}
          />
        )}
      </PhoneFrame>

    </div>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);

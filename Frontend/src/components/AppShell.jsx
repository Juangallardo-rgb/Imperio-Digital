import Navbar from "./Navbar";

function AppShell({ children }) {
  return (
    <div className="app-shell">
      <Navbar />
      <main className="app-shell-main" id="main-content">
        <div className="app-shell-content">{children}</div>
      </main>
    </div>
  );
}

export default AppShell;

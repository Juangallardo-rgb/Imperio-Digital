import { BrowserRouter, Routes, Route } from "react-router-dom";
import Navbar from "./components/Navbar";
import ProtectedRoute from "./routes/ProtectedRoute";
import LoginPage from "./pages/LoginPage";
import DashboardPage from "./pages/DashboardPage";
import ScenariosPage from "./pages/ScenariosPage";
import CreateScenarioPage from "./pages/CreateScenarioPage";
import CreateVariablePage from "./pages/CreateVariablePage";
import SimulationPage from "./pages/SimulationPage";
import SimulationHistoryPage from "./pages/SimulationHistoryPage";
import SimulationDetailPage from "./pages/SimulationDetailPage";

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/" element={<LoginPage />} />

        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/scenarios"
          element={
            <ProtectedRoute>
              <ScenariosPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/scenarios/create"
          element={
            <ProtectedRoute>
              <CreateScenarioPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/variables/create"
          element={
            <ProtectedRoute>
              <CreateVariablePage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/simulate/:scenarioId"
          element={
            <ProtectedRoute>
              <SimulationPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/simulations/history"
          element={
            <ProtectedRoute>
              <SimulationHistoryPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/simulations/:id"
          element={
            <ProtectedRoute>
              <SimulationDetailPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Navbar from "./components/Navbar";
import ProtectedRoute from "./routes/ProtectedRoute";
import LoginPage from "./pages/LoginPage";
import DashboardPage from "./pages/DashboardPage";

// Flujo anterior
import ScenariosPage from "./pages/ScenariosPage";
import CreateScenarioPage from "./pages/CreateScenarioPage";
import CreateVariablePage from "./pages/CreateVariablePage";
import SimulationPage from "./pages/SimulationPage";
import SimulationHistoryPage from "./pages/SimulationHistoryPage";
import SimulationDetailPage from "./pages/SimulationDetailPage";

// Nuevo flujo Design Thinking
import CreateDesignThinkingScenarioPage from "./pages/designThinking/CreateDesignThinkingScenarioPage";
import MyDesignThinkingScenariosPage from "./pages/designThinking/MyDesignThinkingScenariosPage";
import DesignThinkingScenarioDetailPage from "./pages/designThinking/DesignThinkingScenarioDetailPage";
import PublishedDesignThinkingScenariosPage from "./pages/designThinking/PublishedDesignThinkingScenariosPage";
import DesignThinkingSimulationPage from "./pages/designThinking/DesignThinkingSimulationPage";
import DesignThinkingResultsPage from "./pages/designThinking/DesignThinkingResultsPage";
import DesignThinkingHistoryPage from "./pages/designThinking/DesignThinkingHistoryPage";

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

        {/* Flujo anterior: lo dejamos disponible por seguridad */}
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

        {/* Nuevo flujo Design Thinking - Docente */}
        <Route
          path="/design-thinking/scenarios"
          element={
            <ProtectedRoute>
              <MyDesignThinkingScenariosPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/design-thinking/scenarios/create"
          element={
            <ProtectedRoute>
              <CreateDesignThinkingScenarioPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/design-thinking/scenarios/:id"
          element={
            <ProtectedRoute>
              <DesignThinkingScenarioDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Nuevo flujo Design Thinking - Estudiante */}
        <Route
          path="/design-thinking/published"
          element={
            <ProtectedRoute>
              <PublishedDesignThinkingScenariosPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/design-thinking/simulate/:attemptId"
          element={
            <ProtectedRoute>
              <DesignThinkingSimulationPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/design-thinking/results/:attemptId"
          element={
            <ProtectedRoute>
              <DesignThinkingResultsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/design-thinking/history"
          element={
            <ProtectedRoute>
              <DesignThinkingHistoryPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
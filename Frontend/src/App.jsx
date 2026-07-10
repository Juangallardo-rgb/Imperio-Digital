import { BrowserRouter, Routes, Route } from "react-router-dom";
import ProtectedRoute from "./routes/ProtectedRoute";

import LoginPage from "./pages/LoginPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage";
import ResetPasswordPage from "./pages/ResetPasswordPage";
import ChangeTemporaryPasswordPage from "./pages/ChangeTemporaryPasswordPage";
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

// Cursos
import TeacherCoursesPage from "./pages/courses/TeacherCoursesPage";
import CreateCoursePage from "./pages/courses/CreateCoursePage";
import CourseDetailPage from "./pages/courses/CourseDetailPage";
import AvailableCoursesPage from "./pages/courses/AvailableCoursesPage";
import MyCoursesPage from "./pages/courses/MyCoursesPage";
import StudentCourseDetailPage from "./pages/courses/StudentCourseDetailPage";
import CourseResultsPage from "./pages/courses/CourseResultsPage";
import CourseSimulationResultDetailPage from "./pages/courses/CourseSimulationResultDetailPage";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Autenticación */}
        <Route path="/" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route
          path="/change-temporary-password"
          element={
            <ProtectedRoute>
              <ChangeTemporaryPasswordPage />
            </ProtectedRoute>
          }
        />

        {/* Dashboard */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />

        {/* Cursos - Docente */}
        <Route
          path="/courses"
          element={
            <ProtectedRoute>
              <TeacherCoursesPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/courses/create"
          element={
            <ProtectedRoute>
              <CreateCoursePage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/courses/:id"
          element={
            <ProtectedRoute>
              <CourseDetailPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/courses/:id/results"
          element={
            <ProtectedRoute>
              <CourseResultsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/courses/:courseId/results/:attemptId"
          element={
            <ProtectedRoute>
              <CourseSimulationResultDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Cursos - Estudiante */}
        <Route
          path="/courses/available"
          element={
            <ProtectedRoute>
              <AvailableCoursesPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/my-courses"
          element={
            <ProtectedRoute>
              <MyCoursesPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/my-courses/:id"
          element={
            <ProtectedRoute>
              <StudentCourseDetailPage />
            </ProtectedRoute>
          }
        />

        {/* Design Thinking - Docente */}
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

        {/* Design Thinking - Estudiante */}
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

        {/* Flujo anterior: se mantiene por seguridad */}
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

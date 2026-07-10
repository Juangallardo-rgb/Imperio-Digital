import {
  useCallback,
  useEffect,
  useState,
} from "react";
import { Link, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import useRealtimeRefresh from "../../hooks/useRealtimeRefresh";

const COURSE_DETAIL_EVENTS = [
  "CoursesChanged",
  "EnrollmentsChanged",
  "CourseScenariosChanged",
  "ResultsChanged",
];

function CourseDetailPage() {
  const { id } = useParams();
  const courseId = Number(id);

  const [course, setCourse] = useState(null);
  const [scenarios, setScenarios] = useState([]);
  const [selectedScenarioId, setSelectedScenarioId] =
    useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [assigning, setAssigning] = useState(false);
  const [importFile, setImportFile] = useState(null);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState(null);
  const [importMessage, setImportMessage] = useState("");

  const loadCourse = useCallback(
    async (showLoader = false) => {
      if (showLoader) {
        setLoading(true);
        setMessage("");
      }

      try {
        const token = getToken();

        const response = await api.get(
          `/courses/${courseId}`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        setCourse(response.data);
      } catch (error) {
        console.error("Error cargando curso:", error);

        if (showLoader) {
          setMessage(
            error.response
              ? `Error ${
                  error.response.status
                }: ${JSON.stringify(
                  error.response.data
                )}`
              : "No hubo respuesta del backend."
          );
        }
      } finally {
        if (showLoader) {
          setLoading(false);
        }
      }
    },
    [courseId]
  );

  const loadScenarios = useCallback(async () => {
    try {
      const token = getToken();

      const response = await api.get(
        "/design-thinking/scenarios/my",
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setScenarios(
        Array.isArray(response.data)
          ? response.data
          : []
      );
    } catch (error) {
      console.error(
        "Error cargando escenarios:",
        error
      );
    }
  }, []);

  const refreshCourse = useCallback(
    (payload) => {
      if (
        payload?.courseId &&
        Number(payload.courseId) !== courseId
      ) {
        return Promise.resolve();
      }

      return Promise.all([
        loadCourse(false),
        loadScenarios(),
      ]);
    },
    [courseId, loadCourse, loadScenarios]
  );

  useRealtimeRefresh(
    COURSE_DETAIL_EVENTS,
    refreshCourse,
    15000
  );

  const assignScenario = async () => {
    if (!selectedScenarioId) {
      setMessage(
        "Selecciona un escenario para asignar."
      );
      return;
    }

    setAssigning(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/courses/${courseId}/scenarios/${selectedScenarioId}`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(
        typeof response.data === "string"
          ? response.data
          : response.data?.message ||
              "Escenario asignado correctamente."
      );

      setSelectedScenarioId("");
      await loadCourse(false);
    } catch (error) {
      console.error(
        "Error asignando escenario:",
        error
      );

      setMessage(
        error.response
          ? `Error ${
              error.response.status
            }: ${JSON.stringify(
              error.response.data
            )}`
          : "No hubo respuesta del backend."
      );
    } finally {
      setAssigning(false);
    }
  };

  const downloadTemplateCsv = () => {
    downloadCsv(
      "plantilla-estudiantes.csv",
      [
        ["name", "email"],
        ["Estudiante Uno", "estudiante1@udla.edu.ec"],
        ["Estudiante Dos", "estudiante2@udla.edu.ec"],
      ]
    );
  };

  const handleImportFileChange = (event) => {
    const file = event.target.files?.[0] || null;

    setImportFile(file);
    setImportResult(null);
    setImportMessage("");

    if (file && !file.name.toLowerCase().endsWith(".csv")) {
      setImportMessage("Solo se aceptan archivos .csv.");
    }
  };

  const importStudents = async () => {
    if (!importFile) {
      setImportMessage("Selecciona un archivo CSV.");
      return;
    }

    if (!importFile.name.toLowerCase().endsWith(".csv")) {
      setImportMessage("Solo se aceptan archivos .csv.");
      return;
    }

    setImporting(true);
    setImportMessage("");
    setImportResult(null);

    try {
      const token = getToken();
      const formData = new FormData();
      formData.append("file", importFile);

      const response = await api.post(
        `/courses/${courseId}/students/import`,
        formData,
        {
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "multipart/form-data",
          },
        }
      );

      setImportResult(response.data);
      setImportMessage("Importación completada.");
      setImportFile(null);
      await loadCourse(false);
    } catch (error) {
      setImportMessage(
        error.response
          ? typeof error.response.data === "string"
            ? error.response.data
            : JSON.stringify(error.response.data)
          : "No se pudo importar el archivo."
      );
    } finally {
      setImporting(false);
    }
  };

  const downloadTemporaryCredentials = () => {
    const credentials = importResult?.credentials || [];

    if (!credentials.length) return;

    downloadCsv(
      `credenciales-${course.code}.csv`,
      [
        [
          "name",
          "email",
          "temporaryPassword",
          "courseCode",
        ],
        ...credentials.map((credential) => [
          credential.name,
          credential.email,
          credential.temporaryPassword,
          credential.courseCode,
        ]),
      ],
      true
    );
  };

  useEffect(() => {
    void Promise.all([
      loadCourse(true),
      loadScenarios(),
    ]);
  }, [loadCourse, loadScenarios]);

  if (loading) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <p>Cargando curso...</p>
        </div>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <h2>No se encontró el curso</h2>

          {message && (
            <div className="message">
              {message}
            </div>
          )}
        </div>
      </div>
    );
  }

  const students = Array.isArray(course.students)
    ? course.students
    : [];

  const assignedScenarios = Array.isArray(
    course.scenarios
  )
    ? course.scenarios
    : [];

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">
            Curso académico
          </span>

          <h1>{course.name}</h1>
          <p>{course.description}</p>
        </div>

        <div className="phase-pill">
          <span>Código</span>
          <strong>{course.code}</strong>
        </div>
      </div>

      {message && (
        <div className="message pro-message">
          {message}
        </div>
      )}

      <div className="dashboard-stats">
        <div className="stat-card-pro">
          <span>Estudiantes inscritos</span>
          <strong>{students.length}</strong>
        </div>

        <div className="stat-card-pro">
          <span>Escenarios asignados</span>
          <strong>{assignedScenarios.length}</strong>
        </div>

        <div className="stat-card-pro">
          <span>Estado</span>
          <strong>
            {course.isActive
              ? "Activo"
              : "Inactivo"}
          </strong>
        </div>
      </div>

      <div className="pro-layout-2">
        <div className="pro-card">
          <div className="section-header">
            <div>
              <span className="eyebrow">
                Asignación
              </span>

              <h2>Asignar escenario</h2>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="scenario-select">
              Escenario
            </label>

            <select
              id="scenario-select"
              value={selectedScenarioId}
              onChange={(event) =>
                setSelectedScenarioId(
                  event.target.value
                )
              }
            >
              <option value="">
                Selecciona un escenario
              </option>

              {scenarios.map((scenario) => (
                <option
                  key={scenario.id}
                  value={scenario.id}
                >
                  {scenario.title} -{" "}
                  {scenario.methodologyName ||
                    scenario.methodology}{" "}
                  {scenario.isPublished
                    ? ""
                    : "(Borrador)"}
                </option>
              ))}
            </select>
          </div>

          <button
            className="primary-action"
            onClick={assignScenario}
            disabled={assigning}
          >
            {assigning
              ? "Asignando..."
              : "Asignar al curso"}
          </button>
        </div>

        <div className="pro-card">
          <div className="section-header">
            <div>
              <span className="eyebrow">
                Analítica
              </span>

              <h2>Resultados</h2>
            </div>
          </div>

          <p>
            Revisa el desempeño de estudiantes,
            intentos finalizados y puntajes.
          </p>

          <Link
            className="button-link"
            to={`/courses/${course.id}/results`}
          >
            Ver resultados del curso
          </Link>
        </div>
      </div>

      <div className="pro-card">
        <h2>Escenarios asignados</h2>

        {assignedScenarios.length === 0 ? (
          <p>No hay escenarios asignados.</p>
        ) : (
          <div className="table-list">
            {assignedScenarios.map((scenario) => (
              <div
                key={scenario.scenarioId}
                className="table-row-card"
              >
                <div>
                  <strong>{scenario.title}</strong>

                  <p>
                    {scenario.methodologyName ||
                      scenario.methodology ||
                      "Metodología no definida"}{" "}
                    · Dificultad:{" "}
                    {scenario.difficulty}
                  </p>
                </div>

                <span
                  className={
                    scenario.isPublished
                      ? "status-pill success"
                      : "status-pill warning"
                  }
                >
                  {scenario.isPublished
                    ? "Publicado"
                    : "Borrador"}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="pro-card">
        <div className="section-header">
          <div>
            <span className="eyebrow">
              Matrícula masiva
            </span>

            <h2>Importar estudiantes</h2>
          </div>
        </div>

        <p>
          Sube un archivo CSV en formato UTF-8 con las columnas name,email.
          Las cuentas nuevas se crearán como Estudiante y quedarán inscritas en
          este curso.
        </p>

        <div className="import-actions">
          <button
            type="button"
            className="button-link"
            onClick={downloadTemplateCsv}
          >
            Descargar plantilla CSV
          </button>

          <label className="csv-file-picker">
            <span>Archivo CSV</span>
            <input
              type="file"
              accept=".csv,text/csv"
              onChange={handleImportFileChange}
            />
          </label>

          <button
            type="button"
            className="primary-action"
            onClick={importStudents}
            disabled={importing || !importFile}
          >
            {importing
              ? "Importando..."
              : "Crear cuentas e inscribir"}
          </button>
        </div>

        {importFile && (
          <p className="import-file-name">
            Archivo seleccionado: {importFile.name}
          </p>
        )}

        {importMessage && (
          <div className="message pro-message">
            {importMessage}
          </div>
        )}

        {importResult && (
          <div className="import-result-panel">
            <h3>Importación completada</h3>

            <div className="import-summary-grid">
              <div>
                <span>Registros procesados</span>
                <strong>{importResult.totalRows}</strong>
              </div>

              <div>
                <span>Cuentas nuevas</span>
                <strong>{importResult.newUsersCreated}</strong>
              </div>

              <div>
                <span>Existentes matriculados</span>
                <strong>
                  {importResult.existingStudentsEnrolled}
                </strong>
              </div>

              <div>
                <span>Ya inscritos</span>
                <strong>{importResult.alreadyEnrolled}</strong>
              </div>

              <div>
                <span>Registros con error</span>
                <strong>{importResult.failedRows}</strong>
              </div>
            </div>

            {(importResult.credentials || []).length > 0 && (
              <div className="temporary-credentials-warning">
                <strong>
                  Las contraseñas temporales se muestran una sola vez.
                </strong>
                <p>
                  Guarda el archivo en un lugar seguro y entrégalas
                  individualmente a los estudiantes.
                </p>

                <button
                  type="button"
                  className="primary-action"
                  onClick={downloadTemporaryCredentials}
                >
                  Descargar credenciales nuevas
                </button>
              </div>
            )}

            {(importResult.errors || []).length > 0 && (
              <div className="import-error-list">
                <h4>Errores</h4>

                {(importResult.errors || []).map((error) => (
                  <div
                    key={`${error.rowNumber}-${error.email}`}
                    className="import-error-row"
                  >
                    <strong>Fila {error.rowNumber}</strong>
                    <span>{error.email || "Sin correo"}</span>
                    <p>{error.message}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      <div className="pro-card">
        <h2>Estudiantes inscritos</h2>

        {students.length === 0 ? (
          <p>
            No hay estudiantes inscritos todavía.
          </p>
        ) : (
          <div className="table-list">
            {students.map((student) => (
              <div
                key={student.studentId}
                className="table-row-card"
              >
                <div>
                  <strong>{student.name}</strong>
                  <p>{student.email}</p>
                </div>

                <span>
                  {new Date(
                    student.enrolledAt
                  ).toLocaleDateString()}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function downloadCsv(fileName, rows, sanitize = false) {
  const csv = rows
    .map((row) =>
      row
        .map((value) =>
          escapeCsvValue(sanitize ? sanitizeCsvCell(value) : value)
        )
        .join(";")
    )
    .join("\r\n");

  const blob = new Blob([csv], {
    type: "text/csv;charset=utf-8;",
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download = fileName;
  link.click();

  URL.revokeObjectURL(url);
}

function escapeCsvValue(value) {
  const safeValue = String(value ?? "");
  return `"${safeValue.replaceAll('"', '""')}"`;
}

function sanitizeCsvCell(value) {
  const text = String(value ?? "");
  return /^[=+\-@]/.test(text) ? `'${text}` : text;
}

export default CourseDetailPage;

import { useEffect, useRef } from "react";
import mvpExplainerIllustration from "../../../../assets/methodologyExperience/mvp-explainer.svg";

const concepts = [
  ["MVP", "La solucion minima necesaria para validar una hipotesis y aprender."],
  ["Producto completo", "Una propuesta con alcance mayor, mas alla de lo necesario para la primera validacion."],
  ["Prototipo", "Una representacion que permite explorar una idea antes de comprometer construccion completa."],
  ["Experimento", "Una prueba deliberada con una pregunta, una senal y un criterio de aprendizaje."],
  ["Aprendizaje validado", "Una conclusion sustentada en evidencia observada durante la prueba."],
];

function ConceptModal({ isOpen, onClose }) {
  const closeButtonRef = useRef(null);

  useEffect(() => {
    if (!isOpen) return undefined;

    closeButtonRef.current?.focus();
    const onKeyDown = (event) => {
      if (event.key === "Escape") onClose();
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="dt-modal-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="dt-concept-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="mvp-concepts-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="dt-modal-header">
          <div>
            <span className="experience-eyebrow">Conceptos de producto</span>
            <h2 id="mvp-concepts-title">MVP y aprendizaje validado</h2>
          </div>
          <button ref={closeButtonRef} type="button" onClick={onClose} aria-label="Cerrar conceptos">
            Cerrar
          </button>
        </div>
        <img src={mvpExplainerIllustration} alt="Explicacion visual de un MVP" />
        <p className="dt-modal-lead">
          Un MVP no es un producto de mala calidad. Es la solucion minima necesaria
          para validar una hipotesis y aprender antes de invertir mas recursos.
        </p>
        <section className="dt-mvp-comparison" aria-label="Comparacion entre producto completo y MVP">
          <article>
            <h3>Producto completo</h3>
            <ul>
              <li>Redisenar toda la plataforma.</li>
              <li>Crear una aplicacion movil.</li>
              <li>Integrar pagos avanzados.</li>
              <li>Automatizar todo el proceso.</li>
            </ul>
          </article>
          <article>
            <h3>MVP</h3>
            <ul>
              <li>Mostrar costos claros.</li>
              <li>Reducir pasos criticos.</li>
              <li>Agregar una confirmacion visible.</li>
              <li>Probar si disminuye la friccion del usuario.</li>
            </ul>
          </article>
        </section>
        <dl>
          {concepts.map(([term, description]) => (
            <div key={term}><dt>{term}</dt><dd>{description}</dd></div>
          ))}
        </dl>
      </section>
    </div>
  );
}

export default ConceptModal;

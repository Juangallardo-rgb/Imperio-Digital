function BpmChoiceChips({ label, choices, value, onChange }) {
  return (
    <fieldset className="bpm-chip-field">
      <legend>{label}</legend>
      <div className="bpm-chip-group">
        {choices.map((choice) => {
          const isActive = value === choice.key;

          return (
            <button
              key={choice.key}
              type="button"
              className={`bpm-choice-chip ${isActive ? "is-active" : ""}`}
              aria-pressed={isActive}
              onClick={() => onChange(isActive ? "" : choice.key)}
            >
              {choice.label}
            </button>
          );
        })}
      </div>
    </fieldset>
  );
}

export default BpmChoiceChips;

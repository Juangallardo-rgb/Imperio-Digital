import { Component } from "react";

class ExperienceErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = {
      hasError: false,
      resetKey: props.resetKey,
    };
  }

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  static getDerivedStateFromProps(props, state) {
    if (props.resetKey !== state.resetKey) {
      return {
        hasError: false,
        resetKey: props.resetKey,
      };
    }

    return null;
  }

  componentDidCatch(error) {
    console.error("No se pudo renderizar la experiencia metodologica V2.", error);
  }

  render() {
    return this.state.hasError ? this.props.fallback : this.props.children;
  }
}

export default ExperienceErrorBoundary;

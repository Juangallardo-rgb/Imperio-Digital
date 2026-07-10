function AppIcon({ name, size = 20, title }) {
  const commonProps = {
    width: size,
    height: size,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.9,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": title ? undefined : true,
    focusable: "false",
  };

  const paths = {
    menu: <><path d="M4 7h16" /><path d="M4 12h16" /><path d="M4 17h16" /></>,
    close: <><path d="m6 6 12 12" /><path d="m18 6-12 12" /></>,
    dashboard: <><rect x="3.5" y="3.5" width="6.5" height="6.5" rx="1" /><rect x="14" y="3.5" width="6.5" height="6.5" rx="1" /><rect x="3.5" y="14" width="6.5" height="6.5" rx="1" /><rect x="14" y="14" width="6.5" height="6.5" rx="1" /></>,
    courses: <><path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16Z" /><path d="M4 5.5A2.5 2.5 0 0 1 6.5 8H20" /><path d="M8 12h8" /></>,
    scenarios: <><path d="M5 19.5V6.8a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v12.7" /><path d="M3.5 19.5h17" /><path d="M9 9h6" /><path d="M9 13h6" /></>,
    history: <><path d="M3.5 12a8.5 8.5 0 1 0 2.5-6" /><path d="M3.5 4.5v4h4" /><path d="M12 7v5l3.5 2" /></>,
    plus: <><path d="M12 5v14" /><path d="M5 12h14" /></>,
    logout: <><path d="M10 5H5v14h5" /><path d="M14 8l4 4-4 4" /><path d="M9 12h9" /></>,
    chevron: <path d="m9 18 6-6-6-6" />,
  };

  return (
    <svg {...commonProps} role={title ? "img" : undefined}>
      {title && <title>{title}</title>}
      {paths[name] || paths.dashboard}
    </svg>
  );
}

export default AppIcon;

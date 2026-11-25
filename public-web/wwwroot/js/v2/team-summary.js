(() => {
  function show(root, button) {
    const detail = root.querySelector("[data-v2-evo-detail]");
    const link = root.querySelector("[data-v2-evo-link]");
    root.querySelectorAll("[data-v2-evo-match]").forEach((item) => {
      const on = item === button;
      item.classList.toggle("is-selected", on);
      item.setAttribute("aria-pressed", on ? "true" : "false");
    });
    if (detail) detail.textContent = button.getAttribute("data-v2-evo-text") || "";
    if (link instanceof HTMLAnchorElement) {
      const href = button.getAttribute("data-v2-evo-href");
      if (href) {
        link.href = href;
        link.hidden = false;
      }
    }
  }

  function bind(root) {
    const buttons = Array.from(root.querySelectorAll("[data-v2-evo-match]"));
    buttons.forEach((button) => {
      button.addEventListener("click", () => show(root, button));
      button.addEventListener("focus", () => show(root, button));
    });
  }

  function init() {
    document.querySelectorAll("[data-v2-evo]").forEach(bind);
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
  else init();
})();

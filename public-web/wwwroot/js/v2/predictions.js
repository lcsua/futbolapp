(function () {
  function percent(value) {
    var points = value * 100;
    if (points < 0.1) return "<0,1%";
    if (points > 99.9) return ">99,9%";
    return points.toLocaleString("es-AR", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + "%";
  }

  function delta(value) {
    var points = value * 100;
    var text = Math.abs(points) < 0.05 ? "0,0" : points.toLocaleString("es-AR", { minimumFractionDigits: 1, maximumFractionDigits: 1, signDisplay: "always" });
    return text + " pp";
  }

  document.querySelectorAll("[data-story-path]").forEach(function (root) {
    var select = root.querySelector("[data-path-select]");
    var panels = root.querySelectorAll("[data-path]");
    function show() {
      panels.forEach(function (panel) {
        panel.hidden = panel.getAttribute("data-path") !== select.value;
      });
    }
    select.addEventListener("change", show);
    show();
  });

  document.querySelectorAll("[data-story-if]").forEach(function (root) {
    var select = root.querySelector("[data-if-select]");
    var panels = root.querySelectorAll("[data-if]");

    function apply(panel, outcome) {
      var reliable = panel.getAttribute("data-" + outcome + "-ok") === "1";
      var value = Number(panel.getAttribute("data-" + outcome));
      var current = Number(panel.getAttribute("data-current"));
      var result = panel.querySelector("[data-if-result]");
      result.textContent = reliable
        ? percent(value) + " (" + delta(value - current) + " respecto de " + percent(current) + ")"
        : "No hay muestra suficiente para estimar ese resultado con confianza.";
      panel.querySelectorAll("[data-outcome]").forEach(function (button) {
        button.setAttribute("aria-pressed", button.getAttribute("data-outcome") === outcome ? "true" : "false");
      });
      panel.querySelectorAll("[data-rival]").forEach(function (row) {
        var rivalValue = Number(row.getAttribute("data-" + outcome));
        var rivalCurrent = Number(row.getAttribute("data-current"));
        row.querySelector("[data-rival-result]").textContent = reliable
          ? percent(rivalValue) + " · " + delta(rivalValue - rivalCurrent)
          : "–";
      });
    }

    panels.forEach(function (panel) {
      panel.querySelectorAll("[data-outcome]").forEach(function (button) {
        button.addEventListener("click", function () {
          apply(panel, button.getAttribute("data-outcome"));
        });
      });
      apply(panel, "win");
    });

    function show() {
      panels.forEach(function (panel) {
        panel.hidden = panel.getAttribute("data-if") !== select.value;
      });
    }
    select.addEventListener("change", show);
    show();
  });
})();

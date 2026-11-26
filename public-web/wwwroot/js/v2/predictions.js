(function () {
  function percent(value) {
    if (!(value > 0)) return "0%";
    if (value >= 1) return "100%";
    var points = value * 100;
    if (points < 0.1) return "<0,1%";
    if (points > 99.9) return ">99,9%";
    return points.toLocaleString("es-AR", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + "%";
  }

  function delta(value) {
    var points = value * 100;
    var text = Math.abs(points) < 0.05
      ? "0,0"
      : points.toLocaleString("es-AR", { minimumFractionDigits: 1, maximumFractionDigits: 1, signDisplay: "always" });
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

  document.querySelectorAll("[data-story-history]").forEach(function (root) {
    var jsonNode = root.querySelector("[data-history-json]");
    var svg = root.querySelector("[data-history-chart]");
    var tip = root.querySelector("[data-history-tip]");
    var roundSelect = root.querySelector("[data-history-round]");
    var moves = root.querySelector("[data-history-moves]");
    if (!jsonNode || !svg) return;

    var data;
    try {
      data = JSON.parse(jsonNode.textContent);
    } catch (err) {
      return;
    }

    var pad = { l: 36, r: 12, t: 16, b: 36 };
    var width = 360;
    var height = 210;
    var plotW = width - pad.l - pad.r;
    var plotH = height - pad.t - pad.b;
    var rounds = data.rounds || [];
    var series = data.series || [];
    var hidden = {};

    function xAt(index) {
      if (rounds.length <= 1) return pad.l + plotW / 2;
      return pad.l + (index * plotW) / (rounds.length - 1);
    }

    function yAt(value) {
      return pad.t + plotH * (1 - Math.max(0, Math.min(1, value || 0)));
    }

    function ns(name) {
      return document.createElementNS("http://www.w3.org/2000/svg", name);
    }

    function render() {
      while (svg.firstChild) svg.removeChild(svg.firstChild);

      [0, 0.25, 0.5, 0.75, 1].forEach(function (tick) {
        var y = yAt(tick);
        var line = ns("line");
        line.setAttribute("x1", pad.l);
        line.setAttribute("x2", width - pad.r);
        line.setAttribute("y1", y);
        line.setAttribute("y2", y);
        line.setAttribute("class", "v2-story-chart__grid");
        svg.appendChild(line);
        var label = ns("text");
        label.setAttribute("x", pad.l - 6);
        label.setAttribute("y", y + 3);
        label.setAttribute("text-anchor", "end");
        label.setAttribute("class", "v2-story-chart__ylabel");
        label.textContent = Math.round(tick * 100) + "%";
        svg.appendChild(label);
      });

      rounds.forEach(function (round, index) {
        var x = xAt(index);
        var label = ns("text");
        label.setAttribute("x", x);
        label.setAttribute("y", height - 10);
        label.setAttribute("text-anchor", "middle");
        label.setAttribute("class", "v2-story-chart__xlabel");
        label.textContent = "F" + round.round + (round.reconstructed ? "*" : "");
        svg.appendChild(label);
      });

      series.forEach(function (item) {
        if (hidden[item.id]) return;
        var liveStart = -1;
        for (var i = 0; i < rounds.length; i++) {
          if (!rounds[i].reconstructed) {
            liveStart = i;
            break;
          }
        }
        if (liveStart < 0) liveStart = rounds.length - 1;

        function appendPath(from, to, dashed) {
          var parts = [];
          var drawing = false;
          for (var i = from; i <= to; i++) {
            var value = item.points[i];
            if (value == null) {
              drawing = false;
              continue;
            }
            parts.push((drawing ? "L" : "M") + xAt(i).toFixed(1) + " " + yAt(value).toFixed(1));
            drawing = true;
          }
          if (!parts.length) return;
          var path = ns("path");
          path.setAttribute("d", parts.join(" "));
          path.setAttribute("fill", "none");
          path.setAttribute("stroke", item.color);
          path.setAttribute("stroke-width", "2.5");
          if (dashed) path.setAttribute("stroke-dasharray", "5 4");
          path.setAttribute("class", "v2-story-chart__line");
          svg.appendChild(path);
        }

        if (liveStart > 0) appendPath(0, liveStart, true);
        appendPath(liveStart, rounds.length - 1, false);

        item.points.forEach(function (value, index) {
          if (value == null) return;
          var circle = ns("circle");
          circle.setAttribute("cx", xAt(index));
          circle.setAttribute("cy", yAt(value));
          circle.setAttribute("r", rounds[index].reconstructed ? 3.2 : 4.2);
          circle.setAttribute("fill", rounds[index].reconstructed ? "#fff" : item.color);
          circle.setAttribute("stroke", item.color);
          circle.setAttribute("stroke-width", "2");
          circle.setAttribute("class", "v2-story-chart__dot");
          circle.setAttribute("data-series", item.id);
          circle.setAttribute("data-index", String(index));
          circle.addEventListener("pointerenter", function (event) {
            showTip(event, item, index, value);
          });
          circle.addEventListener("pointerleave", hideTip);
          circle.addEventListener("click", function () {
            if (roundSelect) {
              roundSelect.value = String(index);
              updateMoves(index);
            }
          });
          svg.appendChild(circle);
        });
      });
    }

    function showTip(event, item, index, value) {
      if (!tip) return;
      var prev = index > 0 ? item.points[index - 1] : null;
      var change = prev == null ? null : value - prev;
      tip.hidden = false;
      tip.replaceChildren();
      var name = document.createElement("strong");
      name.textContent = item.name;
      tip.appendChild(name);
      tip.appendChild(document.createElement("br"));
      tip.appendChild(
        document.createTextNode(
          "Fecha " + rounds[index].round + (rounds[index].reconstructed ? "*" : "")
        )
      );
      tip.appendChild(document.createElement("br"));
      tip.appendChild(
        document.createTextNode(percent(value) + (change == null ? "" : " · " + delta(change)))
      );
      var shell = root.querySelector(".v2-story-chart-shell");
      var rect = shell.getBoundingClientRect();
      tip.style.left = Math.min(rect.width - 140, Math.max(8, event.clientX - rect.left + 8)) + "px";
      tip.style.top = Math.max(8, event.clientY - rect.top - 48) + "px";
    }

    function hideTip() {
      if (tip) tip.hidden = true;
    }

    function updateMoves(index) {
      if (!moves) return;
      var prevIndex = Math.max(0, index - 1);
      series.forEach(function (item) {
        var row = moves.querySelector('[data-team="' + item.id + '"]');
        if (!row) return;
        var now = item.points[index];
        var before = item.points[prevIndex];
        var pctNode = row.querySelector("[data-move-pct]");
        var ppNode = row.querySelector("[data-move-pp]") || row.querySelector("b");
        if (now == null) {
          if (pctNode) pctNode.textContent = "–";
          if (ppNode) {
            ppNode.textContent = "–";
            ppNode.className = "";
          }
          return;
        }
        if (pctNode) pctNode.textContent = percent(now);
        if (!ppNode) return;
        if (before == null || index === 0) {
          ppNode.textContent = "–";
          ppNode.className = "";
          return;
        }
        var change = now - before;
        if (Math.abs(change * 100) < 0.05) {
          ppNode.textContent = "sin cambio";
          ppNode.className = "";
        } else {
          ppNode.textContent = delta(change);
          ppNode.className = change > 0 ? "is-up" : "is-down";
        }
      });
    }

    root.querySelectorAll("[data-series-toggle]").forEach(function (button) {
      button.addEventListener("click", function () {
        var id = button.getAttribute("data-series-toggle");
        hidden[id] = !hidden[id];
        button.setAttribute("aria-pressed", hidden[id] ? "false" : "true");
        button.parentElement.classList.toggle("is-off", !!hidden[id]);
        render();
      });
    });

    if (roundSelect) {
      roundSelect.addEventListener("change", function () {
        updateMoves(Number(roundSelect.value) || 0);
      });
    }

    render();
    updateMoves(rounds.length - 1);
  });
})();

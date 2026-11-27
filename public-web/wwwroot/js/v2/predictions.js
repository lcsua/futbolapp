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

  document.querySelectorAll("[data-story-if-joint]").forEach(function (root) {
    var node = root.querySelector("[data-joint-json]");
    if (!node) return;
    var data;
    try {
      data = JSON.parse(node.textContent);
    } catch (err) {
      return;
    }

    var options = {};
    (data.options || []).forEach(function (item) {
      options[item.id] = item;
    });
    var singles = {};
    (data.singles || []).forEach(function (item) {
      singles[item.teamId] = item;
    });
    var pairs = data.pairs || [];
    var slotA = root.querySelector('[data-joint-slot="a"]');
    var slotB = root.querySelector('[data-joint-slot="b"]');
    var state = {
      a: String(data.defaultA || ""),
      b: String(data.defaultB || ""),
      oa: "none",
      ob: "none"
    };

    function outcomeCode(value) {
      if (value === "win") return 0;
      if (value === "draw") return 1;
      if (value === "loss") return 2;
      return null;
    }

    function pointsDelta(code) {
      if (code === 0) return 3;
      if (code === 1) return 1;
      if (code === 2) return 0;
      return null;
    }

    function moveText(diff) {
      var abs = Math.abs(diff * 100);
      if (abs < 0.05) return { text: "Sin cambio", klass: "is-flat" };
      var points = abs.toLocaleString("es-AR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
      if (diff > 0) return { text: "▲ " + points + " puntos", klass: "is-up" };
      return { text: "▼ " + points + " puntos", klass: "is-down" };
    }

    function findPair(a, b) {
      for (var i = 0; i < pairs.length; i++) {
        var pair = pairs[i];
        if ((pair.a === a && pair.b === b) || (pair.a === b && pair.b === a)) return pair;
      }
      return null;
    }

    function cellFor(pair, a, b, oa, ob) {
      if (!pair) return null;
      var wantA = pair.a === a ? oa : ob;
      var wantB = pair.a === a ? ob : oa;
      for (var i = 0; i < pair.cells.length; i++) {
        var cell = pair.cells[i];
        if (cell.oa === wantA && cell.ob === wantB) {
          return {
            titleA: pair.a === a ? cell.titleA : cell.titleB,
            titleB: pair.a === a ? cell.titleB : cell.titleA,
            ok: cell.ok,
            others: (cell.others || []).map(function (other) {
              return { id: other.id, name: other.name, title: other.title };
            })
          };
        }
      }
      return null;
    }

    function mirror(code) {
      if (code === 1) return 1;
      if (code === 0) return 2;
      if (code === 2) return 0;
      return null;
    }

    function fillSelect(select, selected, blocked) {
      select.innerHTML = "";
      Object.keys(options).forEach(function (id) {
        if (id === blocked) return;
        var opt = document.createElement("option");
        opt.value = id;
        opt.textContent = options[id].name;
        if (id === selected) opt.selected = true;
        select.appendChild(opt);
      });
    }

    function paintSlot(slotEl, teamId, outcome) {
      var team = options[teamId];
      var crest = slotEl.querySelector("[data-joint-crest]");
      var vs = slotEl.querySelector("[data-joint-vs]");
      if (!team) return;
      if (crest) {
        if (team.logo) {
          crest.hidden = false;
          crest.src = team.logo;
          crest.alt = "";
        } else {
          crest.hidden = true;
          crest.removeAttribute("src");
        }
      }
      if (vs) vs.textContent = "vs. " + team.opponent + " · Fecha " + team.round;
      slotEl.querySelectorAll("[data-outcome]").forEach(function (button) {
        var value = button.getAttribute("data-outcome");
        button.setAttribute("aria-pressed", value === outcome ? "true" : "false");
        button.classList.toggle("is-win", value === "win" && value === outcome);
        button.classList.toggle("is-draw", value === "draw" && value === outcome);
        button.classList.toggle("is-loss", value === "loss" && value === outcome);
        button.classList.toggle("is-none", value === "none" && value === outcome);
      });
    }

    function renderSide(sideEl, team, title, current, afterPoints, defined) {
      sideEl.querySelector("[data-side-name]").textContent = team.name;
      sideEl.querySelector("[data-side-pct]").textContent = percent(title);
      var move = moveText(title - current);
      var deltaEl = sideEl.querySelector("[data-side-delta]");
      deltaEl.textContent = defined ? move.text : "";
      deltaEl.className = "v2-story-if__delta " + (defined ? move.klass : "is-flat");
      sideEl.querySelector("[data-side-now]").textContent = "Probabilidad actual: " + percent(current);
      sideEl.querySelector("[data-side-after]").textContent = defined
        ? afterPoints + " pts · Después de su partido"
        : team.points + " pts · Sin resultado fijado";
    }

    function summaryText(teamA, teamB, ptsA, ptsB, oa, ob) {
      if (oa == null && ob == null) {
        return "Elegí un resultado para ver cómo cambia la pelea por el título.";
      }
      if (ptsA === ptsB) {
        return teamA.name + " y " + teamB.name + " quedarían igualados en puntos.";
      }
      var leader = ptsA > ptsB ? teamA : teamB;
      var trail = ptsA > ptsB ? teamB : teamA;
      var gap = Math.abs(ptsA - ptsB);
      return leader.name + " quedaría " + gap + (gap === 1 ? " punto" : " puntos") + " por delante de " + trail.name + ".";
    }

    function apply() {
      var teamA = options[state.a];
      var teamB = options[state.b];
      if (!teamA || !teamB) return;

      paintSlot(slotA, state.a, state.oa);
      paintSlot(slotB, state.b, state.ob);

      var oa = outcomeCode(state.oa);
      var ob = outcomeCode(state.ob);
      var ptsA = teamA.points + (pointsDelta(oa) == null ? 0 : pointsDelta(oa));
      var ptsB = teamB.points + (pointsDelta(ob) == null ? 0 : pointsDelta(ob));
      var titleA = teamA.title;
      var titleB = teamB.title;
      var reliable = true;
      var others = [];
      var note = root.querySelector("[data-joint-note]");
      var othersEl = root.querySelector("[data-joint-others]");
      note.hidden = true;
      note.textContent = "";

      if (oa != null && ob != null) {
        var pair = findPair(state.a, state.b);
        var cell = cellFor(pair, state.a, state.b, oa, ob);
        if (!cell) {
          reliable = false;
          note.hidden = false;
          note.textContent = pair && pair.h2h
            ? "Esa combinación no es posible: los equipos se enfrentan entre sí."
            : "No hay estimación conjunta precalculada para este cruce.";
        } else if (!cell.ok) {
          reliable = false;
          note.hidden = false;
          note.textContent = "Hay poca muestra para este escenario; tomalo como orientación.";
          titleA = cell.titleA;
          titleB = cell.titleB;
          others = cell.others || [];
        } else {
          titleA = cell.titleA;
          titleB = cell.titleB;
          others = cell.others || [];
        }
      } else if (oa != null || ob != null) {
        var focusId = oa != null ? state.a : state.b;
        var focusCode = oa != null ? oa : ob;
        var otherId = oa != null ? state.b : state.a;
        var single = singles[focusId];
        var key = focusCode === 0 ? "win" : focusCode === 1 ? "draw" : "loss";
        var okKey = key + "Ok";
        if (!single || !single[okKey]) {
          reliable = false;
          note.hidden = false;
          note.textContent = "No hay muestra suficiente para estimar ese resultado con confianza.";
        } else {
          if (oa != null) titleA = single[key];
          else titleB = single[key];
          (single.rivals || []).forEach(function (rival) {
            if (rival.teamId === otherId) {
              if (oa != null) titleB = rival[key];
              else titleA = rival[key];
            } else {
              others.push({ id: rival.teamId, name: rival.name, title: rival[key], current: rival.current });
            }
          });
        }
      }

      var sideA = root.querySelector('[data-joint-side="a"]');
      var sideB = root.querySelector('[data-joint-side="b"]');
      renderSide(sideA, teamA, titleA, teamA.title, ptsA, oa != null || ob != null);
      renderSide(sideB, teamB, titleB, teamB.title, ptsB, oa != null || ob != null);
      sideA.classList.toggle("is-leader", titleA >= titleB);
      sideB.classList.toggle("is-leader", titleB > titleA);

      var total = Math.max(titleA + titleB, 0.0001);
      var bar = root.querySelector("[data-joint-bar]");
      if (bar) bar.style.width = ((titleA / total) * 100).toFixed(1) + "%";

      root.querySelector("[data-joint-summary]").textContent = summaryText(teamA, teamB, ptsA, ptsB, oa, ob);

      othersEl.innerHTML = "";
      if (others.length && (oa != null || ob != null) && reliable) {
        othersEl.hidden = false;
        others.forEach(function (other) {
          var li = document.createElement("li");
          var current = other.current != null ? other.current : (options[other.id] ? options[other.id].title : 0);
          var move = moveText(other.title - current);
          li.innerHTML = "<span>" + other.name + "</span><b>" + percent(other.title) + " · " + move.text + "</b>";
          othersEl.appendChild(li);
        });
      } else {
        othersEl.hidden = true;
      }
    }

    function syncHeadToHead(source) {
      var pair = findPair(state.a, state.b);
      if (!pair || !pair.h2h) return;
      if (source === "a" && state.oa !== "none") {
        var code = mirror(outcomeCode(state.oa));
        state.ob = code === 0 ? "win" : code === 1 ? "draw" : code === 2 ? "loss" : "none";
      } else if (source === "b" && state.ob !== "none") {
        var codeB = mirror(outcomeCode(state.ob));
        state.oa = codeB === 0 ? "win" : codeB === 1 ? "draw" : codeB === 2 ? "loss" : "none";
      } else if (source === "a" && state.oa === "none") {
        state.ob = "none";
      } else if (source === "b" && state.ob === "none") {
        state.oa = "none";
      }
    }

    function refreshSelects() {
      fillSelect(slotA.querySelector("[data-joint-team]"), state.a, state.b);
      fillSelect(slotB.querySelector("[data-joint-team]"), state.b, state.a);
    }

    function reset() {
      state.a = String(data.defaultA || "");
      state.b = String(data.defaultB || "");
      state.oa = "none";
      state.ob = "none";
      refreshSelects();
      apply();
    }

    slotA.querySelector("[data-joint-team]").addEventListener("change", function (event) {
      state.a = event.target.value;
      if (state.a === state.b) {
        var fallback = Object.keys(options).find(function (id) { return id !== state.a; });
        if (fallback) state.b = fallback;
      }
      state.oa = "none";
      state.ob = "none";
      refreshSelects();
      apply();
    });
    slotB.querySelector("[data-joint-team]").addEventListener("change", function (event) {
      state.b = event.target.value;
      if (state.b === state.a) {
        var fallback = Object.keys(options).find(function (id) { return id !== state.b; });
        if (fallback) state.a = fallback;
      }
      state.oa = "none";
      state.ob = "none";
      refreshSelects();
      apply();
    });

    slotA.querySelectorAll("[data-outcome]").forEach(function (button) {
      button.addEventListener("click", function () {
        state.oa = button.getAttribute("data-outcome");
        syncHeadToHead("a");
        apply();
      });
    });
    slotB.querySelectorAll("[data-outcome]").forEach(function (button) {
      button.addEventListener("click", function () {
        state.ob = button.getAttribute("data-outcome");
        syncHeadToHead("b");
        apply();
      });
    });

    var resetBtn = root.querySelector("[data-joint-reset]");
    if (resetBtn) resetBtn.addEventListener("click", reset);

    refreshSelects();
    apply();
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

    function paint() {
      render();
      updateMoves(rounds.length - 1);
    }

    // Re-paint when a collapsed <details> opens so the SVG gets a real layout box.
    var fold = root.closest("details");
    if (fold && !fold.open) {
      fold.addEventListener("toggle", function onToggle() {
        if (!fold.open) return;
        fold.removeEventListener("toggle", onToggle);
        requestAnimationFrame(function () {
          requestAnimationFrame(paint);
        });
      });
    } else {
      paint();
    }
  });
})();

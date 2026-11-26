/* Same ranking as FootballManager.Application.Standings.StandingsProjector.FromBaseline. */
(function (root, factory) {
  var api = factory();
  if (typeof module === 'object' && module.exports) {
    module.exports = api;
  }
  root.MiLigaStandings = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  var TIE = ['points', 'goalDifference', 'goalsFor'];

  function unsupportedReason(rules) {
    rules = rules || {};
    var win = Number(rules.winPoints);
    var draw = Number(rules.drawPoints);
    var loss = Number(rules.lossPoints);
    if (win !== 3 || draw !== 1 || loss !== 0) {
      return 'Este campeonato no usa la puntuación de 3 puntos por victoria y 1 por empate. El simulador no arma una tabla para no mostrar un orden incorrecto.';
    }
    var tie = rules.tieBreakers || [];
    if (tie.length !== 3 || tie[0] !== TIE[0] || tie[1] !== TIE[1] || tie[2] !== TIE[2]) {
      return 'Este campeonato usa un desempate que el simulador todavía no aplica. No mostramos una tabla proyectada para no inventar posiciones.';
    }
    return null;
  }

  function tryScore(homeGoals, awayGoals) {
    if (homeGoals === null || homeGoals === undefined || homeGoals === '') return null;
    if (awayGoals === null || awayGoals === undefined || awayGoals === '') return null;
    if (typeof homeGoals === 'string' && !/^\d{1,2}$/.test(homeGoals.trim())) return null;
    if (typeof awayGoals === 'string' && !/^\d{1,2}$/.test(awayGoals.trim())) return null;
    var home = Number(homeGoals);
    var away = Number(awayGoals);
    if (!Number.isInteger(home) || !Number.isInteger(away)) return null;
    if (home < 0 || away < 0 || home > 99 || away > 99) return null;
    return { home: home, away: away };
  }

  function key(id) {
    return String(id || '').toLowerCase();
  }

  function blank(id, name) {
    return {
      teamId: id,
      name: name || 'Equipo',
      position: 0,
      officialPosition: 0,
      played: 0,
      won: 0,
      drawn: 0,
      lost: 0,
      goalsFor: 0,
      goalsAgainst: 0,
      points: 0,
      baselinePoints: 0,
      seen: Number.MAX_SAFE_INTEGER
    };
  }

  function cloneOfficial(row) {
    var position = Number(row.position) || 0;
    return {
      teamId: row.teamId,
      name: row.name || 'Equipo',
      position: position,
      officialPosition: position,
      played: Number(row.played) || 0,
      won: Number(row.won) || 0,
      drawn: Number(row.drawn) || 0,
      lost: Number(row.lost) || 0,
      goalsFor: Number(row.goalsFor) || 0,
      goalsAgainst: Number(row.goalsAgainst) || 0,
      points: Number(row.points) || 0,
      baselinePoints: Number(row.points) || 0,
      seen: position > 0 ? position : Number.MAX_SAFE_INTEGER
    };
  }

  function apply(team, goalsFor, goalsAgainst, rules) {
    team.played += 1;
    team.goalsFor += goalsFor;
    team.goalsAgainst += goalsAgainst;
    if (goalsFor > goalsAgainst) {
      team.won += 1;
      team.points += rules.winPoints;
    } else if (goalsFor < goalsAgainst) {
      team.lost += 1;
      team.points += rules.lossPoints;
    } else {
      team.drawn += 1;
      team.points += rules.drawPoints;
    }
  }

  function compare(a, b) {
    if (b.points !== a.points) return b.points - a.points;
    var ag = a.goalsFor - a.goalsAgainst;
    var bg = b.goalsFor - b.goalsAgainst;
    if (bg !== ag) return bg - ag;
    if (b.goalsFor !== a.goalsFor) return b.goalsFor - a.goalsFor;
    if (a.seen !== b.seen) return a.seen - b.seen;
    return String(a.name).localeCompare(String(b.name), 'es', { sensitivity: 'base' });
  }

  function projectBaseline(officialRows, scores, rules) {
    var limitation = unsupportedReason(rules);
    if (limitation) {
      return { supported: false, limitation: limitation, simulated: 0, rows: [] };
    }

    var map = new Map();
    (officialRows || []).forEach(function (row) {
      map.set(key(row.teamId), cloneOfficial(row));
    });

    (rules.adjustments || []).forEach(function (adjustment) {
      var id = key(adjustment.teamId);
      var team = map.get(id);
      if (!team) {
        team = blank(adjustment.teamId, adjustment.name);
        map.set(id, team);
      }
      team.points += Number(adjustment.points) || 0;
    });

    var simulated = 0;
    (scores || []).forEach(function (score) {
      if (!score.homeCounts && !score.awayCounts) return;
      var parsed = tryScore(score.homeGoals, score.awayGoals);
      if (!parsed) return;
      simulated += 1;

      if (score.homeCounts) {
        var homeId = key(score.homeId);
        var home = map.get(homeId);
        if (!home) {
          home = blank(score.homeId, score.homeName);
          map.set(homeId, home);
        }
        apply(home, parsed.home, parsed.away, rules);
      }

      if (score.awayCounts) {
        var awayId = key(score.awayId);
        var away = map.get(awayId);
        if (!away) {
          away = blank(score.awayId, score.awayName);
          map.set(awayId, away);
        }
        apply(away, parsed.away, parsed.home, rules);
      }
    });

    var rows = Array.from(map.values()).sort(compare);
    rows.forEach(function (row, index) {
      row.position = index + 1;
      row.goalDifference = row.goalsFor - row.goalsAgainst;
      row.pointsDelta = row.points - row.baselinePoints;
    });

    return { supported: true, limitation: null, simulated: simulated, rows: rows };
  }

  return {
    unsupportedReason: unsupportedReason,
    tryScore: tryScore,
    projectBaseline: projectBaseline
  };
});

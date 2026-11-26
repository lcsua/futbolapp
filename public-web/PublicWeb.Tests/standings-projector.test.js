const assert = require('assert');
const { projectBaseline, tryScore, unsupportedReason } = require('../wwwroot/js/v2/standings-projector.js');

const A = '10000000-0000-0000-0000-000000000001';
const B = '10000000-0000-0000-0000-000000000002';
const C = '10000000-0000-0000-0000-000000000003';
const rules = {
  winPoints: 3,
  drawPoints: 1,
  lossPoints: 0,
  tieBreakers: ['points', 'goalDifference', 'goalsFor'],
  adjustments: []
};

function row(id, name, position, points, played, wins, draws, losses, gf, ga) {
  return {
    teamId: id,
    name,
    position,
    played,
    won: wins,
    drawn: draws,
    lost: losses,
    goalsFor: gf,
    goalsAgainst: ga,
    points
  };
}

function score(home, away, homeGoals, awayGoals, homeCounts = true, awayCounts = true) {
  return { homeId: home, homeName: 'Local', homeCounts, awayId: away, awayName: 'Visita', awayCounts, homeGoals, awayGoals };
}

const official = [
  row(A, 'Atlas', 1, 3, 1, 1, 0, 0, 1, 0),
  row(B, 'Boca', 2, 0, 1, 0, 0, 1, 0, 1)
];

assert.strictEqual(tryScore('', ''), null);
assert.strictEqual(tryScore('1', ''), null);
assert.strictEqual(tryScore('2', null), null);
assert.strictEqual(tryScore('-1', '0'), null);
assert.strictEqual(tryScore('1.5', '0'), null);
assert.deepStrictEqual(tryScore('0', '0'), { home: 0, away: 0 });

const untouched = projectBaseline(official, [score(A, B, '', '')], rules);
assert.strictEqual(untouched.simulated, 0);
assert.deepStrictEqual(untouched.rows.map((item) => item.teamId), [A, B]);
assert.strictEqual(untouched.rows[0].pointsDelta, 0);
assert.strictEqual(untouched.rows[0].points, 3);

const draw = projectBaseline(official, [score(A, B, 1, 1)], rules);
assert.strictEqual(draw.simulated, 1);
const atlas = draw.rows.find((item) => item.teamId === A);
assert.strictEqual(atlas.points, 4);
assert.strictEqual(atlas.played, 2);
assert.strictEqual(atlas.drawn, 1);
assert.strictEqual(atlas.goalsFor, 2);
assert.strictEqual(atlas.goalsAgainst, 1);
assert.strictEqual(atlas.goalDifference, 1);
assert.strictEqual(atlas.pointsDelta, 1);

const win = projectBaseline(official, [score(B, A, 2, 0)], rules);
assert.strictEqual(win.rows[0].teamId, B);
assert.strictEqual(win.rows[0].points, 3);
assert.strictEqual(win.rows[0].pointsDelta, 3);
assert.ok(win.rows[0].position < win.rows[0].officialPosition);

const partial = projectBaseline(
  [row(A, 'Atlas', 1, 0, 0, 0, 0, 0, 0, 0), row(B, 'Boca', 2, 0, 0, 0, 0, 0, 0, 0)],
  [score(A, C, 2, 0), score(B, C, '', '')],
  rules
);
assert.strictEqual(partial.simulated, 1);
assert.strictEqual(partial.rows.find((item) => item.teamId === A).points, 3);
assert.strictEqual(partial.rows.find((item) => item.teamId === B).played, 0);

const tied = projectBaseline(
  [row(B, 'Boca', 1, 4, 2, 1, 1, 0, 2, 2), row(A, 'Atlas', 2, 4, 2, 1, 1, 0, 2, 2)],
  [],
  rules
);
assert.deepStrictEqual(tied.rows.map((item) => item.teamId), [B, A]);

const sanctioned = projectBaseline(
  [row(A, 'Atlas', 1, 3, 1, 1, 0, 0, 1, 0), row(B, 'Boca', 2, 3, 1, 1, 0, 0, 1, 0)],
  [],
  { ...rules, adjustments: [{ teamId: A, points: -3 }] }
);
assert.strictEqual(sanctioned.rows[0].teamId, B);
assert.strictEqual(sanctioned.rows.find((item) => item.teamId === A).points, 0);

const blocked = projectBaseline(official, [score(A, B, 1, 0)], {
  ...rules,
  tieBreakers: ['points', 'headToHead']
});
assert.strictEqual(blocked.supported, false);
assert.strictEqual(blocked.rows.length, 0);
assert.ok(unsupportedReason(blocked && { winPoints: 2, drawPoints: 1, lossPoints: 0, tieBreakers: rules.tieBreakers }));

console.log('standings-projector: ok');

-- Clean "liga"/"ligas" tokens from public league slugs and keep uniqueness.
-- Safe to re-run: only updates rows whose slug would change.

WITH cleaned AS (
    SELECT
        id,
        slug AS old_slug,
        COALESCE(
            NULLIF(
                (
                    SELECT string_agg(tok, '-' ORDER BY ord)
                    FROM unnest(string_to_array(slug, '-')) WITH ORDINALITY AS u(tok, ord)
                    WHERE lower(tok) NOT IN ('liga', 'ligas')
                ),
                ''
            ),
            slug
        ) AS base_slug
    FROM leagues
    WHERE slug IS NOT NULL AND slug <> ''
),
ranked AS (
    SELECT
        id,
        old_slug,
        base_slug,
        row_number() OVER (PARTITION BY base_slug ORDER BY id) AS rn
    FROM cleaned
),
final AS (
    SELECT
        id,
        old_slug,
        CASE
            WHEN rn = 1 THEN base_slug
            ELSE base_slug || '-' || (rn - 1)::text
        END AS new_slug
    FROM ranked
)
UPDATE leagues l
SET slug = f.new_slug
FROM final f
WHERE l.id = f.id
  AND l.slug IS DISTINCT FROM f.new_slug;

-- Preview helper (optional):
-- SELECT id, name, slug FROM leagues ORDER BY name;

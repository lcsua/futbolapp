# SEO — MiLiga public site

## Architecture

Public pages are **server-rendered Razor** (`public-web`) against the anonymous API (`/api/public/...`).
Titles, descriptions, canonical, Open Graph and JSON-LD are emitted in the initial HTML (`Views/Shared/V2/_V2Layout.cshtml`).
They do **not** depend on client-side JS.

Canonical public origin (no PathBase):

```
Seo:PublicBaseUrl = https://miliga.com.ar
```

Internal PathBase (`/public-web`) is an ops detail. SEO URLs always use `PublicBaseUrl`.

## Key files

| Area | Location |
|------|----------|
| Options / URL builder | `public-web/Seo/` |
| Sitemap + robots | `SitemapDocumentService`, `Controllers/SeoController` |
| Copy (title/description) | `SeoCopy` |
| Layout metadata | `Views/Shared/V2/_V2Layout.cshtml` |
| Backend payload | `GET /api/public/sitemap` |

## Sitemap

- Public URL: `https://miliga.com.ar/sitemap.xml`
- Built dynamically from `GET /api/public/sitemap` (public + active leagues and team slugs)
- Also includes `/` and `/ligas`
- Argentine competitions: `/ligas/argentina/{slug}` when the pro API responds
- Includes `lastmod` only when `UpdatedAt` exists
- Does **not** invent `changefreq` / `priority`
- Soft budget ~40k URLs; index split endpoints are reserved under `/sitemaps/{name}.xml`

Example league URLs for Veteranos de Perico:

```
https://miliga.com.ar/ligas/veteranos-de-perico
https://miliga.com.ar/ligas/veteranos-de-perico/fixture
https://miliga.com.ar/ligas/veteranos-de-perico/posiciones
https://miliga.com.ar/ligas/veteranos-de-perico/resultados
https://miliga.com.ar/ligas/veteranos-de-perico/informacion
https://miliga.com.ar/ligas/veteranos-de-perico/{teamSlug}
```

## robots.txt

- `https://miliga.com.ar/robots.txt`
- Allows public crawling
- Disallows `/admin`, `/api/`, `/error/`, `/login`
- Declares `Sitemap: https://miliga.com.ar/sitemap.xml`

Nginx must expose `/sitemap.xml`, `/robots.txt` and `/sitemaps/` to the public-web app (see deploy workflow).

## Canonical + query strings

Filters (`season`, `division`, `round`, `fecha`, pagination, etc.) are treated as **UI state**.

Canonical examples:

| Request | Canonical |
|---------|-----------|
| `/ligas/veteranos-de-perico/fixture?season=clausura-2026&division=A` | `/ligas/veteranos-de-perico/fixture` |
| `/ligas/veteranos-de-perico/posiciones?division=A` | `/ligas/veteranos-de-perico/posiciones` |

Future option (not implemented): clean season URLs like `/ligas/{slug}/{season}/fixture`.

## Metadata

Page copy is centralized in `SeoCopy` and applied from each V2 view via `SeoPageApplicator`.

Open Graph: `og:title`, `og:description`, `og:url`, `og:type`, `og:image`, `og:site_name`, plus Twitter card basics.
Default image: `/branding/blue/icon-512.png`. League logo / team crest when available.

## Structured data

JSON-LD in layout:

- `WebSite` + `Organization` (site-wide)
- `BreadcrumbList` when `ViewBag.SeoBreadcrumbs` is set

Breadcrumb microdata was replaced by JSON-LD to avoid duplication.

## Indexability

Indexable: home, ligas, league sections, teams, información.
`noindex`: friendly 404 (`/error/404`).

## Redirects / host

- App middleware: lowercase paths + strip trailing slash (301)
- Apex HTTPS / www → `https://miliga.com.ar` must remain configured in **nginx** (not in the ASP.NET app)

## Google Analytics

Existing gtag `G-RDP3H0YFV9` and Clarity remain in `_V2Layout`. Do not duplicate tags.

## Google Search Console

1. Verify property `https://miliga.com.ar`
2. Submit sitemap `https://miliga.com.ar/sitemap.xml`

## Adding a new public page type

1. Add a `SeoCopy.*` factory (title, description, canonical path, breadcrumbs, H1)
2. Call `SeoPageApplicator.Apply(...)` in the Razor view
3. If the URL should be indexed, append it in `SitemapDocumentService.BuildEntriesAsync`
4. Keep a single logical H1

## Verify locally

```bash
dotnet test public-web/PublicWeb.Tests
curl -s http://localhost:5xxx/robots.txt
curl -s http://localhost:5xxx/sitemap.xml | head
curl -s http://localhost:5xxx/ligas/veteranos-de-perico | grep -E 'canonical|og:title|application/ld\+json'
```

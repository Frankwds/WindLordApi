# Task: Publish the "main" takeoff forecast snapshot to Supabase Storage

Instructions for the **WindLordApi (.NET worker)** agent. Read this fully before starting.
There is a companion frontend agent (the **WindAlert** Next.js app) that will consume what you
produce. A section at the end lists exactly what you must **report back** so the frontend can be
wired up.

---

## 1. Why this exists (context)

The WindAlert frontend renders a Google Map of **main** paragliding takeoffs, each with an embedded
short-range forecast. Today the browser asks a Vercel API route, which queries Supabase live, on a
~30 min client cache. An attempt to add a Vercel server-side cache was reverted because it
brotli-compressed a ~12 MB in-memory object at quality 11 on a weak serverless CPU (~55 s per refresh,
the triggering user waited the whole time, and it burned Vercel "fluid compute").

**New design:** *you* — the worker that already fetches upstream weather and continuously refreshes
`forecast_cache` (50 locations per batch, ~every 5 min, full round ~3 h) — also publish a
ready-to-serve, pre-compressed JSON **snapshot** of the main dataset to **Supabase Storage** after
each refresh. The browser downloads that static, CDN-cached file directly.

Result: no Vercel compute, no per-user DB egress for the map, freshness bounded by *your* publish
clock (not by traffic), and every user is served fast.

You own the **producer** side only: build the snapshot and upload it. You do **not** touch the frontend.

### Freshness model (important — read once)

- **Publish cadence (you → file):** ~every 5 min. The file is a *rolling* snapshot of the whole main
  set; on each publish ~50 locations are freshly updated and the rest carry their values from earlier
  in the ~3 h round. That is expected and correct.
- **Client cadence (file → browser):** the browser keeps the file for **30–60 min** before
  re-downloading. This is intentional — only a small fraction of forecasts change in that window, so
  re-downloading more often wastes bandwidth for no visible benefit.

Because of this, there is **no manifest and no versioned filenames** — a single stable file that you
overwrite is exactly right. Keep it simple.

---

## 2. What to produce — the data contract (MUST match exactly)

The frontend already consumes the result of the existing Supabase query
`getAllMainLocationsWithForecast`. Your snapshot must be **shape-compatible** with that result so the
frontend can swap its data source with zero downstream changes.

### 2.1 Selection rules

- Locations: **`is_active = true` AND `is_main = true`**.
- For each location, embed its `forecast_cache` rows where:
  - `time >= <generation instant, UTC now>` AND
  - `time <  <forecast range end>`
  - where **forecast range end = UTC midnight of *today* + 4 calendar days** (today through the next
    3 days inclusive, all hours). Reference implementation (TypeScript, runs in UTC on Vercel):
    ```
    end = new Date(now); end.setHours(0,0,0,0); end.setDate(end.getDate() + 4);
    ```
    Compute this in **UTC** to match current behavior. (The frontend re-groups hours by each
    location's own timezone for display, so the exact boundary is not safety-critical, but match this.)
- **Include locations even if they have zero forecast rows** in the window — emit `forecast_cache: []`.
  (The frontend renders such locations without a forecast; do not drop them.)
- Order is not relied upon.

### 2.2 JSON shape

Top-level: a **JSON array** of location objects. **Keys are snake_case and must match exactly.**
Use explicit DTOs to guarantee the contract regardless of your EF entity names — do **not** rely on
EF/JSON default casing.

Each location object:

| key | type | notes |
|---|---|---|
| `id` | string (uuid) | |
| `name` | string | |
| `latitude` | number | |
| `longitude` | number | |
| `altitude` | number | |
| `flightlog_id` | string | yes, a string even if numeric-looking |
| `timezone` | string | IANA tz, e.g. `Europe/Oslo` |
| `n` | boolean | wind-direction flag |
| `e` | boolean | |
| `s` | boolean | |
| `w` | boolean | |
| `ne` | boolean | |
| `se` | boolean | |
| `sw` | boolean | |
| `nw` | boolean | |
| `landing_latitude` | number \| null | optional; emit `null` if absent |
| `landing_longitude` | number \| null | optional; emit `null` if absent |
| `landing_altitude` | number \| null | optional; emit `null` if absent |
| `forecast_cache` | array | see below; `[]` if none |

Each `forecast_cache` element (nested array, one per hour):

| key | type | notes |
|---|---|---|
| `updated_at` | string (ISO 8601 UTC) | when this forecast row was written |
| `time` | string (ISO 8601 UTC) | the forecast valid time |
| `is_day` | number (`0` or `1`) | **number, not boolean** |
| `weather_code` | string | |
| `temperature` | number | |
| `wind_speed` | number | |
| `wind_gusts` | number \| null | |
| `wind_direction` | number | degrees |
| `landing_wind` | number \| null | |
| `landing_gust` | number \| null | |
| `landing_wind_direction` | number \| null | |

> Do **not** add extra fields (keep the payload lean). `is_main` is not required (every row is main);
> you may omit it. Do not include the heavier atmospheric/pressure-level columns — only the keys above.

### 2.3 Sample (decompressed snapshot)

```json
[
  {
    "id": "7b2c1e90-1111-4f22-9a01-abc123def456",
    "name": "Vetan",
    "latitude": 60.123,
    "longitude": 10.456,
    "altitude": 850,
    "flightlog_id": "1234",
    "timezone": "Europe/Oslo",
    "n": false, "e": true, "s": true, "w": false,
    "ne": true, "se": true, "sw": false, "nw": false,
    "landing_latitude": 60.118,
    "landing_longitude": 10.461,
    "landing_altitude": 300,
    "forecast_cache": [
      {
        "updated_at": "2026-06-08T12:00:00Z",
        "time": "2026-06-08T16:00:00Z",
        "is_day": 1,
        "weather_code": "clearsky_day",
        "temperature": 18.4,
        "wind_speed": 4.2,
        "wind_gusts": 7.1,
        "wind_direction": 210,
        "landing_wind": 2.1,
        "landing_gust": 3.4,
        "landing_wind_direction": 190
      }
    ]
  }
]
```

### 2.4 Assembling the array efficiently — keep it in memory, don't re-read the DB

Object storage has no partial update: every publish replaces the whole `main.json.gz`. But the *source*
for that file does **not** need to come from a fresh full DB read each time. Re-querying every main
location's forecast every ~5 min would pull the entire ~12 MB raw dataset over the Postgres wire
(Npgsql is uncompressed) ~288×/day — on the order of **~100 GB/month of DB egress**, re-introducing,
server-side, the very egress this whole change exists to avoid. (The worker does not do a full forecast
read today, so this would be *new* cost.)

Because the worker is a long-lived process and is the **sole writer** of `forecast_cache`, keep the
assembled main dataset **in memory** and patch it:

1. **On startup:** one full read to seed the in-memory main dataset (locations + in-window forecast).
2. **On each batch:** you already computed those ~50 locations' forecast in memory before writing them to
   the DB — merge those same values into the in-memory dataset for any that are `is_main`. No extra DB read.
3. **On each publish:** re-apply the `time >= now` window filter to every location's forecast array (drops
   hours that have become past) — pure in-memory work — then serialize → gzip → upload.
4. **Periodic reconcile (e.g. once per full round, or hourly):** one full read to pick up *metadata*
   changes the forecast pipeline doesn't see — new/removed main locations, `is_main` toggles, edited
   coords/landing/timezone (these can be changed via the frontend admin UI).

Mind thread-safety: the publish step reads the shared in-memory dataset while batches mutate it — guard
with a lock or snapshot-on-publish.

If this in-memory bookkeeping is more than you want right now, a full re-query per publish is *correct* and
simpler — just be aware of the egress cost above and confirm your Supabase plan has the headroom.

---

## 3. Compression (important — read carefully)

- The uncompressed JSON is large (~12 MB). You **must** store it **gzip-compressed** (~1 MB).
- **Do NOT rely on the HTTP `Content-Encoding` header.** Supabase Storage does not reliably persist
  or serve `Content-Encoding`, so the browser will **not** auto-decompress. The frontend decompresses
  **explicitly** via the browser `DecompressionStream('gzip')`.
- Therefore:
  - Compress the snapshot bytes with **gzip** (not brotli — the browser's `DecompressionStream`
    supports gzip/deflate, not brotli).
  - Upload the gzip bytes as the object body.
  - Set object **`Content-Type: application/gzip`** (so nothing tries to interpret it as text).
  - **Do not** set `Content-Encoding`.
- Compression level: `Optimal` is fine (runs on the VM's real CPU). gzip in .NET:
  `System.IO.Compression.GZipStream`. Serialize with `System.Text.Json` using explicit
  `[JsonPropertyName]` DTOs (recommended) — verify the exact keys in §2.2.

---

## 4. Where it goes — bucket & object

- **Bucket:** create a **public** bucket named **`forecast-snapshots`** (suggested; if you choose a
  different name, report it). Public is required so the browser can read without auth.
  - The "public" flag is a Supabase concept, not an S3 one. Easiest: create it in the
    **Supabase dashboard → Storage → New bucket → Public**. (Creating via the S3 `CreateBucket` call
    may produce a *private* bucket; if so, flip it to public via the dashboard.)
- **Single object key (stable, overwritten each publish):** `main.json.gz`
  - No versioning, no manifest, no cleanup. S3 `PutObject` overwrites atomically — readers always get
    either the old complete file or the new complete file, never a partial one.
- **Cache-Control** (set on upload; verify it is honored — see §7/§8):
  `public, max-age=300` — i.e. the CDN/browser may hold the file up to 5 min. This is a *backstop*:
  combined with the client's own 30–60 min gate, effective staleness stays within tolerance even if
  Supabase applies its own default instead. (Supabase also auto-invalidates the CDN cache when an
  object is overwritten — a bonus, but the design does not depend on it.)
- **Public URL** the frontend will use (note the host differs from the S3 upload host):
  `https://<project-ref>.supabase.co/storage/v1/object/public/forecast-snapshots/main.json.gz`

---

## 5. How to upload — S3-compatible API from .NET

Use the AWS SDK for .NET (`AWSSDK.S3`) pointed at Supabase's S3 endpoint.

- **Endpoint (upload host):** `https://<project-ref>.storage.supabase.co/storage/v1/s3`
  (note `.storage.supabase.co`, different from the public download host `.supabase.co`).
- **Region:** your project's region (e.g. `eu-north-1`) — find it on the dashboard S3 settings page.
- **Credentials:** generate **S3 access keys** in **Supabase dashboard → Storage → S3 Access** (Storage
  settings). These are server-side, full-access, and **different** from the Postgres connection string
  and from the service-role/anon API keys.
- **`ForcePathStyle = true`** is required.

Sketch (adapt to your DI/config conventions):

```csharp
using Amazon.S3;
using Amazon.S3.Model;

var s3 = new AmazonS3Client(
    accessKey, secretKey,
    new AmazonS3Config
    {
        ServiceURL = "https://<project-ref>.storage.supabase.co/storage/v1/s3",
        ForcePathStyle = true,
        AuthenticationRegion = "<project-region>", // e.g. "eu-north-1"
    });

await using var gz = BuildGzippedSnapshotStream(locations); // your code: serialize -> gzip -> MemoryStream

await s3.PutObjectAsync(new PutObjectRequest
{
    BucketName   = "forecast-snapshots",
    Key          = "main.json.gz",
    InputStream  = gz,
    ContentType  = "application/gzip",
    Headers      = { CacheControl = "public, max-age=300" },
    DisablePayloadSigning = true, // see note below
});
```

Notes / things to verify while wiring this up:

- Some S3-compatible providers reject `STREAMING-AWS4-HMAC-SHA256-PAYLOAD`. If `PutObject` fails with
  a signature/streaming error, set `DisablePayloadSigning = true` (shown) and/or buffer the body to a
  `MemoryStream` with a known length. Figure out which combination Supabase accepts and note it.
- Do not set ACLs/`CannedACL` — public access comes from the bucket's public flag, not per-object ACLs.

---

## 6. When to run — scheduling

- **Republish whenever main-location forecast data changes** — i.e. **after each batch that updates
  one or more `is_main` locations** (~every 5 min in practice). That keeps the file as fresh as the DB
  with no separate timer.
  - A simple **5-minute timer** is an acceptable fallback if it's easier in your architecture.
  - Skipping republish for batches that touched **no** main location avoids redundant uploads.
- **Publish once on worker startup**, so a fresh deploy/restart has a current file immediately.
- **Throttle:** never publish more than once per ~60 s.
- **Optional optimization:** hash the serialized JSON and skip the upload if it's identical to the last
  published hash (avoids redundant CDN churn when nothing main-relevant changed).
- Each publish is cheap: assemble from **in-memory state** (see §2.4 — avoid a full DB re-read per
  publish), gzip ~1 MB, one PUT.

---

## 7. Robustness

- **Atomic overwrite.** `PutObject` to the stable key is atomic per object, so there are no torn reads.
- **Failure handling.** If a publish fails, log and retry next cycle — the previous file stays in place
  and the frontend keeps serving it. Never leave a half-written or empty file.
- **Validate before publishing (recommended).** Skip the publish (and log loudly) if the assembled
  snapshot looks wrong — e.g. `0` locations, or a large drop vs the previous count — to avoid pushing a
  broken/empty map to users.

---

## 8. What you MUST figure out / decide yourself

- Your **project ref**, **region**, and generating the **S3 access keys** (dashboard).
- **Creating the public bucket** and confirming it is actually public (anonymous GET works).
- Mapping your **EF Core entities/queries** to the exact JSON contract in §2 (field names, types, null
  handling, the forecast time window). You know the schema; produce the contract.
- Exactly **where in the Worker** to hook the publish step (after a main-touching batch, or a timer)
  and how to schedule it per your existing patterns.
- The **S3 client quirks** for Supabase (payload signing, content-length/streaming) — settle on a
  `PutObject` configuration that works and note it.
- Whether Supabase **honors the `Cache-Control`** you set (verify via response headers; not critical,
  but report what you observe).
- Where to store the new secrets (follow your existing user-secrets/appsettings convention):
  `SUPABASE_S3_ENDPOINT`, `SUPABASE_S3_REGION`, `SUPABASE_S3_ACCESS_KEY`, `SUPABASE_S3_SECRET_KEY`,
  `SNAPSHOT_BUCKET`.

---

## 9. What to REPORT BACK (to the frontend agent)

After you have a working publish, hand back the following so the frontend can be wired and tested.
Please fill in this block:

```
SNAPSHOT PUBLISH — HANDOFF
- Public snapshot URL:    https://<ref>.supabase.co/storage/v1/object/public/forecast-snapshots/main.json.gz
- Bucket name:            forecast-snapshots            (or: __________)
- Bucket is public:       yes / no   (anonymous GET works: yes / no)
- Compression:            gzip, Content-Type=application/gzip, Content-Encoding NOT set
- Publish cadence:        after each main-touching batch (~5 min) / 5-min timer  (state which) + on startup

Observed response headers on a public GET of the snapshot (run e.g. `curl -I <snapshot URL>`):
- HTTP status:            __________ (200 expected)
- Content-Type:           __________ (application/gzip expected)
- Content-Encoding:       __________ (expected: absent)
- Cache-Control:          __________
- Content-Length:         __________ (~1 MB expected)

Contract conformance:
- Confirm the snapshot JSON matches §2 exactly. List ANY deviations (key names, casing, types,
  null vs missing, extra/missing fields, the forecast time window, locations-without-forecast handling).
- Anything ambiguous you had to decide, and what you chose.

Open questions / blockers for the frontend agent: __________
```

### Frontend side (FYI — not your task)

For your awareness so the contract makes sense: the browser will fetch the stable URL only when its
local IndexedDB copy is older than its TTL (30–60 min), then decompress the bytes with
`DecompressionStream('gzip')`, `JSON.parse`, and store them. So the two things that matter most from
you are: the **exact JSON shape (§2)** and that the file is **plain gzip bytes with no
`Content-Encoding` header (§3)**.

---

## 10. Acceptance checklist

- [ ] Public bucket exists; anonymous `GET .../forecast-snapshots/main.json.gz` returns 200.
- [ ] Body is gzip; decompresses to a JSON array matching §2 (validate a few records by hand).
- [ ] Array length is plausible (> 0); locations with no in-window forecast appear with `forecast_cache: []`.
- [ ] Served with `Content-Type: application/gzip` and **no** `Content-Encoding`.
- [ ] Re-publishes after main-touching batches (~5 min) and on startup; throttled; a failed publish
      leaves the previous file intact (never empty/partial).
- [ ] Handoff block in §9 filled in and returned.
```

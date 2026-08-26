# Media capability

Transverse MyClub capability for storing and serving binary media (images in v1). Product domains hold **references** (`MediaId` / Guid) only — they do not own files or storage technology.

Play’up composition root hosts the HTTP endpoints today; Play’up competition logos (`LogoUri` / `logoPath`) are **not** migrated to Media yet.

## Projects

| Project | Role |
| :------ | :--- |
| `MyClub.Media.Domain` | `MediaItem`, `MediaId`, allowlist / size policies |
| `MyClub.Media.Application` | `MediaService`, `IMediaRepository`, `IMediaStorage` |
| `MyClub.Media.Infrastructure` | `MediaDbContext` (schema `media`), `LocalFileMediaStorage`, DI |

## Configuration (Host)

| Key | Purpose |
| :-- | :------ |
| `ConnectionStrings:Media` | PostgreSQL for Media metadata (falls back to `ConnectionStrings:PlayUp` when omitted) |
| `Media:StorageRoot` | Local filesystem root for binaries (default `.local/media` under the Host content root; gitignored via `.local/`) |

Apply migrations:

```bash
dotnet ef database update --project src/Media/MyClub.Media.Infrastructure --startup-project src/PlayUp/MyClub.PlayUp.Host --context MediaDbContext
```

## HTTP contract (composed in PlayUp Host)

| Method | Route | Body / notes |
| :----- | :---- | :----------- |
| `POST` | `/media` | `multipart/form-data` with field `file` → `201` + metadata |
| `GET` | `/media/{id}` | Metadata JSON |
| `GET` | `/media/{id}/content` | Binary stream + `Content-Type` |
| `DELETE` | `/media/{id}` | `204` when deleted; `404` when missing |

Metadata JSON shape: `id`, `contentType`, `byteSize`, `originalName`, `createdAt`.

Failures use ProblemDetails with a `code` extension (e.g. `Media.InvalidContentType`, `Media.NotFound`).

## Orchestration rules

**Create**

1. Validate content type / size (`MediaItem.Create`)
2. Write bytes via `IMediaStorage`
3. Persist metadata via `IMediaRepository`
4. If step 3 fails after step 2 succeeded → best-effort delete of the stored file

**Delete** (PostgreSQL and filesystem are not one transaction)

1. Load metadata (404 if missing)
2. Delete metadata and save
3. Delete file — already missing = success; filesystem failure after metadata commit = `Media.StorageDeleteFailed` (orphan file; no GC in v1)

## Provisional policies (v1)

Not formal product Décisions yet:

- Allowed types: `image/png`, `image/jpeg`, `image/webp`
- Max size: 2 MiB
- Content is immutable (new upload = new `MediaId`)
- Public read by id (Host has no auth yet)
- No retention / soft-delete / thumbnails

## Decoupling note

`MediaId` lives in `MyClub.Media.Domain`. Future domains may store a Guid reference **without** taking a project dependency on Media until they need Media APIs.

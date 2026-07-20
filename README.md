# StrongKingJames

An open-source, fully local KJV Bible study tool. Browse the King James Version with
every word clickable and tagged with its Strong's number, jump straight into the
Hebrew and Greek lexicon, search by Strong's number or by meaning (semantic /
vector search), and ask questions of a local RAG chat assistant that answers with
verse citations. Everything runs on your own machine — PostgreSQL with pgvector for
storage and search, and [Ollama](https://ollama.com) on the host for embeddings and
chat. No cloud services, no API keys, no data leaving your computer.

## Features

- Full King James Version text with Strong's numbers on every original-language word.
- Click any word to open its Strong's Hebrew/Greek lexicon entry (lemma, transliteration, definition).
- Strong's-number search: find every verse that uses a given Strong's number.
- Semantic search: find verses by meaning using pgvector embeddings, not just keywords.
- Local RAG chat assistant: ask a question and get a streamed answer grounded in Scripture, with citations.
- 100% local and offline-capable (once models and data are downloaded) — nothing is sent to the cloud.
- One-command Docker startup, or a rich developer experience via .NET Aspire.

## Screenshots

_Add screenshots here._

<!-- e.g. ![Reader view](docs/screenshots/reader.png) -->

## Prerequisites

**To run (Docker):**

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Compose v2).
- [Ollama](https://ollama.com) installed and **running on the host** (not in Docker). Containers reach
  it via `host.docker.internal:11434`, so Ollama must listen on all interfaces:
  - Set `OLLAMA_HOST=0.0.0.0` before starting Ollama (on macOS/Windows set it in the Ollama app's
    environment; on Linux set it for the `ollama serve` process / systemd unit).
  - Pull the models used for embeddings and chat:

    ```bash
    ollama pull nomic-embed-text
    ollama pull llama3.1:8b
    ```

**Additionally, for development:**

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the build pins SDK `10.0.302` via `global.json`,
  `rollForward: latestFeature`).
- The Aspire CLI: `dotnet tool install --global aspire.cli`.

## Get the data

The Bible text and lexicon data are **not committed** to this repository (`data/*.xml` is gitignored).
Download the three public-domain / freely-licensed files into a `./data` directory at the repo root:

```bash
mkdir -p data

# KJV OSIS with Strong's numbers (public domain text)
curl -L -o data/kjv.xml \
  https://raw.githubusercontent.com/seven1m/open-bibles/master/eng-kjv.osis.xml

# Strong's Hebrew dictionary
curl -L -o data/strongshebrew.xml \
  https://raw.githubusercontent.com/openscriptures/strongs/master/hebrew/StrongHebrewG.xml

# Strong's Greek dictionary (zipped — extract the XML)
curl -L -o data/strongsgreek.zip \
  https://raw.githubusercontent.com/openscriptures/strongs/master/greek/StrongsGreekDictionaryXML_1.4.zip
unzip -p data/strongsgreek.zip '*.xml' > data/strongsgreek.xml
rm data/strongsgreek.zip
```

After this you should have `data/kjv.xml`, `data/strongshebrew.xml`, and `data/strongsgreek.xml`.

**Attribution & licensing:** The KJV text is in the **public domain**. The Strong's lemma/morphology
lexicon data from [openscriptures](https://github.com/openscriptures/strongs) is licensed
**CC-BY 4.0** — attribution is required if you redistribute it. Please retain this notice.

## Quick start (Docker)

With Ollama running on the host (models pulled) and the data files in `./data`:

```bash
docker compose up --build
```

This will:

1. Start **PostgreSQL** (pgvector) in a container with a persistent named volume.
2. Run the **importer** to completion — it applies migrations, seeds the books/verses/Strong's
   entries, then backfills vector embeddings by calling the host's Ollama.
3. Start the **web app** once the importer finishes, served at **http://localhost:8080**.

> **First run is slow.** The importer embeds ~31,000 verses through Ollama, which can take a while
> depending on your hardware. Subsequent runs reuse the database volume and skip already-embedded verses.

To stop: `docker compose down`. To also wipe the database: `docker compose down -v`.

## Developer start (Aspire)

For an interactive dev loop with the Aspire dashboard (logs, traces, resource graph):

```bash
aspire run --project src/StrongKingJames.AppHost
```

Aspire launches the PostgreSQL container, runs the importer to completion, and then serves the web
app — with the dashboard showing everything. **Ollama must be running on the host** (the AppHost
passes `http://localhost:11434` by default via the `ollama-url` parameter).

## Manual importer run (optional)

You can run the importer directly against any PostgreSQL instance:

```bash
dotnet run --project src/StrongKingJames.Importer -- \
  --osis data/kjv.xml \
  --hebrew data/strongshebrew.xml \
  --greek data/strongsgreek.xml \
  --connection "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres"
```

Flags: `--osis`, `--hebrew`, `--greek`, `--connection`, `--ollama-url`, `--embedding-model`.
The connection string and Ollama URL can also come from configuration/environment
(`ConnectionStrings__bible`, `Ollama__BaseUrl`).

## Architecture

.NET 10 solution orchestrated by [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/).

| Project | Role |
| --- | --- |
| `src/StrongKingJames.Core` | Domain models and services: Ollama embedding/chat clients, RAG pipeline. |
| `src/StrongKingJames.Data` | EF Core `DbContext`, migrations, PostgreSQL + pgvector access. |
| `src/StrongKingJames.Importer` | Console app: seeds the DB from the OSIS/lexicon XML and backfills embeddings, then exits. |
| `src/StrongKingJames.Web` | Blazor Server UI + minimal API endpoints (the app users interact with). |
| `src/StrongKingJames.AppHost` | Aspire AppHost — declares the Postgres container, the Ollama URL parameter, and wires up the importer and web app. |
| `src/StrongKingJames.ServiceDefaults` | Shared Aspire service defaults (telemetry, health checks, resilience). |
| `tests/*` | `Core.Tests`, `Importer.Tests` (unit), `Data.Tests` (integration, Testcontainers). |

**Data & runtime topology:** PostgreSQL (with the pgvector extension) stores books, verses, per-word
Strong's tags, lexicon entries, and verse embeddings. **Ollama runs on the host** and provides both
the embedding model (`nomic-embed-text`) and the chat model (`llama3.1:8b`). Containers reach Ollama
via `host.docker.internal:11434`; local dev uses `localhost:11434`. The base URL is read from the
`Ollama__BaseUrl` environment variable (Aspire injects it); model names come from the `Ollama`
section of `appsettings.json`.

**RAG chat flow:**

1. Embed the user's question with Ollama (`nomic-embed-text`).
2. Query pgvector for the top-k most similar verses (cosine distance).
3. Expand each hit with its neighboring verses for surrounding context.
4. Build a prompt from the retrieved passages and send it to Ollama's chat model (`llama3.1:8b`).
5. Stream the answer back to the browser with verse citations.

## Running tests

```bash
dotnet test
```

- `StrongKingJames.Core.Tests` and `StrongKingJames.Importer.Tests` are plain unit tests — no Docker needed.
- `StrongKingJames.Data.Tests` is an **integration** suite that spins up PostgreSQL via
  [Testcontainers](https://testcontainers.com), so it requires **Docker** to be running. It is run
  locally, not in CI.

To run only the unit suites:

```bash
dotnet test tests/StrongKingJames.Core.Tests
dotnet test tests/StrongKingJames.Importer.Tests
```

## Contributing

Contributions are welcome. Please open an issue to discuss substantial changes first. For pull
requests: keep the build warning-clean (warnings are treated as errors), add or update tests where it
makes sense, and make sure `dotnet build` and the unit test suites pass. The CI workflow
(`.github/workflows/ci.yml`) builds in Release and runs the Core and Importer test suites on every
push and pull request.

## License

Released under the [MIT License](LICENSE). Copyright (c) 2026 StrongKingJames contributors.

The KJV text is public domain; the openscriptures Strong's lexicon data is CC-BY 4.0 (see
**Get the data** above).

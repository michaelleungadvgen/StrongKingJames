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

## Initialize the project

Before you can run StrongKingJames for the first time, do these two things once.

### 1. Start Ollama and pull the models

[Ollama](https://ollama.com) must be installed and running **on the host** (not in Docker). Containers
reach it via `host.docker.internal:11434`, so Ollama must listen on all interfaces:

- Set `OLLAMA_HOST=0.0.0.0` before starting Ollama (on macOS/Windows set it in the Ollama app's
  environment; on Linux set it for the `ollama serve` process / systemd unit).
- Pull the models used for embeddings and chat:

  ```bash
  ollama pull nomic-embed-text
  ollama pull llama3.1:8b
  ```

### 2. Download the Bible data

The Bible text and lexicon data are **not committed** to this repository (`data/` is gitignored).
You need a KJV text **with Strong's numbers on every original-language word**, covering **both** the
Old Testament (Hebrew `H####`) and the New Testament (Greek `G####`), plus a matching lexicon.

The recommended source is the [1John419/kjs](https://github.com/1John419/kjs) JSON files, which carry
per-word Strong's tags for the whole Bible (≈412k tagged words, including ≈132k Greek tags in the NT)
and a single Hebrew+Greek lexicon. Download the three files into a `./data` directory at the repo root:

```bash
mkdir -p data

# KJV text + verse index (31,102 verses)
curl -L -o data/kjv_pure.json \
  https://raw.githubusercontent.com/1John419/kjs/master/json/kjv_pure.json

# Per-word Strong's numbers (joined to verses by index)
curl -L -o data/strong_pure.json \
  https://raw.githubusercontent.com/1John419/kjs/master/json/strong_pure.json

# Strong's Hebrew + Greek lexicon (one file, both testaments)
curl -L -o data/strong_dict.json \
  https://raw.githubusercontent.com/1John419/kjs/master/json/strong_dict.json
```

After this you should have `data/kjv_pure.json`, `data/strong_pure.json`, and `data/strong_dict.json`.
The importer defaults to this `kjs` format.

> **Why not the `eng-kjv.osis.xml` from seven1m/open-bibles?** That file is plain KJV text with
> **no Strong's tags at all**, so neither testament would get clickable Strong's words. The kjs
> source is used instead precisely because it tags both the OT and the NT.

**Attribution & licensing:** The KJV text is in the **public domain**. The
[1John419/kjs](https://github.com/1John419/kjs) data (text + Strong's tagging + lexicon) is
**GPL-3.0** © Clayton Carney. StrongKingJames is MIT-licensed and does **not** bundle or
redistribute the kjs data — you download it yourself at runtime as an input to the importer.
If you redistribute the kjs files yourself, you must comply with their GPL-3.0 terms.

### Alternative: OSIS sources

The importer also still supports an OSIS pipeline (`--format osis`) for users who already have a
Strong's-tagged KJV OSIS file. Note: the openscriptures Greek dictionary
(`StrongsGreekDictionaryXML_1.4`) is a non-OSIS DTD format that the OSIS parser does **not** load,
so the OSIS path currently gives Hebrew lexicon entries but no Greek lexicon entries — the `kjs`
path above is recommended for full OT+NT coverage.

```bash
mkdir -p data
curl -L -o data/kjv.xml \
  https://raw.githubusercontent.com/seven1m/open-bibles/master/eng-kjv.osis.xml
curl -L -o data/strongshebrew.xml \
  https://raw.githubusercontent.com/openscriptures/strongs/master/hebrew/StrongHebrewG.xml
```

## Run the project

Once Ollama is running, the models are pulled, and the `data/` files are in place, choose one of the
run options below. **First run is slow:** the importer embeds ~31,000 verses through Ollama, which
can take a while depending on your hardware. Subsequent runs reuse the database and skip
already-embedded verses.

### Option 1: Docker Compose (simplest)

```bash
docker compose up --build
```

This starts PostgreSQL, runs the importer to completion, then serves the web app at
**http://localhost:8080**.

To stop: `docker compose down`. To also wipe the database: `docker compose down -v`.

### Option 2: .NET Aspire (recommended for development)

For an interactive dev loop with the Aspire dashboard (logs, traces, resource graph):

```bash
aspire run --project src/StrongKingJames.AppHost
```

Aspire launches the PostgreSQL container, runs the importer to completion, and then serves the web
app — with the dashboard showing everything. **Ollama must be running on the host** (the AppHost
passes `http://localhost:11434` by default via the `ollama-url` parameter).

### Option 3: VS Code

Open the repo folder in VS Code. Make sure you have the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
and the .NET 10 SDK installed. Then:

1. Open a terminal in VS Code (`Ctrl+` `` ` ``).
2. Run the initialization steps above (Ollama + `data/` download).
3. Run Aspire from the terminal:

   ```bash
   aspire run --project src/StrongKingJames.AppHost
   ```

The Aspire dashboard opens automatically; the web app is served once the importer finishes.

### Option 4: Visual Studio 2022

Visual Studio 2022 must be **17.14+** to recognize the .NET 10 SDK. Open `StrongKingJames.slnx`,
set `StrongKingJames.AppHost` as the startup project, and press **F5**. The AppHost orchestrates
PostgreSQL, the importer, and the web app just like the command-line Aspire run.

## Manual importer run (optional)

You can run the importer directly against any PostgreSQL instance. With the kjs JSON files in
`./data` (the default `--format kjs`):

```bash
dotnet run --project src/StrongKingJames.Importer -- \
  --format kjs \
  --kjs-bible data/kjv_pure.json \
  --kjs-strongs data/strong_pure.json \
  --kjs-dict data/strong_dict.json \
  --connection "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres"
```

Or, with OSIS sources (`--format osis`):

```bash
dotnet run --project src/StrongKingJames.Importer -- \
  --format osis \
  --osis data/kjv.xml \
  --hebrew data/strongshebrew.xml \
  --greek data/strongsgreek.xml \
  --connection "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres"
```

Flags: `--format` (`kjs`|`osis`), `--kjs-bible`, `--kjs-strongs`, `--kjs-dict`, `--osis`,
`--hebrew`, `--greek`, `--connection`, `--ollama-url`, `--embedding-model`.
The connection string and Ollama URL can also come from configuration/environment
(`ConnectionStrings__bible`, `Ollama__BaseUrl`).

## Architecture

.NET 10 solution orchestrated by [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/).

| Project | Role |
| --- | --- |
| `src/StrongKingJames.Core` | Domain models and services: Ollama embedding/chat clients, RAG pipeline. |
| `src/StrongKingJames.Data` | EF Core `DbContext`, migrations, PostgreSQL + pgvector access. |
| `src/StrongKingJames.Importer` | Console app: seeds the DB from kjs JSON (or OSIS) and backfills embeddings, then exits. |
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

## Credits

StrongKingJames is copyright **AdvanGeneration Pty Ltd** and is released under the MIT License.
The project depends on many open-source libraries and data sets. See [CREDITS.md](CREDITS.md) for
the full list of third-party software, infrastructure, and Bible data sources, along with their
respective licenses.

## License

Released under the [MIT License](LICENSE).

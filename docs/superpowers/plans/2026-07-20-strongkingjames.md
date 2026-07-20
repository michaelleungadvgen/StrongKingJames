# StrongKingJames Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an open-source KJV Bible study tool with Strong's-number concordance, semantic search, and a local RAG chat assistant — Blazor Server UI over PostgreSQL/pgvector and Ollama.

**Architecture:** A .NET 10 solution layered as Core (domain + interfaces), Data (EF Core + Npgsql + pgvector), Importer (console seeder), and Web (Blazor Server + minimal APIs), all orchestrated by **.NET Aspire**. An Aspire AppHost declares every resource — a pgvector PostgreSQL container, the importer (run-to-completion), and the web app — with connection strings and endpoints injected automatically via service discovery. **Ollama runs on the host** (the user already has it installed and models pulled); Aspire references it as an external endpoint rather than starting a container for it. The browse view is reconstructed word-by-word from `verse_words` so every tagged word is interactive. Ollama sits behind `IEmbeddingService`/`IChatService` interfaces so all pipeline logic is testable without a live model. The .NET services and PostgreSQL are containerized: Aspire runs them in dev, and `aspire publish` generates a Docker Compose file so db + importer + web come up with one command, all pointing at the host's Ollama.

**Tech Stack:** .NET 10, .NET Aspire (AppHost + ServiceDefaults), C#, ASP.NET Core Blazor Server, EF Core 10 + Npgsql, pgvector / `pgvector-dotnet`, host-installed Ollama (`nomic-embed-text`, `llama3.1`) referenced as an external Aspire resource, xUnit, Testcontainers, Docker / Docker Compose (Aspire-generated), GitHub Actions.

**Reference spec:** `docs/superpowers/specs/2026-07-20-strongkingjames-design.md`

---

## Prerequisites (verify before Task 1)

The implementer needs these installed locally:
- .NET 10 SDK (`dotnet --version` → 10.x)
- Docker Desktop, running (`docker ps` works) — Aspire runs all dependencies as containers, and Testcontainers needs it too
- The Aspire CLI (`dotnet tool install -g Aspire.Cli`, then `aspire --version`) — used to run the AppHost and to publish the Docker Compose file
- **Ollama running on the host** with the models pulled (the user already has Ollama installed):
  - `ollama pull nomic-embed-text`
  - `ollama pull llama3.1`
  Aspire and the containers reach it at `http://localhost:11434` in dev and `http://host.docker.internal:11434` from inside containers. No Ollama container is started.

Source data files (downloaded once, paths passed to the importer — NOT committed):
- KJV OSIS XML from openscriptures: `kjv.xml`
- Strong's Hebrew dictionary XML: `strongshebrew.xml`
- Strong's Greek dictionary XML: `strongsgreek.xml`

The README (Task 25) documents exact download URLs.

---

## File Structure

```
StrongKingJames.sln
Directory.Build.props                      # shared: net10.0, nullable enable, langversion
.gitignore
.editorconfig
LICENSE                                     # MIT
README.md
.github/workflows/ci.yml

src/
  StrongKingJames.AppHost/                  # .NET Aspire orchestration (entry point for dev)
    AppHost.cs
    StrongKingJames.AppHost.csproj
  StrongKingJames.ServiceDefaults/          # Aspire shared defaults (health, telemetry, resilience)
    Extensions.cs

  StrongKingJames.Core/
    Models/Book.cs
    Models/Verse.cs
    Models/VerseWord.cs
    Models/StrongsEntry.cs
    Models/SearchResult.cs
    Models/SearchMode.cs
    Models/ChatMessage.cs
    Models/RetrievedPassage.cs
    Services/IEmbeddingService.cs
    Services/IChatService.cs
    Services/ISearchService.cs
    Services/IBibleRepository.cs
    Services/IRagService.cs
    Search/SearchModeDetector.cs
    Rag/RagPromptBuilder.cs
    Rag/CitationExtractor.cs

  StrongKingJames.Data/
    BibleDbContext.cs
    Configurations/BookConfiguration.cs
    Configurations/VerseConfiguration.cs
    Configurations/VerseWordConfiguration.cs
    Configurations/StrongsEntryConfiguration.cs
    Configurations/VerseEmbeddingConfiguration.cs
    Entities/VerseEmbedding.cs
    Repositories/BibleRepository.cs
    Repositories/SearchService.cs
    Migrations/                             # EF-generated
    DependencyInjection.cs

  StrongKingJames.Importer/
    Program.cs
    ImporterOptions.cs
    Parsing/OsisParser.cs
    Parsing/StrongsDictionaryParser.cs
    Seeding/DatabaseSeeder.cs
    Seeding/EmbeddingBackfiller.cs

  StrongKingJames.Web/
    Program.cs
    appsettings.json
    Ollama/OllamaEmbeddingService.cs
    Ollama/OllamaChatService.cs
    Ollama/OllamaOptions.cs
    Rag/RagService.cs
    Endpoints/ApiEndpoints.cs
    Components/App.razor
    Components/Routes.razor
    Components/_Imports.razor
    Components/Layout/MainLayout.razor
    Components/Pages/Browse.razor
    Components/Pages/Search.razor
    Components/Pages/Chat.razor
    Components/Shared/VerseView.razor
    Components/Shared/StrongsPopover.razor
    Components/Shared/OllamaStatusBanner.razor

tests/
  StrongKingJames.Core.Tests/
    SearchModeDetectorTests.cs
    RagPromptBuilderTests.cs
    CitationExtractorTests.cs
  StrongKingJames.Importer.Tests/
    OsisParserTests.cs
    StrongsDictionaryParserTests.cs
    TestData/                               # tiny XML fixtures
  StrongKingJames.Data.Tests/
    DatabaseFixture.cs                      # Testcontainers pgvector
    BibleRepositoryTests.cs
    SearchServiceTests.cs
```

---

## Phase 1 — Solution scaffold

### Task 1: Create the solution and shared build config

**Files:**
- Create: `StrongKingJames.sln`, `Directory.Build.props`, `.gitignore`, `.editorconfig`

- [ ] **Step 1: Create solution and directories**

Run:
```bash
dotnet new sln -n StrongKingJames
```

- [ ] **Step 2: Add `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Add a .NET `.gitignore`**

Run:
```bash
dotnet new gitignore
```

Then append these lines so source data and local secrets are never committed:
```
# Bible source data (public domain, downloaded separately)
data/*.xml
*.user
```

- [ ] **Step 4: Add `.editorconfig`** with basic C# conventions (4-space indent, `file_header_template` off, `dotnet_style_namespace_declarations = file_scoped`).

- [ ] **Step 5: Verify and commit**

Run: `dotnet build`
Expected: succeeds (empty solution, no projects yet — "Build succeeded").

```bash
git add -A
git commit -m "chore: scaffold solution and shared build config"
```

---

### Task 2: Create all projects and wire references

**Files:**
- Create: the `.csproj` for each project listed in File Structure.

- [ ] **Step 1: Create projects**

Run:
```bash
dotnet new classlib -o src/StrongKingJames.Core
dotnet new classlib -o src/StrongKingJames.Data
dotnet new console  -o src/StrongKingJames.Importer
dotnet new blazor   -o src/StrongKingJames.Web --interactivity Server --empty
dotnet new xunit    -o tests/StrongKingJames.Core.Tests
dotnet new xunit    -o tests/StrongKingJames.Importer.Tests
dotnet new xunit    -o tests/StrongKingJames.Data.Tests
```

- [ ] **Step 2: Add all projects to the solution**

Run:
```bash
dotnet sln add (Get-ChildItem -Recurse *.csproj)
```
(PowerShell; on bash use `dotnet sln add $(find . -name '*.csproj')`.)

- [ ] **Step 3: Wire project references**

Run:
```bash
dotnet add src/StrongKingJames.Data reference src/StrongKingJames.Core
dotnet add src/StrongKingJames.Importer reference src/StrongKingJames.Core src/StrongKingJames.Data
dotnet add src/StrongKingJames.Web reference src/StrongKingJames.Core src/StrongKingJames.Data
dotnet add tests/StrongKingJames.Core.Tests reference src/StrongKingJames.Core
dotnet add tests/StrongKingJames.Importer.Tests reference src/StrongKingJames.Importer src/StrongKingJames.Core
dotnet add tests/StrongKingJames.Data.Tests reference src/StrongKingJames.Data src/StrongKingJames.Core
```

- [ ] **Step 4: Add NuGet packages**

Run:
```bash
dotnet add src/StrongKingJames.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/StrongKingJames.Data package Pgvector.EntityFrameworkCore
dotnet add src/StrongKingJames.Data package Microsoft.EntityFrameworkCore.Design
dotnet add src/StrongKingJames.Data.Tests package Testcontainers.PostgreSql
dotnet add src/StrongKingJames.Data.Tests package Microsoft.EntityFrameworkCore.Design
```
(Use the latest versions compatible with .NET 10 / EF Core 10.)

- [ ] **Step 5: Verify and commit**

Run: `dotnet build`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "chore: create projects and wire references"
```

---

### Task 2B: Add .NET Aspire orchestration (AppHost + ServiceDefaults)

**Files:**
- Create: `src/StrongKingJames.AppHost/`, `src/StrongKingJames.ServiceDefaults/`

This makes Aspire the dev entry point and the source of every connection string / endpoint. It also enables `aspire publish` for the fully-Dockerized release (Task 25).

- [ ] **Step 1: Create the Aspire projects**

Run:
```bash
dotnet new aspire-apphost -o src/StrongKingJames.AppHost
dotnet new aspire-servicedefaults -o src/StrongKingJames.ServiceDefaults
dotnet sln add src/StrongKingJames.AppHost src/StrongKingJames.ServiceDefaults
```
(If these templates are missing, install them: `dotnet new install Aspire.ProjectTemplates`.)

- [ ] **Step 2: Reference ServiceDefaults from Web and Importer**

Run:
```bash
dotnet add src/StrongKingJames.Web reference src/StrongKingJames.ServiceDefaults
dotnet add src/StrongKingJames.Importer reference src/StrongKingJames.ServiceDefaults
```

- [ ] **Step 3: Add AppHost references + Aspire hosting integrations**

Run:
```bash
dotnet add src/StrongKingJames.AppHost reference src/StrongKingJames.Web src/StrongKingJames.Importer
dotnet add src/StrongKingJames.AppHost package Aspire.Hosting.PostgreSQL
```
(Use latest versions compatible with the installed Aspire. No Ollama hosting package — Ollama is external.)

- [ ] **Step 4: Write `AppHost.cs`** — declare pgvector, an external Ollama endpoint, importer (run-to-completion), and web.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with the pgvector image, persistent volume.
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg16")
    .WithDataVolume();
var bibleDb = postgres.AddDatabase("bible", databaseName: "strongkingjames");

// Ollama runs on the HOST — reference it as an external endpoint, not a container.
// A parameter (with a default) lets a container reach the host via host.docker.internal.
var ollamaUrl = builder.AddParameter("ollama-url", "http://localhost:11434");

// Importer: runs to completion, seeds DB and backfills embeddings, then exits.
var importer = builder.AddProject<Projects.StrongKingJames_Importer>("importer")
    .WithReference(bibleDb).WaitFor(bibleDb)
    .WithEnvironment("Ollama__BaseUrl", ollamaUrl);

// Web app: waits for the importer to finish seeding before serving.
builder.AddProject<Projects.StrongKingJames_Web>("web")
    .WithReference(bibleDb).WaitFor(bibleDb)
    .WithEnvironment("Ollama__BaseUrl", ollamaUrl)
    .WaitForCompletion(importer);

builder.Build().Run();
```

Notes for the engineer:
- `WithReference(bibleDb)` injects `ConnectionStrings__bible`. The Ollama base URL is passed as the `Ollama__BaseUrl` environment variable (bound by `OllamaOptions`, Task 16/20). The embedding/chat model names stay in the app's config defaults (Task 20 appsettings).
- For the published Compose (Task 25), set the `ollama-url` parameter to `http://host.docker.internal:11434` so the db/importer/web containers reach the host's Ollama. In dev (`aspire run`) the `http://localhost:11434` default works because the projects run on the host.

- [ ] **Step 5: Wire ServiceDefaults into Web**

In `src/StrongKingJames.Web/Program.cs`, immediately after `CreateBuilder`, add `builder.AddServiceDefaults();` and before `app.Run()` add `app.MapDefaultEndpoints();`. (Task 20 assumes this is present.)

- [ ] **Step 6: Verify build**

Run: `dotnet build src/StrongKingJames.AppHost`
Expected: "Build succeeded". (Full run happens in Task 20/25 once Web and Importer exist.)

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "chore(aspire): add AppHost and ServiceDefaults orchestration"
```

---

## Phase 2 — Core domain models

### Task 3: Define domain models and enums

**Files:**
- Create: all files under `src/StrongKingJames.Core/Models/`

- [ ] **Step 1: Create the model records/classes**

`SearchMode.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public enum SearchMode { Auto, Reference, Strongs, Semantic }
```

`Book.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public class Book
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Testament { get; set; } = ""; // "OT" or "NT"
    public int SortOrder { get; set; }
    public List<Verse> Verses { get; set; } = [];
}
```

`Verse.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public class Verse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int Chapter { get; set; }
    public int VerseNumber { get; set; }
    public string OsisId { get; set; } = ""; // e.g. "Gen.1.1"
    public string Text { get; set; } = "";    // plain concatenated text
    public List<VerseWord> Words { get; set; } = [];
}
```

`VerseWord.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public class VerseWord
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public int Position { get; set; }
    public string WordText { get; set; } = "";
    public string? StrongsNumber { get; set; } // null for untagged tokens/punctuation
}
```

`StrongsEntry.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public class StrongsEntry
{
    public string Number { get; set; } = ""; // PK, e.g. "H7225"
    public string Lemma { get; set; } = "";
    public string Transliteration { get; set; } = "";
    public string Pronunciation { get; set; } = "";
    public string Definition { get; set; } = "";
    public string KjvUsage { get; set; } = "";
}
```

`SearchResult.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public record SearchResult(
    int VerseId,
    string OsisId,
    string Reference, // e.g. "John 3:16"
    string Text,
    double? Score);   // similarity score for semantic; null otherwise
```

`RetrievedPassage.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public record RetrievedPassage(
    string Reference,     // "John 3:16"
    string Text,          // the hit plus expanded neighbors, joined
    double Score);
```

`ChatMessage.cs`:
```csharp
namespace StrongKingJames.Core.Models;

public record ChatMessage(string Role, string Content); // Role: "system" | "user" | "assistant"
```

- [ ] **Step 2: Verify and commit**

Run: `dotnet build src/StrongKingJames.Core`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat(core): add domain models"
```

---

### Task 4: Define service interfaces

**Files:**
- Create: all files under `src/StrongKingJames.Core/Services/`

- [ ] **Step 1: Create the interfaces**

`IEmbeddingService.cs`:
```csharp
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
```

`IChatService.cs`:
```csharp
namespace StrongKingJames.Core.Services;

public interface IChatService
{
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<StrongKingJames.Core.Models.ChatMessage> messages,
        CancellationToken ct = default);
}
```

`IBibleRepository.cs`:
```csharp
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface IBibleRepository
{
    Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Verse>> GetChapterAsync(string bookAbbrev, int chapter, CancellationToken ct = default);
    Task<StrongsEntry?> GetStrongsEntryAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResult>> GetVersesByStrongsAsync(string number, CancellationToken ct = default);
    Task<Verse?> GetVerseByReferenceAsync(string bookAbbrev, int chapter, int verse, CancellationToken ct = default);
    Task<IReadOnlyList<Verse>> GetNeighborsAsync(int verseId, int radius, CancellationToken ct = default);
}
```

`ISearchService.cs`:
```csharp
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default);
}
```

`IRagService.cs`:
```csharp
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Services;

public interface IRagService
{
    IAsyncEnumerable<string> AnswerAsync(string question, CancellationToken ct = default);
}
```

- [ ] **Step 2: Verify and commit**

Run: `dotnet build src/StrongKingJames.Core`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat(core): add service interfaces"
```

---

## Phase 3 — Pure logic (TDD)

### Task 5: SearchModeDetector — reference detection

**Files:**
- Create: `src/StrongKingJames.Core/Search/SearchModeDetector.cs`
- Test: `tests/StrongKingJames.Core.Tests/SearchModeDetectorTests.cs`

The detector classifies a raw query string into a `SearchMode` when `Auto` is requested. Rules (checked in order):
1. Matches a Strong's pattern `^[HG]\d{1,5}$` (case-insensitive) → `Strongs`.
2. Matches a reference pattern (book name/abbrev + chapter, optional `:verse`) → `Reference`.
3. Otherwise → `Semantic`.

- [ ] **Step 1: Write failing tests**

```csharp
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Search;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class SearchModeDetectorTests
{
    [Theory]
    [InlineData("H7225", SearchMode.Strongs)]
    [InlineData("g26", SearchMode.Strongs)]
    [InlineData("G0026", SearchMode.Strongs)]
    [InlineData("John 3:16", SearchMode.Reference)]
    [InlineData("1 John 4", SearchMode.Reference)]
    [InlineData("Gen 1:1", SearchMode.Reference)]
    [InlineData("Genesis 1", SearchMode.Reference)]
    [InlineData("what does the bible say about love", SearchMode.Semantic)]
    [InlineData("forgiveness", SearchMode.Semantic)]
    public void Detect_classifies_query(string query, SearchMode expected)
    {
        Assert.Equal(expected, SearchModeDetector.Detect(query));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter SearchModeDetectorTests`
Expected: FAIL — `SearchModeDetector` does not exist.

- [ ] **Step 3: Implement**

```csharp
using System.Text.RegularExpressions;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Search;

public static partial class SearchModeDetector
{
    [GeneratedRegex(@"^[HGhg]\d{1,5}$")]
    private static partial Regex StrongsRegex();

    // Book name (letters, optional leading number like "1 John") then chapter, optional :verse
    [GeneratedRegex(@"^(?:[1-3]\s+)?[A-Za-z]+(?:\s+[A-Za-z]+)?\s+\d{1,3}(?::\d{1,3})?$")]
    private static partial Regex ReferenceRegex();

    public static SearchMode Detect(string query)
    {
        var q = query.Trim();
        if (StrongsRegex().IsMatch(q)) return SearchMode.Strongs;
        if (ReferenceRegex().IsMatch(q)) return SearchMode.Reference;
        return SearchMode.Semantic;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter SearchModeDetectorTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(core): search mode detector with tests"
```

---

### Task 6: RagPromptBuilder

**Files:**
- Create: `src/StrongKingJames.Core/Rag/RagPromptBuilder.cs`
- Test: `tests/StrongKingJames.Core.Tests/RagPromptBuilderTests.cs`

Builds the `ChatMessage` list: a system message with the instructions, plus a user message embedding the passages and the question.

- [ ] **Step 1: Write failing tests**

```csharp
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Rag;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class RagPromptBuilderTests
{
    [Fact]
    public void Build_includes_system_user_and_passages_and_question()
    {
        var passages = new[]
        {
            new RetrievedPassage("John 3:16", "For God so loved the world...", 0.9),
            new RetrievedPassage("1 John 4:8", "God is love.", 0.8),
        };

        var messages = RagPromptBuilder.Build("What is love?", passages);

        Assert.Equal(2, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Contains("cite", messages[0].Content, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal("user", messages[1].Role);
        Assert.Contains("John 3:16", messages[1].Content);
        Assert.Contains("God is love.", messages[1].Content);
        Assert.Contains("What is love?", messages[1].Content);
    }

    [Fact]
    public void Build_with_no_passages_still_produces_user_message()
    {
        var messages = RagPromptBuilder.Build("anything", System.Array.Empty<RetrievedPassage>());
        Assert.Equal(2, messages.Count);
        Assert.Contains("anything", messages[1].Content);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter RagPromptBuilderTests`
Expected: FAIL — type missing.

- [ ] **Step 3: Implement**

```csharp
using System.Text;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Core.Rag;

public static class RagPromptBuilder
{
    private const string SystemPrompt =
        "You are a Bible study assistant. Answer the user's question using ONLY the " +
        "scripture passages provided below. Cite every claim with its reference in the " +
        "form Book chapter:verse (e.g. John 3:16). If the passages do not answer the " +
        "question, say so plainly and do not invent scripture.";

    public static IReadOnlyList<ChatMessage> Build(
        string question, IReadOnlyList<RetrievedPassage> passages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Passages:");
        foreach (var p in passages)
        {
            sb.AppendLine($"[{p.Reference}] {p.Text}");
        }
        sb.AppendLine();
        sb.AppendLine($"Question: {question}");

        return
        [
            new ChatMessage("system", SystemPrompt),
            new ChatMessage("user", sb.ToString()),
        ];
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter RagPromptBuilderTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(core): RAG prompt builder with tests"
```

---

### Task 7: CitationExtractor

**Files:**
- Create: `src/StrongKingJames.Core/Rag/CitationExtractor.cs`
- Test: `tests/StrongKingJames.Core.Tests/CitationExtractorTests.cs`

Extracts distinct `Book chapter:verse` citations from generated answer text, preserving order of first appearance. Used by the UI to render citation links.

- [ ] **Step 1: Write failing tests**

```csharp
using StrongKingJames.Core.Rag;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class CitationExtractorTests
{
    [Fact]
    public void Extract_finds_references_in_order_without_duplicates()
    {
        var text = "Love is central (John 3:16). See also 1 John 4:8 and again John 3:16.";
        var refs = CitationExtractor.Extract(text);
        Assert.Equal(new[] { "John 3:16", "1 John 4:8" }, refs);
    }

    [Fact]
    public void Extract_returns_empty_when_none()
    {
        Assert.Empty(CitationExtractor.Extract("No citations here."));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter CitationExtractorTests`
Expected: FAIL.

- [ ] **Step 3: Implement**

```csharp
using System.Text.RegularExpressions;

namespace StrongKingJames.Core.Rag;

public static partial class CitationExtractor
{
    // Matches "Book c:v" including a leading ordinal (1/2/3) and a two-word book name.
    // NOTE: multi-word names with lowercase connectors (e.g. "Song of Solomon") are only
    // partially matched; broaden the book-name alternation here if full coverage is needed.
    [GeneratedRegex(@"(?:[1-3]\s+)?[A-Z][a-z]+(?:\s+[A-Z][a-z]+)?\s+\d{1,3}:\d{1,3}")]
    private static partial Regex CitationRegex();

    public static IReadOnlyList<string> Extract(string text)
    {
        var seen = new List<string>();
        foreach (Match m in CitationRegex().Matches(text))
        {
            if (!seen.Contains(m.Value)) seen.Add(m.Value);
        }
        return seen;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter CitationExtractorTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(core): citation extractor with tests"
```

---

## Phase 4 — Data layer

### Task 8: EF Core entities, DbContext, and configurations

**Files:**
- Create: `src/StrongKingJames.Data/Entities/VerseEmbedding.cs`, `BibleDbContext.cs`, all files under `Configurations/`

The Core domain models (`Book`, `Verse`, `VerseWord`, `StrongsEntry`) are used directly as EF entities. `VerseEmbedding` is a Data-layer entity (pgvector type stays out of Core).

- [ ] **Step 1: Create `VerseEmbedding` entity**

```csharp
using Pgvector;

namespace StrongKingJames.Data.Entities;

public class VerseEmbedding
{
    public int VerseId { get; set; }
    public Vector Embedding { get; set; } = null!;
}
```

- [ ] **Step 2: Create configurations**

`BookConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.ToTable("books");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.Property(x => x.Abbreviation).IsRequired();
        b.HasIndex(x => x.Abbreviation).IsUnique();
        b.HasMany(x => x.Verses).WithOne(v => v.Book!).HasForeignKey(v => v.BookId);
    }
}
```

`VerseConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class VerseConfiguration : IEntityTypeConfiguration<Verse>
{
    public void Configure(EntityTypeBuilder<Verse> b)
    {
        b.ToTable("verses");
        b.HasKey(x => x.Id);
        b.Property(x => x.OsisId).IsRequired();
        b.HasIndex(x => x.OsisId).IsUnique();
        b.HasIndex(x => new { x.BookId, x.Chapter, x.VerseNumber });
        b.HasMany(x => x.Words).WithOne().HasForeignKey(w => w.VerseId);
    }
}
```

`VerseWordConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class VerseWordConfiguration : IEntityTypeConfiguration<VerseWord>
{
    public void Configure(EntityTypeBuilder<VerseWord> b)
    {
        b.ToTable("verse_words");
        b.HasKey(x => x.Id);
        b.Property(x => x.WordText).IsRequired();
        b.HasIndex(x => x.StrongsNumber);
        b.HasIndex(x => new { x.VerseId, x.Position });
    }
}
```

`StrongsEntryConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Data.Configurations;

public class StrongsEntryConfiguration : IEntityTypeConfiguration<StrongsEntry>
{
    public void Configure(EntityTypeBuilder<StrongsEntry> b)
    {
        b.ToTable("strongs_entries");
        b.HasKey(x => x.Number);
        b.Property(x => x.Number).HasMaxLength(8);
    }
}
```

`VerseEmbeddingConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Data.Configurations;

public class VerseEmbeddingConfiguration : IEntityTypeConfiguration<VerseEmbedding>
{
    public void Configure(EntityTypeBuilder<VerseEmbedding> b)
    {
        b.ToTable("verse_embeddings");
        b.HasKey(x => x.VerseId);
        b.Property(x => x.Embedding).HasColumnType("vector(768)");
        // HNSW cosine index added via raw SQL in the migration (Task 9).
    }
}
```

- [ ] **Step 3: Create `BibleDbContext`**

```csharp
using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Data;

public class BibleDbContext(DbContextOptions<BibleDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Verse> Verses => Set<Verse>();
    public DbSet<VerseWord> VerseWords => Set<VerseWord>();
    public DbSet<StrongsEntry> StrongsEntries => Set<StrongsEntry>();
    public DbSet<VerseEmbedding> VerseEmbeddings => Set<VerseEmbedding>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasPostgresExtension("vector");
        mb.ApplyConfigurationsFromAssembly(typeof(BibleDbContext).Assembly);
    }
}
```

- [ ] **Step 4: Verify and commit**

Run: `dotnet build src/StrongKingJames.Data`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat(data): dbcontext, entities, and configurations"
```

---

### Task 9: DI registration, connection config, and initial migration

**Files:**
- Create: `src/StrongKingJames.Data/DependencyInjection.cs`
- Create: `src/StrongKingJames.Data/Migrations/` (EF-generated)

- [ ] **Step 1: Create DI helper**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Core.Services;
using StrongKingJames.Data.Repositories;

namespace StrongKingJames.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddBibleData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BibleDbContext>(opt =>
            opt.UseNpgsql(connectionString, o => o.UseVector()));
        services.AddScoped<IBibleRepository, BibleRepository>();
        services.AddScoped<ISearchService, SearchService>();
        return services;
    }
}
```

(Repositories are created in Tasks 10–11; this file will fail to compile until then. Reorder is acceptable — if executing strictly, comment out the two `AddScoped` lines here and uncomment in Task 11 Step 3.)

- [ ] **Step 2: Add a design-time factory** so `dotnet ef` can build the context without the Web host.

Create `src/StrongKingJames.Data/DesignTimeDbContextFactory.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace StrongKingJames.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BibleDbContext>
{
    public BibleDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("SKJ_CONNECTION")
                 ?? "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<BibleDbContext>()
            .UseNpgsql(cs, o => o.UseVector())
            .Options;
        return new BibleDbContext(options);
    }
}
```

- [ ] **Step 3: Start a local PostgreSQL with pgvector** (needed for `ef` and integration tests)

Run:
```bash
docker run -d --name skj-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 pgvector/pgvector:pg16
```

- [ ] **Step 4: Create the initial migration**

Run:
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/StrongKingJames.Data -s src/StrongKingJames.Data
```
Expected: a migration is generated under `Migrations/`.

- [ ] **Step 5: Add the HNSW index by editing the migration**

In the generated migration's `Up`, after table creation, append:
```csharp
migrationBuilder.Sql(
    "CREATE INDEX IF NOT EXISTS ix_verse_embeddings_hnsw " +
    "ON verse_embeddings USING hnsw (embedding vector_cosine_ops);");
```
And in `Down`, prepend:
```csharp
migrationBuilder.Sql("DROP INDEX IF EXISTS ix_verse_embeddings_hnsw;");
```

- [ ] **Step 6: Apply and verify**

Run:
```bash
dotnet ef database update -p src/StrongKingJames.Data -s src/StrongKingJames.Data
```
Expected: "Done." Tables exist.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(data): DI, design-time factory, initial migration with HNSW index"
```

---

### Task 10: Data test fixture (Testcontainers)

**Files:**
- Create: `tests/StrongKingJames.Data.Tests/DatabaseFixture.cs`

- [ ] **Step 1: Implement the fixture**

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace StrongKingJames.Data.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public BibleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BibleDbContext>()
            .UseNpgsql(ConnectionString, o => o.UseVector())
            .Options;
        return new BibleDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("db")]
public class DbCollection : ICollectionFixture<DatabaseFixture> { }
```

- [ ] **Step 2: Verify the fixture compiles and container starts** with a trivial test

```csharp
using Xunit;

namespace StrongKingJames.Data.Tests;

[Collection("db")]
public class FixtureSmokeTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Migrations_apply_and_context_connects()
    {
        await using var ctx = fx.CreateContext();
        Assert.True(await ctx.Database.CanConnectAsync());
    }
}
```

Run: `dotnet test tests/StrongKingJames.Data.Tests --filter FixtureSmokeTests`
Expected: PASS (Docker required).

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test(data): testcontainers pgvector fixture"
```

---

### Task 11: BibleRepository (TDD against the fixture)

**Files:**
- Create: `src/StrongKingJames.Data/Repositories/BibleRepository.cs`
- Test: `tests/StrongKingJames.Data.Tests/BibleRepositoryTests.cs`

- [ ] **Step 1: Write failing tests** — seed a tiny dataset, then assert each repository method.

```csharp
using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Repositories;
using Xunit;

namespace StrongKingJames.Data.Tests;

[Collection("db")]
public class BibleRepositoryTests(DatabaseFixture fx)
{
    private async Task SeedAsync()
    {
        await using var ctx = fx.CreateContext();
        // Clean slate
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE verse_words, verse_embeddings, verses, strongs_entries, books RESTART IDENTITY CASCADE;");

        var john = new Book { Name = "John", Abbreviation = "John", Testament = "NT", SortOrder = 43 };
        ctx.Books.Add(john);
        await ctx.SaveChangesAsync();

        var v16 = new Verse { BookId = john.Id, Chapter = 3, VerseNumber = 16, OsisId = "John.3.16", Text = "For God so loved the world" };
        var v17 = new Verse { BookId = john.Id, Chapter = 3, VerseNumber = 17, OsisId = "John.3.17", Text = "For God sent not his Son to condemn" };
        ctx.Verses.AddRange(v16, v17);
        await ctx.SaveChangesAsync();

        ctx.VerseWords.Add(new VerseWord { VerseId = v16.Id, Position = 1, WordText = "God", StrongsNumber = "G2316" });
        ctx.VerseWords.Add(new VerseWord { VerseId = v17.Id, Position = 1, WordText = "God", StrongsNumber = "G2316" });
        ctx.StrongsEntries.Add(new StrongsEntry { Number = "G2316", Lemma = "θεός", Transliteration = "theos", Pronunciation = "theh'-os", Definition = "a deity", KjvUsage = "God" });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetChapter_returns_ordered_verses_with_words()
    {
        await SeedAsync();
        var repo = new BibleRepository(fx.CreateContext());
        var verses = await repo.GetChapterAsync("John", 3);
        Assert.Equal(2, verses.Count);
        Assert.Equal(16, verses[0].VerseNumber);
        Assert.Contains(verses[0].Words, w => w.StrongsNumber == "G2316");
    }

    [Fact]
    public async Task GetStrongsEntry_returns_entry()
    {
        await SeedAsync();
        var repo = new BibleRepository(fx.CreateContext());
        var entry = await repo.GetStrongsEntryAsync("G2316");
        Assert.NotNull(entry);
        Assert.Equal("theos", entry!.Transliteration);
    }

    [Fact]
    public async Task GetVersesByStrongs_returns_all_matches()
    {
        await SeedAsync();
        var repo = new BibleRepository(fx.CreateContext());
        var results = await repo.GetVersesByStrongsAsync("G2316");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.StartsWith("John 3:", r.Reference));
    }

    [Fact]
    public async Task GetNeighbors_returns_window_within_chapter()
    {
        await SeedAsync();
        await using var ctx = fx.CreateContext();
        var v16 = await ctx.Verses.FirstAsync(v => v.OsisId == "John.3.16");
        var repo = new BibleRepository(fx.CreateContext());
        var neighbors = await repo.GetNeighborsAsync(v16.Id, radius: 2);
        Assert.Contains(neighbors, v => v.VerseNumber == 16);
        Assert.Contains(neighbors, v => v.VerseNumber == 17);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Data.Tests --filter BibleRepositoryTests`
Expected: FAIL — `BibleRepository` missing.

- [ ] **Step 3: Implement `BibleRepository`**

```csharp
using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Data.Repositories;

public class BibleRepository(BibleDbContext db) : IBibleRepository
{
    public async Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken ct = default) =>
        await db.Books.OrderBy(b => b.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<Verse>> GetChapterAsync(string bookAbbrev, int chapter, CancellationToken ct = default) =>
        await db.Verses
            .Include(v => v.Words.OrderBy(w => w.Position))
            .Where(v => v.Book!.Abbreviation == bookAbbrev && v.Chapter == chapter)
            .OrderBy(v => v.VerseNumber)
            .ToListAsync(ct);

    public async Task<StrongsEntry?> GetStrongsEntryAsync(string number, CancellationToken ct = default) =>
        await db.StrongsEntries.FirstOrDefaultAsync(e => e.Number == number, ct);

    public async Task<IReadOnlyList<SearchResult>> GetVersesByStrongsAsync(string number, CancellationToken ct = default) =>
        await db.VerseWords
            .Where(w => w.StrongsNumber == number)
            .Select(w => w.VerseId).Distinct()
            .Join(db.Verses, id => id, v => v.Id, (id, v) => v)
            .OrderBy(v => v.Book!.SortOrder).ThenBy(v => v.Chapter).ThenBy(v => v.VerseNumber)
            .Select(v => new SearchResult(v.Id, v.OsisId, v.Book!.Name + " " + v.Chapter + ":" + v.VerseNumber, v.Text, null))
            .ToListAsync(ct);

    public async Task<Verse?> GetVerseByReferenceAsync(string bookAbbrev, int chapter, int verse, CancellationToken ct = default) =>
        await db.Verses.FirstOrDefaultAsync(v =>
            v.Book!.Abbreviation == bookAbbrev && v.Chapter == chapter && v.VerseNumber == verse, ct);

    public async Task<IReadOnlyList<Verse>> GetNeighborsAsync(int verseId, int radius, CancellationToken ct = default)
    {
        var v = await db.Verses.FirstOrDefaultAsync(x => x.Id == verseId, ct);
        if (v is null) return [];
        return await db.Verses
            .Where(x => x.BookId == v.BookId && x.Chapter == v.Chapter
                        && x.VerseNumber >= v.VerseNumber - radius
                        && x.VerseNumber <= v.VerseNumber + radius)
            .OrderBy(x => x.VerseNumber)
            .ToListAsync(ct);
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Data.Tests --filter BibleRepositoryTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(data): bible repository with integration tests"
```

---

### Task 12: SearchService (vector similarity, TDD)

**Files:**
- Create: `src/StrongKingJames.Data/Repositories/SearchService.cs`
- Test: `tests/StrongKingJames.Data.Tests/SearchServiceTests.cs`

- [ ] **Step 1: Write failing test** — seed verses with known embeddings, query with a vector close to one of them, assert ranking.

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector;
using StrongKingJames.Core.Models;
using StrongKingJames.Data.Entities;
using StrongKingJames.Data.Repositories;
using Xunit;

namespace StrongKingJames.Data.Tests;

[Collection("db")]
public class SearchServiceTests(DatabaseFixture fx)
{
    private static float[] UnitVec(int dim, int hot)
    {
        var a = new float[dim];
        a[hot] = 1f;
        return a;
    }

    [Fact]
    public async Task SemanticSearch_ranks_closest_first()
    {
        await using (var ctx = fx.CreateContext())
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "TRUNCATE verse_words, verse_embeddings, verses, strongs_entries, books RESTART IDENTITY CASCADE;");
            var b = new Book { Name = "Gen", Abbreviation = "Gen", Testament = "OT", SortOrder = 1 };
            ctx.Books.Add(b);
            await ctx.SaveChangesAsync();
            var v1 = new Verse { BookId = b.Id, Chapter = 1, VerseNumber = 1, OsisId = "Gen.1.1", Text = "one" };
            var v2 = new Verse { BookId = b.Id, Chapter = 1, VerseNumber = 2, OsisId = "Gen.1.2", Text = "two" };
            ctx.Verses.AddRange(v1, v2);
            await ctx.SaveChangesAsync();
            ctx.VerseEmbeddings.Add(new VerseEmbedding { VerseId = v1.Id, Embedding = new Vector(UnitVec(768, 0)) });
            ctx.VerseEmbeddings.Add(new VerseEmbedding { VerseId = v2.Id, Embedding = new Vector(UnitVec(768, 5)) });
            await ctx.SaveChangesAsync();
        }

        var svc = new SearchService(fx.CreateContext());
        var results = await svc.SemanticSearchAsync(UnitVec(768, 0), topK: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("Gen.1.1", results[0].OsisId); // closest
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Data.Tests --filter SearchServiceTests`
Expected: FAIL.

- [ ] **Step 3: Implement `SearchService`** using pgvector cosine distance ordering.

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Data.Repositories;

public class SearchService(BibleDbContext db) : ISearchService
{
    public async Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(
        float[] queryEmbedding, int topK, CancellationToken ct = default)
    {
        var q = new Vector(queryEmbedding);
        // Project the cosine distance once and reuse it for both ordering and Score,
        // so pgvector's distance is not computed twice per candidate row.
        return await db.VerseEmbeddings
            .Select(e => new { e.VerseId, Distance = e.Embedding.CosineDistance(q) })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Join(db.Verses, x => x.VerseId, v => v.Id, (x, v) => new { x.Distance, v })
            .Select(x => new SearchResult(
                x.v.Id, x.v.OsisId,
                x.v.Book!.Name + " " + x.v.Chapter + ":" + x.v.VerseNumber,
                x.v.Text,
                1.0 - x.Distance))
            .ToListAsync(ct);
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Data.Tests --filter SearchServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(data): pgvector semantic search service with test"
```

---

## Phase 5 — Importer

### Task 13: OSIS parser (TDD)

**Files:**
- Create: `src/StrongKingJames.Importer/Parsing/OsisParser.cs` (ImporterOptions.cs is created later, in Task 17)
- Test: `tests/StrongKingJames.Importer.Tests/OsisParserTests.cs`, `tests/StrongKingJames.Importer.Tests/TestData/sample-osis.xml`

The parser reads OSIS XML and yields `Verse` objects (with `VerseWord`s and plain `Text`). OSIS marks verses with `<verse osisID="Gen.1.1">...</verse>` and tagged words with `<w lemma="strong:H430">God</w>`. Untagged text between `<w>` elements (punctuation, supplied words in `<transChange>`) becomes `VerseWord`s with `StrongsNumber = null`.

- [ ] **Step 1: Create a tiny OSIS fixture** at `tests/StrongKingJames.Importer.Tests/TestData/sample-osis.xml`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<osis xmlns="http://www.bibletechnologies.net/2003/OSIS/namespace">
  <osisText osisIDWork="KJV">
    <div type="book" osisID="Gen">
      <chapter osisID="Gen.1">
        <verse osisID="Gen.1.1" sID="Gen.1.1"/>
        In <w lemma="strong:H07225">the beginning</w> <w lemma="strong:H0430">God</w> created.
        <verse eID="Gen.1.1"/>
      </chapter>
    </div>
  </osisText>
</osis>
```

Note: openscriptures uses milestone verse markers (`sID`/`eID`). The parser must handle the milestone style (text lives between the `sID` and `eID` markers, not nested inside a `<verse>` element).

- [ ] **Step 2: Write failing tests**

```csharp
using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class OsisParserTests
{
    private static string FixturePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-osis.xml");

    [Fact]
    public void Parse_extracts_verse_with_reference_and_text()
    {
        var verses = new OsisParser().Parse(FixturePath).ToList();
        var v = Assert.Single(verses);
        Assert.Equal("Gen.1.1", v.OsisId);
        Assert.Equal(1, v.Chapter);
        Assert.Equal(1, v.VerseNumber);
        Assert.Contains("beginning", v.Text);
        Assert.Contains("God", v.Text);
    }

    [Fact]
    public void Parse_captures_strongs_tagged_words()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        Assert.Contains(v.Words, w => w.StrongsNumber == "H0430" && w.WordText.Contains("God"));
        Assert.Contains(v.Words, w => w.StrongsNumber == "H07225");
    }

    [Fact]
    public void Parse_keeps_untagged_tokens_with_null_strongs()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        Assert.Contains(v.Words, w => w.StrongsNumber == null);
    }

    [Fact]
    public void Parse_assigns_sequential_positions()
    {
        var v = new OsisParser().Parse(FixturePath).Single();
        var positions = v.Words.Select(w => w.Position).ToList();
        Assert.Equal(positions.OrderBy(p => p).ToList(), positions);
        Assert.Equal(positions.Distinct().Count(), positions.Count);
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Importer.Tests --filter OsisParserTests`
Expected: FAIL.

- [ ] **Step 4: Implement `OsisParser`**

Implementation notes for the engineer:
- Use `System.Xml.Linq` with `XmlReader` streaming for the full 4MB file, but for clarity a full `XDocument.Load` is acceptable at this size.
- Walk the document in order. Maintain a "current verse" that opens on a `<verse sID=...>` milestone and closes on the matching `<verse eID=...>`.
- While a verse is open, iterate descendant text nodes and `<w>` elements in document order:
  - For a `<w>` element: read `lemma`, extract the Strong's number after `strong:` (there may be multiple, space-separated → emit one `VerseWord` per number at the same position; strip the `strong:` prefix, keep leading `H`/`G` and digits). Append the element's text to the plain-text buffer.
  - For raw text nodes not inside `<w>`: split on whitespace, emit each token as a `VerseWord` with `StrongsNumber = null`; append to plain-text buffer.
- Derive `Chapter`/`VerseNumber` by splitting `osisID` on `.` (e.g. `Gen.1.1` → book `Gen`, chapter `1`, verse `1`). Keep the book abbreviation on the verse via a transient field or resolve against the book list in the seeder.
- Normalize whitespace in `Text` (collapse runs to single spaces, trim).

```csharp
using System.Xml.Linq;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

public class OsisParser
{
    private static readonly XNamespace Osis = "http://www.bibletechnologies.net/2003/OSIS/namespace";

    public IEnumerable<ParsedVerse> Parse(string path)
    {
        var doc = XDocument.Load(path);
        ParsedVerse? current = null;
        var textBuffer = new System.Text.StringBuilder();
        int position = 0;

        foreach (var node in Descendants(doc.Root!))
        {
            if (node is XElement el && el.Name == Osis + "verse")
            {
                var sid = el.Attribute("sID")?.Value;
                var eid = el.Attribute("eID")?.Value;
                if (sid is not null)
                {
                    current = NewVerse(sid);
                    textBuffer.Clear();
                    position = 0;
                }
                else if (eid is not null && current is not null)
                {
                    current.Text = NormalizeWhitespace(textBuffer.ToString());
                    yield return current;
                    current = null;
                }
            }
            else if (current is not null && node is XElement w && w.Name == Osis + "w")
            {
                var lemma = w.Attribute("lemma")?.Value ?? "";
                var numbers = ExtractStrongs(lemma);
                var text = w.Value;
                textBuffer.Append(text).Append(' ');
                position++;
                if (numbers.Count == 0)
                    current.Words.Add(new VerseWord { Position = position, WordText = text, StrongsNumber = null });
                else
                    foreach (var n in numbers)
                        current.Words.Add(new VerseWord { Position = position, WordText = text, StrongsNumber = n });
            }
            else if (current is not null && node is XText t)
            {
                // Only top-level text (not inside <w>) — see Descendants filtering.
                foreach (var token in t.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    textBuffer.Append(token).Append(' ');
                    position++;
                    current.Words.Add(new VerseWord { Position = position, WordText = token, StrongsNumber = null });
                }
            }
        }
    }

    // Yields elements and the text nodes that are NOT inside a <w> element, in document order.
    private static IEnumerable<XNode> Descendants(XElement root)
    {
        foreach (var node in root.Nodes())
        {
            if (node is XElement el)
            {
                yield return el;
                if (el.Name != Osis + "w")
                    foreach (var child in Descendants(el))
                        yield return child;
            }
            else if (node is XText txt && node.Parent?.Name != Osis + "w")
            {
                yield return txt;
            }
        }
    }

    private static ParsedVerse NewVerse(string osisId)
    {
        var parts = osisId.Split('.');
        return new ParsedVerse
        {
            OsisId = osisId,
            BookAbbrev = parts[0],
            Chapter = int.Parse(parts[1]),
            VerseNumber = int.Parse(parts[2]),
        };
    }

    private static List<string> ExtractStrongs(string lemma) =>
        lemma.Split(' ', StringSplitOptions.RemoveEmptyEntries)
             .Where(p => p.StartsWith("strong:"))
             .Select(p => p["strong:".Length..])
             .ToList();

    private static string NormalizeWhitespace(string s) =>
        string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public class ParsedVerse : Verse
{
    public string BookAbbrev { get; set; } = "";
}
```

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Importer.Tests --filter OsisParserTests`
Expected: PASS. If the fixture's milestone nesting differs from real openscriptures output, adjust the fixture to match a real snippet and re-run.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(importer): OSIS parser with tests"
```

---

### Task 14: Strong's dictionary parser (TDD)

**Files:**
- Create: `src/StrongKingJames.Importer/Parsing/StrongsDictionaryParser.cs`
- Test: `tests/StrongKingJames.Importer.Tests/StrongsDictionaryParserTests.cs`, `TestData/sample-strongs.xml`

- [ ] **Step 1: Create a fixture** `TestData/sample-strongs.xml` matching openscriptures' `strongshebrew.xml` structure (entries as `<div type="entry" osisID="strong:H430" n="H0430">` containing `<w ...>` lemma, `<foreign>` transliteration, and definition markup). Use a real snippet; a representative shape:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<osis xmlns="http://www.bibletechnologies.net/2003/OSIS/namespace">
  <osisText>
    <div type="entry" n="H0430" osisID="strong:H0430">
      <w ID="H0430" lemma="אֱלֹהִים" xlit="ʼĕlôhîym" POS="n-m" xml:lang="heb">אֱלֹהִים</w>
      <list><item><label>Definition</label>
        <p>gods in the ordinary sense; specifically the supreme God</p></item>
        <item><label>KJV Usage</label><p>God, gods, judges</p></item>
      </list>
    </div>
  </osisText>
</osis>
```

- [ ] **Step 2: Write failing tests**

```csharp
using System.Linq;
using StrongKingJames.Importer.Parsing;
using Xunit;

namespace StrongKingJames.Importer.Tests;

public class StrongsDictionaryParserTests
{
    private static string FixturePath =>
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "sample-strongs.xml");

    [Fact]
    public void Parse_extracts_entry_fields()
    {
        var entry = new StrongsDictionaryParser().Parse(FixturePath).Single();
        Assert.Equal("H0430", entry.Number);
        Assert.Equal("אֱלֹהִים", entry.Lemma);
        Assert.Equal("ʼĕlôhîym", entry.Transliteration);
        Assert.Contains("supreme God", entry.Definition);
        Assert.Contains("judges", entry.KjvUsage);
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Importer.Tests --filter StrongsDictionaryParserTests`
Expected: FAIL.

- [ ] **Step 4: Implement `StrongsDictionaryParser`**

```csharp
using System.Xml.Linq;
using StrongKingJames.Core.Models;

namespace StrongKingJames.Importer.Parsing;

public class StrongsDictionaryParser
{
    private static readonly XNamespace Osis = "http://www.bibletechnologies.net/2003/OSIS/namespace";

    public IEnumerable<StrongsEntry> Parse(string path)
    {
        var doc = XDocument.Load(path);
        foreach (var div in doc.Descendants(Osis + "div").Where(d => (string?)d.Attribute("type") == "entry"))
        {
            var n = (string?)div.Attribute("n");
            if (string.IsNullOrEmpty(n)) continue;
            var w = div.Elements(Osis + "w").FirstOrDefault();

            yield return new StrongsEntry
            {
                Number = n,
                Lemma = w?.Value ?? "",
                Transliteration = (string?)w?.Attribute("xlit") ?? "",
                Pronunciation = (string?)w?.Attribute("POS") ?? "", // real files carry pronunciation; map the correct attribute when confirmed
                Definition = SectionText(div, "Definition"),
                KjvUsage = SectionText(div, "KJV Usage"),
            };
        }
    }

    private static string SectionText(XElement div, string label)
    {
        var item = div.Descendants(Osis + "item")
            .FirstOrDefault(i => (string?)i.Element(Osis + "label")?.Value == label);
        return item is null ? "" :
            string.Join(' ', item.Elements(Osis + "p").Select(p => p.Value)).Trim();
    }
}
```

Note: pronunciation attribute naming varies by dictionary release; the engineer should inspect the real file and map the correct attribute/element, updating the test fixture to match.

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Importer.Tests --filter StrongsDictionaryParserTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(importer): strongs dictionary parser with tests"
```

---

### Task 15: DatabaseSeeder

**Files:**
- Create: `src/StrongKingJames.Importer/Seeding/DatabaseSeeder.cs`, `src/StrongKingJames.Importer/BookData.cs`

The seeder writes books, verses (resolving `BookAbbrev` → `BookId`), verse words, and Strong's entries into PostgreSQL. Book metadata (name, abbrev, testament, sort order for all 66 books) lives in a static table.

- [ ] **Step 1: Create `BookData.cs`** — a static list of the 66 books with `Name`, `Abbreviation` (OSIS abbrev, e.g. `Gen`, `Exod`, `Matt`), `Testament`, `SortOrder`. (Full list; abbreviations must match OSIS book IDs used in the KJV file.)

- [ ] **Step 2: Implement the seeder** (no unit test — covered by the smoke run in Task 17; keep it thin):

```csharp
using Microsoft.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Data;
using StrongKingJames.Importer.Parsing;

namespace StrongKingJames.Importer.Seeding;

public class DatabaseSeeder(BibleDbContext db)
{
    public async Task SeedBooksAsync(CancellationToken ct = default)
    {
        if (await db.Books.AnyAsync(ct)) return;
        db.Books.AddRange(BookData.All);
        await db.SaveChangesAsync(ct);
    }

    public async Task SeedVersesAsync(string osisPath, CancellationToken ct = default)
    {
        if (await db.Verses.AnyAsync(ct)) return;
        var byAbbrev = await db.Books.ToDictionaryAsync(b => b.Abbreviation, ct);
        var parser = new OsisParser();
        var batch = new List<Verse>();
        foreach (ParsedVerse pv in parser.Parse(osisPath))
        {
            if (!byAbbrev.TryGetValue(pv.BookAbbrev, out var book)) continue;
            pv.BookId = book.Id;
            batch.Add(pv);
            if (batch.Count >= 500) { db.Verses.AddRange(batch); await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); batch.Clear(); }
        }
        if (batch.Count > 0) { db.Verses.AddRange(batch); await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); }
    }

    public async Task SeedStrongsAsync(string hebrewPath, string greekPath, CancellationToken ct = default)
    {
        if (await db.StrongsEntries.AnyAsync(ct)) return;
        var parser = new StrongsDictionaryParser();
        foreach (var path in new[] { hebrewPath, greekPath })
        {
            var entries = parser.Parse(path).ToList();
            db.StrongsEntries.AddRange(entries);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }
}
```

- [ ] **Step 3: Verify build and commit**

Run: `dotnet build src/StrongKingJames.Importer`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat(importer): database seeder and book metadata"
```

---

### Task 16: Ollama embedding service + EmbeddingBackfiller

**Files:**
- Create: `src/StrongKingJames.Core/Ollama/OllamaOptions.cs`, `src/StrongKingJames.Core/Ollama/OllamaEmbeddingService.cs` (placed in **Core** — see note — so both Web and Importer reuse them without a circular reference)
- Create: `src/StrongKingJames.Importer/Seeding/EmbeddingBackfiller.cs`

Note on placement: the Ollama HTTP clients are needed by both Web and Importer. To avoid a circular reference, create them in a small shared spot. Simplest: put `OllamaOptions`, `OllamaEmbeddingService`, `OllamaChatService` in **Core** under `Core/Ollama/` (Core already defines the interfaces). They use `System.Net.Http.Json` (in the framework), so Core needs no new package. Adjust namespaces accordingly. This plan places them in Core.

- [ ] **Step 1: Create `Core/Ollama/OllamaOptions.cs`**

```csharp
namespace StrongKingJames.Core.Ollama;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    // Pinned to the tag actually installed on the host; bare "llama3.1" resolves to
    // ":latest" which may not be pulled. Override via the appsettings Ollama section.
    public string ChatModel { get; set; } = "llama3.1:8b";
}
```

- [ ] **Step 2: Create `Core/Ollama/OllamaEmbeddingService.cs`**

```csharp
using System.Net.Http.Json;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Ollama;

public class OllamaEmbeddingService(HttpClient http, OllamaOptions options) : IEmbeddingService
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(
            $"{options.BaseUrl}/api/embeddings",
            new { model = options.EmbeddingModel, prompt = text }, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return body!.Embedding;
    }

    private record EmbeddingResponse(float[] Embedding);
}
```

- [ ] **Step 3: Create `EmbeddingBackfiller`** — resumable: only embeds verses with no `verse_embeddings` row.

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Importer.Seeding;

public class EmbeddingBackfiller(BibleDbContext db, IEmbeddingService embedder)
{
    public async Task<int> RunAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var pending = await db.Verses
            .Where(v => !db.VerseEmbeddings.Any(e => e.VerseId == v.Id))
            .Select(v => new { v.Id, v.Text })
            .ToListAsync(ct);

        int done = 0;
        foreach (var v in pending)
        {
            var vec = await embedder.EmbedAsync(v.Text, ct);
            db.VerseEmbeddings.Add(new VerseEmbedding { VerseId = v.Id, Embedding = new Vector(vec) });
            await db.SaveChangesAsync(ct);
            progress?.Report(++done);
        }
        return done;
    }
}
```

- [ ] **Step 4: Verify build and commit**

Run: `dotnet build`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat: ollama embedding service and resumable backfiller"
```

---

### Task 17: Importer Program (CLI wiring + manual smoke run)

**Files:**
- Create: `src/StrongKingJames.Importer/Program.cs`, `ImporterOptions.cs`

- [ ] **Step 1: Implement `ImporterOptions`** — bind from args/env: `--osis`, `--hebrew`, `--greek`, `--connection`, plus Ollama base URL/model overrides.

- [ ] **Step 2: Implement `Program.cs`** — a `Host.CreateApplicationBuilder`, call `builder.AddServiceDefaults()`, read the Aspire-injected `bible` connection string and Ollama reference (fall back to args/env when run standalone), register `BibleDbContext` (via `AddBibleData`), `HttpClient` + `OllamaEmbeddingService`, then run in order: `Database.MigrateAsync()`, `SeedBooksAsync`, `SeedVersesAsync`, `SeedStrongsAsync`, then `EmbeddingBackfiller.RunAsync` with a console progress writer. Print counts at the end (books, verses, verse_words, strongs_entries, embeddings), then exit 0 so Aspire's `WaitForCompletion` releases the web app. The OSIS/dictionary file paths still come from args/env (they are not Aspire resources); default to `/data/kjv.xml` etc. so the container can mount them.

```csharp
// Sketch — see ImporterOptions for arg parsing.
using Microsoft.EntityFrameworkCore;
using StrongKingJames.Data;
using StrongKingJames.Importer.Seeding;
// ... build services, then:
await db.Database.MigrateAsync();
var seeder = new DatabaseSeeder(db);
await seeder.SeedBooksAsync();
await seeder.SeedVersesAsync(options.OsisPath);
await seeder.SeedStrongsAsync(options.HebrewPath, options.GreekPath);
var backfiller = new EmbeddingBackfiller(db, embedder);
var count = await backfiller.RunAsync(new Progress<int>(n => { if (n % 100 == 0) Console.WriteLine($"Embedded {n}..."); }));
Console.WriteLine($"Done. Books={await db.Books.CountAsync()} Verses={await db.Verses.CountAsync()} Embeddings={count}");
```

- [ ] **Step 3: Smoke run via Aspire** (requires downloaded data in `./data`; Aspire brings up pgvector + Ollama containers and pulls models)

Run:
```bash
aspire run --project src/StrongKingJames.AppHost
```
Watch the Aspire dashboard: the `postgres` and `ollama` containers start, the models pull, then the `importer` resource runs to completion. Its log ends with `Books=66 Verses=31102 Embeddings=...` (KJV has 31,102 verses). Re-running should embed 0 new (resumable). The `web` resource starts only after the importer completes.

For standalone importer runs (outside Aspire), the CLI args still work:
```bash
dotnet run --project src/StrongKingJames.Importer -- --osis data/kjv.xml --hebrew data/strongshebrew.xml --greek data/strongsgreek.xml --connection "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres"
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat(importer): CLI program orchestrating seed and embedding"
```

---

## Phase 6 — Web: Ollama chat, RAG service, and API

### Task 18: OllamaChatService (streaming)

**Files:**
- Create: `src/StrongKingJames.Core/Ollama/OllamaChatService.cs`

- [ ] **Step 1: Implement streaming chat** against `/api/chat` with `stream: true` (NDJSON lines, each with `message.content`).

```csharp
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Ollama;

public class OllamaChatService(HttpClient http, OllamaOptions options) : IChatService
{
    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new
        {
            model = options.ChatModel,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl}/api/chat")
        {
            Content = JsonContent.Create(request),
        };
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("message", out var msg)
                && msg.TryGetProperty("content", out var content))
            {
                var chunk = content.GetString();
                if (!string.IsNullOrEmpty(chunk)) yield return chunk;
            }
        }
    }
}
```

- [ ] **Step 2: Verify build and commit**

Run: `dotnet build src/StrongKingJames.Core`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat: ollama streaming chat service"
```

---

### Task 19: RagService (TDD with fakes)

**Files:**
- Create: `src/StrongKingJames.Web/Rag/RagService.cs`
- Test: add to `tests/StrongKingJames.Core.Tests/` a `RagServiceTests.cs` — but `RagService` lives in Web. To keep it unit-testable without the Web host, **place `RagService` in Core** (`Core/Rag/RagService.cs`) since it depends only on Core interfaces. This plan places it in Core.
- Test: `tests/StrongKingJames.Core.Tests/RagServiceTests.cs`

`RagService.AnswerAsync` orchestrates: embed question → semantic search topK → expand neighbors via repository → build prompt → stream chat. It composes `IEmbeddingService`, `ISearchService`, `IBibleRepository`, `IChatService`.

- [ ] **Step 1: Write failing test with fakes**

```csharp
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Rag;
using StrongKingJames.Core.Services;
using Xunit;

namespace StrongKingJames.Core.Tests;

public class RagServiceTests
{
    private sealed class FakeEmbedder : IEmbeddingService
    {
        public Task<float[]> EmbedAsync(string t, CancellationToken ct = default) => Task.FromResult(new float[768]);
    }
    private sealed class FakeSearch : ISearchService
    {
        public Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(float[] e, int k, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SearchResult>>(new[]
            { new SearchResult(1, "John.3.16", "John 3:16", "For God so loved the world", 0.9) });
    }
    private sealed class FakeRepo : IBibleRepository
    {
        public Task<IReadOnlyList<Book>> GetBooksAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task<IReadOnlyList<Verse>> GetChapterAsync(string b, int c, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Verse>>([]);
        public Task<StrongsEntry?> GetStrongsEntryAsync(string n, CancellationToken ct = default) => Task.FromResult<StrongsEntry?>(null);
        public Task<IReadOnlyList<SearchResult>> GetVersesByStrongsAsync(string n, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SearchResult>>([]);
        public Task<Verse?> GetVerseByReferenceAsync(string b, int c, int v, CancellationToken ct = default) => Task.FromResult<Verse?>(null);
        public Task<IReadOnlyList<Verse>> GetNeighborsAsync(int id, int r, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Verse>>(new[] { new Verse { Id = 1, Chapter = 3, VerseNumber = 16, Text = "For God so loved the world" } });
    }
    private sealed class FakeChat : IChatService
    {
        public async IAsyncEnumerable<string> StreamAsync(IReadOnlyList<ChatMessage> m,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            // Echo that the passage reached the prompt.
            Assert.Contains(m, x => x.Content.Contains("For God so loved the world"));
            yield return "God ";
            yield return "is love (John 3:16).";
        }
    }

    [Fact]
    public async Task AnswerAsync_streams_answer_from_retrieved_passages()
    {
        var svc = new RagService(new FakeEmbedder(), new FakeSearch(), new FakeRepo(), new FakeChat());
        var chunks = new List<string>();
        await foreach (var c in svc.AnswerAsync("What is love?"))
            chunks.Add(c);
        var full = string.Concat(chunks);
        Assert.Contains("John 3:16", full);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter RagServiceTests`
Expected: FAIL.

- [ ] **Step 3: Implement `Core/Rag/RagService.cs`**

```csharp
using System.Runtime.CompilerServices;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Core.Rag;

public class RagService(
    IEmbeddingService embedder,
    ISearchService search,
    IBibleRepository repo,
    IChatService chat) : IRagService
{
    private const int TopK = 8;
    private const int NeighborRadius = 2;

    public async IAsyncEnumerable<string> AnswerAsync(
        string question, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var qvec = await embedder.EmbedAsync(question, ct);
        var hits = await search.SemanticSearchAsync(qvec, TopK, ct);

        var passages = new List<RetrievedPassage>();
        foreach (var hit in hits)
        {
            var neighbors = await repo.GetNeighborsAsync(hit.VerseId, NeighborRadius, ct);
            var text = neighbors.Count > 0
                ? string.Join(' ', neighbors.Select(n => n.Text))
                : hit.Text;
            passages.Add(new RetrievedPassage(hit.Reference, text, hit.Score ?? 0));
        }

        var messages = RagPromptBuilder.Build(question, passages);
        await foreach (var chunk in chat.StreamAsync(messages, ct))
            yield return chunk;
    }
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test tests/StrongKingJames.Core.Tests --filter RagServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(core): RAG orchestration service with tests"
```

---

### Task 20: Web host wiring + Ollama health check

**Files:**
- Modify: `src/StrongKingJames.Web/Program.cs`, `appsettings.json`
- Create: `src/StrongKingJames.Web/Ollama/OllamaHealth.cs`

- [ ] **Step 1: `appsettings.json`** — Aspire injects `ConnectionStrings__bible` and overrides `Ollama__BaseUrl` (env var → config) at runtime. The `Ollama` section holds the model names and a local-dev default URL:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text",
    "ChatModel": "llama3.1:8b"
  }
}
```

- [ ] **Step 2: `Program.cs`** — add ServiceDefaults, read the Aspire-injected connection string (`bible`) and Ollama endpoint, register everything:

```csharp
using StrongKingJames.Core.Ollama;
using StrongKingJames.Core.Rag;
using StrongKingJames.Core.Services;
using StrongKingJames.Data;
using StrongKingJames.Web.Components;
using StrongKingJames.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();          // Aspire: health, telemetry, service discovery, resilience
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Aspire injects ConnectionStrings__bible from the AppHost's WithReference(bibleDb).
builder.Services.AddBibleData(builder.Configuration.GetConnectionString("bible")!);

// Ollama base URL comes from the Ollama__BaseUrl env var (Aspire sets it to the host
// endpoint); model names come from the appsettings Ollama section.
var ollama = builder.Configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new();
builder.Services.AddSingleton(ollama);
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddHttpClient<IChatService, OllamaChatService>();
builder.Services.AddScoped<IRagService, RagService>();

var app = builder.Build();
app.MapDefaultEndpoints();              // Aspire health endpoints
app.UseStaticFiles();
app.MapApiEndpoints();                  // Task 21
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

Because `.NET` config maps the `Ollama__BaseUrl` environment variable onto `Ollama:BaseUrl`, binding the `Ollama` section is all that's needed — no custom resolver.

- [ ] **Step 3: `OllamaHealth`** — a small service that pings `GET {BaseUrl}/api/tags` and reports reachable + whether required models are present, for the status banner.

```csharp
using System.Net.Http.Json;
using StrongKingJames.Core.Ollama;

namespace StrongKingJames.Web.Ollama;

public class OllamaHealth(HttpClient http, OllamaOptions options)
{
    public async Task<(bool Reachable, bool ModelsReady, string? Message)> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await http.GetAsync($"{options.BaseUrl}/api/tags", ct);
            if (!resp.IsSuccessStatusCode) return (false, false, $"Ollama returned {(int)resp.StatusCode}.");
            var tags = await resp.Content.ReadFromJsonAsync<TagsResponse>(ct);
            var names = tags?.Models?.Select(m => m.Name) ?? [];
            var hasEmbed = names.Any(n => n.StartsWith(options.EmbeddingModel));
            var hasChat = names.Any(n => n.StartsWith(options.ChatModel));
            if (hasEmbed && hasChat) return (true, true, null);
            var missing = string.Join(", ",
                new[] { hasEmbed ? null : options.EmbeddingModel, hasChat ? null : options.ChatModel }
                .Where(x => x is not null));
            return (true, false, $"Run: ollama pull {missing}");
        }
        catch (Exception ex)
        {
            return (false, false, $"Cannot reach Ollama at {options.BaseUrl}: {ex.Message}");
        }
    }

    private record TagsResponse(List<ModelInfo>? Models);
    private record ModelInfo(string Name);
}
```

Register in `Program.cs`: `builder.Services.AddHttpClient<OllamaHealth>();`

- [ ] **Step 4: Verify build and commit**

Run: `dotnet build src/StrongKingJames.Web`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat(web): host wiring, DI, and ollama health check"
```

---

### Task 21: Minimal API endpoints

**Files:**
- Create: `src/StrongKingJames.Web/Endpoints/ApiEndpoints.cs`

- [ ] **Step 1: Implement endpoints** over the service interfaces + `SearchModeDetector`.

```csharp
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Search;
using StrongKingJames.Core.Services;

namespace StrongKingJames.Web.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/books", (IBibleRepository repo) => repo.GetBooksAsync());

        api.MapGet("/books/{abbrev}/chapters/{ch:int}",
            (string abbrev, int ch, IBibleRepository repo) => repo.GetChapterAsync(abbrev, ch));

        api.MapGet("/strongs/{number}", async (string number, IBibleRepository repo) =>
            await repo.GetStrongsEntryAsync(number) is { } e ? Results.Ok(e) : Results.NotFound());

        api.MapGet("/strongs/{number}/verses",
            (string number, IBibleRepository repo) => repo.GetVersesByStrongsAsync(number));

        api.MapGet("/search", async (string q, string? mode,
            IBibleRepository repo, ISearchService search, IEmbeddingService embed) =>
        {
            var m = Enum.TryParse<SearchMode>(mode, true, out var parsed) ? parsed : SearchMode.Auto;
            if (m == SearchMode.Auto) m = SearchModeDetector.Detect(q);
            return m switch
            {
                SearchMode.Strongs => Results.Ok(await repo.GetVersesByStrongsAsync(q.Trim())),
                SearchMode.Semantic => Results.Ok(await search.SemanticSearchAsync(await embed.EmbedAsync(q), 20)),
                // Reference navigation is a UI concern; the API returns an empty list for
                // reference-mode queries rather than mis-running a Strong's lookup.
                _ => Results.Ok(Array.Empty<SearchResult>())
            };
        });

        api.MapPost("/chat", (ChatRequest body, IRagService rag, HttpContext ctx) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            return Results.Empty; // streaming handled below
        });

        // SSE chat endpoint
        api.MapPost("/chat/stream", async (ChatRequest body, IRagService rag, HttpContext ctx) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            await foreach (var chunk in rag.AnswerAsync(body.Question, ctx.RequestAborted))
            {
                await ctx.Response.WriteAsync($"data: {chunk}\n\n", ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
        });
    }

    public record ChatRequest(string Question);
}
```

(Consolidate to a single `/api/chat` SSE endpoint; the placeholder above is illustrative — the engineer should keep only the streaming version at `/api/chat`.)

- [ ] **Step 2: Manual verify** (importer must have run first)

Run: `dotnet run --project src/StrongKingJames.Web` then in another shell:
```bash
curl http://localhost:5000/api/books
curl "http://localhost:5000/api/strongs/G2316"
```
Expected: JSON book list; Strong's entry for G2316.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat(web): minimal API endpoints for books, strongs, search, chat"
```

---

## Phase 7 — Blazor UI

### Task 22: VerseView + StrongsPopover components

**Files:**
- Create: `src/StrongKingJames.Web/Components/Shared/VerseView.razor`, `StrongsPopover.razor`, `OllamaStatusBanner.razor`

- [ ] **Step 1: `VerseView.razor`** — renders a verse by iterating `Words` in order; tagged words are clickable buttons carrying their Strong's number, untagged tokens render as plain text. Distinct positions with multiple Strong's numbers render once (group by `Position`).

```razor
@using StrongKingJames.Core.Models

<span class="verse">
    <sup class="verse-num">@Verse.VerseNumber</sup>
    @foreach (var group in Verse.Words.GroupBy(w => w.Position).OrderBy(g => g.Key))
    {
        var word = group.First();
        var strongs = group.Where(w => w.StrongsNumber != null).Select(w => w.StrongsNumber!).ToList();
        if (strongs.Count > 0)
        {
            <button class="tagged-word" @onclick="() => OnWordClicked.InvokeAsync(strongs)">@word.WordText</button>
        }
        else
        {
            @word.WordText
        }
        @(" ")
    }
</span>

@code {
    [Parameter, EditorRequired] public Verse Verse { get; set; } = null!;
    [Parameter] public EventCallback<List<string>> OnWordClicked { get; set; }
}
```

- [ ] **Step 2: `StrongsPopover.razor`** — given a Strong's number, loads the entry via `IBibleRepository` and shows lemma, transliteration, pronunciation, definition, KJV usage, plus a link to `/search?q={number}`.

- [ ] **Step 3: `OllamaStatusBanner.razor`** — calls `OllamaHealth.CheckAsync` on init; if not reachable or models missing, renders a dismissible warning banner with the message (e.g. the `ollama pull` command). Otherwise renders nothing.

- [ ] **Step 4: Verify build and commit**

Run: `dotnet build src/StrongKingJames.Web`
Expected: "Build succeeded".

```bash
git add -A
git commit -m "feat(web): verse view, strongs popover, ollama banner components"
```

---

### Task 23: Browse and Search pages

**Files:**
- Create: `src/StrongKingJames.Web/Components/Pages/Browse.razor`, `Search.razor`
- Modify: `src/StrongKingJames.Web/Components/Layout/MainLayout.razor` (nav links to Browse / Search / Chat)

- [ ] **Step 1: `Browse.razor`** (`@page "/"` and `@page "/browse/{Book}/{Chapter:int}"`) — book/chapter pickers (from `GetBooksAsync`), renders each verse with `VerseView`; clicking a word opens `StrongsPopover`. Include `OllamaStatusBanner` at top.

- [ ] **Step 2: `Search.razor`** (`@page "/search"`, optional `?q=` query) — a search box; on submit, run `SearchModeDetector.Detect` (unless a mode toggle is set):
  - Reference → parse and navigate to `/browse/{book}/{chapter}` (anchor to verse).
  - Strongs → list results from `GetVersesByStrongsAsync`.
  - Semantic → embed + `SemanticSearchAsync`; if Ollama unreachable, show the banner and disable this mode (reference/strongs still work).
  Each result links to its verse in Browse.

- [ ] **Step 3: Manual verify** (importer run + app running): browse John 3, click "God", see the popover; search `G2316` and `love`.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat(web): browse and search pages"
```

---

### Task 24: Chat page (streaming RAG)

**Files:**
- Create: `src/StrongKingJames.Web/Components/Pages/Chat.razor`

- [ ] **Step 1: `Chat.razor`** (`@page "/chat"`) — a question input and an answer area. On submit, call `IRagService.AnswerAsync` and append streamed chunks to the answer, calling `StateHasChanged()` per chunk. After completion, run `CitationExtractor.Extract` on the full answer and render each citation as a link to its verse in Browse. Show `OllamaStatusBanner`; if Ollama unreachable, disable the input with an explanatory message.

```razor
@page "/chat"
@using StrongKingJames.Core.Rag
@using StrongKingJames.Core.Services
@inject IRagService Rag
@rendermode InteractiveServer

<OllamaStatusBanner />
<textarea @bind="_question" disabled="@_busy"></textarea>
<button @onclick="AskAsync" disabled="@_busy">Ask</button>

<div class="answer">@_answer</div>
@if (_citations.Count > 0)
{
    <ul>@foreach (var c in _citations) { <li>@c</li> }</ul>
}

@code {
    private string _question = "";
    private string _answer = "";
    private bool _busy;
    private IReadOnlyList<string> _citations = [];

    private async Task AskAsync()
    {
        _busy = true; _answer = ""; _citations = [];
        await foreach (var chunk in Rag.AnswerAsync(_question))
        {
            _answer += chunk;
            StateHasChanged();
        }
        _citations = CitationExtractor.Extract(_answer);
        _busy = false;
    }
}
```

- [ ] **Step 2: Manual verify** (Ollama running, models pulled, importer run): ask "What does the Bible say about love?" — answer streams in with citations.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat(web): streaming RAG chat page with citations"
```

---

## Phase 8 — Packaging & release

### Task 25: README, LICENSE, Aspire-published Docker Compose, CI

The .NET services and PostgreSQL run in Docker; Ollama runs on the host. Rather than hand-maintain a Compose file, generate it from the Aspire AppHost (the single source of truth for topology) via the Docker Compose publisher, and commit the output. The generated Compose has no Ollama service — the containers point at the host's Ollama via `host.docker.internal`.

**Files:**
- Create: `LICENSE`, `README.md`, `.github/workflows/ci.yml`
- Create (generated): `docker-compose.yml` (via `aspire publish`)

- [ ] **Step 1: `LICENSE`** — MIT, copyright holder = the project owner, year 2026.

- [ ] **Step 2: Add the Docker Compose publisher to the AppHost**

In `AppHost.cs`, before `builder.Build().Run()`, add a Docker Compose environment so `aspire publish` emits Compose:
```csharp
builder.AddDockerComposeEnvironment("compose");
```
Run:
```bash
dotnet add src/StrongKingJames.AppHost package Aspire.Hosting.Docker
```
(Confirm the current package/API name for the Compose publisher against the installed Aspire version; it may be surfaced through `AddDockerComposeEnvironment` or an equivalent publisher API.)

- [ ] **Step 3: Generate the Compose file**

Set the `ollama-url` parameter to the host address reachable from containers, then publish:
```bash
aspire publish --project src/StrongKingJames.AppHost --publisher compose --output-path . -- --ollama-url http://host.docker.internal:11434
```
(Or set the parameter's published value via the AppHost. The key point: the generated env for db/importer/web uses `host.docker.internal`, not `localhost`.)
Expected: a `docker-compose.yml` (plus any `.env`) describing db (pgvector), importer, and web — **no ollama service** — with the right dependencies, volumes, and environment. On Linux hosts, add `extra_hosts: ["host.docker.internal:host-gateway"]` to the importer/web services if the generator doesn't. Review it; commit the generated file so users who only want `docker compose up` don't need the Aspire CLI.

- [ ] **Step 4: Ensure the importer's data is mountable in Compose**

The importer container needs the OSIS/dictionary XML. In `AppHost.cs`, bind-mount `./data` into the importer (`.WithBindMount("./data", "/data")`) so the generated Compose mounts it; document that users drop the three XML files into `./data` before `docker compose up`.

- [ ] **Step 5: `README.md`** — sections: what it is; prerequisites (Docker + host Ollama for end users; .NET 10 + Aspire CLI for developers); **host Ollama setup:** `ollama pull nomic-embed-text` and `ollama pull llama3.1` (with a note that Ollama must be listening on `0.0.0.0:11434` — set `OLLAMA_HOST=0.0.0.0` — so containers can reach it); download links for openscriptures `kjv.xml`, `strongshebrew.xml`, `strongsgreek.xml` (public-domain note) and the instruction to place them in `./data`; **Quick start (Docker):** `docker compose up` (db + importer seeds + web serves, all using the host's Ollama); **Developer start:** `aspire run --project src/StrongKingJames.AppHost`; architecture overview; contributing; license.

- [ ] **Step 6: Verify the full path**

With host Ollama running and models pulled:
```bash
docker compose up --build
```
Expected: db + importer + web containers start; importer reaches the host Ollama at `host.docker.internal:11434`, seeds and exits; web is reachable (default `http://localhost:8080`). Browse/Search/Chat work end-to-end.

- [ ] **Step 7: `.github/workflows/ci.yml`** — on push/PR: setup .NET 10, `dotnet build`, then `dotnet test` for the **Core** and **Importer** test projects only (Data tests need Docker; note them as local-only, matching the spec).

```yaml
name: CI
on: [push, pull_request]
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet build --configuration Release
      - run: dotnet test tests/StrongKingJames.Core.Tests --configuration Release
      - run: dotnet test tests/StrongKingJames.Importer.Tests --configuration Release
```

- [ ] **Step 6: Verify CI locally** — run the same build/test commands; confirm green.

Run: `dotnet build --configuration Release; dotnet test tests/StrongKingJames.Core.Tests; dotnet test tests/StrongKingJames.Importer.Tests`
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "chore: MIT license, README, docker-compose, and CI"
```

---

### Task 26: Final full-suite verification

- [ ] **Step 1: Run the entire test suite** (Docker running for Data tests)

Run: `dotnet test`
Expected: all test projects PASS.

- [ ] **Step 2: End-to-end smoke** — with host Ollama running (models pulled), bring the stack up (`docker compose up --build`, or `aspire run` for the dev path). Confirm: importer reaches host Ollama, seeds and exits, web serves; then verify Browse (click word → popover), Search (`G2316`, `love`), Chat (streamed answer with citations).

- [ ] **Step 3: Commit any fixes and tag**

```bash
git add -A
git commit -m "test: full suite green; e2e smoke verified"
git tag v0.1.0
```

---

## Notes for the implementer

- **Placement decisions locked in this plan:** Ollama services (`OllamaOptions`, `OllamaEmbeddingService`, `OllamaChatService`) and `RagService` live in **Core** (they depend only on Core interfaces and framework HTTP/JSON), so both Web and Importer reuse them without circular references. `OllamaHealth` lives in Web (UI concern).
- **Aspire is the orchestration source of truth.** The AppHost declares every resource; the `bible` connection string comes from `WithReference` injection and the Ollama base URL from the `Ollama__BaseUrl` env var. `aspire run` is the dev entry point; `aspire publish` generates the committed `docker-compose.yml`. Confirm the Docker Compose publisher's package/API name against the installed Aspire version — it moves faster than the framework, so adjust names as needed while keeping the topology described here.
- **Ollama runs on the host, not in Docker.** The user already has Ollama with models pulled. Containers reach it via `host.docker.internal:11434`; dev projects reach it via `localhost:11434`. PostgreSQL and the .NET services are the only containers. Ollama must listen on `0.0.0.0` (`OLLAMA_HOST=0.0.0.0`) for the containers to reach it.
- **Real data may differ from fixtures.** Tasks 13–14 use small hand-written fixtures. Before the smoke run, open the real openscriptures files and confirm element/attribute names (milestone verses, `lemma` format, dictionary `xlit`/pronunciation attributes); adjust parser + fixtures together, keeping tests green.
- **Embedding dimension** is fixed at 768 for `nomic-embed-text`. If a different embedding model is chosen, update `vector(768)` in the configuration/migration and the HNSW index.
- **Follow TDD**: each logic task writes the failing test first. Parsers, detectors, prompt builder, citation extractor, repository, and search service are all covered.
- Reference skills: @superpowers:test-driven-development, @superpowers:systematic-debugging when tests misbehave.
```


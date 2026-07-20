var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with the pgvector image, persistent volume.
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg16")
    .WithDataVolume();
var bibleDb = postgres.AddDatabase("bible", databaseName: "strongkingjames");

// Ollama runs on the HOST — reference it as an external endpoint, not a container.
var ollamaUrl = builder.AddParameter("ollama-url", "http://localhost:11434");

// Resolve the repo root so the importer finds the ./data files regardless of where
// Aspire executes the project from (defaults are relative to the working directory).
// Walk up from the AppHost output directory until we find the .git/data folder.
var repoRoot = AppContext.BaseDirectory;
while (!Directory.Exists(Path.Combine(repoRoot, "data")) && !Directory.Exists(Path.Combine(repoRoot, ".git")))
{
    var parent = Directory.GetParent(repoRoot)?.FullName;
    if (parent is null || parent == repoRoot) break;
    repoRoot = parent;
}

// Importer: runs to completion, seeds DB and backfills embeddings, then exits.
var importer = builder.AddProject<Projects.StrongKingJames_Importer>("importer")
    .WithReference(bibleDb).WaitFor(bibleDb)
    .WithEnvironment("Ollama__BaseUrl", ollamaUrl)
    .WithEnvironment("Importer__Format", "kjs")
    .WithEnvironment("Importer__KjsBiblePath", Path.Combine(repoRoot, "data", "kjv_pure.json"))
    .WithEnvironment("Importer__KjsStrongsPath", Path.Combine(repoRoot, "data", "strong_pure.json"))
    .WithEnvironment("Importer__KjsStrongsDictPath", Path.Combine(repoRoot, "data", "strong_dict.json"));

// Web app: waits for the importer to finish seeding before serving.
builder.AddProject<Projects.StrongKingJames_Web>("web")
    .WithReference(bibleDb).WaitFor(bibleDb)
    .WithEnvironment("Ollama__BaseUrl", ollamaUrl)
    .WaitForCompletion(importer);

builder.Build().Run();

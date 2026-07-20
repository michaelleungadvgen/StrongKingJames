var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with the pgvector image, persistent volume.
var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg16")
    .WithDataVolume();
var bibleDb = postgres.AddDatabase("bible", databaseName: "strongkingjames");

// Ollama runs on the HOST — reference it as an external endpoint, not a container.
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

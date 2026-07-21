@echo off
REM ============================================================================
REM  StrongKingJames launcher — brings up the whole app via .NET Aspire:
REM    * pgvector PostgreSQL (container, with a persistent data volume)
REM    * the importer (seeds the KJV + Strong's data, then backfills embeddings)
REM    * the Blazor web app
REM  Ollama is expected to be running on the HOST (not in a container).
REM ============================================================================
setlocal

echo(
echo === StrongKingJames ===
echo(

REM --- Make sure Ollama is reachable and the required models are pulled ---
where ollama >nul 2>&1
if %ERRORLEVEL%==0 (
    echo Ensuring Ollama models are available ^(this is quick if already pulled^)...
    ollama pull nomic-embed-text
    ollama pull llama3.1:8b
) else (
    echo WARNING: 'ollama' not found on PATH. Make sure Ollama is installed and running,
    echo          listening on 0.0.0.0:11434 ^(set OLLAMA_HOST=0.0.0.0^), with the models:
    echo            ollama pull nomic-embed-text
    echo            ollama pull llama3.1:8b
)

echo(
echo Starting Aspire AppHost ^(first run seeds the database and embeds ~31k verses^)...
echo The Aspire dashboard URL will be printed below.
echo(

aspire run --project "%~dp0src\StrongKingJames.AppHost"

endlocal

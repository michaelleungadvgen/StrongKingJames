using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Core.Models;
using StrongKingJames.Core.Services;
using StrongKingJames.Data.Entities;

namespace StrongKingJames.Data.Repositories;

public class NoteRepository(BibleDbContext db) : INoteRepository
{
    public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default) =>
        await db.Notes.OrderByDescending(n => n.UpdatedAt).ToListAsync(ct);

    public async Task<Note?> GetAsync(int id, CancellationToken ct = default) =>
        await db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<Note> AddAsync(Note note, CancellationToken ct = default)
    {
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = note.CreatedAt;
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);
        return note;
    }

    public async Task UpdateAsync(Note note, CancellationToken ct = default)
    {
        note.UpdatedAt = DateTime.UtcNow;
        db.Notes.Update(note);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (note is null) return;
        db.Notes.Remove(note); // cascade removes the embedding row
        await db.SaveChangesAsync(ct);
    }

    public async Task SetEmbeddingAsync(int noteId, float[] embedding, CancellationToken ct = default)
    {
        var vector = new Vector(embedding);
        var existing = await db.NoteEmbeddings.FirstOrDefaultAsync(e => e.NoteId == noteId, ct);
        if (existing is null)
            db.NoteEmbeddings.Add(new NoteEmbedding { NoteId = noteId, Embedding = vector });
        else
            existing.Embedding = vector;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NoteSearchResult>> SemanticSearchNotesAsync(
        float[] queryEmbedding, int topK, CancellationToken ct = default)
    {
        var q = new Vector(queryEmbedding);
        return await db.NoteEmbeddings
            .Select(e => new { e.NoteId, Distance = e.Embedding.CosineDistance(q) })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Join(db.Notes, x => x.NoteId, n => n.Id, (x, n) => new { x.Distance, n })
            .Select(x => new NoteSearchResult(
                x.n.Id, x.n.Type, x.n.Reference, x.n.Title, x.n.Body, 1.0 - x.Distance))
            .ToListAsync(ct);
    }
}

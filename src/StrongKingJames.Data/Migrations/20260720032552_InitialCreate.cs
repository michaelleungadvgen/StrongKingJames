using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace StrongKingJames.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Abbreviation = table.Column<string>(type: "text", nullable: false),
                    Testament = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "strongs_entries",
                columns: table => new
                {
                    Number = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Lemma = table.Column<string>(type: "text", nullable: false),
                    Transliteration = table.Column<string>(type: "text", nullable: false),
                    Pronunciation = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    KjvUsage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strongs_entries", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "verse_embeddings",
                columns: table => new
                {
                    VerseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verse_embeddings", x => x.VerseId);
                });

            migrationBuilder.CreateTable(
                name: "verses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Chapter = table.Column<int>(type: "integer", nullable: false),
                    VerseNumber = table.Column<int>(type: "integer", nullable: false),
                    OsisId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_verses_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verse_words",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VerseId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    WordText = table.Column<string>(type: "text", nullable: false),
                    StrongsNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verse_words", x => x.Id);
                    table.ForeignKey(
                        name: "FK_verse_words_verses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "verses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_books_Abbreviation",
                table: "books",
                column: "Abbreviation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verse_words_StrongsNumber",
                table: "verse_words",
                column: "StrongsNumber");

            migrationBuilder.CreateIndex(
                name: "IX_verse_words_VerseId_Position",
                table: "verse_words",
                columns: new[] { "VerseId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_verses_BookId_Chapter_VerseNumber",
                table: "verses",
                columns: new[] { "BookId", "Chapter", "VerseNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_verses_OsisId",
                table: "verses",
                column: "OsisId",
                unique: true);

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_verse_embeddings_hnsw " +
                "ON verse_embeddings USING hnsw (\"Embedding\" vector_cosine_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_verse_embeddings_hnsw;");

            migrationBuilder.DropTable(
                name: "strongs_entries");

            migrationBuilder.DropTable(
                name: "verse_embeddings");

            migrationBuilder.DropTable(
                name: "verse_words");

            migrationBuilder.DropTable(
                name: "verses");

            migrationBuilder.DropTable(
                name: "books");
        }
    }
}

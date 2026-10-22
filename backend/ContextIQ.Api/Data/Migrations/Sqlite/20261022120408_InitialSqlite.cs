using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContextIQ.Api.Data.Migrations.Sqlite
{
    public partial class InitialSqlite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Energy = table.Column<double>(type: "REAL", nullable: false),
                    MoodTags = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                    table.CheckConstraint("CK_Catalog_Energy", "\"Energy\" >= 0 AND \"Energy\" <= 1");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuestSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoodEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DetectedMood = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Energy = table.Column<double>(type: "REAL", nullable: false),
                    Valence = table.Column<double>(type: "REAL", nullable: false),
                    Themes = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    AnalysisMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoodEntries", x => x.Id);
                    table.UniqueConstraint("AK_MoodEntries_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_Mood_Ranges", "\"Energy\" >= 0 AND \"Energy\" <= 1 AND \"Valence\" >= -1 AND \"Valence\" <= 1 AND \"Confidence\" >= 0 AND \"Confidence\" <= 1");
                    table.ForeignKey(
                        name: "FK_MoodEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FavoriteGenres = table.Column<string>(type: "TEXT", nullable: false),
                    DislikedGenres = table.Column<string>(type: "TEXT", nullable: false),
                    FavoriteCategories = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MoodEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CatalogItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.UniqueConstraint("AK_Recommendations_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_Recommendation_Score", "\"Score\" >= 0 AND \"Score\" <= 1");
                    table.ForeignKey(
                        name: "FK_Recommendations_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recommendations_MoodEntries_MoodEntryId_UserId",
                        columns: x => new { x.MoodEntryId, x.UserId },
                        principalTable: "MoodEntries",
                        principalColumns: new[] { "Id", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recommendations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.CheckConstraint("CK_Feedback_Rating", "\"Rating\" IN (-1, 1)");
                    table.ForeignKey(
                        name: "FK_Feedbacks_Recommendations_RecommendationId_UserId",
                        columns: x => new { x.RecommendationId, x.UserId },
                        principalTable: "Recommendations",
                        principalColumns: new[] { "Id", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Category",
                table: "CatalogItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_SourceId",
                table: "CatalogItems",
                column: "SourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_RecommendationId_UserId",
                table: "Feedbacks",
                columns: new[] { "RecommendationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_UserId_RecommendationId",
                table: "Feedbacks",
                columns: new[] { "UserId", "RecommendationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_ExpiresAt",
                table: "GuestSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_TokenHash",
                table: "GuestSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_UserId",
                table: "GuestSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MoodEntries_UserId_CreatedAt_Id",
                table: "MoodEntries",
                columns: new[] { "UserId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_CatalogItemId",
                table: "Recommendations",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_MoodEntryId_CatalogItemId",
                table: "Recommendations",
                columns: new[] { "MoodEntryId", "CatalogItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_MoodEntryId_UserId",
                table: "Recommendations",
                columns: new[] { "MoodEntryId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_UserId_CreatedAt",
                table: "Recommendations",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "GuestSessions");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "CatalogItems");

            migrationBuilder.DropTable(
                name: "MoodEntries");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

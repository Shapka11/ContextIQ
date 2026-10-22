using System;
using ContextIQ.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContextIQ.Api.Data.Migrations.Postgres
{
    [DbContext(typeof(PostgresAppDbContext))]
    partial class PostgresAppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ContextIQ.Api.Models.CatalogItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<double>("Energy")
                        .HasColumnType("double precision");

                    b.Property<string>("ExternalUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.PrimitiveCollection<string>("MoodTags")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.PrimitiveCollection<string>("Tags")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.HasKey("Id");

                    b.HasIndex("Category");

                    b.HasIndex("SourceId")
                        .IsUnique();

                    b.ToTable("CatalogItems", t =>
                        {
                            t.HasCheckConstraint("CK_Catalog_Energy", "\"Energy\" >= 0 AND \"Energy\" <= 1");
                        });
                });

            modelBuilder.Entity("ContextIQ.Api.Models.Feedback", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Rating")
                        .HasColumnType("integer");

                    b.Property<Guid>("RecommendationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RecommendationId", "UserId");

                    b.HasIndex("UserId", "RecommendationId")
                        .IsUnique();

                    b.ToTable("Feedbacks", t =>
                        {
                            t.HasCheckConstraint("CK_Feedback_Rating", "\"Rating\" IN (-1, 1)");
                        });
                });

            modelBuilder.Entity("ContextIQ.Api.Models.GuestSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ExpiresAt");

                    b.HasIndex("TokenHash")
                        .IsUnique();

                    b.HasIndex("UserId");

                    b.ToTable("GuestSessions");
                });

            modelBuilder.Entity("ContextIQ.Api.Models.MoodEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AnalysisMode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<double>("Confidence")
                        .HasColumnType("double precision");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DetectedMood")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("Emoji")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<double>("Energy")
                        .HasColumnType("double precision");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasMaxLength(8000)
                        .HasColumnType("character varying(8000)");

                    b.PrimitiveCollection<string>("Themes")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<double>("Valence")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "CreatedAt", "Id");

                    b.ToTable("MoodEntries", t =>
                        {
                            t.HasCheckConstraint("CK_Mood_Ranges", "\"Energy\" >= 0 AND \"Energy\" <= 1 AND \"Valence\" >= -1 AND \"Valence\" <= 1 AND \"Confidence\" >= 0 AND \"Confidence\" <= 1");
                        });
                });

            modelBuilder.Entity("ContextIQ.Api.Models.Recommendation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CatalogItemId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Explanation")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<Guid>("MoodEntryId")
                        .HasColumnType("uuid");

                    b.Property<double>("Score")
                        .HasColumnType("double precision");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CatalogItemId");

                    b.HasIndex("MoodEntryId", "CatalogItemId")
                        .IsUnique();

                    b.HasIndex("MoodEntryId", "UserId");

                    b.HasIndex("UserId", "CreatedAt");

                    b.ToTable("Recommendations", t =>
                        {
                            t.HasCheckConstraint("CK_Recommendation_Score", "\"Score\" >= 0 AND \"Score\" <= 1");
                        });
                });

            modelBuilder.Entity("ContextIQ.Api.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ContextIQ.Api.Models.UserProfile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.PrimitiveCollection<string>("DislikedGenres")
                        .IsRequired()
                        .HasColumnType("text");

                    b.PrimitiveCollection<string>("FavoriteCategories")
                        .IsRequired()
                        .HasColumnType("text");

                    b.PrimitiveCollection<string>("FavoriteGenres")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserProfiles");
                });

            modelBuilder.Entity("ContextIQ.Api.Models.Feedback", b =>
                {
                    b.HasOne("ContextIQ.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ContextIQ.Api.Models.Recommendation", null)
                        .WithMany()
                        .HasForeignKey("RecommendationId", "UserId")
                        .HasPrincipalKey("Id", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ContextIQ.Api.Models.GuestSession", b =>
                {
                    b.HasOne("ContextIQ.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ContextIQ.Api.Models.MoodEntry", b =>
                {
                    b.HasOne("ContextIQ.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ContextIQ.Api.Models.Recommendation", b =>
                {
                    b.HasOne("ContextIQ.Api.Models.CatalogItem", "CatalogItem")
                        .WithMany()
                        .HasForeignKey("CatalogItemId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ContextIQ.Api.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ContextIQ.Api.Models.MoodEntry", null)
                        .WithMany()
                        .HasForeignKey("MoodEntryId", "UserId")
                        .HasPrincipalKey("Id", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CatalogItem");
                });

            modelBuilder.Entity("ContextIQ.Api.Models.UserProfile", b =>
                {
                    b.HasOne("ContextIQ.Api.Models.User", null)
                        .WithOne()
                        .HasForeignKey("ContextIQ.Api.Models.UserProfile", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

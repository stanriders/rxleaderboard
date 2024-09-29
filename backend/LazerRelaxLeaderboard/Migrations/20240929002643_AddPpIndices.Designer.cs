﻿// <auto-generated />
using System;
using LazerRelaxLeaderboard.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LazerRelaxLeaderboard.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240929002643_AddPpIndices")]
    partial class AddPpIndices
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LazerRelaxLeaderboard.Database.Models.Beatmap", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double>("ApproachRate")
                        .HasColumnType("double precision");

                    b.Property<string>("Artist")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("BeatmapSetId")
                        .HasColumnType("integer");

                    b.Property<double>("BeatsPerMinute")
                        .HasColumnType("double precision");

                    b.Property<double>("CircleSize")
                        .HasColumnType("double precision");

                    b.Property<int>("Circles")
                        .HasColumnType("integer");

                    b.Property<int>("CreatorId")
                        .HasColumnType("integer");

                    b.Property<string>("DifficultyName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("HealthDrain")
                        .HasColumnType("double precision");

                    b.Property<double>("OverallDifficulty")
                        .HasColumnType("double precision");

                    b.Property<int>("Sliders")
                        .HasColumnType("integer");

                    b.Property<int>("Spinners")
                        .HasColumnType("integer");

                    b.Property<double?>("StarRating")
                        .HasColumnType("double precision");

                    b.Property<double>("StarRatingNormal")
                        .HasColumnType("double precision");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Beatmaps");
                });

            modelBuilder.Entity("LazerRelaxLeaderboard.Database.Models.Score", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<double>("Accuracy")
                        .HasColumnType("double precision");

                    b.Property<int>("BeatmapId")
                        .HasColumnType("integer");

                    b.Property<int>("Combo")
                        .HasColumnType("integer");

                    b.Property<int>("Count100")
                        .HasColumnType("integer");

                    b.Property<int>("Count300")
                        .HasColumnType("integer");

                    b.Property<int>("Count50")
                        .HasColumnType("integer");

                    b.Property<int>("CountMiss")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Grade")
                        .HasColumnType("integer");

                    b.Property<string[]>("Mods")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<double?>("Pp")
                        .HasColumnType("double precision");

                    b.Property<int>("TotalScore")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Pp");

                    b.HasIndex("UserId");

                    b.ToTable("Scores");
                });

            modelBuilder.Entity("LazerRelaxLeaderboard.Database.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("CountryCode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double?>("TotalAccuracy")
                        .HasColumnType("double precision");

                    b.Property<double?>("TotalPp")
                        .HasColumnType("double precision");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("TotalPp");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("LazerRelaxLeaderboard.Database.Models.Score", b =>
                {
                    b.HasOne("LazerRelaxLeaderboard.Database.Models.User", "User")
                        .WithMany("Scores")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("LazerRelaxLeaderboard.Database.Models.User", b =>
                {
                    b.Navigation("Scores");
                });
#pragma warning restore 612, 618
        }
    }
}

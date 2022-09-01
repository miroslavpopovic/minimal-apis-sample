﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MinimalApis.MinimalSample.Data;
using MinimalApis.MinimalSample.Refactored.Data;

#nullable disable

namespace MinimalApis.MinimalSample.Data.Migrations
{
    [DbContext(typeof(TimeTrackerDbContext))]
    partial class TimeTrackerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.8");

            modelBuilder.Entity("MinimalApis.MvcSample.Domain.Client", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Clients");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            Name = "Client 1"
                        },
                        new
                        {
                            Id = 2L,
                            Name = "Client 2"
                        });
                });

            modelBuilder.Entity("MinimalApis.MvcSample.Domain.Project", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("ClientId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.ToTable("Projects");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            ClientId = 1L,
                            Name = "Project 1"
                        },
                        new
                        {
                            Id = 2L,
                            ClientId = 1L,
                            Name = "Project 2"
                        },
                        new
                        {
                            Id = 3L,
                            ClientId = 2L,
                            Name = "Project 3"
                        });
                });

            modelBuilder.Entity("MinimalApis.MvcSample.Domain.TimeEntry", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EntryDate")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("HourRate")
                        .HasColumnType("TEXT");

                    b.Property<int>("Hours")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.HasIndex("UserId");

                    b.ToTable("TimeEntries");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            Description = "Time entry description 1",
                            EntryDate = new DateTime(2022, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            HourRate = 25m,
                            Hours = 5,
                            ProjectId = 1L,
                            UserId = 1L
                        },
                        new
                        {
                            Id = 2L,
                            Description = "Time entry description 2",
                            EntryDate = new DateTime(2022, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            HourRate = 25m,
                            Hours = 2,
                            ProjectId = 2L,
                            UserId = 1L
                        },
                        new
                        {
                            Id = 3L,
                            Description = "Time entry description 3",
                            EntryDate = new DateTime(2022, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            HourRate = 25m,
                            Hours = 1,
                            ProjectId = 3L,
                            UserId = 1L
                        },
                        new
                        {
                            Id = 4L,
                            Description = "Time entry description 4",
                            EntryDate = new DateTime(2022, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            HourRate = 30m,
                            Hours = 8,
                            ProjectId = 3L,
                            UserId = 2L
                        });
                });

            modelBuilder.Entity("MinimalApis.MvcSample.Domain.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("HourRate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            HourRate = 25m,
                            Name = "John Doe"
                        },
                        new
                        {
                            Id = 2L,
                            HourRate = 30m,
                            Name = "Joan Doe"
                        });
                });

            modelBuilder.Entity("MinimalApis.MvcSample.Domain.Project", b =>
                {
                    b.HasOne("MinimalApis.MvcSample.Domain.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");
                });

            modelBuilder.Entity("MinimalApis.MvcSample.Domain.TimeEntry", b =>
                {
                    b.HasOne("MinimalApis.MvcSample.Domain.Project", "Project")
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MinimalApis.MvcSample.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}

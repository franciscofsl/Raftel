﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Raftel.Data.Tests.Types.Models;

#nullable disable

namespace Raftel.Data.Tests.Migrations
{
    [DbContext(typeof(TestingDbContext))]
    [Migration("20240928102539_AddProperties")]
    partial class AddProperties
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Raftel.Data.Outbox.OutboxMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Content")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("OccurredOn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ProcessedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Type")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("OutboxMessages");
                });

            modelBuilder.Entity("Raftel.Data.Tests.Types.Models.SampleAggregate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("IntegerValue")
                        .HasColumnType("int");

                    b.Property<bool>("Processed")
                        .HasColumnType("bit");

                    b.Property<string>("StringValue")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SampleModels");
                });

            modelBuilder.Entity("Raftel.Data.Tests.Types.Models.SampleNotAuditedAggregate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Processed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("SampleNotAuditedAggregates");
                });
#pragma warning restore 612, 618
        }
    }
}
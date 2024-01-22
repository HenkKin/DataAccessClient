﻿// <auto-generated />
using System;
using DataAccessClientExample.DataLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DataAccessClientExample.Migrations.ExampleDatabase
{
    [DbContext(typeof(ExampleDbContext))]
    partial class ExampleDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CreatedById")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("DeletedById")
                        .HasColumnType("int");

                    b.Property<DateTime?>("DeletedOn")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int?>("ModifiedById")
                        .HasColumnType("int");

                    b.Property<DateTime?>("ModifiedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ExampleEntities", (string)null);
                });

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleEntityTranslation", b =>
                {
                    b.Property<int>("TranslatedEntityId")
                        .HasColumnType("int");

                    b.Property<string>("LocaleId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("TranslatedEntityId", "LocaleId");

                    b.ToTable("ExampleEntityTranslation", (string)null);
                });

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleEntityView", b =>
                {
                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<string>("LocaleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.ToTable((string)null);

                    b.ToSqlQuery("SELECT e.Id, et.LocaleId, e.Name, et.Description FROM dbo.ExampleEntities e INNER JOIN dbo.ExampleEntityTranslation et ON e.Id = et.TranslatedEntityId");
                });

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleEntityTranslation", b =>
                {
                    b.HasOne("DataAccessClientExample.DataLayer.ExampleEntity", "TranslatedEntity")
                        .WithMany("Translations")
                        .HasForeignKey("TranslatedEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TranslatedEntity");
                });

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleEntity", b =>
                {
                    b.Navigation("Translations");
                });
#pragma warning restore 612, 618
        }
    }
}
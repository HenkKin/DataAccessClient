﻿// <auto-generated />
using System;
using DataAccessClientExample.DataLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DataAccessClientExample.Migrations.ExampleSecondDatabase
{
    [DbContext(typeof(ExampleSecondDbContext))]
    [Migration("20191015143848_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleSecondEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.HasKey("Id");

                    b.ToTable("ExampleSecondEntities");
                });

            modelBuilder.Entity("DataAccessClientExample.DataLayer.ExampleSecondEntity", b =>
                {
                    b.OwnsOne("DataAccessClient.EntityBehaviors.TranslatedProperty", "Code", b1 =>
                        {
                            b1.Property<int>("ExampleSecondEntityId")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int")
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.HasKey("ExampleSecondEntityId");

                            b1.ToTable("ExampleSecondEntities");

                            b1.WithOwner()
                                .HasForeignKey("ExampleSecondEntityId");

                            b1.OwnsMany("DataAccessClient.EntityBehaviors.PropertyTranslation", "Translations", b2 =>
                                {
                                    b2.Property<int>("OwnerId")
                                        .HasColumnType("int");

                                    b2.Property<int>("Id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("int")
                                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                                    b2.Property<string>("Language")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.Property<string>("Translation")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("OwnerId", "Id");

                                    b2.ToTable("ExampleSecondEntity_CodeTranslations");

                                    b2.WithOwner()
                                        .HasForeignKey("OwnerId");
                                });
                        });

                    b.OwnsOne("DataAccessClient.EntityBehaviors.TranslatedProperty", "Description", b1 =>
                        {
                            b1.Property<int>("ExampleSecondEntityId")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int")
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.HasKey("ExampleSecondEntityId");

                            b1.ToTable("ExampleSecondEntities");

                            b1.WithOwner()
                                .HasForeignKey("ExampleSecondEntityId");

                            b1.OwnsMany("DataAccessClient.EntityBehaviors.PropertyTranslation", "Translations", b2 =>
                                {
                                    b2.Property<int>("OwnerId")
                                        .HasColumnType("int");

                                    b2.Property<int>("Id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("int")
                                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                                    b2.Property<string>("Language")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.Property<string>("Translation")
                                        .IsRequired()
                                        .HasColumnType("nvarchar(max)");

                                    b2.HasKey("OwnerId", "Id");

                                    b2.ToTable("ExampleSecondEntity_DescriptionTranslations");

                                    b2.WithOwner()
                                        .HasForeignKey("OwnerId");
                                });
                        });
                });
#pragma warning restore 612, 618
        }
    }
}

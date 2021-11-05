﻿// <auto-generated />
using System;
using CocurentTransaction.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ConcurentTransaction.Migrations
{
    [DbContext(typeof(WalletContext))]
    partial class WalletContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("CocurentTransaction.Models.Wallet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Balance")
                        .HasColumnType("decimal(18,2)");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("Wallets");

                    b.HasData(
                        new
                        {
                            Id = new Guid("6816bd4e-296f-414d-a196-2833d1a980d7"),
                            Amount = 0m,
                            Balance = 0m,
                            UserId = new Guid("05fbaad2-994a-4f5f-9ff6-ad5023514fb3")
                        },
                        new
                        {
                            Id = new Guid("fb3d9286-c971-4a6e-9028-59493ff143fc"),
                            Amount = 0m,
                            Balance = 0m,
                            UserId = new Guid("8a36dd49-e7af-4957-8707-dad7e06e8dc7")
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
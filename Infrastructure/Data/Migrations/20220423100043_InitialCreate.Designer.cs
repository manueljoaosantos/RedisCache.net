﻿// <auto-generated />
using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    [DbContext(typeof(FeedContext))]
    [Migration("20220423100043_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.4");

            modelBuilder.Entity("Core.Entities.ArticleMatrix", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Author")
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<int>("Likes")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("PubDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .HasColumnType("TEXT");

                    b.Property<string>("Views")
                        .HasColumnType("TEXT");

                    b.Property<double>("ViewsCount")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("ArticleMatrices");
                });
#pragma warning restore 612, 618
        }
    }
}
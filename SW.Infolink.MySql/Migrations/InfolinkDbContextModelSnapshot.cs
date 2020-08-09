﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SW.Infolink;

namespace SW.Infolink.MySql.Migrations
{
    [DbContext(typeof(InfolinkDbContext))]
    partial class InfolinkDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("SW.Infolink.Domain.AccessKeySet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Key1")
                        .IsRequired()
                        .HasColumnType("varchar(1024) CHARACTER SET utf8mb4")
                        .HasMaxLength(1024)
                        .IsUnicode(false);

                    b.Property<string>("Key2")
                        .IsRequired()
                        .HasColumnType("varchar(1024) CHARACTER SET utf8mb4")
                        .HasMaxLength(1024)
                        .IsUnicode(false);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(200) CHARACTER SET utf8mb4")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.ToTable("AccessKeySets");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Adapter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(200) CHARACTER SET utf8mb4")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("Properties")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("ServerlessId")
                        .HasColumnType("varchar(200) CHARACTER SET utf8mb4")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<int>("Timeout")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Adapters");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Document", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<bool>("BusEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("BusMessageTypeName")
                        .HasColumnType("varchar(500) CHARACTER SET utf8mb4")
                        .HasMaxLength(500)
                        .IsUnicode(false);

                    b.Property<int>("DuplicateInterval")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<string>("PromotedProperties")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Receiver", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<string>("Properties")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime?>("ReceiveOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("ReceiverId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Receivers");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Subscriber", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("Aggregate")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("DocumentFilter")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<int>("HandlerId")
                        .HasColumnType("int");

                    b.Property<bool>("Inactive")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("KeySetId")
                        .HasColumnType("int");

                    b.Property<int>("MapperId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100) CHARACTER SET utf8mb4")
                        .HasMaxLength(100);

                    b.Property<string>("Properties")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("ResponseSubscriberId")
                        .HasColumnType("int");

                    b.Property<bool>("Temporary")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("Subscribers");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Xchange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime?>("DeliverOn")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("DeliveredOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<string>("Exception")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<DateTime?>("FinishedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("HandlerId")
                        .HasColumnType("int");

                    b.Property<string>("InputFileHash")
                        .IsRequired()
                        .HasColumnType("varchar(50) CHARACTER SET utf8mb4")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("InputFileName")
                        .HasColumnType("varchar(200) CHARACTER SET utf8mb4")
                        .HasMaxLength(200);

                    b.Property<int>("InputFileSize")
                        .HasColumnType("int");

                    b.Property<int>("MapperId")
                        .HasColumnType("int");

                    b.Property<string>("References")
                        .HasColumnType("varchar(1024) CHARACTER SET utf8mb4")
                        .HasMaxLength(1024);

                    b.Property<int>("ResponseXchangeId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<int>("SubscriberId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("DeliverOn");

                    b.HasIndex("DeliveredOn");

                    b.HasIndex("InputFileHash");

                    b.HasIndex("SubscriberId");

                    b.ToTable("Xchanges");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeBlob", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte[]>("Content")
                        .HasColumnType("longblob");

                    b.HasKey("Id", "Type");

                    b.ToTable("XchangeFiles");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Receiver", b =>
                {
                    b.OwnsMany("SW.Infolink.Schedule", "Schedules", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            b1.Property<bool>("Backwards")
                                .HasColumnType("tinyint(1)");

                            b1.Property<TimeSpan>("On")
                                .HasColumnType("time(6)");

                            b1.Property<int>("ReceiverId")
                                .HasColumnType("int");

                            b1.Property<byte>("Recurrence")
                                .HasColumnName("Recurrence")
                                .HasColumnType("tinyint unsigned");

                            b1.HasKey("Id");

                            b1.HasIndex("ReceiverId");

                            b1.ToTable("ReceiverSchedules");

                            b1.WithOwner()
                                .HasForeignKey("ReceiverId");
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Subscriber", b =>
                {
                    b.OwnsMany("SW.Infolink.Schedule", "Schedules", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            b1.Property<bool>("Backwards")
                                .HasColumnType("tinyint(1)");

                            b1.Property<TimeSpan>("On")
                                .HasColumnType("time(6)");

                            b1.Property<byte>("Recurrence")
                                .HasColumnName("Recurrence")
                                .HasColumnType("tinyint unsigned");

                            b1.Property<int>("SubscriberId")
                                .HasColumnType("int");

                            b1.HasKey("Id");

                            b1.HasIndex("SubscriberId");

                            b1.ToTable("SubscriberSchedules");

                            b1.WithOwner()
                                .HasForeignKey("SubscriberId");
                        });
                });
#pragma warning restore 612, 618
        }
    }
}

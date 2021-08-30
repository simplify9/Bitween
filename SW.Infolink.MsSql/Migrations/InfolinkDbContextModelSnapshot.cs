﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SW.Infolink;

namespace SW.Infolink.MsSql.Migrations
{
    [DbContext(typeof(InfolinkDbContext))]
    partial class InfolinkDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SW.Infolink.Domain.Document", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<bool>("BusEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("BusMessageTypeName")
                        .HasColumnType("varchar(500)")
                        .HasMaxLength(500)
                        .IsUnicode(false);

                    b.Property<int>("DuplicateInterval")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("PromotedProperties")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BusMessageTypeName")
                        .IsUnique()
                        .HasFilter("[BusMessageTypeName] IS NOT NULL");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Documents");

                    b.HasData(
                        new
                        {
                            Id = 10001,
                            BusEnabled = false,
                            DuplicateInterval = 0,
                            Name = "Aggregation Document",
                            PromotedProperties = "{}"
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Notifier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("HandlerId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("HandlerProperties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Inactive")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)")
                        .HasMaxLength(100);

                    b.Property<bool>("RunOnBadResult")
                        .HasColumnType("bit");

                    b.Property<bool>("RunOnFailedResult")
                        .HasColumnType("bit");

                    b.Property<bool>("RunOnSuccessfulResult")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Notifiers");
                });

            modelBuilder.Entity("SW.Infolink.Domain.OnHoldXchange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("BadData")
                        .HasColumnType("bit");

                    b.Property<string>("Data")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FileName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("References")
                        .HasColumnType("nvarchar(1024)")
                        .HasMaxLength(1024);

                    b.Property<int>("SubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("OnHoldXchanges");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.ToTable("Partners");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "SYSTEM"
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Subscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("AggregateOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("AggregationForId")
                        .HasColumnType("int");

                    b.Property<byte>("AggregationTarget")
                        .HasColumnType("tinyint");

                    b.Property<int>("ConsecutiveFailures")
                        .HasColumnType("int");

                    b.Property<string>("DocumentFilter")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<string>("HandlerId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("HandlerProperties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Inactive")
                        .HasColumnType("bit");

                    b.Property<string>("LastException")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MapperId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("MapperProperties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(100)")
                        .HasMaxLength(100);

                    b.Property<int?>("PartnerId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("PausedOn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("ReceiveOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("ReceiverId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("ReceiverProperties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ResponseMessageTypeName")
                        .HasColumnType("varchar(500)")
                        .HasMaxLength(500)
                        .IsUnicode(false);

                    b.Property<int?>("ResponseSubscriptionId")
                        .HasColumnType("int");

                    b.Property<bool>("Temporary")
                        .HasColumnType("bit");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.Property<string>("ValidatorId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("ValidatorProperties")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AggregationForId");

                    b.HasIndex("DocumentId");

                    b.HasIndex("PartnerId");

                    b.HasIndex("ResponseSubscriptionId");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Xchange", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("CorrelationId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<string>("HandlerId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("HandlerProperties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("InputContentType")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("InputHash")
                        .IsRequired()
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("InputName")
                        .HasColumnType("nvarchar(200)")
                        .HasMaxLength(200);

                    b.Property<int>("InputSize")
                        .HasColumnType("int");

                    b.Property<string>("MapperId")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("MapperProperties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("References")
                        .HasColumnType("nvarchar(1024)")
                        .HasMaxLength(1024);

                    b.Property<string>("ResponseMessageTypeName")
                        .HasColumnType("varchar(500)")
                        .HasMaxLength(500)
                        .IsUnicode(false);

                    b.Property<int?>("ResponseSubscriptionId")
                        .HasColumnType("int");

                    b.Property<string>("RetryFor")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<DateTime>("StartedOn")
                        .HasColumnType("datetime2");

                    b.Property<int?>("SubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("InputHash");

                    b.HasIndex("RetryFor");

                    b.HasIndex("StartedOn");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("Xchanges");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeAggregation", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<DateTime>("AggregatedOn")
                        .HasColumnType("datetime2");

                    b.Property<string>("AggregationXchangeId")
                        .IsRequired()
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.HasIndex("AggregationXchangeId");

                    b.ToTable("XchangeAggregations");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeDelivery", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<DateTime>("DeliveredOn")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("DeliveredOn");

                    b.ToTable("XchangeDeliveries");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Exception")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("FinishedOn")
                        .HasColumnType("datetime2");

                    b.Property<int>("NotifierId")
                        .HasColumnType("int");

                    b.Property<string>("NotifierName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Success")
                        .HasColumnType("bit");

                    b.Property<string>("XchangeId")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.ToTable("XchangeNotifications");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangePromotedProperties", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("Hits")
                        .HasColumnType("varchar(2000)")
                        .HasMaxLength(2000)
                        .IsUnicode(false);

                    b.Property<string>("Properties")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertiesRaw")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("PropertiesRaw");

                    b.ToTable("XchangePromotedProperties");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeResult", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("Exception")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("FinishedOn")
                        .HasColumnType("datetime2");

                    b.Property<bool>("OutputBad")
                        .HasColumnType("bit");

                    b.Property<string>("OutputContentType")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("OutputHash")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("OutputName")
                        .HasColumnType("nvarchar(200)")
                        .HasMaxLength(200);

                    b.Property<int>("OutputSize")
                        .HasColumnType("int");

                    b.Property<bool>("ResponseBad")
                        .HasColumnType("bit");

                    b.Property<string>("ResponseContentType")
                        .HasColumnType("varchar(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("ResponseHash")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("ResponseName")
                        .HasColumnType("nvarchar(200)")
                        .HasMaxLength(200);

                    b.Property<int>("ResponseSize")
                        .HasColumnType("int");

                    b.Property<string>("ResponseXchangeId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Success")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("XchangeResults");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.OwnsMany("SW.Infolink.Domain.ApiCredential", "ApiCredentials", b1 =>
                        {
                            b1.Property<int>("PartnerId")
                                .HasColumnType("int");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int")
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<string>("Key")
                                .IsRequired()
                                .HasColumnType("varchar(500)")
                                .HasMaxLength(500)
                                .IsUnicode(false);

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasColumnType("nvarchar(500)")
                                .HasMaxLength(500);

                            b1.HasKey("PartnerId", "Id");

                            b1.HasIndex("Key")
                                .IsUnique();

                            b1.ToTable("PartnerApiCredentials");

                            b1.WithOwner()
                                .HasForeignKey("PartnerId");

                            b1.HasData(
                                new
                                {
                                    PartnerId = 1,
                                    Id = 1,
                                    Key = "7facc758283844b49cc4ffd26a75b1de",
                                    Name = "default"
                                });
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Subscription", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Subscription", null)
                        .WithMany()
                        .HasForeignKey("AggregationForId")
                        .HasConstraintName("FK_Subscriptions_AggFor")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SW.Infolink.Domain.Document", null)
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SW.Infolink.Domain.Partner", null)
                        .WithMany("Subscriptions")
                        .HasForeignKey("PartnerId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SW.Infolink.Domain.Subscription", null)
                        .WithMany()
                        .HasForeignKey("ResponseSubscriptionId")
                        .HasConstraintName("FK_Subscriptions_RespSub")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.OwnsMany("SW.Infolink.Domain.Schedule", "Schedules", b1 =>
                        {
                            b1.Property<int>("SubscriptionId")
                                .HasColumnType("int");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int")
                                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                            b1.Property<bool>("Backwards")
                                .HasColumnType("bit");

                            b1.Property<long>("On")
                                .HasColumnType("bigint");

                            b1.Property<byte>("Recurrence")
                                .HasColumnType("tinyint");

                            b1.HasKey("SubscriptionId", "Id");

                            b1.ToTable("SubscriptionSchedules");

                            b1.WithOwner()
                                .HasForeignKey("SubscriptionId");
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Xchange", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Document", null)
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeAggregation", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangeAggregation", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeDelivery", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangeDelivery", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangePromotedProperties", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangePromotedProperties", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeResult", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangeResult", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

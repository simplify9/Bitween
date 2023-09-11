﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SW.Infolink;

#nullable disable

namespace SW.Infolink.MySql.Migrations
{
    [DbContext(typeof(InfolinkDbContext))]
    [Migration("20230910151711_SubscriptionCategory")]
    partial class SubscriptionCategory
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.20")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("SW.Infolink.Domain.Accounts.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Deleted")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Disabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("Email")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<byte>("EmailProvider")
                        .HasColumnType("tinyint unsigned");

                    b.Property<byte>("LoginMethods")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("ModifiedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Password")
                        .HasMaxLength(500)
                        .IsUnicode(false)
                        .HasColumnType("varchar(500)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Accounts", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 9999,
                            CreatedOn = new DateTime(2021, 12, 31, 22, 0, 0, 0, DateTimeKind.Utc),
                            Deleted = false,
                            Disabled = false,
                            DisplayName = "Admin",
                            Email = "admin@infolink.systems",
                            EmailProvider = (byte)0,
                            LoginMethods = (byte)2,
                            Password = "$SWHASH$V1$10000$VQCi48eitH4Ml5juvBMOFZrMdQwBbhuIQVXe6RR7qJdDF2bJ",
                            Role = 0
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Accounts.RefreshToken", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("AccountId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<byte>("LoginMethod")
                        .HasColumnType("tinyint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.ToTable("RefreshTokens", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.Document", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<bool>("BusEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("BusMessageTypeName")
                        .HasMaxLength(500)
                        .IsUnicode(false)
                        .HasColumnType("varchar(500)");

                    b.Property<bool?>("DisregardsUnfilteredMessages")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("DocumentFormat")
                        .HasColumnType("int");

                    b.Property<int>("DuplicateInterval")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("PromotedProperties")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("BusMessageTypeName")
                        .IsUnique();

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Documents", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 10001,
                            BusEnabled = false,
                            DocumentFormat = 0,
                            DuplicateInterval = 0,
                            Name = "Aggregation Document",
                            PromotedProperties = "{}"
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.DocumentTrail", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("Code")
                        .HasColumnType("int");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<string>("StateAfter")
                        .HasColumnType("longtext");

                    b.Property<string>("StateBefore")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("CreatedOn");

                    b.HasIndex("DocumentId");

                    b.ToTable("DocumentTrail");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Notifier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("HandlerId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("HandlerProperties")
                        .HasColumnType("longtext");

                    b.Property<bool>("Inactive")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<bool>("RunOnBadResult")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("RunOnFailedResult")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("RunOnSubscriptions")
                        .HasColumnType("longtext");

                    b.Property<bool>("RunOnSuccessfulResult")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("Notifiers", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.OnHoldXchange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("BadData")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Data")
                        .HasColumnType("longtext");

                    b.Property<string>("FileName")
                        .HasColumnType("longtext");

                    b.Property<string>("References")
                        .HasMaxLength(1024)
                        .HasColumnType("varchar(1024)");

                    b.Property<int>("SubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("OnHoldXchanges", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.HasKey("Id");

                    b.ToTable("Partners", (string)null);

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
                        .HasColumnType("int");

                    b.Property<DateTime?>("AggregateOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("AggregationForId")
                        .HasColumnType("int");

                    b.Property<byte>("AggregationTarget")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int?>("CategoryId")
                        .HasColumnType("int");

                    b.Property<int>("ConsecutiveFailures")
                        .HasColumnType("int");

                    b.Property<string>("DocumentFilter")
                        .HasColumnType("longtext");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<string>("HandlerId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("HandlerProperties")
                        .HasColumnType("longtext");

                    b.Property<bool>("Inactive")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsRunning")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("LastException")
                        .HasColumnType("longtext");

                    b.Property<string>("MapperId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("MapperProperties")
                        .HasColumnType("longtext");

                    b.Property<string>("MatchExpression")
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<int?>("PartnerId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("PausedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("ReceiveOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("ReceiverId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ReceiverProperties")
                        .HasColumnType("longtext");

                    b.Property<string>("ResponseMessageTypeName")
                        .HasMaxLength(500)
                        .IsUnicode(false)
                        .HasColumnType("varchar(500)");

                    b.Property<int?>("ResponseSubscriptionId")
                        .HasColumnType("int");

                    b.Property<bool>("Temporary")
                        .HasColumnType("tinyint(1)");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("ValidatorId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ValidatorProperties")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("AggregationForId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("DocumentId");

                    b.HasIndex("PartnerId");

                    b.HasIndex("ResponseSubscriptionId");

                    b.ToTable("Subscriptions", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.SubscriptionCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Code")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<string>("ModifiedBy")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("ModifiedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.ToTable("SubscriptionCategory");
                });

            modelBuilder.Entity("SW.Infolink.Domain.SubscriptionTrail", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("Code")
                        .HasColumnType("int");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("StateAfter")
                        .HasColumnType("longtext");

                    b.Property<string>("StateBefore")
                        .HasColumnType("longtext");

                    b.Property<int>("SubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CreatedOn");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("SubscriptionTrail");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Xchange", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("CorrelationId")
                        .HasColumnType("longtext");

                    b.Property<int>("DocumentId")
                        .HasColumnType("int");

                    b.Property<string>("HandlerId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("HandlerProperties")
                        .HasColumnType("longtext");

                    b.Property<string>("InputContentType")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("InputHash")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("InputName")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<int>("InputSize")
                        .HasColumnType("int");

                    b.Property<string>("MapperId")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("MapperProperties")
                        .HasColumnType("longtext");

                    b.Property<string>("References")
                        .HasMaxLength(1024)
                        .HasColumnType("varchar(1024)");

                    b.Property<string>("ResponseMessageTypeName")
                        .HasMaxLength(500)
                        .IsUnicode(false)
                        .HasColumnType("varchar(500)");

                    b.Property<int?>("ResponseSubscriptionId")
                        .HasColumnType("int");

                    b.Property<string>("RetryFor")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("StartedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("SubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("InputHash");

                    b.HasIndex("RetryFor");

                    b.HasIndex("StartedOn");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("Xchanges", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeAggregation", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("AggregatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("AggregationXchangeId")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("AggregationXchangeId");

                    b.ToTable("XchangeAggregations", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeDelivery", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<DateTime>("DeliveredOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("DeliveredOn");

                    b.ToTable("XchangeDeliveries", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Exception")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("FinishedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("NotifierId")
                        .HasColumnType("int");

                    b.Property<string>("NotifierName")
                        .HasColumnType("longtext");

                    b.Property<bool>("Success")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("XchangeId")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.HasKey("Id");

                    b.ToTable("XchangeNotifications", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangePromotedProperties", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Hits")
                        .HasMaxLength(2000)
                        .IsUnicode(false)
                        .HasColumnType("varchar(2000)");

                    b.Property<string>("Properties")
                        .HasColumnType("longtext");

                    b.Property<string>("PropertiesRaw")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("PropertiesRaw");

                    b.ToTable("XchangePromotedProperties", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeResult", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Exception")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("FinishedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("OutputBad")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("OutputContentType")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("OutputHash")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("OutputName")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<int>("OutputSize")
                        .HasColumnType("int");

                    b.Property<bool>("ResponseBad")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("ResponseContentType")
                        .HasMaxLength(200)
                        .IsUnicode(false)
                        .HasColumnType("varchar(200)");

                    b.Property<string>("ResponseHash")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ResponseName")
                        .HasMaxLength(200)
                        .HasColumnType("varchar(200)");

                    b.Property<int>("ResponseSize")
                        .HasColumnType("int");

                    b.Property<string>("ResponseXchangeId")
                        .HasColumnType("longtext");

                    b.Property<bool>("Success")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("XchangeResults", (string)null);
                });

            modelBuilder.Entity("SW.Infolink.RunFlagUpdater+RunningResult", b =>
                {
                    b.Property<bool>("IsRunning")
                        .HasColumnType("tinyint(1)");

                    b.ToView(null);
                });

            modelBuilder.Entity("SW.Infolink.Domain.Accounts.RefreshToken", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Accounts.Account", null)
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.DocumentTrail", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Document");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.OwnsMany("SW.Infolink.Domain.ApiCredential", "ApiCredentials", b1 =>
                        {
                            b1.Property<int>("PartnerId")
                                .HasColumnType("int");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            b1.Property<string>("Key")
                                .IsRequired()
                                .HasMaxLength(500)
                                .IsUnicode(false)
                                .HasColumnType("varchar(500)");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(500)
                                .HasColumnType("varchar(500)");

                            b1.HasKey("PartnerId", "Id");

                            b1.HasIndex("Key")
                                .IsUnique();

                            b1.ToTable("PartnerApiCredentials", (string)null);

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

                    b.Navigation("ApiCredentials");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Subscription", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Subscription", null)
                        .WithMany()
                        .HasForeignKey("AggregationForId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_Subscriptions_AggFor");

                    b.HasOne("SW.Infolink.Domain.SubscriptionCategory", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId");

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
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_Subscriptions_RespSub");

                    b.OwnsMany("SW.Infolink.Domain.Schedule", "Schedules", b1 =>
                        {
                            b1.Property<int>("SubscriptionId")
                                .HasColumnType("int");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            b1.Property<bool>("Backwards")
                                .HasColumnType("tinyint(1)");

                            b1.Property<long>("On")
                                .HasColumnType("bigint");

                            b1.Property<byte>("Recurrence")
                                .HasColumnType("tinyint unsigned");

                            b1.HasKey("SubscriptionId", "Id");

                            b1.ToTable("SubscriptionSchedules", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("SubscriptionId");
                        });

                    b.Navigation("Category");

                    b.Navigation("Schedules");
                });

            modelBuilder.Entity("SW.Infolink.Domain.SubscriptionTrail", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Subscription", "Subscription")
                        .WithMany()
                        .HasForeignKey("SubscriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subscription");
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

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.Navigation("Subscriptions");
                });
#pragma warning restore 612, 618
        }
    }
}

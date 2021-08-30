﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SW.Infolink.PgSql;

namespace SW.Infolink.PgSql.Migrations
{
    [DbContext(typeof(InfolinkDbContext))]
    partial class InfolinkDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("infolink")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("SW.Infolink.Domain.Document", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnName("id")
                        .HasColumnType("integer");

                    b.Property<bool>("BusEnabled")
                        .HasColumnName("bus_enabled")
                        .HasColumnType("boolean");

                    b.Property<string>("BusMessageTypeName")
                        .HasColumnName("bus_message_type_name")
                        .HasColumnType("character varying(500)")
                        .HasMaxLength(500);

                    b.Property<int>("DuplicateInterval")
                        .HasColumnName("duplicate_interval")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("character varying(100)")
                        .HasMaxLength(100);

                    b.Property<string>("PromotedProperties")
                        .HasColumnName("promoted_properties")
                        .HasColumnType("jsonb");

                    b.HasKey("Id")
                        .HasName("pk_document");

                    b.HasIndex("BusMessageTypeName")
                        .IsUnique()
                        .HasName("ix_document_bus_message_type_name");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasName("ix_document_name");

                    b.ToTable("document","infolink");

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
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("HandlerId")
                        .HasColumnName("handler_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200)
                        .IsUnicode(false);

                    b.Property<string>("HandlerProperties")
                        .HasColumnName("handler_properties")
                        .HasColumnType("text");

                    b.Property<bool>("Inactive")
                        .HasColumnName("inactive")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("character varying(100)")
                        .HasMaxLength(100);

                    b.Property<bool>("RunOnBadResult")
                        .HasColumnName("run_on_bad_result")
                        .HasColumnType("boolean");

                    b.Property<bool>("RunOnFailedResult")
                        .HasColumnName("run_on_failed_result")
                        .HasColumnType("boolean");

                    b.Property<bool>("RunOnSuccessfulResult")
                        .HasColumnName("run_on_successful_result")
                        .HasColumnType("boolean");

                    b.HasKey("Id")
                        .HasName("pk_notifier");

                    b.ToTable("notifier","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.OnHoldXchange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("BadData")
                        .HasColumnName("bad_data")
                        .HasColumnType("boolean");

                    b.Property<string>("Data")
                        .HasColumnName("data")
                        .HasColumnType("text");

                    b.Property<string>("FileName")
                        .HasColumnName("file_name")
                        .HasColumnType("text");

                    b.Property<string[]>("References")
                        .HasColumnName("references")
                        .HasColumnType("text[]");

                    b.Property<int>("SubscriptionId")
                        .HasColumnName("subscription_id")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_on_hold_xchange");

                    b.HasIndex("SubscriptionId")
                        .HasName("ix_on_hold_xchange_subscription_id");

                    b.ToTable("on_hold_xchange","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.HasKey("Id")
                        .HasName("pk_partner");

                    b.ToTable("partner","infolink");

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
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime?>("AggregateOn")
                        .HasColumnName("aggregate_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("AggregationForId")
                        .HasColumnName("aggregation_for_id")
                        .HasColumnType("integer");

                    b.Property<byte>("AggregationTarget")
                        .HasColumnName("aggregation_target")
                        .HasColumnType("smallint");

                    b.Property<int>("ConsecutiveFailures")
                        .HasColumnName("consecutive_failures")
                        .HasColumnType("integer");

                    b.Property<IReadOnlyDictionary<string, string>>("DocumentFilter")
                        .HasColumnName("document_filter")
                        .HasColumnType("jsonb");

                    b.Property<int>("DocumentId")
                        .HasColumnName("document_id")
                        .HasColumnType("integer");

                    b.Property<string>("HandlerId")
                        .HasColumnName("handler_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<IReadOnlyDictionary<string, string>>("HandlerProperties")
                        .HasColumnName("handler_properties")
                        .HasColumnType("jsonb");

                    b.Property<bool>("Inactive")
                        .HasColumnName("inactive")
                        .HasColumnType("boolean");

                    b.Property<string>("LastException")
                        .HasColumnName("last_exception")
                        .HasColumnType("text");

                    b.Property<string>("MapperId")
                        .HasColumnName("mapper_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<IReadOnlyDictionary<string, string>>("MapperProperties")
                        .HasColumnName("mapper_properties")
                        .HasColumnType("jsonb");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("character varying(100)")
                        .HasMaxLength(100);

                    b.Property<int?>("PartnerId")
                        .HasColumnName("partner_id")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("PausedOn")
                        .HasColumnName("paused_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("ReceiveOn")
                        .HasColumnName("receive_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ReceiverId")
                        .HasColumnName("receiver_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<IReadOnlyDictionary<string, string>>("ReceiverProperties")
                        .HasColumnName("receiver_properties")
                        .HasColumnType("jsonb");

                    b.Property<string>("ResponseMessageTypeName")
                        .HasColumnName("response_message_type_name")
                        .HasColumnType("character varying(500)")
                        .HasMaxLength(500);

                    b.Property<int?>("ResponseSubscriptionId")
                        .HasColumnName("response_subscription_id")
                        .HasColumnType("integer");

                    b.Property<bool>("Temporary")
                        .HasColumnName("temporary")
                        .HasColumnType("boolean");

                    b.Property<byte>("Type")
                        .HasColumnName("type")
                        .HasColumnType("smallint");

                    b.Property<string>("ValidatorId")
                        .HasColumnName("validator_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<IReadOnlyDictionary<string, string>>("ValidatorProperties")
                        .HasColumnName("validator_properties")
                        .HasColumnType("jsonb");

                    b.HasKey("Id")
                        .HasName("pk_subscription");

                    b.HasIndex("AggregationForId")
                        .HasName("ix_subscription_aggregation_for_id");

                    b.HasIndex("DocumentId")
                        .HasName("ix_subscription_document_id");

                    b.HasIndex("PartnerId")
                        .HasName("ix_subscription_partner_id");

                    b.HasIndex("ResponseSubscriptionId")
                        .HasName("ix_subscription_response_subscription_id");

                    b.ToTable("subscription","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Xchange", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnName("id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<string>("CorrelationId")
                        .HasColumnName("correlation_id")
                        .HasColumnType("text");

                    b.Property<int>("DocumentId")
                        .HasColumnName("document_id")
                        .HasColumnType("integer");

                    b.Property<string>("HandlerId")
                        .HasColumnName("handler_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<IReadOnlyDictionary<string, string>>("HandlerProperties")
                        .HasColumnName("handler_properties")
                        .HasColumnType("jsonb");

                    b.Property<string>("InputContentType")
                        .HasColumnName("input_content_type")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<string>("InputHash")
                        .IsRequired()
                        .HasColumnName("input_hash")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<string>("InputName")
                        .HasColumnName("input_name")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<int>("InputSize")
                        .HasColumnName("input_size")
                        .HasColumnType("integer");

                    b.Property<string>("MapperId")
                        .HasColumnName("mapper_id")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<IReadOnlyDictionary<string, string>>("MapperProperties")
                        .HasColumnName("mapper_properties")
                        .HasColumnType("jsonb");

                    b.Property<string[]>("References")
                        .HasColumnName("references")
                        .HasColumnType("text[]");

                    b.Property<string>("ResponseMessageTypeName")
                        .HasColumnName("response_message_type_name")
                        .HasColumnType("character varying(500)")
                        .HasMaxLength(500);

                    b.Property<int?>("ResponseSubscriptionId")
                        .HasColumnName("response_subscription_id")
                        .HasColumnType("integer");

                    b.Property<string>("RetryFor")
                        .HasColumnName("retry_for")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<DateTime>("StartedOn")
                        .HasColumnName("started_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("SubscriptionId")
                        .HasColumnName("subscription_id")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_xchange");

                    b.HasIndex("DocumentId")
                        .HasName("ix_xchange_document_id");

                    b.HasIndex("InputHash")
                        .HasName("ix_xchange_input_hash");

                    b.HasIndex("RetryFor")
                        .HasName("ix_xchange_retry_for");

                    b.HasIndex("StartedOn")
                        .HasName("ix_xchange_started_on");

                    b.HasIndex("SubscriptionId")
                        .HasName("ix_xchange_subscription_id");

                    b.ToTable("xchange","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeAggregation", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnName("id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<DateTime>("AggregatedOn")
                        .HasColumnName("aggregated_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("AggregationXchangeId")
                        .IsRequired()
                        .HasColumnName("aggregation_xchange_id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.HasKey("Id")
                        .HasName("pk_xchange_aggregation");

                    b.HasIndex("AggregationXchangeId")
                        .HasName("ix_xchange_aggregation_aggregation_xchange_id");

                    b.ToTable("xchange_aggregation","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeDelivery", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnName("id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<DateTime>("DeliveredOn")
                        .HasColumnName("delivered_on")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id")
                        .HasName("pk_xchange_delivery");

                    b.HasIndex("DeliveredOn")
                        .HasName("ix_xchange_delivery_delivered_on");

                    b.ToTable("xchange_delivery","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Exception")
                        .HasColumnName("exception")
                        .HasColumnType("text");

                    b.Property<DateTime>("FinishedOn")
                        .HasColumnName("finished_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("NotifierId")
                        .HasColumnName("notifier_id")
                        .HasColumnType("integer");

                    b.Property<string>("NotifierName")
                        .HasColumnName("notifier_name")
                        .HasColumnType("text");

                    b.Property<bool>("Success")
                        .HasColumnName("success")
                        .HasColumnType("boolean");

                    b.Property<string>("XchangeId")
                        .HasColumnName("xchange_id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("Id")
                        .HasName("pk_xchange_notification");

                    b.ToTable("xchange_notification","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangePromotedProperties", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnName("id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<int[]>("Hits")
                        .HasColumnName("hits")
                        .HasColumnType("integer[]");

                    b.Property<IReadOnlyDictionary<string, string>>("Properties")
                        .HasColumnName("properties")
                        .HasColumnType("jsonb");

                    b.Property<string>("PropertiesRaw")
                        .HasColumnName("properties_raw")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_xchange_promoted_properties");

                    b.HasIndex("PropertiesRaw")
                        .HasName("ix_xchange_promoted_properties_properties_raw");

                    b.ToTable("xchange_promoted_properties","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeResult", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnName("id")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<string>("Exception")
                        .HasColumnName("exception")
                        .HasColumnType("text");

                    b.Property<DateTime>("FinishedOn")
                        .HasColumnName("finished_on")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("OutputBad")
                        .HasColumnName("output_bad")
                        .HasColumnType("boolean");

                    b.Property<string>("OutputContentType")
                        .HasColumnName("output_content_type")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<string>("OutputHash")
                        .HasColumnName("output_hash")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<string>("OutputName")
                        .HasColumnName("output_name")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<int>("OutputSize")
                        .HasColumnName("output_size")
                        .HasColumnType("integer");

                    b.Property<bool>("ResponseBad")
                        .HasColumnName("response_bad")
                        .HasColumnType("boolean");

                    b.Property<string>("ResponseContentType")
                        .HasColumnName("response_content_type")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<string>("ResponseHash")
                        .HasColumnName("response_hash")
                        .HasColumnType("character varying(50)")
                        .HasMaxLength(50);

                    b.Property<string>("ResponseName")
                        .HasColumnName("response_name")
                        .HasColumnType("character varying(200)")
                        .HasMaxLength(200);

                    b.Property<int>("ResponseSize")
                        .HasColumnName("response_size")
                        .HasColumnType("integer");

                    b.Property<string>("ResponseXchangeId")
                        .HasColumnName("response_xchange_id")
                        .HasColumnType("text");

                    b.Property<bool>("Success")
                        .HasColumnName("success")
                        .HasColumnType("boolean");

                    b.HasKey("Id")
                        .HasName("pk_xchange_result");

                    b.ToTable("xchange_result","infolink");
                });

            modelBuilder.Entity("SW.Infolink.Domain.Partner", b =>
                {
                    b.OwnsMany("SW.Infolink.Domain.ApiCredential", "ApiCredentials", b1 =>
                        {
                            b1.Property<int>("PartnerId")
                                .HasColumnName("partner_id")
                                .HasColumnType("integer");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnName("id")
                                .HasColumnType("integer")
                                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                            b1.Property<string>("Key")
                                .IsRequired()
                                .HasColumnName("key")
                                .HasColumnType("character varying(500)")
                                .HasMaxLength(500);

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasColumnName("name")
                                .HasColumnType("character varying(500)")
                                .HasMaxLength(500);

                            b1.HasKey("PartnerId", "Id")
                                .HasName("pk_api_credential");

                            b1.HasIndex("Key")
                                .IsUnique()
                                .HasName("ix_partner_api_credential_key");

                            b1.ToTable("partner_api_credential","infolink");

                            b1.WithOwner()
                                .HasForeignKey("PartnerId")
                                .HasConstraintName("fk_api_credential_partner_partner_id");

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
                        .HasConstraintName("fk_subscription_aggregation_for")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SW.Infolink.Domain.Document", null)
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .HasConstraintName("fk_subscription_document_document_id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SW.Infolink.Domain.Partner", null)
                        .WithMany("Subscriptions")
                        .HasForeignKey("PartnerId")
                        .HasConstraintName("fk_subscription_partner_partner_id")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("SW.Infolink.Domain.Subscription", null)
                        .WithMany()
                        .HasForeignKey("ResponseSubscriptionId")
                        .HasConstraintName("fk_subscription_response_subscriber")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.OwnsMany("SW.Infolink.Domain.Schedule", "Schedules", b1 =>
                        {
                            b1.Property<int>("SubscriptionId")
                                .HasColumnName("subscription_id")
                                .HasColumnType("integer");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnName("id")
                                .HasColumnType("integer")
                                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                            b1.Property<bool>("Backwards")
                                .HasColumnName("backwards")
                                .HasColumnType("boolean");

                            b1.Property<long>("On")
                                .HasColumnName("on")
                                .HasColumnType("bigint");

                            b1.Property<byte>("Recurrence")
                                .HasColumnName("recurrence")
                                .HasColumnType("smallint");

                            b1.HasKey("SubscriptionId", "Id")
                                .HasName("pk_schedule");

                            b1.ToTable("subscription_schedule","infolink");

                            b1.WithOwner()
                                .HasForeignKey("SubscriptionId")
                                .HasConstraintName("fk_schedule_subscription_subscription_id");
                        });
                });

            modelBuilder.Entity("SW.Infolink.Domain.Xchange", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Document", null)
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .HasConstraintName("fk_xchange_document_document_id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeAggregation", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangeAggregation", "Id")
                        .HasConstraintName("fk_xchange_aggregation_xchange_xchange_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeDelivery", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangeDelivery", "Id")
                        .HasConstraintName("fk_xchange_delivery_xchange_xchange_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangePromotedProperties", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangePromotedProperties", "Id")
                        .HasConstraintName("fk_xchange_promoted_properties_xchange_xchange_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("SW.Infolink.Domain.XchangeResult", b =>
                {
                    b.HasOne("SW.Infolink.Domain.Xchange", null)
                        .WithOne()
                        .HasForeignKey("SW.Infolink.Domain.XchangeResult", "Id")
                        .HasConstraintName("fk_xchange_result_xchange_xchange_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

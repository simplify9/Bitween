﻿using Microsoft.EntityFrameworkCore;
using SW.EfCoreExtensions;
using SW.Infolink.Domain;
using SW.PrimitiveTypes;
using System.Threading;
using System.Threading.Tasks;

namespace SW.Infolink
{
    public class InfolinkDbContext : DbContext
    {
        private readonly IPublish publish;

        public const string ConnectionString = "InfolinkDb";

        public InfolinkDbContext(DbContextOptions options, IPublish publish) : base(options)
        {
            this.publish = publish;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>(b =>
            {
                b.ToTable("Documents");
                b.Property(p => p.Id).ValueGeneratedNever();
                b.Property(p => p.Name).HasMaxLength(100).IsUnicode(false).IsRequired();
                b.Property(p => p.BusMessageTypeName).IsUnicode(false).HasMaxLength(500);
                b.Property(p => p.PromotedProperties).StoreAsJson();

                b.HasIndex(p => p.Name).IsUnique();
                b.HasIndex(p => p.BusMessageTypeName).IsUnique();

                b.HasMany<Subscription>().WithOne().HasForeignKey(p => p.DocumentId).OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<Partner>(b =>
            {
                b.ToTable("Partners");
                b.Metadata.SetNavigationAccessMode(PropertyAccessMode.Field);
                b.Property(p => p.Name).IsRequired().IsUnicode(false).HasMaxLength(200);
                b.HasMany(p => p.Subscriptions).WithOne().IsRequired(false).HasForeignKey(p => p.PartnerId).OnDelete(DeleteBehavior.Restrict);
                b.OwnsMany(p => p.ApiCredentials, apicred =>
                {
                    apicred.ToTable("PartnerApiCredentials");
                    apicred.Property(p => p.Name).IsRequired().HasMaxLength(500);
                    apicred.Property(p => p.Key).IsRequired().IsUnicode(false).HasMaxLength(500);
                    apicred.HasIndex(p => p.Key).IsUnique();
                    apicred.WithOwner().HasForeignKey("PartnerId");

                    apicred.HasData(
                        new
                        {
                            Id = 1,
                            PartnerId = Partner.SystemId,
                            Name = "default",
                            Key = "7facc758283844b49cc4ffd26a75b1de",
                        });
                });

                b.HasData(
                    new
                    {
                        Id = Partner.SystemId,
                        Name = "SYSTEM"
                    });

            });

            modelBuilder.Entity<Subscription>(b =>
            {
                b.ToTable("Subscribers");
                b.Property(p => p.Name).HasMaxLength(100).IsRequired();
                b.OwnsMany(p => p.Schedules, schedules => schedules.BuildSchedule("SubscriptionSchedules"));
                b.OwnsMany(p => p.ReceiveSchedules, schedules => schedules.BuildSchedule("SubscriptionReceiveSchedules"));
                b.Property(p => p.HandlerProperties).StoreAsJson();
                b.Property(p => p.MapperProperties).StoreAsJson();
                b.Property(p => p.ReceiverProperties).StoreAsJson();
                b.Property(p => p.ValidatorProperties).StoreAsJson();
                b.Property(p => p.DocumentFilter).StoreAsJson();
                b.Property(p => p.MapperId).HasMaxLength(200).IsUnicode(false);
                b.Property(p => p.HandlerId).HasMaxLength(200).IsUnicode(false);
                b.Property(p => p.ReceiverId).HasMaxLength(200).IsUnicode(false);
                b.Property(p => p.ValidatorProperties).HasMaxLength(200).IsUnicode(false);
                b.Property(p => p.Type).HasConversion<byte>();

                //b.HasIndex(p => new { p.PartnerId, p.DocumentId }).IsUnique();
                b.HasOne<Subscription>().WithOne().HasForeignKey<Subscription>(p => p.ResponseSubscriptionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<Xchange>(b =>
            {
                b.ToTable("Xchanges");
                b.Property(p => p.Id).IsUnicode(false).HasMaxLength(50);
                b.Property(p => p.References).IsSeparatorDelimited().HasMaxLength(1024);
                b.Property(p => p.InputHash).IsRequired().IsUnicode(false).HasMaxLength(50);
                b.Property(p => p.InputName).HasMaxLength(200);
                b.Property(p => p.MapperId).HasMaxLength(200).IsUnicode(false);
                b.Property(p => p.HandlerId).HasMaxLength(200).IsUnicode(false);
                b.Property(p => p.HandlerProperties).StoreAsJson();
                b.Property(p => p.MapperProperties).StoreAsJson();

                b.HasIndex(i => i.InputHash);
                b.HasIndex(i => i.DeliverOn);
                b.HasIndex(i => i.SubscriptionId);
                b.HasIndex(i => i.StartedOn);

            });

            modelBuilder.Entity<XchangeResult>(b =>
            {
                b.ToTable("XchangeResults");
                b.Property(p => p.Id).IsUnicode(false).HasMaxLength(50);
                b.Property(p => p.OutputHash).IsUnicode(false).HasMaxLength(50);
                b.Property(p => p.OutputName).HasMaxLength(200);
                b.Property(p => p.ResponseHash).IsUnicode(false).HasMaxLength(50);
                b.Property(p => p.ResponseName).HasMaxLength(200);

                b.HasOne<Xchange>().WithOne().HasForeignKey<XchangeResult>(p => p.Id).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<XchangeDelivery>(b =>
            {
                b.ToTable("XchangeDeliveries");
                b.Property(p => p.Id).IsUnicode(false).HasMaxLength(50);
                b.HasIndex(i => i.DeliveredOn);
                b.HasOne<Xchange>().WithOne().HasForeignKey<XchangeDelivery>(p => p.Id).OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<XchangePromotedProperties>(b =>
            {
                b.ToTable("XchangePromotedProperties");
                b.Property(p => p.Id).IsUnicode(false).HasMaxLength(50);
                b.Property(p => p.Properties).StoreAsJson();
                b.HasOne<Xchange>().WithOne().HasForeignKey<XchangePromotedProperties>(p => p.Id).OnDelete(DeleteBehavior.Cascade);

            });

        }

        async public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //try
            //{
            using var transaction = Database.BeginTransaction();
            var affectedRecords = await base.SaveChangesAsync(cancellationToken);
            await ChangeTracker.PublishDomainEvents(publish);

            await transaction.CommitAsync();
            return affectedRecords;
            //}
            //catch (DbUpdateException dbUpdateException)
            //{
            //    if (dbUpdateException.InnerException == null)
            //        throw new SWException($"Data Error: {dbUpdateException.Message}");
            //    else
            //        throw new SWException($"Data Error: {dbUpdateException.InnerException.Message}");
            //}
        }
    }
}

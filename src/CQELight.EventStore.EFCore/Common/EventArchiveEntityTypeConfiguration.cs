using CQELight.EventStore.EFCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CQELight.EventStore.EFCore.Common
{
    internal class EventArchiveEntityTypeConfiguration : IEntityTypeConfiguration<ArchiveEvent>
    {
        public void Configure(EntityTypeBuilder<ArchiveEvent> builder)
        {
            builder.HasIndex(e => new { e.HashedAggregateId, e.AggregateType });

            builder.HasKey(e => e.Id);
            builder.Property(e => e.AggregateType).HasMaxLength(1024);
            builder.Property(e => e.EventData).IsRequired();
            builder.Property(e => e.EventType).IsRequired().HasMaxLength(1024);
            builder.Property(e => e.EventTime).IsRequired();
            builder.Property(e => e.Sequence).IsRequired();
        }
    }
}

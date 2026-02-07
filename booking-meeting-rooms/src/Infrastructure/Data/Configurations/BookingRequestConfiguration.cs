using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingMeetingRooms.Infrastructure.Data.Configurations;

public class BookingRequestConfiguration : IEntityTypeConfiguration<BookingRequest>
{
    public void Configure(EntityTypeBuilder<BookingRequest> builder)
    {
        builder.ToTable("BookingRequests");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.RoomId)
            .IsRequired();

        builder.HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // TimeSlot як Value Object
        builder.OwnsOne(b => b.TimeSlot, ts =>
        {
            ts.Property(t => t.StartAt)
                .HasColumnName("StartAt")
                .IsRequired();

            ts.Property(t => t.EndAt)
                .HasColumnName("EndAt")
                .IsRequired();
        });

        // Індекси для ефективної перевірки конфліктів
        builder.HasIndex(b => new { b.RoomId, b.Status });
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.CreatedByUserId);

        builder.Property(b => b.ParticipantEmails)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()))
            .IsRequired();

        builder.Property(b => b.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BookingStatus)Enum.Parse(typeof(BookingStatus), v));

        builder.Property(b => b.CreatedByUserId)
            .IsRequired();

        builder.Property(b => b.RowVersion)
            .IsRowVersion()
            .IsRequired()
            .HasDefaultValueSql("md5(random()::text)::bytea")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt);

        builder.HasMany(b => b.StatusTransitions)
            .WithOne(t => t.BookingRequest)
            .HasForeignKey(t => t.BookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

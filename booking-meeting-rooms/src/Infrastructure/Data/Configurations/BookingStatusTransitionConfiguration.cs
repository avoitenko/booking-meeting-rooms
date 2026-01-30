using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Infrastructure.Data.Configurations;

public class BookingStatusTransitionConfiguration : IEntityTypeConfiguration<BookingStatusTransition>
{
    public void Configure(EntityTypeBuilder<BookingStatusTransition> builder)
    {
        builder.ToTable("BookingStatusTransitions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.BookingRequestId)
            .IsRequired();

        builder.HasOne(t => t.BookingRequest)
            .WithMany(b => b.StatusTransitions)
            .HasForeignKey(t => t.BookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.FromStatus)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BookingStatus)Enum.Parse(typeof(BookingStatus), v));

        builder.Property(t => t.ToStatus)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BookingStatus)Enum.Parse(typeof(BookingStatus), v));

        builder.Property(t => t.ChangedByUserId)
            .IsRequired();

        builder.Property(t => t.Reason)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}

using BookingMeetingRooms.Domain.Common;
using BookingMeetingRooms.Domain.ValueObjects;
using BookingMeetingRooms.Domain.Enums;
using BookingMeetingRooms.Domain.Events;

namespace BookingMeetingRooms.Domain.Entities;

public class BookingRequest : Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid RoomId { get; private set; }
    public Room Room { get; private set; } = null!;
    
    public TimeSlot TimeSlot { get; private set; } = null!;
    
    public List<string> ParticipantEmails { get; private set; } = new();
    
    public string Description { get; private set; } = string.Empty;
    
    public BookingStatus Status { get; private set; } = BookingStatus.Draft;
    
    public Guid CreatedByUserId { get; private set; }
    
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    
    public List<BookingStatusTransition> StatusTransitions { get; private set; } = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private BookingRequest() { }

    public BookingRequest(
        Room room,
        TimeSlot timeSlot,
        List<string> participantEmails,
        string description,
        Guid createdByUserId)
    {
        if (room == null)
            throw new ArgumentNullException(nameof(room));
        if (timeSlot == null)
            throw new ArgumentNullException(nameof(timeSlot));
        if (participantEmails == null || participantEmails.Count == 0)
            throw new ArgumentException("At least one participant is required", nameof(participantEmails));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        RoomId = room.Id;
        Room = room;
        TimeSlot = timeSlot;
        ParticipantEmails = participantEmails;
        Description = description;
        CreatedByUserId = createdByUserId;
        Status = BookingStatus.Draft;
    }

    public void Submit()
    {
        if (Status != BookingStatus.Draft)
            throw new InvalidOperationException($"Cannot submit booking request in {Status} status");

        if (!Room.IsActive)
            throw new InvalidOperationException("Cannot submit booking request for inactive room");

        if (ParticipantEmails.Count == 0)
            throw new InvalidOperationException("Cannot submit booking request without participants");

        if (string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException("Cannot submit booking request without description");

        Status = BookingStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;

        AddStatusTransition(BookingStatus.Draft, BookingStatus.Submitted, CreatedByUserId, null);

        _domainEvents.Add(new BookingSubmittedEvent(
            Id,
            RoomId,
            TimeSlot.StartAt,
            TimeSlot.EndAt,
            new List<string>(ParticipantEmails)));
    }

    public void Confirm(Guid confirmedByUserId)
    {
        if (Status != BookingStatus.Submitted)
            throw new InvalidOperationException($"Cannot confirm booking request in {Status} status");

        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        
        AddStatusTransition(BookingStatus.Submitted, BookingStatus.Confirmed, confirmedByUserId, null);

        _domainEvents.Add(new BookingConfirmedEvent(
            Id,
            RoomId,
            TimeSlot.StartAt,
            TimeSlot.EndAt,
            confirmedByUserId));
    }

    public void Decline(Guid declinedByUserId, string reason)
    {
        if (Status != BookingStatus.Submitted)
            throw new InvalidOperationException($"Cannot decline booking request in {Status} status");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required for decline", nameof(reason));

        Status = BookingStatus.Declined;
        UpdatedAt = DateTime.UtcNow;
        
        AddStatusTransition(BookingStatus.Submitted, BookingStatus.Declined, declinedByUserId, reason);

        _domainEvents.Add(new BookingDeclinedEvent(
            Id,
            RoomId,
            declinedByUserId,
            reason));
    }

    public void Cancel(Guid cancelledByUserId, string reason)
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot cancel booking request in {Status} status");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required for cancellation", nameof(reason));

        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        
        AddStatusTransition(BookingStatus.Confirmed, BookingStatus.Cancelled, cancelledByUserId, reason);

        _domainEvents.Add(new BookingCancelledEvent(
            Id,
            RoomId,
            cancelledByUserId,
            reason));
    }

    public bool CanTransitionTo(BookingStatus targetStatus)
    {
        return Status switch
        {
            BookingStatus.Draft => targetStatus == BookingStatus.Submitted,
            BookingStatus.Submitted => targetStatus == BookingStatus.Confirmed || targetStatus == BookingStatus.Declined,
            BookingStatus.Confirmed => targetStatus == BookingStatus.Cancelled,
            BookingStatus.Declined => false,
            BookingStatus.Cancelled => false,
            _ => false
        };
    }

    public bool CanBeSubmitted()
    {
        return Status == BookingStatus.Draft
            && Room.IsActive
            && ParticipantEmails.Count > 0
            && !string.IsNullOrWhiteSpace(Description);
    }

    private void AddStatusTransition(
        BookingStatus fromStatus,
        BookingStatus toStatus,
        Guid changedByUserId,
        string? reason)
    {
        var transition = new BookingStatusTransition(
            this,
            fromStatus,
            toStatus,
            changedByUserId,
            reason);
        
        StatusTransitions.Add(transition);
    }
}

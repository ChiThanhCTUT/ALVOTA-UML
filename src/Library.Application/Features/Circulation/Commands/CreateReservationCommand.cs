using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Circulation.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Circulation.Commands.CreateReservation;

public sealed record CreateReservationCommand(Guid BookId, Guid MemberId) : IRequest<Result<ReservationDto>>;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}

public sealed class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<ReservationDto>>
{
    private readonly IReservationRepository _reservations;
    private readonly IBookRepository _books;
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CreateReservationCommandHandler(
        IReservationRepository reservations,
        IBookRepository books,
        IMemberRepository members,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _reservations = reservations;
        _books = books;
        _members = members;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<ReservationDto>> Handle(CreateReservationCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var book = await _books.GetByIdAsync(request.BookId, ct);
        if (book is null) return Result.Failure<ReservationDto>(DomainErrors.Catalog.BookNotFound);

        var member = await _members.GetByIdAsync(request.MemberId, ct);
        if (member is null) return Result.Failure<ReservationDto>(DomainErrors.Member.NotFound);

        var reservation = new Reservation
        {
            BookId = book.Id,
            Book = book,
            MemberId = member.Id,
            Member = member,
            ReservedAtUtc = now,
            Status = ReservationStatus.Pending,
            CreatedAtUtc = now,
            CreatedBy = "System"
        };

        await _reservations.AddAsync(reservation, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(ReservationDto.From(reservation));
    }
}

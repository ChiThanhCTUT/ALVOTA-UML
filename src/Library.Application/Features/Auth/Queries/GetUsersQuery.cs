using Library.Application.Common.Interfaces;
using Library.Application.Features.Auth.DTOs;
using Library.Domain.Common;
using MediatR;

namespace Library.Application.Features.Auth.Queries;

public sealed record GetUsersQuery : IRequest<Result<IReadOnlyList<UserDto>>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _users;

    public GetUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var users = await _users.GetAllAsync(ct);
        var dtos = users.Select(UserDto.From).ToList();
        return Result.Success<IReadOnlyList<UserDto>>(dtos);
    }
}

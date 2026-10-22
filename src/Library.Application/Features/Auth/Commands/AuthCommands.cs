using FluentValidation;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Auth.DTOs;
using Library.Domain.Common;
using Library.Domain.Entities;
using MediatR;

namespace Library.Application.Features.Auth.Commands;

// AUTH-01, AUTH-02, AUTH-03
public sealed record LoginCommand(string Username, string Password) : IRequest<Result<LoginResultDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Tên đăng nhập không được để trống.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _users = users;
        _hasher = hasher;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var user = await _users.GetByUsernameAsync(request.Username.Trim(), ct);

        if (user is null)
            return Result.Failure<LoginResultDto>(DomainErrors.Auth.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResultDto>(DomainErrors.Auth.AccountDisabled);

        if (user.IsLockedOut(now))
            return Result.Failure<LoginResultDto>(DomainErrors.Auth.AccountLocked);

        var isValidPassword = _hasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt);
        if (!isValidPassword)
        {
            user.RecordFailedLogin(now);
            _users.Update(user);
            await _uow.SaveChangesAsync(ct);

            if (user.IsLockedOut(now))
                return Result.Failure<LoginResultDto>(DomainErrors.Auth.AccountLocked);

            return Result.Failure<LoginResultDto>(DomainErrors.Auth.InvalidCredentials);
        }

        user.RecordSuccessfulLogin(now);
        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        var token = Guid.NewGuid().ToString("N");
        return Result.Success(new LoginResultDto(UserDto.From(user), token));
    }
}

// AUTH-05: Admin tạo tài khoản
public sealed record CreateUserCommand(
    string Username,
    string Password,
    string FullName,
    string? Email,
    string RoleName) : IRequest<Result<UserDto>>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public CreateUserCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _users = users;
        _hasher = hasher;
        _uow = uow;
        _time = time;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var existing = await _users.GetByUsernameAsync(request.Username.Trim(), ct);
        if (existing != null)
            return Result.Failure<UserDto>(DomainErrors.Auth.DuplicateUsername);

        var role = await _users.GetRoleByNameAsync(request.RoleName, ct);
        if (role == null)
            return Result.Failure<UserDto>(new Error("AUTH.ROLE_NOT_FOUND", "Không tìm thấy vai trò chỉ định."));

        var (hash, salt) = _hasher.HashPassword(request.Password);
        var user = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = hash,
            Salt = salt,
            FullName = request.FullName.Trim(),
            Email = request.Email?.Trim(),
            RoleId = role.Id,
            Role = role,
            IsActive = true,
            CreatedAtUtc = now,
            CreatedBy = "Admin"
        };

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(UserDto.From(user));
    }
}

// AUTH-05: Admin đặt lại mật khẩu
public sealed record ResetPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;

    public ResetPasswordCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IUnitOfWork uow,
        TimeProvider time)
    {
        _users = users;
        _hasher = hasher;
        _uow = uow;
        _time = time;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null) return Result.Failure(DomainErrors.Auth.UserNotFound);

        var (hash, salt) = _hasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.Salt = salt;
        user.Unlock(now);
        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }
}

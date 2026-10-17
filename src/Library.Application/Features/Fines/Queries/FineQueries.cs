using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Fines.DTOs;
using Library.Domain.Common;
using Library.Domain.Enums;
using MediatR;

namespace Library.Application.Features.Fines.Queries;

public sealed record GetFinesQuery(
    PagedRequest Paging,
    FineStatus? Status = null) : IRequest<Result<PagedResult<FineDto>>>;

public sealed class GetFinesQueryHandler : IRequestHandler<GetFinesQuery, Result<PagedResult<FineDto>>>
{
    private readonly IFineRepository _fines;

    public GetFinesQueryHandler(IFineRepository fines)
    {
        _fines = fines;
    }

    public async Task<Result<PagedResult<FineDto>>> Handle(GetFinesQuery request, CancellationToken ct)
    {
        var pagedFines = await _fines.GetPagedAsync(request.Paging, request.Status, ct);
        var dtos = pagedFines.Items.Select(FineDto.From).ToList();

        var result = new PagedResult<FineDto>(
            dtos,
            pagedFines.TotalCount,
            pagedFines.PageIndex,
            pagedFines.PageSize);

        return Result.Success(result);
    }
}

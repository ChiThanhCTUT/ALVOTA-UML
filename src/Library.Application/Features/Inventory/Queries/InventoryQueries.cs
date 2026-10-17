using Library.Application.Common.Interfaces;
using Library.Application.Common.Models;
using Library.Application.Features.Inventory.DTOs;
using Library.Domain.Common;
using MediatR;

namespace Library.Application.Features.Inventory.Queries;

public sealed record GetInventorySessionsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<InventorySessionDto>>>;

public sealed class GetInventorySessionsQueryHandler : IRequestHandler<GetInventorySessionsQuery, Result<PagedResult<InventorySessionDto>>>
{
    private readonly IInventoryRepository _inventory;

    public GetInventorySessionsQueryHandler(IInventoryRepository inventory)
    {
        _inventory = inventory;
    }

    public async Task<Result<PagedResult<InventorySessionDto>>> Handle(GetInventorySessionsQuery request, CancellationToken ct)
    {
        var paged = await _inventory.GetPagedSessionsAsync(request.Paging, ct);
        var dtos = paged.Items.Select(InventorySessionDto.From).ToList();

        return Result.Success(new PagedResult<InventorySessionDto>(
            dtos,
            paged.TotalCount,
            paged.PageIndex,
            paged.PageSize));
    }
}

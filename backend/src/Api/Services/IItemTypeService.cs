using Api.Dtos;

namespace Api.Services;

public interface IItemTypeService
{
    Task<IEnumerable<ItemTypeDto>> ListAsync(
        bool activeOnly = false,
        CancellationToken ct = default
    );
    Task<ItemTypeDto> CreateAsync(CreateItemTypeRequest request, CancellationToken ct = default);
    Task<ItemTypeDto> UpdateAsync(UpdateItemTypeRequest request, CancellationToken ct = default);
    Task EnsureAssignableAsync(
        int itemTypeId,
        int? currentItemTypeId,
        CancellationToken ct = default
    );
}

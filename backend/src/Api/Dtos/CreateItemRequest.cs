namespace Api.Dtos;

public record CreateItemRequest(
    string Title,
    string Description,
    int ProjectId,
    int ItemTypeId,
    int StatusId
);

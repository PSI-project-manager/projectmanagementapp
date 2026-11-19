namespace Api.Dtos;

public record UpdateItemRequest(
    int ItemId,
    string Title,
    string Description,
    int ItemTypeId,
    int StatusId
);

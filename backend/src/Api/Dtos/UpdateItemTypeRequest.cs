namespace Api.Dtos;

public record UpdateItemTypeRequest(int ItemTypeId, string Name, bool IsActive);

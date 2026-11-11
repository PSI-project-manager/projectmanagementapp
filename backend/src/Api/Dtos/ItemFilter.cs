namespace Api.Dtos;

public record ItemFilter(int? ProjectId = null, int? ItemTypeId = null, int? StatusId = null);

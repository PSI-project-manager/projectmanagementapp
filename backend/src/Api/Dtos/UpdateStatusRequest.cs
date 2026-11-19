using Api.Models;

namespace Api.Dtos;

public record UpdateStatusRequest(int StatusId, string Name, int SortOrder, bool IsActive);

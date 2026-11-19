using Api.Models;

namespace Api.Dtos;

public record CreateStatusRequest(string Name, int? SortOrder = null);

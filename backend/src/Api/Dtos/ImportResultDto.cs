namespace Api.Dtos;

public record ImportResultDto(int Total, int Created, IReadOnlyList<string> Errors);

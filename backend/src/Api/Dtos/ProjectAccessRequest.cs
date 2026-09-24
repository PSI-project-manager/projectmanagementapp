namespace Api.Dtos;

public sealed record ProjectAccessRequest(
    int ProjectId,
    int UserId
);
namespace Api.Exceptions;

public class InactiveEntityException : Exception
{
    public InactiveEntityException(string entityKind, int entityId, string entityName)
        : base($"{entityKind} '{entityName}' is inactive and cannot be assigned.")
    {
        EntityKind = entityKind;
        EntityId = entityId;
        EntityName = entityName;
    }

    public string EntityKind { get; }

    public int EntityId { get; }

    public string EntityName { get; }
}

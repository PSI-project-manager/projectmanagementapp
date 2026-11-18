namespace Api.Exceptions;

public class InactiveEntityException : Exception
{
    public InactiveEntityException(string entityKind, string entityName)
        : base($"{entityKind} '{entityName}' is inactive and cannot be assigned.") { }
}

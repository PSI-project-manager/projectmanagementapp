namespace Api.Exceptions;

// the client is authenticated but isn't allowed to do what they asked for,
// so this maps to 403 and not 401 (see the exception handler in Program.cs)
public class PermissionDeniedException(string message) : Exception(message) { }

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling
{
    public enum DbErrorKind
    {
        DuplicateKey,
        ForeignKeyViolation,
        NotNullViolation,
        CheckViolation,
        Deadlock,
        SerializationFailureOrRetryable,
        Unknown
    }
}

namespace HandleConcurrency.Helpers;

/// <summary>
/// Selected SQL error numbers to be using in error checking.
/// </summary>
public enum SqlErrors
{
    Timeout = 1222,
    ViolationUniqueKeyConstraint = 2601
}

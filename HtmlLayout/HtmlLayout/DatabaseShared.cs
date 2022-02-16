namespace DatabaseShared
{
    /// <summary>
    /// For use by database field <c>role</c>,.
    /// </summary>
    public enum DbEnumRole
    {
        Staff,
        Admin
    }

    /// <summary>
    /// For use by database field <c>type</c> (as part of a log object).
    /// </summary>
    public enum DbEnumType
    {
        OneTime,
        Ongoing,
        Closed,
        Hidden
    }

    /// <summary>
    /// For use by database fields <c>deviceStatus</c>, <c>systemStatus</c>.
    /// </summary>
    public enum DbEnumStatus
    {
        Failure,
        Warning,
        Normal
    }

    /// <summary>
    /// For use by database field <c>SystemType</c>.
    /// </summary>
    public enum DbEnumType
    {
        Widefind,
        Fibaro
    }

    /// <summary>
    /// For use by database field <c>importance</c>.
    /// </summary>
    public enum DbEnumImportance
    {
        Failure,
        Warning,
        Info,
        Verbose
    }
}

namespace dbEnums
{
    public enum role
    {
        staff,
        admin
    }
    public enum ConditionStatus
    {
        OneTime,
        Ongoing,
        Ended,
        Hidden

    }

    public enum deviceStatus
    {
        Normal = 0,
        Warning,
        Failure
    }

    // order is for comparing priorities. Failure > Verbose
    public enum importance
    {
        Verbose = 0,
        Info,
        Warning,
        Failure
    }
    public enum systemType
    {
        Widefind,
        Fibaro

    }

}

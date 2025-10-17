namespace SGE.Core.Entities;

/// <summary>
/// Represents the status of a leave request within the system.
/// </summary>
public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
namespace MDMS.Domain.Enums;

/// <summary>A Meter Testing request's lifecycle — trimmed from the reference spec's full set
/// (Submitted/Under Review/Approved/Test Scheduled/Technician Assigned/Meter Tested/Test Result
/// Available/Closed/Rejected/Cancelled/Rescheduled) to the stages this project actually tracks
/// distinct data for.</summary>
public enum MeterTestingStatus
{
    Submitted = 1,
    Scheduled = 2,
    Completed = 3,
    Rejected = 4,
    Cancelled = 5
}

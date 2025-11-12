namespace Visitor.Kiosk.Features.CheckIn;

public enum CheckinResult
{
    UNKNOWN = 0,
    VALID = 1,
    CANCELLED = 2,
}

public enum CheckinMode
{
    UNKNOWN = 0,
    SELF = 1,
    REMOTE = 2,
}
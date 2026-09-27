namespace ExamPlatform.Application.Common.Models;

public static class DateTimeExtensions
{
    /// <summary>Normalizes an incoming timestamp to UTC; values without an explicit kind are treated as UTC.</summary>
    public static DateTime? AsUtc(this DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } v => v,
        { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
        { } v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };
}

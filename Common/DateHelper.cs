using SkillHive.Enums;
namespace SkillHive.Common
{
  
    public static class DateHelper
    {
        /// <summary>
        /// Calculates the end date for a subscription based on interval.
        /// </summary>
        public static DateTime CalculateSubscriptionEndDate(DateTime startDate, SubscriptionInterval interval)
        {
            return interval switch
            {
                SubscriptionInterval.MONTHLY => startDate.AddMonths(1),
                SubscriptionInterval.QUARTERLY => startDate.AddMonths(3),
                SubscriptionInterval.ANNUAL => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
        }

        /// <summary>
        /// Calculates the end date of a free trial.
        /// Default is 7 days.
        /// </summary>
        public static DateTime CalculateTrialEndDate(DateTime startDate, int trialDays = 7)
        {
            return startDate.AddDays(trialDays);
        }

        /// <summary>
        /// Returns the number of whole days between now and the given future date.
        /// Returns a negative number if the date is in the past.
        /// </summary>
        public static int DaysUntil(DateTime target)
        {
            return (int)Math.Ceiling((target - DateTime.UtcNow).TotalDays);
        }

        /// <summary>
        /// Checks if a date is within a given window (inclusive).
        /// </summary>
        public static bool IsWithinWindow(DateTime date, DateTime start, DateTime end)
        {
            return date >= start && date <= end;
        }

        /// <summary>
        /// Returns a short relative string like "2 days ago" or "in 3 hours".
        /// Used for notifications and previews.
        /// </summary>
        public static string FormatDateRelative(DateTime date)
        {
            var diff = date - DateTime.UtcNow;
            var absSeconds = Math.Abs(diff.TotalSeconds);

            if (absSeconds < 60)
                return diff.TotalSeconds < 0 ? "just now" : "in a moment";

            var absMinutes = Math.Abs(diff.TotalMinutes);
            if (absMinutes < 60)
                return diff.TotalMinutes < 0
                    ? $"{(int)absMinutes} minute(s) ago"
                    : $"in {(int)absMinutes} minute(s)";

            var absHours = Math.Abs(diff.TotalHours);
            if (absHours < 24)
                return diff.TotalHours < 0
                    ? $"{(int)absHours} hour(s) ago"
                    : $"in {(int)absHours} hour(s)";

            var absDays = Math.Abs(diff.TotalDays);
            if (absDays < 30)
                return diff.TotalDays < 0
                    ? $"{(int)absDays} day(s) ago"
                    : $"in {(int)absDays} day(s)";

            return date.ToString("MMM dd, yyyy");
        }
    }
}
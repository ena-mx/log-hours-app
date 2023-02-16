using System.ComponentModel.DataAnnotations;

namespace LogHoursApp.Shared
{
    public class LoggedHour
    {
        [Required]
        public DateTime Date { get; set; } = DateTime.Now;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Range(1, 24, ErrorMessage = "Input Hours should be between 1 and 24.")]
        public int Hours { get; set; }
        public bool Approved { get; set; }
    }

    public class LoggedHourInReview
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public int Hours { get; set; }

    }

    public class LoggedHourRecord
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public int Hours { get; set; }
        public bool Approved { get; set; }
    }

    public class MarkAsReviewed
    {
        public int Id { get; set; }

    }

    public enum FilterTypeEnum
    {
        DayFilter = 1,
        WeekFilter,
        MonthFilter
    }

    public class GetLoggedHoursReport
    {
        [Required]
        public string WorkerId { get; set; }
        [Required]
        public int FilterType { get; set; }
        [Required]
        public DateTime DateFilter { get; set; } = DateTime.Now;
    }
    public class Worker
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
    }
}
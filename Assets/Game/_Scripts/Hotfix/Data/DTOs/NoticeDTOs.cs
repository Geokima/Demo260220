using System;

namespace Hotfix.Data.DTOs
{
    public class NoticeData
    {
        public int NoticeId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class NoticeResponse
    {
        public NoticeData Notice { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
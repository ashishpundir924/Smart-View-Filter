namespace SmartViewFilter.Revit.Infrastructure
{
    internal sealed class SmartFilterResult
    {
        public int MatchCount { get; set; }

        public string Message { get; set; }

        public bool IsError { get; set; }

        public static SmartFilterResult Success(int matchCount, string message)
        {
            return new SmartFilterResult
            {
                MatchCount = matchCount,
                Message = message
            };
        }

        public static SmartFilterResult Error(string message)
        {
            return new SmartFilterResult
            {
                IsError = true,
                Message = message
            };
        }
    }
}

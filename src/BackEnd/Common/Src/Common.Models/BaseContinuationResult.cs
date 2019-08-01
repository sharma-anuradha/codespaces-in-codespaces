namespace Common.Models
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseContinuationResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string RetryAfter { get; set; }
    }
}

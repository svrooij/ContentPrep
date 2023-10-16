namespace SvR.ContentPrep.Models
{
    /// <summary>
    /// The execution context of an installer.
    /// </summary>
    public enum ExecutionContext
    {
        /// <summary>
        /// Runs in the system context.
        /// </summary>
        System,
        /// <summary>
        /// Runs in the user context.
        /// </summary>
        User,
        /// <summary>
        /// Runs in any context.
        /// </summary>
        Any,
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SvRooij.ContentPrep.Models
{
    /// <summary>
    /// Application details.
    /// </summary>
    public class ApplicationDetails
    {
        /// <summary>
        /// Application name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Relative path to the setup file or script
        /// </summary>
        public string? SetupFile { get; set; }
        /// <summary>
        /// (optional) Msi info.
        /// </summary>
        public MsiInfo? MsiInfo { get; set; }

    }
}

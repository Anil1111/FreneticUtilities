//
// This file is created by Frenetic LLC.
// This code is Copyright (C) 2016-2018 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreneticUtilities.FreneticDataSyntax
{
    /// <summary>
    /// Represents an exception throw while inputting data to an FDS section.
    /// </summary>
    [Serializable]
    public class FDSInputException : Exception
    {
        /// <summary>
        /// Construct the FDS exception.
        /// </summary>
        /// <param name="message">The message explaining the error.</param>
        public FDSInputException(string message)
            : base(message)
        {
            // No init needed.
        }
    }
}

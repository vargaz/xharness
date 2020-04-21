// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.XHarness.CLI.Common.Arguments
{
    internal class GetStateCommandArguments : ICommandArguments
    {
        public LogLevel Verbosity { get; set; }

        public IList<string> GetValidationErrors() => new List<string>();
    }
}

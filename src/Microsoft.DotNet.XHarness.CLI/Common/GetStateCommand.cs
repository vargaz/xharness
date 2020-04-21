// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.XHarness.CLI.Common.Arguments;

namespace Microsoft.DotNet.XHarness.CLI.Common
{
    internal abstract class GetStateCommand : XHarnessCommand
    {
        protected override ICommandArguments Arguments => new GetStateCommandArguments();

        public GetStateCommand() : base("state")
        {
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.DotNet.XHarness.CLI.Common.Arguments;
using Mono.Options;

namespace Microsoft.DotNet.XHarness.CLI.Common
{
    internal abstract class TestCommand : XHarnessCommand
    {
        protected override ICommandArguments Arguments => TestArguments;
        protected abstract ITestCommandArguments TestArguments { get; }

        public TestCommand() : base("test")
        {
        }

        protected override OptionSet GetOptions()
        {
            var options = new OptionSet
            {
                { "app|a=", "Path to already-packaged app", v => TestArguments.AppPackagePath = v },
                { "targets=", "Comma-delineated list of targets to test for", v=> TestArguments.Targets = v.Split(',') },
                { "output-directory=|o=", "Directory in which the resulting package will be outputted", v => TestArguments.OutputDirectory = v},
                { "working-directory=|w=", "Directory in which the resulting package will be outputted", v => TestArguments.WorkingDirectory = v},
                { "timeout=|t=", "Time span, in seconds, to wait for instrumentation to complete.", v => TestArguments.Timeout = TimeSpan.FromSeconds(int.Parse(v))},
            };

            foreach (var option in base.GetOptions())
            {
                options.Add(option);
            }

            return options;
        }
    }
}

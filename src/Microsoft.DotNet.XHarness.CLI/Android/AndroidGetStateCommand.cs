// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.Android;
using Microsoft.DotNet.XHarness.CLI.Common;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace Microsoft.DotNet.XHarness.CLI.Android
{
    internal class AndroidGetStateCommand : GetStateCommand
    {
        protected override OptionSet GetOptions()
        {
            var options = new OptionSet
            {
                "usage: android state",
                "",
                "Print information about the current machine, such as host machine info and device status"
            };

            foreach (var option in base.GetOptions())
            {
                options.Add(option);
            }

            return options;
        }

        protected override Task<ExitCode> InvokeInternal(ILogger logger)
        {
            logger.LogInformation("Getting state of ADB and attached Android device(s)");
            try
            {
                var runner = new AdbRunner(logger);
                string state = runner.GetAdbState();
                if (string.IsNullOrEmpty(state))
                {
                    state = "No device attached";
                }

                logger.LogInformation($"ADB Version info:{Environment.NewLine}{runner.GetAdbVersion()}");
                logger.LogInformation($"ADB State ('device' if physically attached):{Environment.NewLine}{state}");

                return Task.FromResult(ExitCode.SUCCESS);
            }
            catch (Exception toLog)
            {
                logger.LogCritical(toLog, $"Error: {toLog.Message}");
                return Task.FromResult(ExitCode.GENERAL_FAILURE);
            }
        }
    }
}

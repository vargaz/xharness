﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.XHarness.CLI.Common;
using Microsoft.DotNet.XHarness.iOS.Shared;
using Microsoft.DotNet.XHarness.iOS.Shared.Utilities;

namespace Microsoft.DotNet.XHarness.CLI.iOS
{
    internal class iOSTestCommandArguments : TestCommandArguments
    {
        /// <summary>
        /// Path to where Xcode is located.
        /// </summary>
        public string XcodeRoot { get; set; }

        /// <summary>
        /// Path to the mlaunch binary.
        /// Default comes from the NuGet.
        /// </summary>
        public string MlaunchPath { get; set; } = Path.Join(
            Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(iOSTestCommandArguments)).Location),
            "..", "..", "..", "runtimes", "any", "native", "mlaunch", "bin", "mlaunch");

        /// <summary>
        /// Name of a specific device we want to target.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// How long we wait before app starts and first test should start running.
        /// </summary>
        public TimeSpan LaunchTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public IReadOnlyCollection<TestTarget> TestTargets { get; set; }

        public override IList<string> GetValidationErrors()
        {
            IList<string> errors = base.GetValidationErrors();

            if (Targets == null || Targets.Count == 0)
            {
                errors.Add($@"No targets specified. At least one target must be provided. " +
                    $"Available targets are:{Environment.NewLine}\t" +
                    $"{string.Join(Environment.NewLine + "\t", GetAvailableTargets())}");
            }
            else
            {
                var testTargets = new List<TestTarget>();

                foreach (string targetName in Targets)
                {
                    try
                    {
                        testTargets.Add(targetName.ParseAsAppRunnerTarget());
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        errors.Add($"Failed to parse test target '{targetName}'");
                    }
                }

                TestTargets = testTargets;
            }

            if (!Path.IsPathRooted(MlaunchPath))
            {
                MlaunchPath = Path.Combine(Directory.GetCurrentDirectory(), MlaunchPath);
            }

            if (!File.Exists(MlaunchPath))
            {
                errors.Add($"Failed to find mlaunch at {MlaunchPath}");
            }

            if (XcodeRoot != null && !Path.IsPathRooted(XcodeRoot))
            {
                XcodeRoot = Path.Combine(Directory.GetCurrentDirectory(), XcodeRoot);
            }

            if (XcodeRoot != null && !Directory.Exists(XcodeRoot))
            {
                errors.Add($"Failed to find Xcode root at {XcodeRoot}");
            }

            return errors;
        }

        private static IEnumerable<string> GetAvailableTargets() =>
            Enum.GetValues(typeof(TestTarget))
                .Cast<TestTarget>()
                .Select(t => t.AsString());
    }
}

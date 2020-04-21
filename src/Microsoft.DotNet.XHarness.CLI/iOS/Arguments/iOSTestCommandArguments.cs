// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.DotNet.XHarness.CLI.Common.Arguments;
using Microsoft.DotNet.XHarness.iOS.Shared;
using Microsoft.DotNet.XHarness.iOS.Shared.Utilities;

namespace Microsoft.DotNet.XHarness.CLI.iOS.Arguments
{
    internal class iOSTestCommandArguments : TestCommandArguments
    {
        private IReadOnlyCollection<string>? _targets;
        private string? _xcodeRoot;
        private string _mlaunchPath = Path.Join(
            Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(iOSTestCommandArguments))?.Location),
            "..", "..", "..", "runtimes", "any", "native", "mlaunch", "bin", "mlaunch");

        /// <summary>
        /// Path to where Xcode is located.
        /// </summary>
        [DisallowNull]
        public string? XcodeRoot
        {
            get => _xcodeRoot;
            set
            {
                if (!Directory.Exists(value))
                {
                    throw new ArgumentException($"Failed to find Xcode root at {value}");
                }

                _xcodeRoot = RootPath(value ?? throw new ArgumentException("Path to the Xcode cannot be null"));
            }
        }

        /// <summary>
        /// Path to the mlaunch binary.
        /// Default comes from the NuGet.
        /// </summary>
        public string MlaunchPath
        {
            get => _mlaunchPath;
            set
            {
                if (!File.Exists(value))
                {
                    throw new ArgumentException($"Failed to find mlaunch at {value}");
                }

                _mlaunchPath = RootPath(value ?? throw new ArgumentException("mlaunch path cannot be null"));
            }
        }

        /// <summary>
        /// Name of a specific device we want to target.
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// How long we wait before app starts and first test should start running.
        /// </summary>
        public TimeSpan LaunchTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public override IReadOnlyCollection<string>? Targets
        {
            get => _targets;
            set
            {
                var testTargets = new List<TestTarget>();

                foreach (string targetName in value ?? throw new ArgumentNullException("Targets cannot be empty"))
                {
                    try
                    {
                        testTargets.Add(targetName.ParseAsAppRunnerTarget());
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        throw new ArgumentException(
                            $"Failed to parse test target '{targetName}'" +
                            $"Available targets are:{Environment.NewLine}\t" +
                            $"{string.Join(Environment.NewLine + "\t", GetAvailableTargets())}");
                    }
                }

                TestTargets = testTargets;
                _targets = value;
            }
        }

        public IReadOnlyCollection<TestTarget> TestTargets { get; private set; } = Array.Empty<TestTarget>();

        public static IEnumerable<string> GetAvailableTargets() =>
            Enum.GetValues(typeof(TestTarget))
                .Cast<TestTarget>()
                .Select(t => t.AsString());
    }
}

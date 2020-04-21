// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.DotNet.XHarness.CLI.Common.Arguments
{
    internal abstract class PackageCommandArguments : XHarnessCommandArguments, IPackageCommandArguments
    {
        private string? _appPackageName;
        private string? _outputDirectory;
        private string? _workingDirectory;

        [DisallowNull]
        public string? AppPackageName
        {
            get => _appPackageName;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("App package name must be specified");
                }

                _appPackageName = RootPath(value);
            }
        }

        [DisallowNull]
        public string? OutputDirectory
        {
            get => _outputDirectory;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Output directory cannot be null");
                }

                _outputDirectory = RootPath(value);

                if (!Directory.Exists(_outputDirectory))
                {
                    Directory.CreateDirectory(_outputDirectory);
                }
            }
        }

        [DisallowNull]
        public string? WorkingDirectory
        {
            get => _workingDirectory;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Working directory cannot be null");
                }

                _workingDirectory = RootPath(value);

                if (!Directory.Exists(_workingDirectory))
                {
                    Directory.CreateDirectory(_workingDirectory);
                }
            }
        }
    }
}

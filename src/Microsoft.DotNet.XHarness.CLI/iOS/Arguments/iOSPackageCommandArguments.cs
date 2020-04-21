// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.XHarness.CLI.Common.Arguments;

namespace Microsoft.DotNet.XHarness.CLI.iOS.Arguments
{
    internal class iOSPackageCommandArguments : PackageCommandArguments
    {
        /// <summary>
        /// A path that is the root of the .ignore files that will be used to skip tests if needed
        /// </summary>
        [DisallowNull]
        public string? IgnoreFilesRootDirectory { get; set; }

        /// <summary>
        /// A path that is the root of the traits txt files that will be used to skip tests if needed
        /// </summary>
        [DisallowNull]
        public string? TraitsRootDirectory { get; set; }

        [DisallowNull]
        public string? MtouchExtraArgs { get; set; }

        [DisallowNull]
        public TemplateType SelectedTemplateType { get; set; }
    }
}

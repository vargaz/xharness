// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Options;
using XHarness.Android;
using XHarness.iOS;

namespace Microsoft.DotNet.XHarness
{
	public class Program
	{
		public static int Main(string[] args)
		{
			// create the root command, this will use the platform specific commands to do the right thing.
			var commands = new CommandSet ("xharness");
			// add the command sets per platform, each will have the same verbs but diff implemenation.
			commands.Add (new iOSCommandSet ());
			commands.Add (new AndroidCommandSet ());
			// add shared commands, for example, help and so on. --version, --help, --verbosity and so on
			commands.Add (new HelpCommand ());
			return commands.Run (args);
		}
	}
}
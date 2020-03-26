using System;
using Mono.Options;

namespace XHarness.iOS {
	// Main iOS command set that contains the plaform specific commands. This allows the command line to
	// suport different options in different platforms.
	public class iOSCommandSet : CommandSet {
		public iOSCommandSet () : base ("ios")
		{
			// add the different commands to be used on iOS
			Add (new PackageCommand ());

			// commond verbs shared with android. We should think a smart way to do this
			Add (new iOSTestCommand ());
		}
	}
}

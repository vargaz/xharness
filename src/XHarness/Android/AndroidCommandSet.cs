using System;
using Mono.Options;

namespace XHarness.Android {
	public class AndroidCommandSet : CommandSet {
		public AndroidCommandSet () : base ("android")
		{
			// other useful comamnds could be added

			// commond verbs shared with android. We should think a smart way to do this
			Add (new AndroidTestCommand ());
		}
	}
}

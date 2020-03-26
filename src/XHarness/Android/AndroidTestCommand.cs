using System;
using System.Collections.Generic;

namespace XHarness.Android {
	public class AndroidTestCommand  : TestCommand {

		// where the real action happens!
		public override int Invoke (IEnumerable<string> args)
		{
			// Android implementation of the test command
			return 0;
		}
	}
}

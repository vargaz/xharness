using System;
using Mono.Options;

namespace XHarness {
	public abstract class TestCommand : Command {
		public TestCommand () : base ("test")
		{
		}
	}
}

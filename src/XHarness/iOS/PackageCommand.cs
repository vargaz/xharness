using System;
using System.Collections.Generic;
using Mono.Options;

namespace XHarness.iOS {

	// represents the template to be used. ATM we only support the manged one.
	public enum TemplateType {
		Managed,
		Native,
	}

	// Command that will create the required project generation for the iOS plaform. The command will ensure that all
	// the required .csproj and src are created. The command is part of the parent CommandSet iOS and exposes similar
	// plus extra options to the one that android exposes.
	public class PackageCommand : Command {

		bool showHelp = false;
		// working directories
		string workingDirectory;
		// will be used as the output dir of the generated projects.
		string outputDirectory;
		// path that is the root of the .ignore files that will be used to skip tests if needed.
		string ignoreFilesRootDirectory;
		// path that is the root of the traits txt files that willl be used to skip tests if needed.
		string traitsRootDirectory;

		string name;
		string mtouchExtraArgs;
		TemplateType templateType;

		public PackageCommand () : base ("package", "Create and compile a iOS project that will execute nunit or xunit tests")
		{
			Options = new OptionSet () {
				"usage: ios package [OPTIONS]",
				"",
				"Packaging command that will create a iOS/tvOS/watcOS or macOS application that can be used to run nunit or xunit based tests dlls.",
				{ "name|n=", "The naming of the testing application.",  v => name = v},
				{ "working-directory=", "The directory that will be used to output to generate all the different projects.", v => workingDirectory = v },
				{ "output-directory=", "The directory in which the resulting package will be output.", v => outputDirectory = v},
				{ "ignore-directory=", "The root directory that contains all the *.ignore files that will be used to skip tests if needed.", v => ignoreFilesRootDirectory = v },
				{ "traits-directory=", "The root director that contains all the .txt files with the traits that will be skipped if needed.", v =>  traitsRootDirectory = v },
				{ "template=", "Indicates which template to use. There are two available ones. Managed, which uses Xamarin.[iOS|Mac] and native. Default is managed.",
					v=> {
						if (Enum.TryParse<TemplateType>(v, out var template)) {
							templateType = template;
						} else {
							templateType = TemplateType.Managed; // TODO, throw an error.
						}
					}
				},
				{ "mtouch-extraargs=", "Extra arguments to be passed to mtouch.", v => mtouchExtraArgs = v },
				{ "help|h|?", "Show this message and exit.", v => showHelp = v != null },
			};
		}

		// where the real action happens!
		public override int Invoke (IEnumerable<string> args)
		{
			try {
				var extra = Options.Parse (args);
				if (showHelp) {
					Options.WriteOptionDescriptions (CommandSet.Out);
					return 0;
				}

				if (string.IsNullOrEmpty (name)) {
					Console.Error.WriteLine ("xharness ios package: Missing required argument --name=NAME");
					return 1;
				}

				// do more interesting stuff here
				return 0;

			} catch  {
				return 1;
			}
		}
	}
}

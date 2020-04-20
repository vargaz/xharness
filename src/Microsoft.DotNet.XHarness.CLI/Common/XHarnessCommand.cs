// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace Microsoft.DotNet.XHarness.CLI.Common
{
    internal abstract class XHarnessCommand : Command
    {
        protected abstract ICommandArguments Arguments { get; }

        protected bool ShowHelp = false;

        protected XHarnessCommand(string name) : base(name)
        {
        }

        protected virtual OptionSet GetOptions() => new OptionSet
        {
            {
                "verbosity:|v:",
                "Verbosity level (1-6) where higher means less logging. (default = 2 / Information)",
                v =>
                {
                    if (Enum.TryParse(v, out LogLevel vl))
                    {
                        Arguments.Verbosity = vl;
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown template type '{v}'");
                    }
                }
            },
            { "help|h", "Show this message", v => ShowHelp = v != null }
        };

        public override sealed int Invoke(IEnumerable<string> arguments)
        {
            var options = GetOptions();
            List<string> extra;

            try
            {
                extra = Options.Parse(arguments);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to parse arguments: {e.Message}");
                return 1;
            }

            using var loggerFactory = CreateLoggerFactory(Arguments?.Verbosity ?? LogLevel.Information);
            var logger = loggerFactory.CreateLogger(Name);

            if (ShowHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (extra.Count > 0)
            {
                logger.LogError($"Unknown arguments{string.Join(" ", extra)}");
                options.WriteOptionDescriptions(Console.Out);
                return 1;
            }

            var validationErrors = Arguments?.GetValidationErrors();

            if (validationErrors?.Any() ?? false)
            {
                var message = new StringBuilder("Invalid arguments:");
                foreach (string error in validationErrors)
                {
                    message.Append(Environment.NewLine + "  - " + error);
                }

                logger.LogError(message.ToString());

                return 1;
            }

            return (int) InvokeInternal(logger).GetAwaiter().GetResult();
        }

        protected abstract Task<ExitCode> InvokeInternal(ILogger logger);

        private static ILoggerFactory CreateLoggerFactory(LogLevel verbosity) => LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .AddFilter(level => level >= verbosity);
        });
    }
}

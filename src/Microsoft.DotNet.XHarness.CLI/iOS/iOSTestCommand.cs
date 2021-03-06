﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.CLI.Common;
using Microsoft.DotNet.XHarness.iOS;
using Microsoft.DotNet.XHarness.iOS.Shared;
using Microsoft.DotNet.XHarness.iOS.Shared.Execution;
using Microsoft.DotNet.XHarness.iOS.Shared.Hardware;
using Microsoft.DotNet.XHarness.iOS.Shared.Listeners;
using Microsoft.DotNet.XHarness.iOS.Shared.Logging;
using Microsoft.DotNet.XHarness.iOS.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace Microsoft.DotNet.XHarness.CLI.iOS
{
    /// <summary>
    /// Command which executes a given, already-packaged iOS application, waits on it and returns status based on the outcome.
    /// </summary>
    internal class iOSTestCommand : TestCommand
    {
        private readonly iOSTestCommandArguments _arguments = new iOSTestCommandArguments();
        protected override ITestCommandArguments TestArguments => _arguments;

        public iOSTestCommand() : base()
        {
            Options = new OptionSet()
            {
                "usage: ios test [OPTIONS]",
                "",
                "Packaging command that will create a iOS/tvOS/watchOS or macOS application that can be used to run NUnit or XUnit-based test dlls",
                { "xcode=", "Path where Xcode is installed", v => _arguments.XcodeRoot = v},
                { "mlaunch=", "Path to the mlaunch binary", v => _arguments.MlaunchPath = v},
                { "device-name=", "Name of a specific device, if you wish to target one", v => _arguments.DeviceName = v},
                { "launch-timeout=|lt=", "Time span, in seconds, to wait for the iOS app to start.", v => _arguments.LaunchTimeout = TimeSpan.FromSeconds(int.Parse(v))},
            };

            foreach (var option in CommonOptions)
            {
                Options.Add(option);
            }
        }

        protected override async Task<ExitCode> InvokeInternal()
        {
            var processManager = new ProcessManager(_arguments.XcodeRoot, _arguments.MlaunchPath);
            var deviceLoader = new HardwareDeviceLoader(processManager);
            var simulatorLoader = new SimulatorLoader(processManager);

            var logs = new Logs(_arguments.OutputDirectory);
            var cancellationToken = new CancellationToken(); // TODO: Get cancellation from command line env? Set timeout through it?

            var exitCode = ExitCode.SUCCESS;

            foreach (TestTarget target in _arguments.TestTargets)
            {
                var exitCodeForRun = await RunTest(target, logs, processManager, deviceLoader, simulatorLoader, cancellationToken);

                if (exitCodeForRun != ExitCode.SUCCESS)
                {
                    exitCode = exitCodeForRun;
                }
            }

            return exitCode;
        }

        private async Task<ExitCode> RunTest(TestTarget target,
            Logs logs,
            ProcessManager processManager,
            IHardwareDeviceLoader deviceLoader,
            ISimulatorLoader simulatorLoader,
            CancellationToken cancellationToken = default)
        {
            _log.LogInformation($"Starting test for {target.AsString()}{ (_arguments.DeviceName != null ? " targeting " + _arguments.DeviceName : null) }..");

            string mainLogFile = Path.Join(_arguments.OutputDirectory, $"run-{target}{(_arguments.DeviceName != null ? "-" + _arguments.DeviceName : null)}.log");
            ILog mainLog = logs.Create(mainLogFile, LogType.ExecutionLog.ToString(), true);
            int verbosity = _arguments.Verbosity.ToInt();

            string deviceName = _arguments.DeviceName;

            var appBundleInformationParser = new AppBundleInformationParser(processManager);

            AppBundleInformation appBundleInfo;

            try
            {
                appBundleInfo = await appBundleInformationParser.ParseFromAppBundle(_arguments.AppPackagePath, target, mainLog, cancellationToken);
            }
            catch (Exception e)
            {
                _log.LogError($"Failed to get bundle information: {e.Message}");
                return ExitCode.FAILED_TO_GET_BUNDLE_INFO;
            }

            if (!target.IsSimulator())
            {
                _log.LogInformation($"Installing application '{appBundleInfo.AppName}' on " + (deviceName != null ? " on device '{deviceName}'" : target.AsString()));

                var appInstaller = new AppInstaller(processManager, deviceLoader, mainLog, verbosity);

                ProcessExecutionResult result;

                try
                {
                    (deviceName, result) = await appInstaller.InstallApp(_arguments.AppPackagePath, target, cancellationToken: cancellationToken);
                }
                catch (NoDeviceFoundException)
                {
                    _log.LogError($"Failed to find suitable device for target {target.AsString()}");
                    return ExitCode.DEVICE_NOT_FOUND;
                }
                catch (Exception e)
                {
                    _log.LogError($"Failed to install the app bundle:{Environment.NewLine}{e}");
                    return ExitCode.PACKAGE_INSTALLATION_FAILURE;
                }

                if (!result.Succeeded)
                {
                    _log.LogError($"Failed to install the app bundle (exit code={result.ExitCode})");
                    return ExitCode.PACKAGE_INSTALLATION_FAILURE;
                }

                _log.LogInformation($"Application '{appBundleInfo.AppName}' was installed successfully on device '{deviceName}'");
            }

            _log.LogInformation($"Starting application '{appBundleInfo.AppName}' on " + (deviceName != null ? " on device '{deviceName}'" : target.AsString()));

            int exitCode;

            try
            {
                var appRunner = new AppRunner(
                    processManager,
                    deviceLoader,
                    simulatorLoader,
                    new SimpleListenerFactory(),
                    new CrashSnapshotReporterFactory(processManager),
                    new CaptureLogFactory(),
                    new DeviceLogCapturerFactory(processManager),
                    new TestReporterFactory(processManager),
                    mainLog,
                    logs,
                    new Helpers());

                (deviceName, exitCode) = await appRunner.RunApp(appBundleInfo,
                    target,
                    _arguments.Timeout,
                    _arguments.LaunchTimeout,
                    deviceName,
                    verbosity: verbosity,
                    xmlResultJargon: XmlResultJargon.xUnit);

                if (exitCode != 0)
                {
                    _log.LogError($"App bundle run failed with exit code {exitCode}. Check logs for more information");
                }
                else
                {
                    _log.LogInformation("Application finished the run successfully");
                }

                return 0;
            }
            catch (NoDeviceFoundException)
            {
                _log.LogError($"Failed to find suitable device for target {target.AsString()}");
                return ExitCode.DEVICE_NOT_FOUND;
            }
            catch (Exception e)
            {
                _log.LogError($"Application run failed:{Environment.NewLine}{e}");
                return ExitCode.APP_CRASH;
            }
            finally
            {
                if (!target.IsSimulator())
                {
                    _log.LogInformation($"Uninstalling the application '{appBundleInfo.AppName}' from device '{deviceName}'");

                    var appUninstaller = new AppUninstaller(processManager, mainLog, verbosity);
                    var uninstallResult = await appUninstaller.UninstallApp(deviceName, appBundleInfo.BundleIdentifier, cancellationToken);
                    if (!uninstallResult.Succeeded)
                    {
                        _log.LogError($"Failed to uninstall the app bundle with exit code: {uninstallResult.ExitCode}");
                    }
                    else
                    {
                        _log.LogInformation($"Application '{appBundleInfo.AppName}' was uninstalled successfully");
                    }
                }
            }
        }
    }
}

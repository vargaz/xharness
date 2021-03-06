﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XHarness.iOS;
using Microsoft.DotNet.XHarness.iOS.Shared;
using Microsoft.DotNet.XHarness.iOS.Shared.Execution;
using Microsoft.DotNet.XHarness.iOS.Shared.Execution.Mlaunch;
using Microsoft.DotNet.XHarness.iOS.Shared.Hardware;
using Microsoft.DotNet.XHarness.iOS.Shared.Listeners;
using Microsoft.DotNet.XHarness.iOS.Shared.Logging;
using Microsoft.DotNet.XHarness.iOS.Shared.Utilities;
using Moq;
using NUnit.Framework;

namespace Xharness.Tests
{
    [TestFixture]
    public class AppRunnerTests
    {
        const string AppName = "com.xamarin.bcltests.SystemXunit";
        const string AppBundleIdentifier = AppName + ".ID";

        private static readonly string s_outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private static readonly string s_appPath = Path.Combine(s_outputPath, AppName);

        private static readonly IHardwareDevice s_mockDevice = new Device(
            buildVersion: "17A577",
            deviceClass: DeviceClass.iPhone,
            deviceIdentifier: "8A450AA31EA94191AD6B02455F377CC1",
            interfaceType: "Usb",
            isUsableForDebugging: true,
            name: "Test iPhone",
            productType: "iPhone12,1",
            productVersion: "13.0");

        private Mock<IProcessManager> _processManager;
        private Mock<ILogs> _logs;
        private Mock<ILog> _mainLog;
        private Mock<IHardwareDeviceLoader> _hardwareDeviceLoader;
        private Mock<ISimulatorLoader> _simulatorLoader;
        private Mock<ISimpleListener> _listener;
        private Mock<ICrashSnapshotReporter> _snapshotReporter;
        private Mock<ITestReporter> _testReporter;
        private Mock<IHelpers> _helpers;

        private ISimpleListenerFactory _listenerFactory;
        private ICrashSnapshotReporterFactory _snapshotReporterFactory;
        private ITestReporterFactory _testReporterFactory;

        [SetUp]
        public void SetUp()
        {
            _mainLog = new Mock<ILog>();

            _processManager = new Mock<IProcessManager>();
            _processManager.SetReturnsDefault(Task.FromResult(new ProcessExecutionResult() { ExitCode = 0 }));

            _hardwareDeviceLoader = new Mock<IHardwareDeviceLoader>();
            _hardwareDeviceLoader
                .Setup(x => x.FindDevice(RunMode.iOS, _mainLog.Object, false, false))
                .ReturnsAsync(s_mockDevice);

            _simulatorLoader = new Mock<ISimulatorLoader>();
            _simulatorLoader
                .Setup(x => x.LoadDevices(It.IsAny<ILog>(), false, false, false))
                .Returns(Task.CompletedTask);

            _listener = new Mock<ISimpleListener>();
            _listener
                .SetupGet(x => x.ConnectedTask)
                .Returns(Task.CompletedTask);

            _snapshotReporter = new Mock<ICrashSnapshotReporter>();

            _testReporter = new Mock<ITestReporter>();
            _testReporter
                .Setup(r => r.Success)
                .Returns(true);
            _testReporter
                .Setup(r => r.ParseResult())
                .ReturnsAsync((TestExecutingResult.Succeeded, null));
            _testReporter
                .Setup(x => x.CollectSimulatorResult(It.IsAny<Task<ProcessExecutionResult>>()))
                .Returns(Task.CompletedTask);

            _logs = new Mock<ILogs>();
            _logs.SetupGet(x => x.Directory).Returns(Path.Combine(s_outputPath, "logs"));

            var factory = new Mock<ISimpleListenerFactory>();
            factory.SetReturnsDefault((ListenerTransport.Tcp, _listener.Object, "listener-temp-file"));
            _listenerFactory = factory.Object;
            _listener.SetupGet(x => x.Port).Returns(1020);

            var factory2 = new Mock<ICrashSnapshotReporterFactory>();
            factory2.SetReturnsDefault(_snapshotReporter.Object);
            _snapshotReporterFactory = factory2.Object;

            var factory3 = new Mock<ITestReporterFactory>();
            factory3.SetReturnsDefault(_testReporter.Object);
            _testReporterFactory = factory3.Object;

            _helpers = new Mock<IHelpers>();
            _helpers
                .Setup(x => x.GetTerminalName(It.IsAny<int>()))
                .Returns("tty1");
            _helpers
                .Setup(x => x.GenerateStableGuid(It.IsAny<string>()))
                .Returns(Guid.NewGuid());
            _helpers
                .SetupGet(x => x.Timestamp)
                .Returns("mocked_timestamp");

            Directory.CreateDirectory(s_outputPath);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(s_outputPath, true);
        }

        [Test]
        public void RunOnSimulatorWithNoAvailableSimulatorTest()
        {
            // Mock finding simulators
            string simulatorLogPath = Path.Combine(Path.GetTempPath(), "simulator-logs");

            _simulatorLoader
                .Setup(x => x.FindSimulators(TestTarget.Simulator_tvOS, _mainLog.Object, true, false))
                .ReturnsAsync(new ISimulatorDevice[0]);

            var listenerLogFile = new Mock<ILog>();

            _logs
                .Setup(x => x.Create(It.IsAny<string>(), "TestLog", It.IsAny<bool>()))
                .Returns(listenerLogFile.Object);

            var captureLog = new Mock<ICaptureLog>();
            captureLog
                .SetupGet(x => x.FullPath)
                .Returns(simulatorLogPath);

            var captureLogFactory = new Mock<ICaptureLogFactory>();
            captureLogFactory
                .Setup(x => x.Create(
                   Path.Combine(_logs.Object.Directory, "tvos.log"),
                   "/path/to/simulator.log",
                   true,
                   It.IsAny<string>()))
                .Returns(captureLog.Object);

            // Act
            var appRunner = new AppRunner(_processManager.Object,
                _hardwareDeviceLoader.Object,
                _simulatorLoader.Object,
                _listenerFactory,
                _snapshotReporterFactory,
                captureLogFactory.Object,
                Mock.Of<IDeviceLogCapturerFactory>(),
                _testReporterFactory,
                _mainLog.Object,
                _logs.Object,
                _helpers.Object);

            var appInformation = new AppBundleInformation(AppName, AppBundleIdentifier, s_appPath, s_appPath, null);

            Assert.ThrowsAsync<NoDeviceFoundException>(
                async () => await appRunner.RunApp(
                    appInformation,
                    TestTarget.Simulator_tvOS,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30)),
                "Running requires installed simulators");

            // Verify

            _mainLog.Verify(x => x.WriteLine("Test run completed"), Times.Never);

            _simulatorLoader.VerifyAll();

            _listener.Verify(x => x.Initialize(), Times.AtLeastOnce);
            _listener.Verify(x => x.StartAsync(), Times.AtLeastOnce);
        }

        [Test]
        public async Task RunOnSimulatorSuccessfullyTest()
        {
            string simulatorLogPath = Path.Combine(Path.GetTempPath(), "simulator-logs");

            var simulator = new Mock<ISimulatorDevice>();
            simulator.SetupGet(x => x.Name).Returns("Test iPhone simulator");
            simulator.SetupGet(x => x.UDID).Returns("58F21118E4D34FD69EAB7860BB9B38A0");
            simulator.SetupGet(x => x.LogPath).Returns(simulatorLogPath);
            simulator.SetupGet(x => x.SystemLog).Returns(Path.Combine(simulatorLogPath, "system.log"));

            _simulatorLoader
                .Setup(x => x.FindSimulators(TestTarget.Simulator_tvOS, _mainLog.Object, true, false))
                .ReturnsAsync(new ISimulatorDevice[] { simulator.Object });

            var testResultFilePath = Path.GetTempFileName();
            var listenerLogFile = Mock.Of<ILog>(x => x.FullPath == testResultFilePath);
            File.WriteAllLines(testResultFilePath, new[] { "Some result here", "Tests run: 124", "Some result there" });

            _logs
                .Setup(x => x.Create("test-Simulator_tvOS-mocked_timestamp.log", "TestLog", It.IsAny<bool?>()))
                .Returns(listenerLogFile);

            var captureLog = new Mock<ICaptureLog>();
            captureLog.SetupGet(x => x.FullPath).Returns(simulatorLogPath);

            var captureLogFactory = new Mock<ICaptureLogFactory>();
            captureLogFactory
                .Setup(x => x.Create(
                   Path.Combine(_logs.Object.Directory, simulator.Object.Name + ".log"),
                   simulator.Object.SystemLog,
                   true,
                   It.IsAny<string>()))
                .Returns(captureLog.Object);

            // Act
            var appRunner = new AppRunner(_processManager.Object,
                _hardwareDeviceLoader.Object,
                _simulatorLoader.Object,
                _listenerFactory,
                _snapshotReporterFactory,
                captureLogFactory.Object,
                Mock.Of<IDeviceLogCapturerFactory>(),
                _testReporterFactory,
                _mainLog.Object,
                _logs.Object,
                _helpers.Object);

            var appInformation = new AppBundleInformation(AppName, AppBundleIdentifier, s_appPath, s_appPath, null);

            var (deviceName, exitCode) = await appRunner.RunApp(
                appInformation,
                TestTarget.Simulator_tvOS,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30),
                ensureCleanSimulatorState: true);

            // Verify
            Assert.AreEqual("Test iPhone simulator", deviceName);
            Assert.AreEqual(0, exitCode);

            var expectedArgs = $"-argument=-connection-mode -argument=none -argument=-app-arg:-autostart " +
                $"-setenv=NUNIT_AUTOSTART=true -argument=-app-arg:-autoexit -setenv=NUNIT_AUTOEXIT=true " +
                $"-argument=-app-arg:-enablenetwork -setenv=NUNIT_ENABLE_NETWORK=true -setenv=DISABLE_SYSTEM_PERMISSION_TESTS=1 -v -v " +
                $"-argument=-app-arg:-hostname:127.0.0.1 -setenv=NUNIT_HOSTNAME=127.0.0.1 -argument=-app-arg:-transport:Tcp " +
                $"-setenv=NUNIT_TRANSPORT=TCP -argument=-app-arg:-hostport:{_listener.Object.Port} " +
                $"-setenv=NUNIT_HOSTPORT={_listener.Object.Port} --launchsim {StringUtils.FormatArguments(s_appPath)} " +
                $"--stdout=tty1 --stderr=tty1 --device=:v2:udid={simulator.Object.UDID}";

            _processManager
                .Verify(
                    x => x.ExecuteCommandAsync(
                       It.Is<MlaunchArguments>(args => args.AsCommandLine() == expectedArgs),
                       _mainLog.Object,
                       It.IsAny<TimeSpan>(),
                       null,
                       It.IsAny<CancellationToken>()),
                    Times.Once);

            _listener.Verify(x => x.Initialize(), Times.AtLeastOnce);
            _listener.Verify(x => x.StartAsync(), Times.AtLeastOnce);
            _listener.Verify(x => x.Cancel(), Times.AtLeastOnce);
            _listener.Verify(x => x.Dispose(), Times.AtLeastOnce);

            _simulatorLoader.VerifyAll();

            captureLog.Verify(x => x.StartCapture(), Times.AtLeastOnce);
            captureLog.Verify(x => x.StopCapture(), Times.AtLeastOnce);

            // When ensureCleanSimulatorState == true
            simulator.Verify(x => x.PrepareSimulator(_mainLog.Object, AppBundleIdentifier));
            simulator.Verify(x => x.KillEverything(_mainLog.Object));
        }

        [Test]
        public void RunOnDeviceWithNoAvailableSimulatorTest()
        {
            _hardwareDeviceLoader = new Mock<IHardwareDeviceLoader>();
            _hardwareDeviceLoader
                .Setup(x => x.FindDevice(RunMode.iOS, _mainLog.Object, false, false))
                .ThrowsAsync(new NoDeviceFoundException());

            // Act
            var appRunner = new AppRunner(_processManager.Object,
                _hardwareDeviceLoader.Object,
                _simulatorLoader.Object,
                _listenerFactory,
                _snapshotReporterFactory,
                Mock.Of<ICaptureLogFactory>(),
                Mock.Of<IDeviceLogCapturerFactory>(),
                _testReporterFactory,
                _mainLog.Object,
                _logs.Object,
                _helpers.Object);

            var appInformation = new AppBundleInformation(AppName, AppBundleIdentifier, s_appPath, s_appPath, null);

            Assert.ThrowsAsync<NoDeviceFoundException>(
                async () => await appRunner.RunApp(
                    appInformation,
                    TestTarget.Device_iOS,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30),
                    ensureCleanSimulatorState: true),
                "Running requires connected devices");
        }

        [Test]
        public async Task RunOnDeviceSuccessfullyTest()
        {
            var deviceSystemLog = new Mock<ILog>();
            deviceSystemLog.SetupGet(x => x.FullPath).Returns(Path.GetTempFileName());

            var deviceLogCapturer = new Mock<IDeviceLogCapturer>();

            var deviceLogCapturerFactory = new Mock<IDeviceLogCapturerFactory>();
            deviceLogCapturerFactory
                .Setup(x => x.Create(_mainLog.Object, deviceSystemLog.Object, "Test iPhone"))
                .Returns(deviceLogCapturer.Object);

            var testResultFilePath = Path.GetTempFileName();
            var listenerLogFile = Mock.Of<ILog>(x => x.FullPath == testResultFilePath);
            File.WriteAllLines(testResultFilePath, new[] { "Some result here", "Tests run: 124", "Some result there" });

            _logs
                .Setup(x => x.Create("test-Device_iOS-mocked_timestamp.log", "TestLog", It.IsAny<bool?>()))
                .Returns(listenerLogFile);

            _logs
                .Setup(x => x.Create("device-Test iPhone-mocked_timestamp.log", "Device log", It.IsAny<bool?>()))
                .Returns(deviceSystemLog.Object);

            // Act
            var appRunner = new AppRunner(_processManager.Object,
                _hardwareDeviceLoader.Object,
                _simulatorLoader.Object,
                _listenerFactory,
                _snapshotReporterFactory,
                Mock.Of<ICaptureLogFactory>(),
                deviceLogCapturerFactory.Object,
                _testReporterFactory,
                _mainLog.Object,
                _logs.Object,
                _helpers.Object);

            var appInformation = new AppBundleInformation(AppName, AppBundleIdentifier, s_appPath, s_appPath, null);

            var (deviceName, exitCode) = await appRunner.RunApp(
                appInformation,
                TestTarget.Device_iOS,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));

            // Verify
            Assert.AreEqual("Test iPhone", deviceName);
            Assert.AreEqual(0, exitCode);

            var ips = string.Join(",", System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));

            var expectedArgs = $"-argument=-connection-mode -argument=none -argument=-app-arg:-autostart " +
                $"-setenv=NUNIT_AUTOSTART=true -argument=-app-arg:-autoexit -setenv=NUNIT_AUTOEXIT=true " +
                $"-argument=-app-arg:-enablenetwork -setenv=NUNIT_ENABLE_NETWORK=true -setenv=DISABLE_SYSTEM_PERMISSION_TESTS=1 -v -v " +
                $"-argument=-app-arg:-hostname:{ips} -setenv=NUNIT_HOSTNAME={ips} -argument=-app-arg:-transport:Tcp " +
                $"-setenv=NUNIT_TRANSPORT=TCP -argument=-app-arg:-hostport:{_listener.Object.Port} " +
                $"-setenv=NUNIT_HOSTPORT={_listener.Object.Port} --launchdev {StringUtils.FormatArguments(s_appPath)} " +
                $"--disable-memory-limits --wait-for-exit --devname \"Test iPhone\"";

            _processManager
                .Verify(
                    x => x.ExecuteCommandAsync(
                       It.Is<MlaunchArguments>(args => args.AsCommandLine() == expectedArgs),
                       It.IsAny<ILog>(),
                       It.IsAny<TimeSpan>(),
                       null,
                       It.IsAny<CancellationToken>()),
                    Times.Once);

            _listener.Verify(x => x.Initialize(), Times.AtLeastOnce);
            _listener.Verify(x => x.StartAsync(), Times.AtLeastOnce);
            _listener.Verify(x => x.Cancel(), Times.AtLeastOnce);
            _listener.Verify(x => x.Dispose(), Times.AtLeastOnce);

            _hardwareDeviceLoader.VerifyAll();

            _snapshotReporter.Verify(x => x.StartCaptureAsync(), Times.AtLeastOnce);
            _snapshotReporter.Verify(x => x.StartCaptureAsync(), Times.AtLeastOnce);

            deviceSystemLog.Verify(x => x.Dispose(), Times.AtLeastOnce);
        }
    }
}

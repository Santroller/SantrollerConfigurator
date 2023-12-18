using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GuitarConfigurator.NetCore.Devices;
using GuitarConfigurator.NetCore.Utils;
using Resources = GuitarConfigurator.NetCore.Resources;

namespace GuitarConfigurator.NetCore;

public class PlatformIo
{
    private static Process? _currentProcess;

    private readonly Process _portProcess;
    private readonly string _pythonExecutable;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string _lastBootloaderPort;

    public PlatformIo()
    {
        var appdataFolder = AssetUtils.GetAppDataFolder();
        if (!File.Exists(appdataFolder)) Directory.CreateDirectory(appdataFolder);

        var pioFolder = Path.Combine(appdataFolder, "platformio");
        _pythonExecutable = Path.Combine(appdataFolder, "python", "bin", "python3.11");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _pythonExecutable = Path.Combine(appdataFolder, "python", "python.exe");

        FirmwareDir = Path.Combine(appdataFolder, "Santroller");

        _portProcess = new Process();
        _portProcess.EnableRaisingEvents = true;
        _portProcess.StartInfo.FileName = _pythonExecutable;
        _portProcess.StartInfo.WorkingDirectory = FirmwareDir;
        _portProcess.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;

        _portProcess.StartInfo.Arguments = "-m platformio device list --json-output";
        _portProcess.StartInfo.UseShellExecute = false;
        _portProcess.StartInfo.RedirectStandardOutput = true;
        _portProcess.StartInfo.RedirectStandardError = true;
        _portProcess.StartInfo.CreateNoWindow = true;
    }

    public string FirmwareDir { get; }

    public static void Exit()
    {
        _currentProcess?.Kill(true);
    }

    private async Task InitialisePlatformIoAsync(IObserver<PlatformIoState> platformIoOutput)
    {
        platformIoOutput.OnNext(new PlatformIoState(0, Resources.ExtractingFirmwareMessage, ""));
        var appdataFolder = AssetUtils.GetAppDataFolder();
        var firmwareVersion = Path.Combine(appdataFolder, "firmware.version");
        if (!Directory.Exists(FirmwareDir))
        {
            // If the firmware has not been extracted, make sure the user has enough free space for it.
            var matching = 0;
            long free = 0;
            var info = DriveInfo.GetDrives().First();
            foreach (var driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.RootDirectory.FullName.Length <= matching ||
                    !Path.GetFullPath(FirmwareDir).StartsWith(driveInfo.RootDirectory.FullName)) continue;
                matching = driveInfo.RootDirectory.FullName.Length;
                free = driveInfo.AvailableFreeSpace;
                info = driveInfo;
            }

            free = free / 1024 / 1024 / 1024;
            if (free < 3)
            {
                platformIoOutput.OnError(new Exception(
                    string.Format(Resources.NoFreeSpaceMessage, info.Name)));
                return;
            }
        }
        else
        {
            Directory.Delete(FirmwareDir, true);
        }

        await AssetUtils.ExtractXzAsync("firmware.tar.xz", appdataFolder,
            progress => platformIoOutput.OnNext(new PlatformIoState(progress * 10, Resources.ExtractingFirmwareMessage,
                "")));

        var pythonDir = Path.Combine(appdataFolder, "python");
        var platformIoDir = Path.Combine(appdataFolder, "platformio");
        var platformIoVersion = Path.Combine(appdataFolder, "platformio.version");
        if (Directory.Exists(platformIoDir))
        {
            var outdated = true;
            if (File.Exists(platformIoVersion))
                outdated = await File.ReadAllTextAsync(platformIoVersion) !=
                           await AssetUtils.ReadFileAsync("platformio.version");

            if (outdated)
            {
                Directory.Delete(platformIoDir, true);
                Directory.Delete(pythonDir, true);
            }
        }

        if (!Directory.Exists(platformIoDir))
        {
            platformIoOutput.OnNext(new PlatformIoState(10, Resources.ExtractingPlatformIoMessage, ""));
            await AssetUtils.ExtractXzAsync("platformio.tar.xz", appdataFolder,
                progress => platformIoOutput.OnNext(
                    new PlatformIoState(10 + progress * 90, Resources.ExtractingFirmwareMessage, "")));

            await AssetUtils.ExtractFileAsync("platformio.version", platformIoVersion);
        }

        platformIoOutput.OnCompleted();
    }

    public IObservable<PlatformIoState> InitialisePlatformIo()
    {
        var platformIoOutput =
            new BehaviorSubject<PlatformIoState>(new PlatformIoState(0, Resources.SettingUpMessage, null));
        _ = InitialisePlatformIoAsync(platformIoOutput).ConfigureAwait(false);
        return platformIoOutput;
    }

    public async Task<PlatformIoPort[]?> GetPortsAsync()
    {
        _portProcess.Start();
        var output = await _portProcess.StandardOutput.ReadToEndAsync();
        await _portProcess.WaitForExitAsync();
        return output != "" ? PlatformIoPort.FromJson(output) : null;
    }

    public BehaviorSubject<PlatformIoState> RunAvrdudeErase(IConfigurableDevice device, string progressMessage,
        double progressStartingPercentage, double progressEndingPercentage)
    {
        return device switch
        {
            Arduino {Is32U4Bootloader: true} => RunPlatformIo("microdetect",
                new[] {"run", "-t", "micro_clean",}, progressMessage, progressStartingPercentage,
                progressEndingPercentage, device, true),
            Santroller or Ardwiino => RunPlatformIo("microdetect",
                new[] {"run", "-t", "micro_clean_existing",}, progressMessage, progressStartingPercentage,
                progressEndingPercentage, device, true),
            _ => RunPlatformIo("microdetect", new[] {"run", "-t", "micro_clean_jump",}, progressMessage,
                progressStartingPercentage, progressEndingPercentage, device, true)
        };
    }

    public BehaviorSubject<PlatformIoState> RunAvrdudeErase(Dfu dfu, string progressMessage,
        double progressStartingPercentage, double progressEndingPercentage, Board board)
    {
        return RunPlatformIo("arduino_uno_usb",
            new[]
            {
                "run", "-t", $"{board.Environment}_{dfu.GetRestoreSuffix()}_clean"
            }, progressMessage, progressStartingPercentage, progressEndingPercentage, dfu, true);
    }

    public BehaviorSubject<PlatformIoState> RunPlatformIo(string environment, string[] command,
        string progressMessage,
        double progressStartingPercentage, double progressEndingPercentage,
        IConfigurableDevice? device, bool erase = false)
    {
        var platformIoOutput =
            new BehaviorSubject<PlatformIoState>(new PlatformIoState(progressStartingPercentage, progressMessage,
                null));

        async Task Process()
        {
            var percentageStep = progressEndingPercentage - progressStartingPercentage;
            var currentProgress = progressStartingPercentage;
            var uploading = command.Length > 1;
            var appdataFolder = AssetUtils.GetAppDataFolder();
            var pioFolder = Path.Combine(appdataFolder, "platformio");

            var args = new List<string>(command);
            args.Insert(0, _pythonExecutable);
            args.Insert(1, "-m");
            args.Insert(2, "platformio");
            args.Add("--environment");
            args.Add(environment);
            var sections = 5;
            string? extraEnvs = null;
            var isUsb = false;
            var hasDfu = device?.HasDfuMode() ?? false;
            var hasDfuArdwiino = environment.EndsWith("_usb");
            var inDfu = environment.EndsWith("_usb_serial");
            if (erase && device != null)
            {
                if (device is Arduino arduino2)
                {
                    args.Add("--upload-port");
                    args.Add(arduino2.GetSerialPort());
                }
                else if (device is not (Ardwiino or Santroller))
                {
                    Console.WriteLine("Detecting port please wait");
                    var port = await device.GetUploadPortAsync().ConfigureAwait(false);
                    Console.WriteLine(port);
                    if (port != null)
                    {
                        args.Add("--upload-port");
                        args.Add(port);
                    }
                }
            }
            else
            {
                if (device is Arduino) sections = 10;
                if (hasDfu)
                {
                    platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                        string.Format(Resources.LookingForDeviceMessage, progressMessage), null));
                    currentProgress += percentageStep / sections;

                    sections = 11;
                    extraEnvs = hasDfuArdwiino ? "PROCEED_WITH_SERIAL" : "PROCEED_WITH_USB";
                    if (device != null)
                    {
                        isUsb = true;
                        if (inDfu)
                        {
                            sections = 17;
                        }
                        else if (hasDfuArdwiino)
                        {
                            device.Bootloader();
                        }
                        else
                        {
                            if (device is Arduino arduino2)
                            {
                                args.Add("--upload-port");
                                args.Add(arduino2.GetSerialPort());
                            }
                            else if (!device.Is32U4())
                            {
                                Console.WriteLine("Detecting port please wait");
                                Trace.WriteLine("Detecting port please wait");
                                var port = await device.GetUploadPortAsync().ConfigureAwait(false);
                                Console.WriteLine(port);
                                if (port != null)
                                {
                                    args.Add("--upload-port");
                                    args.Add(port);
                                }
                            }
                        }
                    }
                }

                if (uploading && !isUsb)
                {
                    if (environment.Contains("pico"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.LookingForDeviceMessage, progressMessage), null));
                        currentProgress += percentageStep / sections;
                        sections = 4;
                    }

                    if (device != null)
                    {
                        string? port = null;
                        if (device.Is32U4())
                        {
                            sections += 1;
                            var subject = RunAvrdudeErase(device, Resources.ErasingMessage, 0,
                                percentageStep / sections);
                            subject.Subscribe(s => platformIoOutput.OnNext(s));
                            await subject;
                            currentProgress += percentageStep / sections;
                            if (device is Arduino {Is32U4Bootloader: true} a)
                            {
                                port = a.GetSerialPort();
                            }
                            else
                            {
                                port = _lastBootloaderPort;
                            }
                        }

                        if (port == null)
                        {
                            Console.WriteLine("Detecting port please wait");
                            port = await device.GetUploadPortAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            Console.WriteLine("Continuing with bootloader mode port");
                        }

                        Console.WriteLine(port);
                        if (port != null)
                        {
                            args.Add("--upload-port");
                            args.Add(port);
                        }
                    }
                }
            }

            await _semaphore.WaitAsync();
            if (_currentProcess is {HasExited: false}) _currentProcess.Kill(true);

            _currentProcess = new Process();
            _currentProcess.EnableRaisingEvents = true;
            _currentProcess.StartInfo.FileName = _pythonExecutable;
            _currentProcess.StartInfo.WorkingDirectory = FirmwareDir;
            _currentProcess.StartInfo.EnvironmentVariables["PLATFORMIO_CORE_DIR"] = pioFolder;
            _currentProcess.StartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
            if (extraEnvs != null)
            {
                _currentProcess.StartInfo.EnvironmentVariables[extraEnvs] = "1";
            }

            _currentProcess.StartInfo.CreateNoWindow = true;
            //Some pio stuff uses Standard Output, some uses Standard Error, its easier to just flatten both of those to a single stream
            _currentProcess.StartInfo.Arguments =
                $"-c \"import subprocess;subprocess.run([{string.Join(",", args.Select(s => $"'{s}'"))}],stderr=subprocess.STDOUT)\""
                    .Replace("\\", "\\\\");

            _currentProcess.StartInfo.UseShellExecute = false;
            _currentProcess.StartInfo.RedirectStandardOutput = true;
            _currentProcess.StartInfo.RedirectStandardError = true;

            var state = 0;
            _currentProcess.Start();
            Console.WriteLine("Starting process " + environment);
            Console.WriteLine(_currentProcess.StartInfo.Arguments);
            Trace.WriteLine("Starting process " + environment);
            Trace.WriteLine(_currentProcess.StartInfo.Arguments);

            // process.BeginOutputReadLine();
            // process.BeginErrorReadLine();
            var buffer = new char[1];
            var hasError = false;
            // In detect mode, the pro micro also goes through two separate programming stages.
            var main = device?.HasDfuMode() == false && !(device is Arduino arduino && arduino.Is32U4());
            while (!_currentProcess.HasExited)
            {
                if (state == 0)
                {
                    var line = await _currentProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    Console.WriteLine(line);

                    if (string.IsNullOrEmpty(line))
                    {
                        await Task.Delay(1);
                        continue;
                    }

                    platformIoOutput.OnNext(platformIoOutput.Value.WithLog(line));
                    if (erase)
                    {
                        if (line.StartsWith("Waiting for bootloader device"))
                        {
                            device?.Bootloader();
                        }
                        if (line.StartsWith("PORT: "))
                        {
                            _lastBootloaderPort = line.Replace("PORT: ", "");
                        }
                    }

                    if (uploading)
                    {
                        var matches = Regex.Matches(line, @"Processing (.+?) \(.+\)");
                        if (matches.Count > 0)
                        {
                            var message = string.Format(Resources.BuildingMessage, progressMessage);

                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                message, null));
                            currentProgress += percentageStep / sections;
                        }

                        if (line.StartsWith("Looking for device in DFU mode") && device is not Santroller && !inDfu)
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                string.Format(Resources.DfuMessage, progressMessage), null));
                        }

                        if (platformIoOutput.Value.Message ==
                            string.Format(Resources.DfuMessage, progressMessage) &&
                            line.StartsWith("Calling"))
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                string.Format(Resources.BuildingMessage, progressMessage), null));
                        }

                        if (line.StartsWith("Detecting microcontroller type"))
                        {
                            if (device is Santroller)
                            {
                                device.Bootloader();
                            }
                        }


                        if (line.StartsWith("searching for uno"))
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                string.Format(Resources.WaitingMessageReplug, progressMessage), null));
                        }

                        if (line.StartsWith("Looking for upload port..."))
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                string.Format(Resources.LookingForPortMessage, progressMessage), null));
                            currentProgress += percentageStep / sections;


                            if (device is Santroller or Ardwiino && !isUsb) device.Bootloader();
                        }

                        if (line.StartsWith("Looking for upload disk..."))
                        {
                            platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                string.Format(Resources.LookingForPortMessage, progressMessage), null));
                            currentProgress += percentageStep / sections;
                        }

                        if (line.Contains("SUCCESS"))
                            if (device is PicoDevice || sections == 5)
                                break;
                    }

                    if (line.Contains("AVR device initialized and ready to accept instructions"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.ReadingSettingsMessage, progressMessage), null));
                        state = 1;
                    }

                    if (line.Contains("writing flash"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.UploadingMessage, progressMessage), null));
                        state = 2;
                    }

                    if (line.Contains("rp2040load"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.UploadingMessage, progressMessage), null));
                        ;
                    }

                    if (line.Contains("Loading into Flash:"))
                    {
                        var done = line.Count(s => s == '=') / 30.0;
                        platformIoOutput.OnNext(new PlatformIoState(
                            currentProgress + percentageStep / sections * done,
                            string.Format(Resources.UploadingMessage, progressMessage), null));
                    }

                    if (line.Contains("reading on-chip flash data"))
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.VerifyingMessage, progressMessage), null));
                        state = 3;
                    }

                    if (line.Contains("avrdude done.  Thank you.") && !inDfu)
                    {
                        if (!main)
                        {
                            main = true;
                            continue;
                        }

                        break;
                    }

                    if (!line.Contains("FAILED")) continue;
                    platformIoOutput.OnError(new Exception(string.Format(Resources.ErrorMessage, progressMessage)));
                    hasError = true;
                    break;
                }
                else
                {
                    while (await _currentProcess.StandardOutput.ReadAsync(buffer, 0, 1) > 0)
                    {
                        // process character...for example:
                        if (buffer[0] == '#') currentProgress += percentageStep / 50 / sections;

                        if (buffer[0] == 's')
                        {
                            state = 0;
                            break;
                        }

                        switch (state)
                        {
                            case 1:
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    string.Format(Resources.ReadingSettingsMessage, progressMessage), null));
                                break;
                            case 2:
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    string.Format(Resources.UploadingMessage, progressMessage), null));
                                break;
                            case 3:
                                platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                                    string.Format(Resources.VerifyingMessage, progressMessage), null));
                                break;
                        }
                    }
                }
            }

            await _currentProcess.WaitForExitAsync();

            if (!hasError)
            {
                if (uploading)
                {
                    currentProgress = progressEndingPercentage;
                    if (sections == 11 && !hasDfuArdwiino || sections == 17)
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.WaitingMessageReplug, progressMessage), null));
                    }
                    else
                    {
                        platformIoOutput.OnNext(new PlatformIoState(currentProgress,
                            string.Format(Resources.WaitingMessage, progressMessage), null));
                    }
                }

                platformIoOutput.OnCompleted();
            }

            _semaphore.Release(1);
        }

        _ = Process();
        return platformIoOutput;
    }

    public record PlatformIoState(double Percentage, string Message, string? Log)
    {
        public PlatformIoState WithLog(string log)
        {
            return this with {Log = log};
        }
    }
}
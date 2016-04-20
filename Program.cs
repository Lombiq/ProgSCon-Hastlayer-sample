using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Hast.Common.Configuration;
using Hast.Common.Models;
using Hast.Layer;
using Hast.VhdlBuilder.Representation;

namespace Hast.Samples.Psc
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                using (var hastlayer = Hast.Xilinx.HastlayerFactory.Create())
                {
                    #region Configuration
                    hastlayer.ExecutedOnHardware += (sender, e) =>
                    {
                        Console.WriteLine(
                            "Executing " +
                            e.MemberFullName +
                            " on hardware took " +
                            e.HardwareExecutionInformation.HardwareExecutionTimeMilliseconds +
                            "ms (net) " +
                            e.HardwareExecutionInformation.FullExecutionTimeMilliseconds +
                            " milliseconds (all together)");
                    };


                    var hardwareConfiguration = new HardwareGenerationConfiguration();
                    hardwareConfiguration.PublicHardwareMemberNamePrefixes.Add("Hast.Samples.Psc.PrimeCalculator");
                    var hardwareRepresentation = await hastlayer.GenerateHardware(
                        new[]
                            {
                                typeof(Program).Assembly
                            },
                        hardwareConfiguration);

                    var stopwatch = new Stopwatch();
                    #endregion


                    var primeCalculator = await hastlayer
                        .GenerateProxy(hardwareRepresentation, new PrimeCalculator());


                    #region Basic samples
                    var isPrime = primeCalculator.IsPrimeNumber(15);
                    var isPrime2 = primeCalculator.IsPrimeNumber(13);
                    var arePrimes = primeCalculator.ArePrimeNumbers(new uint[] { 15, 493, 2341, 99237 });
                    var arePrimes2 = primeCalculator.ArePrimeNumbers(new uint[] { 13, 493 });
                    Debugger.Break();
                    #endregion

                    #region Generating some big numbers
                    var numberCount = 4000;
                    var numbers = new uint[numberCount];
                    for (uint i = (uint)(uint.MaxValue - numberCount); i < uint.MaxValue; i++)
                    {
                        numbers[i - (uint.MaxValue - numberCount)] = (uint)i;
                    }
                    #endregion

                    #region Crunching big numbers
                    stopwatch.Restart();
                    var cpuCalculator = new PrimeCalculator();
                    var arePrimesOnCpu = cpuCalculator.ArePrimeNumbers(numbers.Take(20).ToArray());
                    StopAndWriteTime(stopwatch);
                    Debugger.Break();
                    stopwatch.Restart();
                    primeCalculator.ArePrimeNumbers(numbers.Take(20).ToArray());
                    StopAndWriteTime(stopwatch);
                    Debugger.Break();
                    stopwatch.Restart();
                    var arePrimes3 = primeCalculator.ArePrimeNumbers(numbers);
                    StopAndWriteTime(stopwatch);
                    Debugger.Break();
                    #endregion

                    #region Sequential launch
                    stopwatch.Restart();
                    var sequentialLaunchArePrimes = new bool[10][];
                    for (uint i = 0; i < 10; i++)
                    {
                        sequentialLaunchArePrimes[i] = primeCalculator.ArePrimeNumbers(numbers);
                    }
                    StopAndWriteTime(stopwatch);
                    Debugger.Break();
                    #endregion

                    #region Parallel launch
                    var taskScheduler = new QueuedTaskScheduler(2);
                    stopwatch.Restart();
                    var parallelLaunchedIsPrimeTasks = new List<Task<bool[]>>();
                    for (uint i = 0; i < 10; i++)
                    {
                        parallelLaunchedIsPrimeTasks.Add(Task.Factory.StartNew(() =>
                            primeCalculator.ArePrimeNumbers(numbers),
                            new CancellationToken(),
                            TaskCreationOptions.None,
                            taskScheduler));
                    }
                    var parallelLaunchedArePrimes = await Task.WhenAll(parallelLaunchedIsPrimeTasks);
                    StopAndWriteTime(stopwatch);
                    Debugger.Break();
                    #endregion

                    #region Looking at VHDL
                    File.WriteAllText(@"C:\HastlayerSample.vhd", ToVhdl(hardwareRepresentation.HardwareDescription));
                    Debugger.Break();
                    #endregion
                }
            }).Wait();
        }


        private static string ToVhdl(IHardwareDescription hardwareDescription)
        {
            return ((Hast.Transformer.Vhdl.Models.VhdlHardwareDescription)hardwareDescription)
                .Manifest.TopModule.ToVhdl(new VhdlGenerationOptions { FormatCode = true, NameShortener = VhdlGenerationOptions.SimpleNameShortener });
        }

        private static void StopAndWriteTime(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            Console.WriteLine("===========");
            Console.WriteLine("Ellapsed milliseconds: " + stopwatch.ElapsedMilliseconds);
            Console.WriteLine();
        }
    }
}

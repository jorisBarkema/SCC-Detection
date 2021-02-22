using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SCC_Detection.Datastructures;
using SCC_Detection.Input;
using SCC_Detection.SCCDetectors;

namespace SCC_Detection
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4) throw new Exception("Too few arguments given");

            int tests = int.Parse(args[0]);
            int threads = int.Parse(args[1]);
            string algorithms = args[2];

            SCCDetector[] detectors = ToSCCDetectors(algorithms, threads);

            Graph g;
            string input = args[3];

            Console.WriteLine($"Testing {algorithms} on {threads} thread(s) with graph:");

            switch (input)
            {
                case "file":
                    if (args.Length == 6)
                    {
                        g = CreateFileGraph(args[4], args[5], detectors);
                    } else if (args.Length == 5)
                    {
                        g = CreateFileGraph(args[4], "LIST", detectors);
                    } else
                    {
                        throw new Exception("Invalid arguments given for file input");
                    }
                    break;
                case "random":
                    if (args.Length == 6)
                    {
                        g = CreateRandomGraph(int.Parse(args[4]), double.Parse(args[5]), detectors);
                    }
                    else
                    {
                        throw new Exception("Invalid arguments given for file input");
                    }
                    break;
                case "tree":
                    g = CreateTreeGraph();
                    break;
                default:
                    throw new Exception("Invalid input method passed");
            }

            //TODO: timing for the analysis

            //Console.WriteLine(g.ToString());

            // Run at highest priority to minimize fluctuations caused by other processes/threads
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            Stopwatch stopwatch = new Stopwatch();

            foreach (SCCDetector detector in detectors)
            {
                Console.WriteLine("Warming up " + detector.Name);
                ResultSet r = detector.Compute(g);

                List<long> durations = new List<long>();

                for (int i = 0; i < tests; i++)
                {
                    // clean up
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    stopwatch.Start();

                    r = detector.Compute(g);

                    stopwatch.Stop();
                    
                    //Console.WriteLine(r);

                    long elapsedTime = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine(detector.Name + ": " + elapsedTime + "ms");
                    durations.Add(elapsedTime);
                    stopwatch.Reset();
                }

                Console.WriteLine();
                Console.WriteLine("Average duration: " + durations.Average() + "ms");
                Console.WriteLine();
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static Graph CreateFileGraph(string filename, string filetype, SCCDetector[] detectors)
        {
            return filetype == "SNAP" ? GraphParser.ReadFileSNAP(filename) : GraphParser.ReadFile(filename);
        }

        private static Graph CreateRandomGraph(int n, double p, SCCDetector[] detectors)
        {
            return RandomGraph.Generate(n, p);
        }

        private static Graph CreateTreeGraph()
        {
            // Save this one for last if needed
            return null;
        }

        private static SCCDetector[] ToSCCDetectors(string argument, int threads)
        {
            switch(argument)
            {
                case "DCSC":
                    return new SCCDetector[1] { new DCSC(threads) };
                case "OBFR":
                    return new SCCDetector[1] { new OBFR(threads) };
                case "MULTIPIVOT":
                    return new SCCDetector[1] { new MultiPivot(threads) };
                case "ALL":
                    return new SCCDetector[3] { new DCSC(threads), new OBFR(threads), new MultiPivot(threads) };
                default:
                    throw new Exception("Invalid algorithms input passed");
            }
        }
    }
}

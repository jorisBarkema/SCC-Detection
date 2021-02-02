using System;

using SCC_Detection.Datastructures;
using SCC_Detection.Input;
using SCC_Detection.SCCDetectors;

namespace SCC_Detection
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3) throw new Exception("Too few arguments given");

            int threads = int.Parse(args[0]);
            string algorithms = args[1];

            SCCDetector[] detectors = ToSCCDetectors(algorithms, threads);

            Graph g;
            string input = args[2];

            Console.WriteLine($"Testing {algorithms} on {threads} thread(s) with graph:");

            switch (input)
            {
                case "file":
                    if (args.Length == 5)
                    {
                        g = CreateFileGraph(args[3], args[4], detectors);
                    } else if (args.Length == 4)
                    {
                        g = CreateFileGraph(args[3], "LIST", detectors);
                    } else
                    {
                        throw new Exception("Invalid arguments given for file input");
                    }
                    break;
                case "random":
                    if (args.Length == 5)
                    {
                        g = CreateRandomGraph(int.Parse(args[3]), double.Parse(args[4]), detectors);
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

            Console.WriteLine(g.ToString());

            foreach (SCCDetector detector in detectors)
            {
                ResultSet r = detector.Compute(g);
                Console.WriteLine(r);
            }

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
                case "all":
                    return new SCCDetector[1] { new DCSC(threads) };
                default:
                    throw new Exception("Invalid algorithms input passed");
            }
        }
    }
}

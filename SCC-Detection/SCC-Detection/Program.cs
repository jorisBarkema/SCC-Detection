using System;

using SCC_Detection.Datastructures;
using SCC_Detection.Input;

namespace SCC_Detection
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3) throw new Exception("Too few arguments given");

            int threads = int.Parse(args[0]);
            string algorithms = args[1];
            // TODO: Create list/array of classes which can calculate the TSCC/SCCs to pass along to the tests

            string input = args[2];

            switch(input)
            {
                case "file":
                    if (args.Length == 5)
                    {
                        FileGraphTest(args[3], args[4]);
                    } else if (args.Length == 4)
                    {
                        FileGraphTest(args[3], "LIST");
                    } else
                    {
                        throw new Exception("Invalid arguments given for file input");
                    }
                    break;
                case "random":
                    if (args.Length == 5)
                    {
                        RandomGraphTest(int.Parse(args[3]), long.Parse(args[4]));
                    }
                    else
                    {
                        throw new Exception("Invalid arguments given for file input");
                    }
                    break;
                case "tree":
                    TreeGraphTest();
                    break;
                default:
                    throw new Exception("No fitting input method passed");
            }

            Console.ReadLine();
        }

        private static void FileGraphTest(string filename, string filetype)
        {
            Graph g = filetype == "SNAP" ? GraphParser.ReadFileSNAP(filename) : GraphParser.ReadFile(filename);
        }

        private static void RandomGraphTest(int n, long p)
        {
            Graph g = RandomGraph.Generate(n, p);
        }

        private static void TreeGraphTest()
        {
            // Save this one for last if needed
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SCC_Detection.Datastructures;

namespace SCC_Detection.Input
{
    class GraphParser
    {
        public static Graph ReadFile(string filename)
        {
            Dictionary<int, List<int>> map = new Dictionary<int, List<int>>();

            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    // Read all of the ints on the line (separated by whitespace)
                    // and parse them to int
                    List<int> ints = line.Split().Select(x => int.Parse(x)).ToList();
                    map[ints[0]] = new List<int>();

                    if (ints.Count > 1)
                    {
                        map[ints[0]] = ints.GetRange(1, ints.Count - 1);
                    }
                }
            }

            return new Graph(map);
        }

        public static Graph ReadFileSNAP(string filename)
        {
            Dictionary<int, List<int>> map = new Dictionary<int, List<int>>();

            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line[0] == '#') continue;

                    // Read all of the ints on the line (separated by whitespace)
                    // and parse them to int
                    List<int> ints = line.Split().Select(x => int.Parse(x)).ToList();

                    if (ints.Count < 2) throw new Exception($"Exception in SNAP file: no two integers on line {line}");

                    int from = ints[0];
                    int to = ints[1];

                    if (!map.ContainsKey(from)) map[from] = new List<int>();
                    if (!map.ContainsKey(to)) map[to] = new List<int>();

                    map[from].Add(to);
                }
            }

            return new Graph(map);
        }
    }
}

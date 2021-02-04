using System;
using System.Collections.Generic;
using System.Text;

using SCC_Detection.Datastructures;

namespace SCC_Detection.Input
{
    public class RandomGraph
    {
        public static Graph Generate(int n, double p)
        {
            Dictionary<int, List<int>> map = new Dictionary<int, List<int>>();

            Random r = new Random();

            for (int id = 0; id < n; id++)
            {
                List<int> neighbours = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    if (i == id) continue;

                    if (r.NextDouble() < p)
                    {
                        neighbours.Add(i);
                    }
                }

                map[id] = neighbours;
            }

            return new Graph(map);
        }
    }
}

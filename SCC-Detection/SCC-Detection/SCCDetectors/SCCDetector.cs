using SCC_Detection.Datastructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection.SCCDetectors
{
    public abstract class SCCDetector
    {
        public string Name;

        public abstract ResultSet Compute(Graph g);
    }
}

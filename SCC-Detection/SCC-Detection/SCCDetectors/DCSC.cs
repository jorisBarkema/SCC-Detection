using SCC_Detection.Datastructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection.SCCDetectors
{
    class DCSC : SCCDetector
    {
        ResultSet result;
        int threads;

        public DCSC(int threads)
        {
            this.threads = threads;
        }

        public override ResultSet Compute(Graph g)
        {
            this.Process(g, null);
            return this.result;
        }

        private void Process(Graph g, HashSet<int> subset)
        {
            int pivot = g.RandomId();


            return;
        }
    }
}

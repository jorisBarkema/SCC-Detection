﻿using SCC_Detection.Datastructures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCC_Detection.SCCDetectors
{
    public class DCSC : SCCDetector
    {
        CancellationTokenSource source = new CancellationTokenSource();
        readonly object pulseLock = new object();

        ResultSet result;
        int threads;
        ConcurrentQueue<HashSet<int>> taskList;

        bool[] status;
        Graph g;

        public DCSC(int threads)
        {
            this.threads = threads;
            this.status = new bool[threads];

            this.result = new ResultSet();

            this.taskList = new ConcurrentQueue<HashSet<int>>();
        }

        public override ResultSet Compute(Graph g)
        {
            this.g = g;

            taskList.Enqueue(g.Vertices());

            Task[] tasks = new Task[threads];

            CancellationToken token = source.Token;

            for (int i = 0; i < threads; i++)
            {
                // Make a copy to capture the variable
                // https://stackoverflow.com/questions/271440/captured-variable-in-a-loop-in-c-sharp
                int copy = i;
                tasks[i] = Task.Factory.StartNew(() => ThreadTask(copy), token);
            }

            Task.WaitAny(tasks);


            return this.result;
        }

        private void ThreadTask(int id)
        {
            HashSet<int> subgraph;
            
            while(true)
            {
                this.status[id] = false;

                while (taskList.TryDequeue(out subgraph))
                {
                    ProcessSubgraph(subgraph);
                }

                this.status[id] = true;

                if (this.Done())
                {
                    return;
                }

                lock (pulseLock)
                {
                    Monitor.Wait(pulseLock);
                }
            }
            
            /*
            while (true)
            {
                while (taskList.Count == 0)
                {
                    this.status[id] = true;

                    if (this.Done()) {
                        Monitor.Pulse(taskList);
                        return;
                    }

                    Monitor.Wait(taskList);
                }

                this.status[id] = false;
                subgraph = taskList.TryDequeue();
                
                ProcessSubgraph(subgraph);
            }
            */
        }

        private void ProcessSubgraph(HashSet<int> subgraph)
        {
            if (subgraph.Count == 0) return;

            int pivot = g.PivotFromSet(subgraph);

            HashSet<int> forward = g.Forward(pivot, subgraph);
            HashSet<int> backward = g.Backward(pivot, subgraph);

            // Need to clone because IntersectWith modifies the existing set 
            // and we need the original forward for th next step
            HashSet<int> SCC = new HashSet<int>(forward, forward.Comparer);
            SCC.IntersectWith(backward);

            // ResultSet has the locks so no need here
            this.result.Add(SCC);
            
            // Calculate the remainder set
            subgraph.ExceptWith(forward);
            subgraph.ExceptWith(backward);

            forward.ExceptWith(SCC);
            backward.ExceptWith(SCC);
            
            this.taskList.Enqueue(subgraph);
            this.taskList.Enqueue(forward);
            this.taskList.Enqueue(backward);

            lock(pulseLock)
            {
                Monitor.PulseAll(pulseLock);
            }
            
            return;
        }

        private bool Done()
        {
            return !this.status.Contains(false);
        }

        private void PrintHashSet(HashSet<int> set)
        {
            string s = "";

            foreach (int i in set)
            {
                s += i + " ";
            }

            Console.WriteLine(s);
        }
    }
}

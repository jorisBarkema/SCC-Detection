using System;
using System.Collections.Generic;
using System.Text;

namespace SCC_Detection.Datastructures
{
    class ResultSet
    {
        private List<HashSet<int>> list;
        //private ReentrantLock lock;

    public ResultSet()
        {
            list = new List<HashSet<int>>();
            //lock = new ReentrantLock();
        }

        public void Add(HashSet<int> set)
        {
            // Maybe this should be used differently, give class a member
            // private readonly object balanceLock = new object();
            // and lock that instead of this
            lock (this)
            {
                list.Add(set);
            }
        }

        /*used in CH*/
        public void AddAll(List<HashSet<int>> results)
        {
            throw new NotImplementedException();
            //this.list.AddAll(results);
        }

        public int Count()
        {
            lock(this) {
                return list.Count;
            }
        }

        public bool Contains(int id)
        {
            lock(this)
            {
                foreach(HashSet<int> set in list)
                {
                    if (set.Contains(id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        public override string ToString()
        {

            String result = "";
            Dictionary<int, int> components = new Dictionary<int, int>(); //size of component - number of such components
            foreach (HashSet<int> set in list)
            {
                int size = set.Count;

                if (components.ContainsKey(size))
                {
                    int amount = components[size];
                    amount++;
                    components[size] = amount;
                }
                else
                {
                    components[size] = 1;
                }
            }

            foreach (KeyValuePair<int, int> entry in components)
            {
                result += "size " + entry.Key + ": " + entry.Value;
                result += "\n";
            }

            return result;
        }
    }
}

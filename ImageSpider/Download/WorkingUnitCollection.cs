using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Spider
{
    public class WorkingUnitCollection
    {
        private int count;
        private bool[] busy;

        public WorkingUnitCollection(int count)
        {
            this.count = count;
            this.busy = new bool[count];
        }

        public void StartWorking(int index)
        {
            if (!busy[index])
            {
                busy[index] = true;
            }
        }

        public void FinishWorking(int index)
        {
            if (busy[index])
            {
                busy[index] = false;
            }
        }

        public bool IsWorking(int index)
        {
            return busy[index];
        }

        public bool IsAllFinished()
        {
            bool notEnd = false;
            foreach (var b in busy)
            {
                notEnd |= b;
            }
            return !notEnd;
        }

        public void WaitAllFinished()
        {
            while (true)
            {
                if (IsAllFinished())
                {
                    break;
                }
                Thread.Sleep(1000);
            }
        }

        public void AbortAllWork()
        {
            for (int i = 0; i < count; i++)
            {
                busy[i] = false;
            }
        }
    }
}

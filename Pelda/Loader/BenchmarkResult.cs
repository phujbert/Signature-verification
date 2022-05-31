using System;
using System.Collections.Generic;
using System.Text;

namespace SVC2021.Entities
{
    class BenchmarkResult
    {
        public double Threshold { get; set; }
        public int FalseRejection { get; set; }
        public int GenuineCount { get; set; }
        public double FRR => ((double)FalseRejection) / ((double)GenuineCount);
        public int FalseAcceptance { get; set; }
        public int ForgeryCount { get; set; }
        public double FAR => ((double)FalseAcceptance) / ((double)ForgeryCount);
        public double AER => (FAR + FRR) / 2;

        public double DTW { get; set; }

        public override string ToString()
        {
            return $"Threshold: {Threshold} AER: {AER}";
        }


    }
}

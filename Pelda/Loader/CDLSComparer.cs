using SigStat.Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Pelda.Loader
{
    class CDLSComparer : IEqualityComparer<ClassifierDistanceLogState>
    {
        public bool Equals([AllowNull] ClassifierDistanceLogState x, [AllowNull] ClassifierDistanceLogState y)
        {
            return x.Signature1Id == y.Signature1Id && x.Signature2Id == y.Signature2Id;
        }

        public int GetHashCode([DisallowNull] ClassifierDistanceLogState obj)
        {
            return (obj.Signature1Id + ":" + obj.Signature2Id).GetHashCode();
        }
    }
}

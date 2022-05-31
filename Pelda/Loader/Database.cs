using SigStat.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVC2021.Entities
{
    class Database
    {
        public List<Signer> AllSigners { get; set; }

        public List<Signer> Mcyt { get; set; }
        public List<Signer> eBioSignDS1 { get; set; }
        public List<Signer> eBioSignDS2 { get; set; }
        public List<Signer> BiosecurID { get; set; }
        public List<Signer> BiosecureDS2 { get; set; }

        private Dictionary<string, Svc2021Signature> signaturesById { get; set; }

        public Svc2021Signature this[string id]
        {
            get { return signaturesById[id.ToLower()]; }
        }

        public Database (IEnumerable<Signer> signers)
        {
            AllSigners = signers.ToList();

            Mcyt = AllSigners.Where(s => s.Signatures.Any(s => s.GetFeature(Svc2021.DB) == DB.Mcyt)).ToList();
            eBioSignDS1 = AllSigners.Where(s => s.Signatures.Any(s => s.GetFeature(Svc2021.DB) == DB.eBioSignDS1)).ToList();
            eBioSignDS2 = AllSigners.Where(s => s.Signatures.Any(s => s.GetFeature(Svc2021.DB) == DB.eBioSignDS2)).ToList();
            BiosecurID = AllSigners.Where(s => s.Signatures.Any(s => s.GetFeature(Svc2021.DB) == DB.BiosecurID)).ToList();
            BiosecureDS2 = AllSigners.Where(s => s.Signatures.Any(s => s.GetFeature(Svc2021.DB) == DB.BiosecureDS2)).ToList();

            signaturesById = new Dictionary<string, Svc2021Signature>();
            foreach (var signer in AllSigners)
            {
                foreach (Svc2021Signature signature in signer.Signatures)
                {
                    signaturesById.Add(signature.ID.ToLower(), signature);
                }
            }
        }

    }
}

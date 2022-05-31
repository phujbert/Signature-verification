using SigStat.Common;
using SigStat.Common.Helpers.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using static SVC2021.Svc2021Loader;

namespace SVC2021.Entities
{

    class Comparison1v1 : IEnumerable<object>
    {
        static PropertyInfo[] SerializableProperties = typeof(Comparison1v1).GetProperties().Where(p => p.GetCustomAttribute<ExcelIgnoreAttribute>() == null).ToArray();

        [Display(Name = "Reference file")]
        public string ReferenceSignatureFile { get; private set; }
        [ExcelIgnore]
        public Svc2021Signature ReferenceSignature { get; set; }
        [Display(Name = "Reference signer")]
        public string ReferenceSigner { get; private set; }
        [Display(Name = "Reference input")]
        public InputDevice ReferenceInput { get; private set; }

        [Display(Name = "Questioned file")]
        public string QuestionedSignatureFile { get; private set; }
        [ExcelIgnore]
        public Svc2021Signature QuestionedSignature { get; set; }
        [Display(Name = "Questioned signer")]
        public string QuestionedSigner { get; private set; }
        [Display(Name = "Questioned input")]
        public InputDevice QuestionedInput { get; private set; }

        [Display(Name = "Questioned origin")]
        public string Origin { get; private set; }

        [Display(Name = "Prediction")]
        public double Prediction { get; set; }

        [Display(Name = "Expected prediction")]
        public double ExpectedPrediction { get; private set; }

        [ExcelIgnore]
        public List<KeyValuePair<string, double>> Metadata { get; set; } = new List<KeyValuePair<string, double>>();



        public Comparison1v1(string referenceSignatureFile, string questionedSignatureFile, Svc2021Signature referenceSiganture, Svc2021Signature questionedSignature)
        {
            ReferenceSignatureFile = referenceSignatureFile;
            QuestionedSignatureFile = questionedSignatureFile;

            ReferenceSignature = referenceSiganture;
            QuestionedSignature = questionedSignature;

            var rf = new SignatureFile(referenceSignatureFile);
            var qf = new SignatureFile(questionedSignatureFile);
            if (rf.SignerID != qf.SignerID)
                Origin = "Random";
            else if (qf.Origin == SigStat.Common.Origin.Forged)
                Origin = "Forged";
            else
                Origin = "Genuine";

            ExpectedPrediction = Origin == "Genuine" ? 0 : 1;

            ReferenceSigner = rf.SignerID;
            ReferenceInput = referenceSiganture?.InputDevice?? rf.InputDevice;

            QuestionedSigner = qf.SignerID;
            QuestionedInput = questionedSignature?.InputDevice ?? qf.InputDevice;
        }


        public void Add(string key,double value)
        {
            Metadata.Add(new KeyValuePair<string, double>(key, value));
        }

        public IEnumerable<string> GetHeaders()
        {
            return SerializableProperties.Select(p => p.Name).Concat(Metadata.Select(m => m.Key));
        }

        public IEnumerator<object> GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        IEnumerable<object> GetData()
        {
            foreach (var item in SerializableProperties)
            {
                yield return item.GetValue(this, null);
            }
            foreach (var kvp in Metadata)
            {
                yield return kvp.Value;
            }

        }
    }
}

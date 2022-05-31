using SigStat.Common.Algorithms;
using SigStat.Common.Helpers;
using SigStat.Common.Pipeline;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SigStat.Common.Helpers.Serialization;
using SigStat.Common.Logging;
using SigStat.Common;
using SigStat.Common.Algorithms.Distances;
using SVC2021.Entities;

namespace SVC2021.Classifiers
{

    /// <summary>
    /// Represents a trained model for <see cref="DtwMinMaxClassifier"/>
    /// </summary>
    public class Dtw1v1SignerModel : ISignerModel
    {
        /// <inheritdoc/>
        public string SignerID { get; set; }

        public string SignatureID { get; set; }
        /// <summary>
        /// A list a of genuine signatures used for training
        /// </summary>
        public double[][] FeatureValues { get; set; }
    }
    /// <summary>
    /// Classifies Signatures with the DTW algorithm.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Dtw1v1Classifier : PipelineBase, IClassifier
    {
        public double GenuineThreshold { get; set; }
        public double InconclusiveThreshold { get; set; }

        public double ForgeryThreshold { get; set; }

        /// <summary>
        /// The function used to calculate the distance between two data points during DTW calculation
        /// </summary>
        [JsonConverter(typeof(DistanceFunctionJsonConverter))]
        public IDistance<double[][]> DistanceFunction { get; set; } = new DtwDistance();

        /// <summary>
        /// Gets or sets the features to consider during distance calculation
        /// </summary>
        [Input]
        public List<FeatureDescriptor> Features { get; set; } = new List<FeatureDescriptor>();


        public Dtw1v1Classifier(double genuineThreshold, double inconclusiveThreshold, double forgeryThreshold)
        {
            GenuineThreshold = genuineThreshold;
            InconclusiveThreshold = inconclusiveThreshold;
            ForgeryThreshold = forgeryThreshold;

            if (ForgeryThreshold < GenuineThreshold || ForgeryThreshold < InconclusiveThreshold
                || InconclusiveThreshold < GenuineThreshold)
                throw new InvalidOperationException();
        }

        public Dtw1v1Classifier(double genuineThreshold, double forgeryThreshold)
        {
            GenuineThreshold = genuineThreshold;
            InconclusiveThreshold = genuineThreshold + (forgeryThreshold - genuineThreshold) / 2;
            ForgeryThreshold = forgeryThreshold;

            if (ForgeryThreshold < GenuineThreshold)
                throw new InvalidOperationException();
        }

        /// <inheridoc/>
        public ISignerModel Train(List<Signature> signatures)
        {
            if (signatures == null || signatures.Count != 1)
                throw new ArgumentException("Argument 'signatures' can not be null or have more than one element", nameof(signatures));
            var signature = signatures[0];

            return new Dtw1v1SignerModel
            {
                SignerID = signature.Signer?.ID,
                SignatureID = signature.ID,
                FeatureValues = signature.GetAggregateFeature(Features).ToArray()
            };
        }

        /// <inheritdoc/>
        public double Test(ISignerModel model, Signature signature)
        {
            var dtwModel = (Dtw1v1SignerModel)model;
            var testSignature = signature.GetAggregateFeature(Features).ToArray();

            var distance = DistanceFunction.Calculate(dtwModel.FeatureValues, testSignature);
            //dtwModel.DistanceMatrix[signature.ID, reference.Key] = d;
            this.LogTrace(new ClassifierDistanceLogState(model.SignerID, signature?.Signer?.ID, dtwModel.SignatureID, signature.ID, distance));

            //if (avgDistance < dtwModel.CountMinThreshold || avgDistance > dtwModel.CountMaxThreshold)
            //return 0;

            if (distance < GenuineThreshold)
                return 1;
            if (distance > ForgeryThreshold)
                return 0;


            if (distance < InconclusiveThreshold)

                return (InconclusiveThreshold - distance) / (InconclusiveThreshold - GenuineThreshold) / 2 + 0.5;
            else
                return (ForgeryThreshold - distance) / (ForgeryThreshold - InconclusiveThreshold) / 2;

        }
    }
}

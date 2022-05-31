using SigStat.Common.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SigStat.Common.Pipeline
{
    // TODO: Add() nem kene hogy latszodjon leszarmazottakban, kell egy koztes dolog

    /// <summary>
    /// Runs pipeline items in a sequence.
    /// <para>Default Pipeline Output: Output of the last Item in the sequence.</para>
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class ConditionalSequence : SequentialTransformPipeline
    {

       
        [Input]
        public FeatureDescriptor<bool> ConditionFlag { get; set; }

        public ConditionalSequence()
        {

        }

        public ConditionalSequence(FeatureDescriptor<bool> conditionFlag)
        {
            this.ConditionFlag = conditionFlag;
        }

        /// <summary>
        /// Executes transform <see cref="Items"/> in sequence.
        /// Passes input features for each.
        /// Output is the output of the last Item in the sequence.
        /// </summary>
        /// <param name="signature">Signature to execute transform on.</param>
        public override void Transform(Signature signature)
        {
            //Avoid locking if possible
            if (ConditionFlag != null && signature.GetFeature(ConditionFlag) == true)
                return;

            lock (signature)
            {
                //It's essential to check the flag within the lock-block
                if (ConditionFlag != null && signature.GetFeature(ConditionFlag) == true)
                    return;


                base.Transform(signature);

                signature.SetFeature(ConditionFlag, true);
            }
        }
    }
}

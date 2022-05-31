using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pelda
{
    class ModelOutput
    {
        // ColumnName attribute is used to change the column name from
        // its default value, which is the name of the field.


        [ColumnName("PredictedLabel")]
        public string Prediction;

        // No need to specify ColumnName attribute, because the field
        // name "Probability" is the column name we want.
        public float Probability;

        public float Score;

    }
}

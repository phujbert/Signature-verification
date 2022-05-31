using Microsoft.ML.Data;

namespace Pelda
{
    public class ModelInput
    {


        [ LoadColumn(0)]
        public bool Label { get; set; }


        [ColumnName("stdevX1"), LoadColumn(1)]
        public float StdevX1 { get; set; }


        [ColumnName("stdevY1"), LoadColumn(2)]
        public float StdevY1 { get; set; }


        [ColumnName("stdevP1"), LoadColumn(3)]
        public float StdevP1 { get; set; }


        [ColumnName("count1"), LoadColumn(4)]
        public float Count1 { get; set; }


        [ColumnName("duration1"), LoadColumn(5)]
        public float Duration1 { get; set; }


        [ColumnName("stdevX2"), LoadColumn(6)]
        public float StdevX2 { get; set; }


        [ColumnName("stdevY2"), LoadColumn(7)]
        public float StdevY2 { get; set; }


        [ColumnName("stdevP2"), LoadColumn(8)]
        public float StdevP2 { get; set; }


        [ColumnName("count2"), LoadColumn(9)]
        public float Count2 { get; set; }


        [ColumnName("duration2"), LoadColumn(10)]
        public float Duration2 { get; set; }


        [ColumnName("diffDTW"), LoadColumn(11)]
        public float DiffDTW { get; set; }


        [ColumnName("diffX"), LoadColumn(12)]
        public float DiffX { get; set; }


        [ColumnName("diffY"), LoadColumn(13)]
        public float DiffY { get; set; }


        [ColumnName("diffP"), LoadColumn(14)]
        public float DiffP { get; set; }


        [ColumnName("diffCount"), LoadColumn(15)]
        public float DiffCount { get; set; }


        [ColumnName("diffDuration"), LoadColumn(16)]
        public float DiffDuration { get; set; }


    }
}

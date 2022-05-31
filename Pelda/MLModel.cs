using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pelda
{
    class MLModel
    {
        private static string TRAIN_DATA_FILEPATH = @"C:\Users\patri\Desktop\Egyetem\5_Felev\Temalab\Ml_illesztve\Pelda\comparisons20211028_0141_training.csv";
        private static string MODEL_FILEPATH = @"C:\Users\patri\Desktop\Egyetem\5_Felev\Temalab\Ml_illesztve\Pelda\MLModel\MlModel.zip";

        private static MLContext mlContext = new MLContext();

        public static void createModel()
        {
            //Load data
            IDataView trainingDataView = mlContext.Data.LoadFromTextFile<ModelInput>(
                                            path: TRAIN_DATA_FILEPATH,
                                            hasHeader: true,
                                            separatorChar: ';',
                                            allowQuoting: true,
                                            allowSparse: false);

            //Split data into train and test data
            var dataSplit = mlContext.Data.TrainTestSplit(trainingDataView, testFraction: 0.5);
            IDataView trainData = dataSplit.TrainSet;
            IDataView testData = dataSplit.TestSet;

            // Build training pipeline
            IEstimator<ITransformer> trainingPipeline = BuildTrainingPipeline(mlContext);

            // Train Model
            ITransformer mlModel = TrainModel(mlContext, trainingDataView, trainingPipeline);

            // Evaluate quality of Model
            Evaluate(mlContext, testData, mlModel);

            // Save model
            SaveModel(mlContext, mlModel, MODEL_FILEPATH, trainingDataView.Schema);


        }

        public static IEstimator<ITransformer> BuildTrainingPipeline(MLContext mlContext)
        {
            var trainingPipeline = mlContext.Transforms.Concatenate("Features", "stdevX1", "stdevY1", "stdevP1", "count1",
                "duration1", "stdevX2", "stdevY2", "stdevP2", "count2", "duration2", "diffDTW", "diffX", "diffY", "diffP",
                "diffCount", "diffDuration")
                .Append(mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "Features"));

            return trainingPipeline;
        }

        public static ITransformer TrainModel(MLContext mlContext, IDataView trainingDataView, IEstimator<ITransformer> trainingPipeline)
        {
            Console.WriteLine("=============== Training  model ===============");

            ITransformer model = trainingPipeline.Fit(trainingDataView);

            Console.WriteLine("=============== End of training process ===============");
            return model;
        }

        private static void Evaluate(MLContext mlContext, IDataView testDataView, ITransformer trainedModel)
        {
            Console.WriteLine("=============== Evaluate the model, FAR, FRR => EER ===============");
            var predictions = trainedModel.Transform(testDataView);
            var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"FAR: {1 - metrics.NegativeRecall:P2}");
            Console.WriteLine($"FRR: {1 - metrics.PositiveRecall:P2}");

        }

        private static void SaveModel(MLContext mlContext, ITransformer mlModel, string modelRelativePath, DataViewSchema modelInputSchema)
        {
            // Save/persist the trained model to a .ZIP file
            Console.WriteLine($"=============== Saving the model  ===============");
            mlContext.Model.Save(mlModel, modelInputSchema, GetAbsolutePath(modelRelativePath));
            Console.WriteLine("The model is saved to {0}", GetAbsolutePath(modelRelativePath));
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }

}
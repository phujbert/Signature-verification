using Pelda.Loader;
using SigStat.Common;
using SigStat.Common.Helpers;
using SigStat.Common.Loaders;
using SigStat.Common.Logging;
using SigStat.Common.Model;
using SigStat.Common.Pipeline;
using SigStat.Common.PipelineItems.Transforms.Preprocessing;
using SVC2021;
using SVC2021.Classifiers;
using SVC2021.Entities;
using SVC2021.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pelda
{
    class Program
    {
        static Stopwatch sw = Stopwatch.StartNew();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var logger = new SimpleConsoleLogger();

            //Create dataset
            //string file = GenerateTrainingComparisons("C:\\Users\\patri\\Desktop\\Egyetem\\5_Felev\\Temalab\\DeepSignDB.zip");
            //Solve("C:\\Users\\patri\\Desktop\\Egyetem\\5_Felev\\Temalab\\DeepSignDB.zip", file, false);

            //Create MI modul
            MLModel.createModel();

        }


        public static string GenerateTrainingComparisons(string dbPath)
        {
            string filename = $"comparisons.txt";
            File.WriteAllLines(filename, EnumerateComparisons(dbPath).Distinct());
            return filename;
        }

        public static IEnumerable<string> EnumerateComparisons(string dbPath)
        {
            Console.WriteLine("Generating comparisons");
            int randomCount = 40; //4;
            int genuineCount = 20; //5;
            int forgeryCount = 20;//5;

            var logger = new SimpleConsoleLogger();
            Svc2021Loader loader = new Svc2021Loader(dbPath, true) { Logger = logger };
            var signers = loader.EnumerateSigners().ToList();
            foreach (var signer in signers)
            {
                signer.Signatures.RemoveAll(s => s.GetFeature(Svc2021.InputDevice) != InputDevice.Stylus);
            }
            foreach (var signer in signers)
            {
                signer.Signatures.RemoveAll(s => !Split.Development.HasFlag(s.GetFeature(Svc2021.Split)));
            }

            signers = signers.Where(s => s.Signatures.Count > 0).ToList();
            Console.WriteLine("Found " + signers.Count + " signers");

            var allSignatures = signers.SelectMany(s => s.Signatures).ToList();
            var step = allSignatures.Count / randomCount;

            Console.WriteLine("Found " + allSignatures.Count + " signatures");

            Random rnd = new Random();
            foreach (var signer in signers)
            {
                var genuineSignatures = signer.Signatures.Where(s => s.Origin == Origin.Genuine).ToList();
                var forgedSignatures = signer.Signatures.Where(s => s.Origin == Origin.Forged).ToList();
                var randomForgeries = Enumerable.Range(0, randomCount).Select(i => allSignatures[i * step + rnd.Next(step)]);

                genuineSignatures.LimitRandomly(genuineCount);
                forgedSignatures.LimitRandomly(forgeryCount);
                for (int i = 0; i < genuineSignatures.Count; i++)
                {
                    for (int j = i + 1; j < genuineSignatures.Count; j++)
                    {
                        yield return genuineSignatures[i].ID + " " + genuineSignatures[j].ID;
                    }
                }
                for (int i = 0; i < genuineSignatures.Count; i++)
                {
                    for (int j = 0; j < forgedSignatures.Count; j++)
                    {
                        yield return genuineSignatures[i].ID + " " + forgedSignatures[j].ID;
                    }
                }

                foreach (var sig1 in genuineSignatures)
                {
                    foreach (var sig2 in randomForgeries)
                    {
                        if (sig2.Signer.ID == sig1.Signer.ID) continue;
                        yield return sig1.ID + " " + sig2.ID;
                    }
                }


            }
        }

        public static string Solve(string dbPath, string comparisonsFile, bool useAzureClassification)
        {
            // Option1: Decisions must be made exclusively on comparison signature pairs, no other information can be used

            var reportLogger = new ReportInformationLogger();
            var consoleLogger = new SimpleConsoleLogger();
            var logger = new CompositeLogger() { Loggers = { consoleLogger, reportLogger } };


            string fileBase = Path.GetFileNameWithoutExtension(comparisonsFile);
            string predictionsFile = fileBase + DateTime.Now.ToString("yyyyMMdd_HHmm") + "_predictions.txt";
            string resultsFile = fileBase + DateTime.Now.ToString("yyyyMMdd_HHmm") + "_results.xlsx";
            string trainingFile = fileBase + DateTime.Now.ToString("yyyyMMdd_HHmm") + "_training.csv";


            Debug("Loading signatures");
            var loader = new Svc2021Loader(dbPath, true) { Logger = logger };
            var db = new Database(loader.EnumerateSigners());
            Debug($"Found {db.AllSigners.SelectMany(s => s.Signatures).Count()} signatures from {db.AllSigners?.Count} signers");

            Debug("Loading comparison files");

            var comparisons = ComparisonHelper.LoadComparisons(comparisonsFile, db).ToList();
            Debug($"Found {comparisons.Count} comparisons");


            var referenceSignatures = comparisons.Select(s => s.ReferenceSignature).Distinct().ToList();

            Debug($"Found {referenceSignatures.Count} reference signatures");

            var verifiersBySignature = new ConcurrentDictionary<string, Verifier>();

            var stylusPipeline = new ConditionalSequence(Svc2021.IsPreprocessed)
            {
                    new FilterPoints
                    {
                        InputFeatures = new List<FeatureDescriptor<List<double>>> { Features.X, Features.Y, Features.T },
                        OutputFeatures = new List<FeatureDescriptor<List<double>>> { Features.X, Features.Y, Features.T },
                        KeyFeatureInput = Features.Pressure,
                        KeyFeatureOutput = Features.Pressure
                    },
                    new Scale() { InputFeature = Features.X, OutputFeature = Features.X, Mode = ScalingMode.Scaling1 },
                    new Scale() { InputFeature = Features.Y, OutputFeature = Features.Y, Mode = ScalingMode.Scaling1 },
                    new Scale() { InputFeature = Features.Pressure, OutputFeature = Features.Pressure, Mode = ScalingMode.Scaling1 },

                    new TranslatePreproc(OriginType.CenterOfGravity) { InputFeature = Features.X, OutputFeature = Features.X },
                    new TranslatePreproc(OriginType.CenterOfGravity) { InputFeature = Features.Y, OutputFeature = Features.Y },
                    new TranslatePreproc(OriginType.CenterOfGravity) { InputFeature = Features.Pressure, OutputFeature = Features.Pressure }
            };

            var stylusClassifier = new Dtw1v1Classifier(40, 50, 60) { Features = { Features.X, Features.Y, Features.Pressure } };

            var progress = ProgressHelper.StartNew(referenceSignatures.Count, 10);
            Parallel.ForEach(referenceSignatures, new ParallelOptions { MaxDegreeOfParallelism = -1 }, signature =>
            {
                Verifier verifier = new Verifier()
                {
                    Pipeline =stylusPipeline,
                    Classifier = stylusClassifier,
                    Logger = logger
                };
                verifier.Train(new List<Signature>(1) { signature });
                verifiersBySignature[signature.ID] = verifier;
                progress.IncrementValue();
            });

            Debug($"Verifiers trained");

            progress = ProgressHelper.StartNew(comparisons.Count, 10);
            Parallel.ForEach(comparisons, new ParallelOptions {  MaxDegreeOfParallelism = -1}, comparison =>
            {
                //if (useAzureClassification)
                //{
                //    verifiersBySignature[comparison.ReferenceSignature.ID].Test(comparison.QuestionedSignature);
                //    comparison.Prediction = -1; // Predictions will be calculated by Azure
                //}
                //else
                //{
                    comparison.Prediction = 1 - verifiersBySignature[comparison.ReferenceSignature.ID].Test(comparison.QuestionedSignature);
                //}
                progress.IncrementValue();
            });

            Debug($"Predictions ready");

            var distances = reportLogger
                .GetReportLogs()
                .OfType<ClassifierDistanceLogState>().Distinct(new CDLSComparer())
                .ToDictionary(d => d.Signature1Id + "_" + d.Signature2Id, d => d.Distance);


            foreach (var comparison in comparisons)
            {
                var distance = distances[comparison.ReferenceSignatureFile + "_" + comparison.QuestionedSignatureFile];

                var stdevX1 = comparison.ReferenceSignature.X.StdDiviation();
                var stdevY1 = comparison.ReferenceSignature.Y.StdDiviation();
                var stdevP1 = comparison.ReferenceSignature.Pressure.StdDiviation();
                var count1 = comparison.ReferenceSignature.X.Count;
                var duration1 = comparison.ReferenceSignature.T.Max() - comparison.ReferenceSignature.T.Min();

                var stdevX2 = comparison.QuestionedSignature.X.StdDiviation();
                var stdevY2 = comparison.QuestionedSignature.Y.StdDiviation();
                var stdevP2 = comparison.QuestionedSignature.Pressure.StdDiviation();
                var count2 = comparison.QuestionedSignature.X.Count;
                var duration2 = comparison.QuestionedSignature.T.Max() - comparison.QuestionedSignature.T.Min();

                comparison.Add("stdevX1", stdevX1);
                comparison.Add("stdevY1", stdevY1);
                comparison.Add("stdevP1", stdevP1);
                comparison.Add("count1", count1);
                comparison.Add("duration1", duration1);

                comparison.Add("stdevX2", stdevX2);
                comparison.Add("stdevY2", stdevY2);
                comparison.Add("stdevP2", stdevP2);
                comparison.Add("count2", count2);
                comparison.Add("duration2", duration2);

                comparison.Add("diffDTW", distance);
                comparison.Add("diffX", GetDifference(stdevX1, stdevX2));
                comparison.Add("diffY", GetDifference(stdevY1, stdevY2));
                comparison.Add("diffP", GetDifference(stdevP1, stdevP2));
                comparison.Add("diffCount", GetDifference(count1, count2));
                comparison.Add("diffDuration", GetDifference(duration1, duration2));

            }

           

            ComparisonHelper.SavePredictions(comparisons, predictionsFile);
            ComparisonHelper.SaveComparisons(comparisons, resultsFile);
            ComparisonHelper.SaveTrainingCsv(comparisons, trainingFile);

            Debug($"Predictions saved");

            var results = comparisons.GetBenchmarkResults();
            ComparisonHelper.SaveBenchmarkResults(results, resultsFile);

            //Console.WriteLine(results.GetEer());




            Debug($"Ready");
            return trainingFile;
        }


        public static string Test(string dbPath, string comparisonsFile, bool useAzureClassification)
        {

            var reportLogger = new ReportInformationLogger();
            var consoleLogger = new SimpleConsoleLogger();
            var logger = new CompositeLogger() { Loggers = { consoleLogger, reportLogger } };


            string fileBase = Path.GetFileNameWithoutExtension(comparisonsFile);
            string resultsFile = fileBase + DateTime.Now.ToString("yyyyMMdd_HHmm") + "_results.xlsx";


            var loader = new Svc2021Loader(dbPath, true) { Logger = logger };
            var db = new Database(loader.EnumerateSigners());
          


            Debug($"Predictions saved");

            var testComparisons = ComparisonHelper.LoadComparisons(comparisonsFile, db);

            foreach (var signaturePair in testComparisons)
            {
                var stylusClassifier = new Dtw1v1Classifier(40, 50, 60) { Features = { Features.X, Features.Y, Features.Pressure } };
                var model = stylusClassifier.Train(new List<Signature>() { signaturePair.ReferenceSignature });
                var prediction = stylusClassifier.Test(model, signaturePair.QuestionedSignature);


                signaturePair.Prediction = prediction;
            }

            var results = testComparisons.GetBenchmarkResults();
            var allresult = GetEer(results);
            Console.WriteLine(allresult);
            ComparisonHelper.SaveBenchmarkResults(results, resultsFile);

            Debug($"Ready");
            return "";
        }

        public static BenchmarkResult GetEer(IEnumerable<BenchmarkResult> benchmarks)
        {
            if (benchmarks.All(b => double.IsNaN(b.FRR) || double.IsNaN(b.FAR)))
            {
                Console.WriteLine("Invalid FRR or FAR values in evaluation");
                return null;
            }

            var min = benchmarks.Select(c => Math.Abs(c.FAR - c.FRR)).Min();
            return benchmarks.First(c => Math.Abs(c.FAR - c.FRR) == min);
        }
        static double GetDifference(double d1, double d2)
        {
            return Math.Abs(d1 - d2) / d1;
        }

        static void Debug(string msg)
        {
            var mem = Process.GetCurrentProcess().PagedMemorySize64;
            Console.WriteLine($"{msg} (Time: {sw.Elapsed}, Memory: {mem:n0})");
        }
    }
}

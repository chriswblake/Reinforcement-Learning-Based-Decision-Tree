using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Xunit;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Data;

namespace RLDT.Experiments
{
    public class MushroomsExperiments : Experiment
    {
        //Policy defaults
        readonly double defaultExplorationRate = 0.00;
        readonly double defaultDiscountFactor = 0.85;
        readonly bool defaultParallelQueryUpdatesEnabled = true;
        readonly bool defaultParallelReportUpdatesEnabled = false;
        readonly int defaultQueriesLimit = 1000;
        readonly int defaultTestingInterval = 500;

        [Theory]
        [InlineData("original.csv")]
        [InlineData("random.csv")]
        private string DataSets(string name)
        {
            string path = Path.Combine(ResultsDir, name);
            Assert.True(File.Exists(path));
            return path;
        }

        [Theory]
        [InlineData("original.csv")]
        [InlineData("random.csv")]
        public void DataOrder(string datasetName)
        {
            string order = Path.GetFileNameWithoutExtension(datasetName);
            #region Result Storage
            DataTable results = new DataTable();
            results.Columns.Add("Id", typeof(int));
            results.Columns.Add("Order", typeof(string));
            results.Columns.Add("Pass", typeof(int));
            results.Columns.Add("Instance Id", typeof(int));
            results.Columns.Add("Processed Total", typeof(int));
            results.Columns.Add("States Total", typeof(int));
            results.Columns.Add("Testing Accuracy", typeof(double));
            results.Columns.Add("Training Time", typeof(long));
            results.Columns.Add("Testing Time", typeof(long));
            #endregion

            #region Datasets
            //Training parameters
            string trainingCsvPath = DataSets(datasetName);
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing Parameters
            string testingCsvPath = DataSets("random.csv");
            CsvStreamReader testingData = new CsvStreamReader(testingCsvPath);
            string testingLabelName = "class";
            int testingInterval = defaultTestingInterval;
            #endregion

            #region Policy configuration
            Policy thePolicy = new Policy() {
                ExplorationRate = defaultExplorationRate,
                DiscountFactor = defaultDiscountFactor,
                ParallelQueryUpdatesEnabled = defaultParallelQueryUpdatesEnabled,
                ParallelReportUpdatesEnabled = defaultParallelReportUpdatesEnabled,
                QueriesLimit = defaultQueriesLimit,
        };
            
            #endregion

            #region Processing
            Stopwatch stopwatchProcessing = new Stopwatch(); stopwatchProcessing.Start();
            int processedTotal = 0;
            for (int pass = 1; pass <= passes; pass++)
            {
                //Cycle through each instance in the training file
                while (!trainingData.EndOfStream)
                {
                    //Submit to Trainer
                    Stopwatch stopwatchTraining = new Stopwatch();
                    stopwatchTraining.Start();
                    DataVectorTraining instanceTraining = trainingData.ReadLine(trainingLabelName);
                    TrainingStats trainingStats = thePolicy.Learn(instanceTraining);
                    processedTotal++;
                    stopwatchTraining.Stop();

                    //Record training stats
                    DataRow result = results.NewRow();
                    results.Rows.Add(result);
                    result["Id"] = results.Rows.Count;
                    result["Order"] = order;
                    result["Pass"] = pass;
                    result["Instance Id"] = trainingData.LineNumber;
                    result["Processed Total"] = processedTotal;
                    result["States Total"] = trainingStats.StatesTotal;
                    result["Training Time"] = stopwatchTraining.ElapsedMilliseconds;

                    if (processedTotal % testingInterval == 0)
                    { 
                        //Submit to Tester
                        int testedCount = 0;
                        int correctCount = 0;
                        Stopwatch stopwatchTesting = new Stopwatch();
                        stopwatchTesting.Start();
                        while (!testingData.EndOfStream)
                        {
                            DataVectorTraining instanceTesting = testingData.ReadLine(testingLabelName);
                            //Get values to compare
                            object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                            object correctAnswer = instanceTesting.Label.Value;

                            //Check answer
                            testedCount++;
                            if (prediction.Equals(correctAnswer))
                                correctCount++;
                        }
                        stopwatchTesting.Stop();
                        testingData.SeekOriginBegin();

                        //Record testing stats
                        result["Testing Accuracy"] = correctCount / (double)testedCount;
                        result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                    }
                }

                //Reset training dataset
                trainingData.SeekOriginBegin();
            }
            stopwatchProcessing.Stop();
            #endregion

            #region Save Results
            string suffix = order;

            // Save to CSV file
            results.ToCsv(Path.Combine(ResultsDir, "Data "+suffix+".csv"));

            #region Save chart to html and pdf
            //Create charts
            Chart chartStates = new Chart("States vs Processed", "Processed", "States");
            Chart chartAccuracy = new Chart("Accuracy vs Processed", "Processed", "% Correct");

            //Add data to chart
            foreach (DataRow r in results.Rows)
            {
                chartStates.Add("States", (int)r["Processed Total"], (int)r["States Total"]);
                if (r["Testing Accuracy"] != DBNull.Value)
                    chartAccuracy.Add("Accuracy", (int)r["Processed Total"], (double)r["Testing Accuracy"]);
            }

            //Save charts
            chartStates.ToHtml(Path.Combine(ResultsDir, "States " + suffix));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States " + suffix));

            chartAccuracy.ToHtml(Path.Combine(ResultsDir, "Accuracy " + suffix));
            chartAccuracy.ToPdf(Path.Combine(ResultsDir, "Accuracy " + suffix));
            #endregion

            #region Save metadata file
            StreamWriter swMeta = new StreamWriter(Path.Combine(ResultsDir, "details "+suffix+".txt"));
            swMeta.WriteLine("# Training Configuration");
            swMeta.WriteLine("Training File: " + Path.GetFileName(trainingCsvPath));
            swMeta.WriteLine("Training File Path: " + trainingCsvPath);
            swMeta.WriteLine("Testing File: " + Path.GetFileName(testingCsvPath));
            swMeta.WriteLine("Testing File Path: " + testingCsvPath);
            swMeta.WriteLine("Total Processing Time (ms): " + stopwatchProcessing.ElapsedMilliseconds);
            swMeta.WriteLine("Passes: " + passes);

            swMeta.WriteLine();

            swMeta.WriteLine("# Policy Configuration");
            swMeta.WriteLine("Exploration Rate: " + thePolicy.ExplorationRate.ToString("N2"));
            swMeta.WriteLine("Discount Factor: " + thePolicy.DiscountFactor);
            swMeta.WriteLine("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            swMeta.WriteLine("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);
            swMeta.Close();
            #endregion

            #region Save decision tree (as HTML)
            DecisionTree.TreeSettings ts_simple = new DecisionTree.TreeSettings()
            {
                ShowBlanks = false,
                ShowSubScores = false
            };
            File.WriteAllText(Path.Combine(ResultsDir, "tree-full "+suffix+".html"), thePolicy.DecisionTree.ToHtmlTree());
            File.WriteAllText(Path.Combine(ResultsDir, "tree-simple "+suffix+".html"), thePolicy.ToDecisionTree(ts_simple).ToHtmlTree());
            #endregion
            #endregion

            //Close datasets
            trainingData.Close();
            testingData.Close();
        }

        [Theory]
        [InlineData(new double[] {0.40, 0.45})]
        [InlineData(new double[] {0.50, 0.55, 0.60, 0.65, 0.70, 0.75})]
        [InlineData(new double[] {0.80, 0.85, 0.90, 0.95})]
        public void DiscountFactor(double[] discountFactors)
        {
            #region Result Storage
            DataTable results = new DataTable();
            results.Columns.Add("Id", typeof(int));
            results.Columns.Add("Discount Factor", typeof(double));
            results.Columns.Add("Pass", typeof(int));
            results.Columns.Add("Instance Id", typeof(int));
            results.Columns.Add("Processed Total", typeof(int));
            results.Columns.Add("States Total", typeof(int));
            results.Columns.Add("Testing Accuracy", typeof(double));
            results.Columns.Add("Training Time", typeof(long));
            results.Columns.Add("Testing Time", typeof(long));
            #endregion

            #region Datasets
            //Training
            string trainingCsvPath = DataSets("random.csv");
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing
            string testingCsvPath = DataSets("random.csv");
            CsvStreamReader testingData = new CsvStreamReader(testingCsvPath);
            string testingLabelName = "class";
            int testingInterval = defaultTestingInterval;
            #endregion

            #region Processing
            Stopwatch stopwatchProcessing = new Stopwatch(); stopwatchProcessing.Start();
            Policy thePolicy = null;
            foreach (double discountFactor in discountFactors)
            {
                //Policy Configuration
                thePolicy = new Policy() {
                    ExplorationRate = defaultExplorationRate,
                    DiscountFactor = discountFactor,// defaultDiscountFactor;
                    ParallelQueryUpdatesEnabled = defaultParallelQueryUpdatesEnabled,
                    ParallelReportUpdatesEnabled = defaultParallelReportUpdatesEnabled,
                    QueriesLimit = defaultQueriesLimit,
            };
                

                #region Training/Testing
                int processedTotal = 0;
                for (int pass = 1; pass <= passes; pass++)
                {
                    //Cycle through each instance in the training file
                    while (!trainingData.EndOfStream)
                    {
                        //Submit to Trainer
                        Stopwatch stopwatchTraining = new Stopwatch();
                        stopwatchTraining.Start();
                        DataVectorTraining instanceTraining = trainingData.ReadLine(trainingLabelName);
                        TrainingStats trainingStats = thePolicy.Learn(instanceTraining);
                        processedTotal++;
                        stopwatchTraining.Stop();

                        //Record training stats
                        DataRow result = results.NewRow();
                        results.Rows.Add(result);
                        result["Id"] = results.Rows.Count;
                        result["Discount Factor"] = discountFactor;
                        result["Pass"] = pass;
                        result["Instance Id"] = trainingData.LineNumber;
                        result["Processed Total"] = processedTotal;
                        result["States Total"] = trainingStats.StatesTotal;
                        result["Training Time"] = stopwatchTraining.ElapsedMilliseconds;
                        
                        //Perform testing
                        if (processedTotal % testingInterval == 0)
                        {
                            //Submit to Tester
                            int testedCount = 0;
                            int correctCount = 0;
                            Stopwatch stopwatchTesting = new Stopwatch();
                            stopwatchTesting.Start();
                            while (!testingData.EndOfStream)
                            {
                                DataVectorTraining instanceTesting = testingData.ReadLine(testingLabelName);
                                //Get values to compare
                                object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                                object correctAnswer = instanceTesting.Label.Value;

                                //Check answer
                                testedCount++;
                                if (prediction.Equals(correctAnswer))
                                    correctCount++;
                            }
                            stopwatchTesting.Stop();
                            testingData.SeekOriginBegin();

                            //Record testing stats
                            result["Testing Accuracy"] = correctCount / (double)testedCount;
                            result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                        }
                    }

                    //Reset training dataset
                    trainingData.SeekOriginBegin();
                }
                #endregion
            }
            stopwatchProcessing.Stop();
            #endregion

            #region Save Results
            string suffix = string.Join("-", discountFactors.Select(p => p.ToString("N2"))).Replace("0.", "");

            // Save to CSV file
            results.ToCsv(Path.Combine(ResultsDir, "Data "+suffix+".csv"));

            #region Save chart to html and pdf
            //Create charts
            Chart chartStates = new Chart("States vs Processed", "Processed", "States");
            Chart chartAccuracy = new Chart("Accuracy vs Processed", "Processed", "% Correct");

            // Add data to chart
            foreach(double discountFactor in discountFactors)
            foreach (DataRow r in results.Rows.Cast<DataRow>().Where(p=>p["Discount Factor"].Equals(discountFactor)))
            {
                chartStates.Add(discountFactor.ToString("N2"), (int) r["Processed Total"], (int) r["States Total"]);
                if(r["Testing Accuracy"] != DBNull.Value)
                    chartAccuracy.Add(discountFactor.ToString("N2"), (int) r["Processed Total"], (double) r["Testing Accuracy"]);
            }

            // Save charts
            chartStates.ToHtml(Path.Combine(ResultsDir, "States "+ suffix));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States " + suffix));

            chartAccuracy.ToHtml(Path.Combine(ResultsDir, "Accuracy " + suffix));
            chartAccuracy.ToPdf(Path.Combine(ResultsDir, "Accuracy " + suffix));
            #endregion

            #region Save metadata file
            StreamWriter swMeta = new StreamWriter(Path.Combine(ResultsDir, "details "+suffix+".txt"));
            swMeta.WriteLine("# Training Configuration");
            swMeta.WriteLine("Training File: " + Path.GetFileName(trainingCsvPath));
            swMeta.WriteLine("Training File Path: " + trainingCsvPath);
            swMeta.WriteLine("Testing File: " + Path.GetFileName(testingCsvPath));
            swMeta.WriteLine("Testing File Path: " + testingCsvPath);
            swMeta.WriteLine("Total Processing Time (ms): " + stopwatchProcessing.ElapsedMilliseconds);
            swMeta.WriteLine("Passes: " + passes);

            swMeta.WriteLine();

            swMeta.WriteLine("# Policy Configuration");
            swMeta.WriteLine("Exploration Rate: " + thePolicy.ExplorationRate.ToString("N2"));
            swMeta.WriteLine("Discount Factor: " + "varies");
            swMeta.WriteLine("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            swMeta.WriteLine("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);
            swMeta.Close();
            #endregion
            #endregion

            //Close datasets
            trainingData.Close();
            testingData.Close();
        }

        [Theory]
        [InlineData(new double[] {0.0, 0.01, 0.5 })]
        [InlineData(new double[] {0.0, 0.01, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 })]
        public void ExplorationRate(double[] explorationRates)
        {
            #region Result Storage
            DataTable results = new DataTable();
            results.Columns.Add("Id", typeof(int));
            results.Columns.Add("Exploration Rate", typeof(double));
            results.Columns.Add("Pass", typeof(int));
            results.Columns.Add("Instance Id", typeof(int));
            results.Columns.Add("Processed Total", typeof(int));
            results.Columns.Add("States Total", typeof(int));
            results.Columns.Add("States Created", typeof(int));
            results.Columns.Add("Queries Total", typeof(int));
            results.Columns.Add("Testing Accuracy", typeof(double));
            results.Columns.Add("Training Time", typeof(long));
            results.Columns.Add("Testing Time", typeof(long));
            #endregion

            #region Datasets
            //Training
            string trainingCsvPath = DataSets("random.csv");
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing
            string testingCsvPath = DataSets("random.csv");
            CsvStreamReader testingData = new CsvStreamReader(testingCsvPath);
            string testingLabelName = "class";
            int testingInterval = defaultTestingInterval;
            #endregion

            #region Processing
            Stopwatch stopwatchProcessing = new Stopwatch(); stopwatchProcessing.Start();
            Policy thePolicy = null;
            foreach (double explorationRate in explorationRates)
            {
                //Policy Configuration
                thePolicy = new Policy() {
                    ExplorationRate = explorationRate, //defaultExplorationRate;
                    DiscountFactor = defaultDiscountFactor,
                    ParallelQueryUpdatesEnabled = defaultParallelQueryUpdatesEnabled,
                    ParallelReportUpdatesEnabled = defaultParallelReportUpdatesEnabled,
                    QueriesLimit = defaultQueriesLimit,
            };
                

                #region Training/Testing
                int processedTotal = 0;
                for (int pass = 1; pass <= passes; pass++)
                {
                    //Cycle through each instance in the training file
                    while (!trainingData.EndOfStream)
                    {
                        //Submit to Trainer
                        Stopwatch stopwatchTraining = new Stopwatch();
                        stopwatchTraining.Start();
                        DataVectorTraining instanceTraining = trainingData.ReadLine(trainingLabelName);
                        TrainingStats trainingStats = thePolicy.Learn(instanceTraining);
                        processedTotal++;
                        stopwatchTraining.Stop();

                        //Record training stats
                        DataRow result = results.NewRow();
                        results.Rows.Add(result);
                        result["Id"] = results.Rows.Count;
                        result["Exploration Rate"] = explorationRate;
                        result["Pass"] = pass;
                        result["Instance Id"] = trainingData.LineNumber;
                        result["Processed Total"] = processedTotal;
                        result["States Total"] = trainingStats.StatesTotal;
                        result["States Created"] = trainingStats.StatesCreated;
                        result["Queries Total"] = trainingStats.QueriesTotal;
                        result["Training Time"] = stopwatchTraining.ElapsedMilliseconds;

                        //Perform testing
                        if (processedTotal % testingInterval == 0)
                        {
                            //Submit to Tester
                            int testedCount = 0;
                            int correctCount = 0;
                            Stopwatch stopwatchTesting = new Stopwatch();
                            stopwatchTesting.Start();
                            while (!testingData.EndOfStream)
                            {
                                DataVectorTraining instanceTesting = testingData.ReadLine(testingLabelName);
                                //Get values to compare
                                object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                                object correctAnswer = instanceTesting.Label.Value;

                                //Check answer
                                testedCount++;
                                if (prediction.Equals(correctAnswer))
                                    correctCount++;
                            }
                            stopwatchTesting.Stop();
                            testingData.SeekOriginBegin();

                            //Record testing stats
                            result["Testing Accuracy"] = correctCount / (double)testedCount;
                            result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                        }
                    }

                    //Reset training dataset
                    trainingData.SeekOriginBegin();
                }
                #endregion
            }
            stopwatchProcessing.Stop();
            #endregion

            #region Save Results
            string suffix = string.Join("-", explorationRates.Select(p => p.ToString("N2"))).Replace("0.", "");

            // Save to CSV file
            results.ToCsv(Path.Combine(ResultsDir, "Data " + suffix + ".csv"));

            #region Save chart to html and pdf
            //Create charts
            Chart chartStates = new Chart("States vs Processed", "Processed", "States");
            Chart chartAccuracy = new Chart("Accuracy vs Processed", "Processed", "% Correct");
            Chart chartStatesVsExpRate = new Chart("States vs Exploration Rate", "Exploration Rate", "States");
            Chart chartQueriesVsExpRate = new Chart("Queries vs Exploration Rate", "Exploration Rate", "Queries");

            // Add data to chart
            foreach (double explorationRate in explorationRates)
            {
                var data = results.Rows.Cast<DataRow>().Where(p => p["Exploration Rate"].Equals(explorationRate));
                foreach (DataRow r in data)
                {
                    chartStates.Add(explorationRate.ToString("N2"), (int)r["Processed Total"], (int)r["States Total"]);
                    if (r["Testing Accuracy"] != DBNull.Value)
                        chartAccuracy.Add(explorationRate.ToString("N2"), (int)r["Processed Total"], (double)r["Testing Accuracy"]);
                }
                chartStatesVsExpRate.Add("", (double)data.Last()["Exploration Rate"], (int)data.Last()["States Total"]);
                chartQueriesVsExpRate.Add("Max", (double)data.Last()["Exploration Rate"], (int)data.Max(p=> (int) p["Queries Total"]));
                chartQueriesVsExpRate.Add("Avg", (double)data.Last()["Exploration Rate"], (int)data.Average(p=> (int) p["Queries Total"]));
            }

            // Save charts
            chartStates.ToHtml(Path.Combine(ResultsDir, "States " + suffix));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States " + suffix));

            chartAccuracy.ToHtml(Path.Combine(ResultsDir, "Accuracy " + suffix));
            chartAccuracy.ToPdf(Path.Combine(ResultsDir, "Accuracy " + suffix));

            chartStatesVsExpRate.ToHtml(Path.Combine(ResultsDir, "States Vs Exp Rate " + suffix));
            chartStatesVsExpRate.ToPdf(Path.Combine(ResultsDir, "States Vs Exp Rate " + suffix));

            chartQueriesVsExpRate.ToHtml(Path.Combine(ResultsDir, "Queries Vs Exp Rate " + suffix));
            chartQueriesVsExpRate.ToPdf(Path.Combine(ResultsDir, "Quries Vs Exp Rate " + suffix));

            #endregion

            #region Save metadata file
            StreamWriter swMeta = new StreamWriter(Path.Combine(ResultsDir, "details " + suffix + ".txt"));
            swMeta.WriteLine("# Training Configuration");
            swMeta.WriteLine("Training File: " + Path.GetFileName(trainingCsvPath));
            swMeta.WriteLine("Training File Path: " + trainingCsvPath);
            swMeta.WriteLine("Testing File: " + Path.GetFileName(testingCsvPath));
            swMeta.WriteLine("Testing File Path: " + testingCsvPath);
            swMeta.WriteLine("Total Processing Time (ms): " + stopwatchProcessing.ElapsedMilliseconds);
            swMeta.WriteLine("Passes: " + passes);

            swMeta.WriteLine();

            swMeta.WriteLine("# Policy Configuration");
            swMeta.WriteLine("Exploration Rate: " + "varies");
            swMeta.WriteLine("Discount Factor: " + thePolicy.DiscountFactor.ToString("N2"));
            swMeta.WriteLine("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            swMeta.WriteLine("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);
            swMeta.Close();
            #endregion
            #endregion

            //Close datasets
            trainingData.Close();
            testingData.Close();
        }
    }
}

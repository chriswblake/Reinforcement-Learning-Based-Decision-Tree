using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

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
        readonly int defaultDatasetTrainingPercentage = 80;
        readonly int defaultDatasetTestingPercentage = 80;

        [Theory]
        [InlineData("original", 80)]
        [InlineData("random", 80)]
        [InlineData("randomInversedLabel", 80)]
        public string DataSets(string name, int percentage=100)
        {
            //Remove folder information and extension
            name = Path.GetFileNameWithoutExtension(name);

            //Check that source file is valid
            string pathSource = Path.Combine(ResultsDir, name + ".csv");
            Assert.True(File.Exists(pathSource));

            //Default to source file
            string pathReturn = pathSource;

            //Check the percentages. return the requested sample
            if (percentage > 0 && percentage < 100)
            { 
                int percentTraining = percentage;
                int percentTesting = Math.Abs(100 - percentage);
                if (percentTesting > percentTraining)
                {
                    percentTraining = percentTesting;
                    percentTesting = percentage;
                }

                //Create samples folder, if not existing
                string pathSamplesDir = Path.Combine(ResultsDir, "samples");
                if (!Directory.Exists(pathSamplesDir))
                    Directory.CreateDirectory(pathSamplesDir);

                //Sample the source CV file and create training and testing versions.
                int lineCounter = 0;
                string pathTraining = Path.Combine(ResultsDir, "samples", name + "_"+ percentTraining + ".csv");
                string pathTesting = Path.Combine(ResultsDir, "samples", name + "_" + percentTesting + ".csv");
                if (!File.Exists(pathTraining) || !File.Exists(pathTesting))
                {
                    StreamReader sr = new StreamReader(pathSource);
                    StreamWriter swTraining = new StreamWriter(pathTraining);
                    StreamWriter swTesting = new StreamWriter(pathTesting);
                    Random rand = new Random();

                    //Add headers
                    string headers = sr.ReadLine();
                    swTraining.WriteLine(headers);
                    swTesting.WriteLine(headers);

                    //Copy data over
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine(); lineCounter++;
                        if (rand.Next(0, 101) <= percentage)
                            swTraining.WriteLine(line);
                        else
                            swTesting.WriteLine(line);
                    }

                    sr.Close();
                    swTraining.Close();
                    swTesting.Close();
                }

                //Provide the training or testing CSV path
                pathReturn = Path.Combine(pathSamplesDir, name + "_" + percentage + ".csv");
            }

            return pathReturn;
        }

        [Theory]
        [InlineData("original")]
        [InlineData("random")]
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
            results.Columns.Add("Confusion Matrix", typeof(ConfusionMatrix));
            #endregion

            #region Datasets
            //Training parameters
            string trainingCsvPath = DataSets(datasetName, defaultDatasetTrainingPercentage);
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing Parameters
            string testingCsvPath = DataSets(datasetName, defaultDatasetTestingPercentage);
            CsvStreamReader testingData = new CsvStreamReader(testingCsvPath);
            string testingLabelName = "class";
            int testingInterval = defaultTestingInterval;
            #endregion

            #region Policy configuration
            Policy thePolicy = new Policy()
            {
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

                    //Perform Testing
                    if (processedTotal % testingInterval == 0)
                    {
                        //Submit to Tester
                        ConfusionMatrix confusionMatrix = new ConfusionMatrix();
                        Stopwatch stopwatchTesting = new Stopwatch();
                        stopwatchTesting.Start();
                        while (!testingData.EndOfStream)
                        {
                            DataVectorTraining instanceTesting = testingData.ReadLine(testingLabelName);
                            //Get values to compare
                            object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                            object correctAnswer = instanceTesting.Label.Value;

                            //Build Confusion matrix
                            confusionMatrix.AddEntry(correctAnswer, prediction);
                        }
                        stopwatchTesting.Stop();
                        testingData.SeekOriginBegin();

                        //Record testing stats
                        result["Testing Accuracy"] = confusionMatrix.Accuracy;
                        result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                        result["Confusion Matrix"] = confusionMatrix;
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
            results.ToCsv(Path.Combine(ResultsDir, "Data " + suffix + ".csv"));

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

            #region  Save confusion matrices
            string htmlConfusionMatrix = "<html>";
            htmlConfusionMatrix += ConfusionMatrix.HtmlStyling;
            htmlConfusionMatrix += "<table>";
            htmlConfusionMatrix += "<tr>";
            htmlConfusionMatrix += "<th>Processed Points</th>";
            htmlConfusionMatrix += "<th>Confusion Matrix</th>";
            htmlConfusionMatrix += "</tr>";
            foreach (DataRow dr in results.Rows.Cast<DataRow>().Where(p => p["Confusion Matrix"] != DBNull.Value))
            {
                ConfusionMatrix cm = (ConfusionMatrix)dr["Confusion Matrix"];

                htmlConfusionMatrix += "<tr>";
                htmlConfusionMatrix += String.Format("<td>{0}</td>", dr["Processed Total"]);
                htmlConfusionMatrix += String.Format("<td>{0}</td>", cm.ToHtml());
                htmlConfusionMatrix += "</tr>";
                htmlConfusionMatrix += Environment.NewLine;
                htmlConfusionMatrix += Environment.NewLine;
            }
            htmlConfusionMatrix += "</table>";
            htmlConfusionMatrix += "</html>";
            File.WriteAllText(Path.Combine(this.ResultsDir, "Confusion Matrix " + suffix + ".html"), htmlConfusionMatrix);
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
            File.WriteAllText(Path.Combine(ResultsDir, "tree-full " + suffix + ".html"), thePolicy.DecisionTree.ToHtmlTree());
            File.WriteAllText(Path.Combine(ResultsDir, "tree-simple " + suffix + ".html"), thePolicy.ToDecisionTree(ts_simple).ToHtmlTree());
            #endregion
            #endregion

            //Close datasets
            trainingData.Close();
            testingData.Close();
        }

        [Theory]
        [InlineData(new double[] { 0.40, 0.45 })]
        [InlineData(new double[] { 0.50, 0.55, 0.60, 0.65, 0.70, 0.75 })]
        [InlineData(new double[] { 0.80, 0.85, 0.90, 0.95 })]
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
            results.Columns.Add("Confusion Matrix", typeof(ConfusionMatrix));
            #endregion

            #region Datasets
            //Training
            string trainingCsvPath = DataSets("random", defaultDatasetTrainingPercentage);
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing
            string testingCsvPath = DataSets("random", defaultDatasetTestingPercentage);
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
                thePolicy = new Policy()
                {
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
                            ConfusionMatrix confusionMatrix = new ConfusionMatrix();
                            Stopwatch stopwatchTesting = new Stopwatch();
                            stopwatchTesting.Start();
                            while (!testingData.EndOfStream)
                            {
                                DataVectorTraining instanceTesting = testingData.ReadLine(testingLabelName);
                                //Get values to compare
                                object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                                object correctAnswer = instanceTesting.Label.Value;

                                //Build Confusion matrix
                                confusionMatrix.AddEntry(correctAnswer, prediction);
                            }
                            stopwatchTesting.Stop();
                            testingData.SeekOriginBegin();

                            //Record testing stats
                            result["Testing Accuracy"] = confusionMatrix.Accuracy;
                            result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                            result["Confusion Matrix"] = confusionMatrix;
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
            results.ToCsv(Path.Combine(ResultsDir, "Data " + suffix + ".csv"));

            #region Save chart to html and pdf
            //Create charts
            Chart chartStates = new Chart("States vs Processed", "Processed", "States");
            Chart chartAccuracy = new Chart("Accuracy vs Processed", "Processed", "% Correct");

            // Add data to chart
            foreach (double discountFactor in discountFactors)
                foreach (DataRow r in results.Rows.Cast<DataRow>().Where(p => p["Discount Factor"].Equals(discountFactor)))
                {
                    chartStates.Add(discountFactor.ToString("N2"), (int)r["Processed Total"], (int)r["States Total"]);
                    if (r["Testing Accuracy"] != DBNull.Value)
                        chartAccuracy.Add(discountFactor.ToString("N2"), (int)r["Processed Total"], (double)r["Testing Accuracy"]);
                }

            // Save charts
            chartStates.ToHtml(Path.Combine(ResultsDir, "States " + suffix));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States " + suffix));

            chartAccuracy.ToHtml(Path.Combine(ResultsDir, "Accuracy " + suffix));
            chartAccuracy.ToPdf(Path.Combine(ResultsDir, "Accuracy " + suffix));
            #endregion

            #region  Save confusion matrices
            string htmlConfusionMatrix = "<html>";
            htmlConfusionMatrix += ConfusionMatrix.HtmlStyling;
            htmlConfusionMatrix += "<table>";
            htmlConfusionMatrix += "<tr>";
            htmlConfusionMatrix += "<th>Processed Points</th>";
            htmlConfusionMatrix += "<th>Confusion Matrix</th>";
            htmlConfusionMatrix += "</tr>";
            foreach (DataRow dr in results.Rows.Cast<DataRow>().Where(p => p["Confusion Matrix"] != DBNull.Value))
            {
                ConfusionMatrix cm = (ConfusionMatrix)dr["Confusion Matrix"];

                htmlConfusionMatrix += "<tr>";
                htmlConfusionMatrix += String.Format("<td>{0}</td>", dr["Processed Total"]);
                htmlConfusionMatrix += String.Format("<td>{0}</td>", cm.ToHtml());
                htmlConfusionMatrix += "</tr>";
                htmlConfusionMatrix += Environment.NewLine;
                htmlConfusionMatrix += Environment.NewLine;
            }
            htmlConfusionMatrix += "</table>";
            htmlConfusionMatrix += "</html>";
            File.WriteAllText(Path.Combine(this.ResultsDir, "Confusion Matrix " + suffix + ".html"), htmlConfusionMatrix);
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
        [InlineData(new double[] { 0.0, 0.01, 0.5 })]
        [InlineData(new double[] { 0.0, 0.01, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 })]
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
            results.Columns.Add("Confusion Matrix", typeof(ConfusionMatrix));
            #endregion

            #region Datasets
            //Training
            string trainingCsvPath = DataSets("random", defaultDatasetTrainingPercentage);
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing
            string testingCsvPath = DataSets("random", defaultDatasetTestingPercentage);
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
                thePolicy = new Policy()
                {
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
                            ConfusionMatrix confusionMatrix = new ConfusionMatrix();
                            Stopwatch stopwatchTesting = new Stopwatch();
                            stopwatchTesting.Start();
                            while (!testingData.EndOfStream)
                            {
                                DataVectorTraining instanceTesting = testingData.ReadLine(testingLabelName);
                                //Get values to compare
                                object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                                object correctAnswer = instanceTesting.Label.Value;

                                //Build Confusion matrix
                                confusionMatrix.AddEntry(correctAnswer, prediction);
                            }
                            stopwatchTesting.Stop();
                            testingData.SeekOriginBegin();

                            //Record testing stats
                            result["Testing Accuracy"] = confusionMatrix.Accuracy;
                            result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                            result["Confusion Matrix"] = confusionMatrix;
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
                chartQueriesVsExpRate.Add("Max", (double)data.Last()["Exploration Rate"], (int)data.Max(p => (int)p["Queries Total"]));
                chartQueriesVsExpRate.Add("Avg", (double)data.Last()["Exploration Rate"], (int)data.Average(p => (int)p["Queries Total"]));
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

            #region  Save confusion matrices
            string htmlConfusionMatrix = "<html>";
            htmlConfusionMatrix += ConfusionMatrix.HtmlStyling;
            htmlConfusionMatrix += "<table>";
            htmlConfusionMatrix += "<tr>";
            htmlConfusionMatrix += "<th>Processed Points</th>";
            htmlConfusionMatrix += "<th>Confusion Matrix</th>";
            htmlConfusionMatrix += "</tr>";
            foreach (DataRow dr in results.Rows.Cast<DataRow>().Where(p => p["Confusion Matrix"] != DBNull.Value))
            {
                ConfusionMatrix cm = (ConfusionMatrix)dr["Confusion Matrix"];

                htmlConfusionMatrix += "<tr>";
                htmlConfusionMatrix += String.Format("<td>{0}</td>", dr["Processed Total"]);
                htmlConfusionMatrix += String.Format("<td>{0}</td>", cm.ToHtml());
                htmlConfusionMatrix += "</tr>";
                htmlConfusionMatrix += Environment.NewLine;
                htmlConfusionMatrix += Environment.NewLine;
            }
            htmlConfusionMatrix += "</table>";
            htmlConfusionMatrix += "</html>";
            File.WriteAllText(Path.Combine(this.ResultsDir, "Confusion Matrix " + suffix + ".html"), htmlConfusionMatrix);
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

        [Fact]
        public void Drifting()
        {
            #region Result Storage
            DataTable results = new DataTable();
            results.Columns.Add("Id", typeof(int));
            results.Columns.Add("InversedLabel", typeof(string));
            results.Columns.Add("Pass", typeof(int));
            results.Columns.Add("Instance Id", typeof(int));
            results.Columns.Add("Processed Total", typeof(int));
            results.Columns.Add("States Total", typeof(int));
            results.Columns.Add("Testing Accuracy", typeof(double));
            results.Columns.Add("Training Time", typeof(int));
            results.Columns.Add("Testing Time", typeof(int));
            results.Columns.Add("Confusion Matrix", typeof(ConfusionMatrix));
            #endregion

            #region Datasets
            int passes = 4; // 1 pass normal, 1 pass inversed, repeat
            string trainingNormalLabelName = "class";
            string testingNormalLabelName = "class";
            int testingInterval = 50;// defaultTestingInterval;

            //Training (normal)
            string trainingNormalCsvPath = DataSets("random", defaultDatasetTrainingPercentage);
            CsvStreamReader trainingNormalData = new CsvStreamReader(trainingNormalCsvPath);

            //Training (inversed)
            string trainingInversedCsvPath = DataSets("randomInversedLabel", defaultDatasetTrainingPercentage);
            CsvStreamReader trainingInversedData = new CsvStreamReader(trainingInversedCsvPath);

            //Testing (normal)
            string testingNormalCsvPath = DataSets("random", defaultDatasetTestingPercentage);
            CsvStreamReader testingNormalData = new CsvStreamReader(testingNormalCsvPath);

            //Testing (inversed)
            string testingInversedCsvPath = DataSets("randomInversedLabel", defaultDatasetTestingPercentage);
            CsvStreamReader testingInversedData = new CsvStreamReader(testingInversedCsvPath);
            #endregion

            #region Policy configuration
            Policy thePolicy = new Policy()
            {
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
                //Choose normal or inversed datasets
                CsvStreamReader trainingData = trainingNormalData;
                CsvStreamReader testingData = testingNormalData;
                bool inversedLabel = false;
                if (pass % 2 == 0)
                {
                    trainingData = trainingInversedData;
                    testingData = testingInversedData;
                    inversedLabel = true;
                }

                //Cycle through each instance in the training file
                while (!trainingData.EndOfStream)
                {
                    //Submit to Trainer
                    Stopwatch stopwatchTraining = new Stopwatch();
                    stopwatchTraining.Start();
                    DataVectorTraining instanceTraining = trainingData.ReadLine(trainingNormalLabelName);
                    TrainingStats trainingStats = thePolicy.Learn(instanceTraining);
                    processedTotal++;
                    stopwatchTraining.Stop();

                    //Record training stats
                    DataRow result = results.NewRow();
                    results.Rows.Add(result);
                    result["Id"] = results.Rows.Count;
                    result["InversedLabel"] = inversedLabel.ToString();
                    result["Pass"] = pass;
                    result["Instance Id"] = trainingData.LineNumber;
                    result["Processed Total"] = processedTotal;
                    result["States Total"] = trainingStats.StatesTotal;
                    result["Training Time"] = stopwatchTraining.ElapsedMilliseconds;

                    //Perform testing
                    if (processedTotal % testingInterval == 0)
                    {
                        //Submit to Tester
                        ConfusionMatrix confusionMatrix = new ConfusionMatrix();
                        Stopwatch stopwatchTesting = new Stopwatch();
                        stopwatchTesting.Start();
                        while (!testingData.EndOfStream)
                        {
                            DataVectorTraining instanceTesting = testingData.ReadLine(testingNormalLabelName);
                            //Get values to compare
                            object prediction = thePolicy.DecisionTree.Classify(instanceTesting);
                            object correctAnswer = instanceTesting.Label.Value;

                            //Build Confusion matrix
                            confusionMatrix.AddEntry(correctAnswer, prediction);
                        }
                        stopwatchTesting.Stop();
                        testingData.SeekOriginBegin();

                        //Record testing stats
                        result["Testing Accuracy"] = confusionMatrix.Accuracy;
                        result["Testing Time"] = stopwatchTesting.ElapsedMilliseconds;
                        result["Confusion Matrix"] = confusionMatrix;
                    }
                }

                //Reset training dataset
                trainingData.SeekOriginBegin();
            }
            stopwatchProcessing.Stop();
            #endregion

            #region Save Results
            // Save to CSV file
            results.ToCsv(Path.Combine(ResultsDir, "Data.csv"));

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
            chartStates.ToHtml(Path.Combine(ResultsDir, "States"));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States"));

            chartAccuracy.ToHtml(Path.Combine(ResultsDir, "Accuracy"));
            chartAccuracy.ToPdf(Path.Combine(ResultsDir, "Accuracy"));
            #endregion

            #region  Save confusion matrix
            string htmlConfusionMatrix = "<html>";
            htmlConfusionMatrix += ConfusionMatrix.HtmlStyling;
            htmlConfusionMatrix += "<table>";
            htmlConfusionMatrix += "<tr>";
            htmlConfusionMatrix += "<th>Processed Points</th>";
            htmlConfusionMatrix += "<th>Confusion Matrix</th>";
            htmlConfusionMatrix += "</tr>";
            foreach (DataRow dr in results.Rows.Cast<DataRow>().Where(p => p["Confusion Matrix"] != DBNull.Value))
            {
                ConfusionMatrix cm = (ConfusionMatrix)dr["Confusion Matrix"];

                htmlConfusionMatrix += "<tr>";
                htmlConfusionMatrix += String.Format("<td>{0}</td>", dr["Processed Total"]);
                htmlConfusionMatrix += String.Format("<td>{0}</td>", cm.ToHtml());
                htmlConfusionMatrix += "</tr>";
                htmlConfusionMatrix += Environment.NewLine;
                htmlConfusionMatrix += Environment.NewLine;
            }
            htmlConfusionMatrix += "</table>";
            htmlConfusionMatrix += "</html>";
            File.WriteAllText(Path.Combine(this.ResultsDir, "Confusion Matrix.html"), htmlConfusionMatrix);
            #endregion

            #region Save metadata file
            StreamWriter swMeta = new StreamWriter(Path.Combine(ResultsDir, "details.txt"));
            swMeta.WriteLine("# Training Configuration");
            swMeta.WriteLine("Training File: " + Path.GetFileName(trainingNormalCsvPath));
            swMeta.WriteLine("Training File Path: " + trainingNormalCsvPath);
            swMeta.WriteLine("Testing File: " + Path.GetFileName(testingNormalCsvPath));
            swMeta.WriteLine("Testing File Path: " + testingNormalCsvPath);
            swMeta.WriteLine("Total Processing Time (ms): " + stopwatchProcessing.ElapsedMilliseconds);

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
            File.WriteAllText(Path.Combine(ResultsDir, "tree-full.html"), thePolicy.DecisionTree.ToHtmlTree());
            File.WriteAllText(Path.Combine(ResultsDir, "tree-simple.html"), thePolicy.ToDecisionTree(ts_simple).ToHtmlTree());
            #endregion
            #endregion

            //Close datasets
            trainingNormalData.Close();
            testingNormalData.Close();
        }

        [Fact]
        public void FeatureSpaceSize()
        {
            #region Result Storage
            DataTable results = new DataTable();
            results.Columns.Add("Feature Space Size", typeof(double));
            results.Columns.Add("States Total", typeof(double));
            results.Columns.Add("Training Time (ms)", typeof(double));
            results.Columns.Add("Training Time (relative)", typeof(double));
            #endregion

            #region Datasets
            //Training parameters
            List<string> datasets = new List<string>() {
                "random.csv",
                "random1_featurespace_1.09E20.csv",
                "random2_featurespace_1.05E30.csv",
                "random3_featurespace_1.06E40.csv",
                "random4_featurespace_1.06E50.csv",
                "random5_featurespace_1.08E60.csv",
                "random6_featurespace_1.03E70.csv",
                "random7_featurespace_1.03E80.csv",
                "random8_featurespace_1.04E90.csv",
                "random9_featurespace_1.1E100.csv",
            };
            string trainingLabelName = "class";
            int passes = 1;
            #endregion

            #region Processing
            Stopwatch stopwatchProcessing = new Stopwatch(); stopwatchProcessing.Start();
            Policy thePolicy = null;
            foreach (string datasetName in datasets)
            {
                //Policy Configuration
                thePolicy = new Policy()
                {
                    ExplorationRate = defaultExplorationRate,
                    DiscountFactor = defaultDiscountFactor,
                    ParallelQueryUpdatesEnabled = defaultParallelQueryUpdatesEnabled,
                    ParallelReportUpdatesEnabled = defaultParallelReportUpdatesEnabled,
                    QueriesLimit = defaultQueriesLimit,
                };

                //Perform Training
                Stopwatch stopwatchTraining = new Stopwatch();
                stopwatchTraining.Start();
                CsvStreamReader trainingData = new CsvStreamReader(DataSets(datasetName));
                for (int pass = 1; pass <= passes; pass++)
                {
                    //Cycle through each instance in the training file
                    while (!trainingData.EndOfStream)
                    {
                        //Submit to Trainer
                        DataVectorTraining instanceTraining = trainingData.ReadLine(trainingLabelName);
                        thePolicy.Learn(instanceTraining);
                    }

                    //Reset training dataset
                    trainingData.SeekOriginBegin();
                }
                trainingData.Close();
                stopwatchTraining.Stop();

                //Record training stats
                DataRow result = results.NewRow();
                results.Rows.Add(result);
                result["Feature Space Size"] = trainingData.FeatureSpaceSize;
                result["States Total"] = thePolicy.StateSpaceCount;
                result["Training Time (ms)"] = stopwatchTraining.ElapsedMilliseconds;
            }
            stopwatchProcessing.Stop();
            #endregion

            #region Save Results
            //Compute relative training times
            double minTime = double.PositiveInfinity;
            foreach (DataRow r in results.Rows)
                if ((double)r["Training Time (ms)"] < minTime)
                    minTime = (double)r["Training Time (ms)"];
            foreach (DataRow r in results.Rows)
                r["Training Time (relative)"] = (double)r["Training Time (ms)"] / minTime;

            // Save to CSV file
            results.ToCsv(Path.Combine(ResultsDir, "Data.csv"));

            #region Save chart to html and pdf
            //Create charts
            Chart chartStates = new Chart("States vs Feature Space", "Feature Space", "States") { xLogarithmic = true };
            Chart chartTrainingTime = new Chart("Training Time (ms) vs Feature Space", "Feature Space", "Training Time (ms)") {xLogarithmic=true };
            Chart chartTrainingTimeRelative = new Chart("Training Time (relative) vs Feature Space", "Feature Space", "Training Time (relative)") {xLogarithmic=true };

            // Add data to chart
            foreach (DataRow r in results.Rows)
            {
                chartStates.Add("States", (double) r["Feature Space Size"], (double)r["States Total"]);
                chartTrainingTime.Add("Training Time (ms)", (double) r["Feature Space Size"], (double)r["Training Time (ms)"]);
                chartTrainingTimeRelative.Add("Training Time (relative)", (double) r["Feature Space Size"], (double)r["Training Time (relative)"]);
            }

            // Save charts
            chartStates.ToHtml(Path.Combine(ResultsDir, "States"));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States"));

            chartTrainingTime.ToHtml(Path.Combine(ResultsDir, "Training Time (ms)"));
            chartTrainingTime.ToPdf(Path.Combine(ResultsDir, "Training Time (ms)"));

            chartTrainingTimeRelative.ToHtml(Path.Combine(ResultsDir, "Training Time (relative)"));
            chartTrainingTimeRelative.ToPdf(Path.Combine(ResultsDir, "Training Time (relative)"));
            #endregion

            #region Save metadata file
            StreamWriter swMeta = new StreamWriter(Path.Combine(ResultsDir, "details.txt"));
            swMeta.WriteLine("# Training Configuration");
            swMeta.WriteLine("Training Files:");
            foreach(string dataset in datasets)
                swMeta.WriteLine("\t"+dataset);
            swMeta.WriteLine("Total Processing Time (ms): " + stopwatchProcessing.ElapsedMilliseconds);
            swMeta.WriteLine("Passes: " + passes);

            swMeta.WriteLine();

            swMeta.WriteLine("# Policy Configuration");
            swMeta.WriteLine("Exploration Rate: " + thePolicy.ExplorationRate.ToString("N2"));
            swMeta.WriteLine("Discount Factor: " + thePolicy.DiscountFactor.ToString("N2"));
            swMeta.WriteLine("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            swMeta.WriteLine("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);
            swMeta.Close();
            #endregion
            #endregion
        }
    }
}

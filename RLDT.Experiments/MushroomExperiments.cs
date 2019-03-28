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
        readonly int defaultDatasetTrainingPercentage = 80;
        readonly int defaultDatasetTestingPercentage = 80;


        [Fact]
        private void Confusion()
        {
            ConfusionMatrix cm = new ConfusionMatrix();
            cm.AddEntry(1, 1);
            cm.AddEntry(1, 2);
            cm.AddEntry(2, 2);
            cm.AddEntry(2, 2);
            cm.AddEntry(2, 2);
            cm.AddEntry(2, 2);
            cm.AddEntry(2, 2);
            cm.AddEntry(2, 2);

            File.WriteAllText(Path.Combine(this.ResultsDir, "Confusion Matrix.html"), cm.ToHtml());
        }

        [Theory]
        [InlineData("original", 80)]
        [InlineData("random", 80)]
        [InlineData("randomInversedLabel", 80)]
        private string DataSets(string name, int percentage)
        {
            //Remove folder information and extension
            name = Path.GetFileNameWithoutExtension(name);

            //Check that source file is valid
            string path = Path.Combine(ResultsDir, name);
            Assert.True(File.Exists(path+".csv"));

            //Check the percentages
            Assert.InRange(percentage, 1, 99);
            int percentTraining = percentage;
            int percentTesting = Math.Abs(100 - percentage);
            if (percentTesting > percentTraining)
            {
                percentTraining = percentTesting;
                percentTesting = percentage;
            }

            //Sample the source CV file and create training and testing versions.
            int lineCounter = 0;
            if(!File.Exists(path+percentage+".csv") || !File.Exists(path+percentTesting+".csv"))
            {
                StreamReader sr = new StreamReader(path+".csv");
                StreamWriter swTraining = new StreamWriter(path+percentTraining+".csv");
                StreamWriter swTesting = new StreamWriter(path+percentTesting+".csv");
                Random rand = new Random();

                //Add headers
                string headers = sr.ReadLine();
                swTraining.WriteLine(headers);
                swTesting.WriteLine(headers);

                //Copy data over
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine(); lineCounter++;
                    if (rand.Next(0,101) <= percentage)
                        swTraining.WriteLine(line);
                    else
                        swTesting.WriteLine(line);
                }

                sr.Close();
                swTraining.Close();
                swTesting.Close();
            }
            Assert.True(File.Exists(path+percentTraining+".csv"));
            Assert.True(File.Exists(path+percentTesting+".csv"));

            //Provide the training or testing CSV path
            string pathRequest = Path.Combine(ResultsDir, name+percentage+".csv");

            return pathRequest;
        }

        [Theory]
        //[InlineData("original")]
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
                        ConfusionMatrix confusionMatrix = new ConfusionMatrix();
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

                            //Build Confusion matrix
                            confusionMatrix.AddEntry(correctAnswer, prediction);
                        }
                        stopwatchTesting.Stop();
                        testingData.SeekOriginBegin();

                        //Record testing stats
                        result["Testing Accuracy"] = correctCount / (double)testedCount;
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

            //Save confusion matrices
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

                    if (processedTotal % testingInterval == 0)
                    {
                        //Submit to Tester
                        int testedCount = 0;
                        int correctCount = 0;
                        Stopwatch stopwatchTesting = new Stopwatch();
                        stopwatchTesting.Start();
                        while (!testingData.EndOfStream)
                        {
                            DataVectorTraining instanceTesting = testingData.ReadLine(testingNormalLabelName);
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

    }
}

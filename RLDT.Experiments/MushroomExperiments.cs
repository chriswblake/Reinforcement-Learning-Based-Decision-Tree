using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Xunit;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace RLDT.Experiments
{
    public class MushroomsExperiments : Experiment
    {
        //Policy defaults
        double defaultExplorationRate = 0.00;
        double defaultDiscountFactor = 0.85;
        bool defaultParallelQueryUpdatesEnabled = true;
        bool defaultParallelReportUpdatesEnabled = false;
        int defaultQueriesLimt = 1000;
        int defaultTestingInterval = 200;

        [Theory]
        [InlineData("original.csv")]
        [InlineData("random.csv")]
        private string DataSets(string name)
        {
            string path = Path.Combine(ResultsDir, name);
            Assert.True(File.Exists(path));
            return path;
        }
        private double TestPolicy(Policy thePolicy, CsvStreamReader testingData, string labelFeatureName)
        {
            int testedCount = 0;
            int correctCount = 0;
            DataVectorTraining instance;
            while ((instance = testingData.ReadLine(labelFeatureName)) != null)
            {
                //Get values to compare
                object prediction = thePolicy.DecisionTree.Classify(instance);
                object correctAnswer = instance.Label.Value;

                //Check answer
                testedCount++;
                if (prediction.Equals(correctAnswer))
                    correctCount++;
            }

            return correctCount / (double)testedCount;
        }

        [Fact]
        public void RandomData()
        {
            #region Experiment Parameters
            //Training parameters
            string trainingCsvPath = DataSets("random.csv");
            CsvStreamReader trainingData = new CsvStreamReader(trainingCsvPath);
            string trainingLabelName = "class";
            int passes = 1;

            //Testing Parameters
            string testingCsvPath = DataSets("random.csv");
            CsvStreamReader testingData = new CsvStreamReader(testingCsvPath);
            string testingLabelName = "class";
            int testingInterval = 20;

            //Policy parameters
            Policy thePolicy = new Policy();
            thePolicy.ExplorationRate = defaultExplorationRate;
            thePolicy.DiscountFactor = defaultDiscountFactor;
            thePolicy.ParallelQueryUpdatesEnabled = defaultParallelQueryUpdatesEnabled;
            thePolicy.ParallelReportUpdatesEnabled = defaultParallelReportUpdatesEnabled;
            thePolicy.QueriesLimit = defaultQueriesLimt;
            #endregion

            #region Training/Testing
            List<ExperimentStats> trainingHistory = new List<ExperimentStats>();
            Stopwatch stopwatchProcessing = new Stopwatch(); stopwatchProcessing.Start();
            for (int passId = 1; passId <= passes; passId++)
            {
                //Cycle through each instance in the training file
                while (!trainingData.EndOfStream)
                {
                    //Submit to Trainer
                    Stopwatch stopwatchTraining = new Stopwatch();
                    stopwatchTraining.Start();
                    DataVectorTraining instanceTraining = trainingData.ReadLine(trainingLabelName);
                    ExperimentStats experimentStats = new ExperimentStats(thePolicy.Learn(instanceTraining)); //processedPoints++;
                    stopwatchTraining.Stop();

                    //Record training stats
                    experimentStats.Id = trainingHistory.Count;
                    experimentStats.Pass = passId;
                    experimentStats.InstanceID = trainingData.LineNumber;
                    experimentStats.TrainingTime = stopwatchTraining.ElapsedMilliseconds;

                    if (experimentStats.Id % testingInterval == 0)
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
                        experimentStats.TestingTime = stopwatchTesting.ElapsedMilliseconds;
                        experimentStats.TestingAccuracy = correctCount / (double)testedCount;
                    }

                    //Save stats
                    trainingHistory.Add(experimentStats);
                }

                //Reset training dataset
                trainingData.SeekOriginBegin();
            }
            stopwatchProcessing.Stop();
            #endregion

            #region Save experiment stats to CSV file
            //Convert data to csv lines
            List<string> statLines = new List<string>();
            //Get fields
            statLines.Add(String.Join(",", trainingHistory[0].GetType().GetProperties().Select(p => p.Name)));
            //Get data
            foreach (ExperimentStats tp in trainingHistory)
                statLines.Add(String.Join(",", tp.GetType().GetProperties().Select(p => p.GetValue(tp, null)).ToList()));
            //Save to file
            File.WriteAllLines(Path.Combine(ResultsDir, "ExperimentStats.csv"), statLines);
            #endregion

            #region Save chart to html and pdf
            Chart chartStates = new Chart("States vs Processed", "Processed", "States");
            Chart chartCounts = new Chart("Counts vs Processed", "Processed", "Count");
            Chart chartAccuracy = new Chart("Accuracy vs Processed", "Processed", "% Correct");

            //Add data to chart
            int sampleRate = 20;
            foreach (ExperimentStats p in trainingHistory)
            {
                if (p.Id % sampleRate == 0)
                { 
                    chartStates.Add("States", p.Id, p.StatesTotal);
                    chartCounts.Add("States Created", p.Id, p.StatesCreated);
                    chartCounts.Add("Queries", p.Id, p.QueriesTotal);
                    chartAccuracy.Add("Accuracy", p.Id, p.TestingAccuracy);
                }
            }

            //Save charts
            chartStates.ToHtml(Path.Combine(ResultsDir, "States"));
            chartStates.ToPdf(Path.Combine(ResultsDir, "States"));

            chartCounts.ToHtml(Path.Combine(ResultsDir, "Counts"));
            chartCounts.ToPdf(Path.Combine(ResultsDir, "Counts"));

            chartAccuracy.ToHtml(Path.Combine(ResultsDir, "Accuracy"));
            chartAccuracy.ToPdf(Path.Combine(ResultsDir, "Accuracy"));
            #endregion

            #region Save metadata file
            List<string> parameters = new List<string>();
            parameters.Add("# Training Configuration");
            parameters.Add("Training File: " + Path.GetFileName(trainingCsvPath));
            parameters.Add("Training File Path: " + trainingCsvPath);
            parameters.Add("Testing File: " + Path.GetFileName(testingCsvPath));
            parameters.Add("Testing File Path: " + testingCsvPath);
            parameters.Add("Total Processing Time (ms): " + stopwatchProcessing.ElapsedMilliseconds);

            parameters.Add("");
            parameters.Add("# Policy Configuration");
            parameters.Add("Exploration Rate: " + thePolicy.ExplorationRate.ToString("N2"));
            parameters.Add("Discount Factor: " + thePolicy.DiscountFactor);
            parameters.Add("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            parameters.Add("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);

            parameters.Add("");
            parameters.Add("# Training Results");
            parameters.Add("Passes: " + passes);
            //parameters.Add("Training Time (ms): " + stopwatchTraining.ElapsedMilliseconds);

            parameters.Add("");
            parameters.Add("# Testing Results");
            //parameters.Add("Instances Checked: " + testedCount);
            //parameters.Add("Correct Count: " + correctCount);
            //parameters.Add("Percent Correct: " + (100.0 * correctCount / testedCount).ToString("N2"));
            //parameters.Add("Testing Time (ms): " + stopwatchTesting.ElapsedMilliseconds);

            File.WriteAllLines(Path.Combine(ResultsDir, "details.txt"), parameters);
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

            //Close the data stream
            trainingData.Close();
            testingData.Close();
        }
    }
}

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
        int defaultTestingInterval = 500;

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
            int testingInterval = defaultTestingInterval;

            //Policy parameters
            Policy thePolicy = new Policy();
            thePolicy.ExplorationRate = defaultExplorationRate;
            thePolicy.DiscountFactor = defaultDiscountFactor;
            thePolicy.ParallelQueryUpdatesEnabled = defaultParallelQueryUpdatesEnabled;
            thePolicy.ParallelReportUpdatesEnabled = defaultParallelReportUpdatesEnabled;
            thePolicy.QueriesLimit = defaultQueriesLimt;
            #endregion

            #region Training
            Stopwatch stopwatchTraining = new Stopwatch();
            stopwatchTraining.Start();
            List<ExperimentStats> trainingHistory = new List<ExperimentStats>();
            int processedPoints = 0;
            for (int passId = 1; passId <= passes; passId++)
            {
                //Peform training
                DataVectorTraining instanceTraining;
                while ((instanceTraining = trainingData.ReadLine(trainingLabelName)) != null)
                {
                    ExperimentStats experimentStats = new ExperimentStats(thePolicy.Learn(instanceTraining));
                    processedPoints++;
                    experimentStats.Id = processedPoints;
                    experimentStats.Pass = passId;
                    experimentStats.InstanceID = trainingData.LineNumber;
                    trainingHistory.Add(experimentStats);
                }

                //Reset the dataset
                trainingData.SeekOriginBegin();
            }
            stopwatchTraining.Stop();
            #endregion

            #region Testing
            Stopwatch stopwatchTesting = new Stopwatch();
            stopwatchTesting.Start();
            int testedCount = 0;
            int correctCount = 0;
            DataVectorTraining instance;
            while ((instance = trainingData.ReadLine(trainingLabelName)) != null)
            {
                //Get values to compare
                object prediction = thePolicy.DecisionTree.Classify(instance);
                object correctAnswer = instance.Label.Value;

                //Check answer
                testedCount++;
                if (prediction.Equals(correctAnswer))
                    correctCount++;
            }
            stopwatchTesting.Stop();
            #endregion

            #region Save experiment stats
            //Convert data to csv lines
            List<string> statLines = new List<string>();
            statLines.Add("Id,Pass,InstanceId,StatesTotal,StatesCreated,QueriesTotal,CorrectClassifications");
            foreach (ExperimentStats p in trainingHistory)
            {
                string line = p.Id + "," + p.Pass + "," + p.InstanceID + "," + p.StatesTotal + "," + p.StatesCreated + "," + p.QueriesTotal + "," + p.CorrectClassifications;
                statLines.Add(line);
            }
            File.WriteAllLines(Path.Combine(ResultsDir, "ExperimentStats.csv"), statLines);
            #endregion

            #region Save chart to html and pdf
            Chart theChart = new Chart("Count vs Processed", "Processed", "Count2");

            //Add data to chart
            int sampleRate = 20;
            foreach (ExperimentStats p in trainingHistory)
            {
                if (p.Id % sampleRate == 0)
                { 
                    theChart.Add("States", p.Id, p.StatesTotal);
                    theChart.Add("States Created", p.Id, p.StatesCreated);
                    theChart.Add("Queries", p.Id, p.QueriesTotal);
                    //htmlChart.Add("Correct", p.Id, p.CorrectClassifications);
                }
            }

            theChart.ToHtml(Path.Combine(ResultsDir, "chart"));
            theChart.ToPdf(Path.Combine(ResultsDir, "chart"));
            #endregion

            #region Save metadata file
            List<string> parameters = new List<string>();
            parameters.Add("# Training Configuration");
            parameters.Add("Training File: " + Path.GetFileName(trainingCsvPath));
            parameters.Add("Training File Path: " + trainingCsvPath);
            //parameters.Add("Testing File: " + Path.GetFileName(testingFileAddress));
            //parameters.Add("Testing File Path: " + testingFileAddress);

            parameters.Add("");
            parameters.Add("# Policy Configuration");
            parameters.Add("Exploration Rate: " + thePolicy.ExplorationRate.ToString("N2"));
            parameters.Add("Discount Factor: " + thePolicy.DiscountFactor);
            parameters.Add("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            parameters.Add("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);

            parameters.Add("");
            parameters.Add("# Training Results");
            parameters.Add("Passes: " + passes);
            parameters.Add("Training Time (ms): " + stopwatchTraining.ElapsedMilliseconds);

            parameters.Add("");
            parameters.Add("# Testing Results");
            parameters.Add("Instances Checked: " + testedCount);
            parameters.Add("Correct Count: " + correctCount);
            parameters.Add("Percent Correct: " + (100.0 * correctCount / testedCount).ToString("N2"));
            parameters.Add("Testing Time (ms): " + stopwatchTesting.ElapsedMilliseconds);

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

        [Theory]
        [InlineData(800, 400)]
        public void ScatterLinePlot(int width, int height)
        {
            Chart theChart = new Chart("Count vs Processed", "Processed", "Count");
            //theChart.xMin = 0;
            //theChart.xMax = 100;
            //theChart.yMin = -100;
            //theChart.yMax = 1000;
            theChart.ToPdf(Path.Combine(ResultsDir, "ScatterLinePlot.pdf"));
        }
    }
}

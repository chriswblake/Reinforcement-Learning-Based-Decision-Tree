using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Xunit;
using RLDT;

namespace RLDT.Experiments
{
    public class MushroomsExperiments : Experiment
    {
        [Theory]
        [InlineData("original.csv")]
        [InlineData("random.csv")]
        private string DataSets(string name)
        {
            string path = Path.Combine(ResultsDir, name);
            Assert.True(File.Exists(path));
            return path;
        }

        [Fact]
        public void RandomData()
        {
            //Open training data stream
            string csvPath = DataSets("random.csv");
            CsvStreamReader trainingData = new CsvStreamReader(csvPath);
            string labelName = "class";

            //Policy parameters
            Policy thePolicy = new Policy();
            thePolicy.ExplorationRate = 0.00;
            thePolicy.DiscountFactor = 0.85;
            thePolicy.ParallelQueryUpdatesEnabled = true;
            thePolicy.ParallelReportUpdatesEnabled = false;
            thePolicy.QueriesLimit = 1000;
            int passes = 1;

            //Training
            for (int i = 1; i <= passes; i++)
            {
                //Peform training
                DataVectorTraining instanceTraining;
                while ((instanceTraining = trainingData.ReadLine(labelName)) != null)
                {
                    DecisionTree.TrainingStats trainingStats = thePolicy.Learn(instanceTraining);
                }

                //Reset the dataset
                trainingData.SeekOriginBegin();
            }

            //Testing
            int testedCount = 0;
            int correctCount = 0;
            DataVectorTraining instance;
            while ((instance = trainingData.ReadLine(labelName)) != null)
            {
                //Get values to compare
                object prediction = thePolicy.DecisionTree.Classify(instance);
                object correctAnswer = instance.Label.Value;

                //Check answer
                testedCount++;
                if (prediction.Equals(correctAnswer))
                    correctCount++;
            }

            //Close the data stream
            trainingData.Close();

            //Create metadata file
            List<string> parameters = new List<string>();
            parameters.Add("Training File: " + Path.GetFileName(csvPath));
            parameters.Add("Training File Path: " + csvPath);
            parameters.Add("Exploration Rate: " + thePolicy.ExplorationRate.ToString("N2"));
            parameters.Add("Discount Factor: " + thePolicy.DiscountFactor);
            parameters.Add("Parallel Query Updates: " + thePolicy.ParallelQueryUpdatesEnabled);
            parameters.Add("Parallel Report Updates: " + thePolicy.ParallelReportUpdatesEnabled);
            parameters.Add("Passes: " + passes);
            parameters.Add("");
            //parameters.Add("Testing File: " + Path.GetFileName(testingFileAddress));
            //parameters.Add("Testing File Path: " + testingFileAddress);
            parameters.Add("Correct Count: " + correctCount);
            parameters.Add("Instances: " + testedCount);
            parameters.Add("Percent Correct: " + (100.0 * correctCount / testedCount).ToString("N2"));
            File.WriteAllLines(Path.Combine(ResultsDir, "details.txt"), parameters);
        }
    }
}

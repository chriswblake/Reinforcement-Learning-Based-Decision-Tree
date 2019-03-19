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
            CsvStreamReader trainingData = new CsvStreamReader(DataSets("random.csv"));
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
            int correctCount = 0;
            DataVectorTraining instance;
            while ((instance = trainingData.ReadLine(labelName)) != null)
            {
                object prediction = thePolicy.DecisionTree.Classify(instance);
                object correctAnswer = instance.Label.Value;
                if (prediction.Equals(correctAnswer))
                    correctCount++;
            }

            //Close the data stream
            trainingData.Close();
        }
    }
}

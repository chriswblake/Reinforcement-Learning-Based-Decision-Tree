using System;
using Xunit;
using RLDT;
using System.Linq;

namespace RLDT.Tests
{
    public class DataVectorTrainingTests
    {
        [Fact]
        public void Constructor_With2Inputs_2()
        {
            string[] featureNames = new string[] {"name1", "name2", "class" };
            object[] featureValues = new object[] { 1.0, 2.0, 3.0 };
            double[] importance = new double[] { -1.0, 0.0, 0.0 };
            string labelFeatureName = "class";

            DataVectorTraining dvt = new DataVectorTraining(featureNames, featureValues, importance, labelFeatureName);

            Assert.Equal(2, dvt.Features.Count);

            Assert.Equal(1.0, dvt["name1"].Value);
            Assert.Equal(-1.0, dvt["name1"].Importance);

            Assert.Equal(2.0, dvt["name2"].Value);
            Assert.Equal(0.0, dvt["name2"].Importance);

            Assert.Equal("class", dvt.Label.Name);
            Assert.Equal(3.0, dvt.Label.Value);
        }
        [Fact]
        public void Constructor_DifferentLengthInputs_Exception()
        {
            string[] featureNames = new string[] { "name1", "name2", "class" };
            object[] featureValues = new object[] { 1.0, 2.0, 3.0 };
            double[] importance = new double[] { -1.0, 0.0 };
            string labelFeatureName = "class";

            Assert.ThrowsAny<FormatException>(delegate {
                DataVectorTraining dvt = new DataVectorTraining(featureNames, featureValues, importance, labelFeatureName);
            });
        }
        [Fact]
        public void Constructor_WrongClassName_Exception()
        {
            string[] featureNames = new string[] { "name1", "name2", "class" };
            object[] featureValues = new object[] { 1.0, 2.0, 3.0 };
            double[] importance = new double[] { -1.0, 0.0, 0.0 };
            string labelFeatureName = "class2";

            Assert.ThrowsAny<ArgumentException>(delegate {
                DataVectorTraining dvt = new DataVectorTraining(featureNames, featureValues, importance, labelFeatureName);
            });
        }
    }
}

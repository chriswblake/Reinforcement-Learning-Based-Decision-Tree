using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Xunit;
using RLDT;


namespace RLDT.Tests
{
    public class PolicyTests
    {
        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public void Learn_xorOperation_SeeAssert(int iterations)
        {
            ///Why does this sometimes fail? There are no bad examples.

            string[] headers = new string[]
            {
                "bool1",
                "bool2",
                "xor"
            };
            double[] importance = new double[] {
                0,
                0,
                0
            };
            Policy thePolicy = new Policy();

            #region Training
            Random rand = new Random();
            for(int i=0; i<iterations; i++)
            {
                bool bool1 = Convert.ToBoolean(rand.Next(0,2));
                bool bool2 = Convert.ToBoolean(rand.Next(0,2));
                object[] data = new object[] {bool1, bool2, (bool1 ^ bool2) };
                DataVectorTraining dvt = new DataVectorTraining(headers, data, importance, "xor");
                thePolicy.Learn(dvt);
            }
            #endregion

            string htmlTree = thePolicy.ToDecisionTree(new DecisionTree.TreeSettings() { ShowSubScores = true, ShowBlanks = true }).ToHtmlTree();
            System.IO.File.WriteAllText("xor" + iterations + ".html", htmlTree);

            #region Assert
            var dataVectorTT = new DataVector(
                new string[] {"bool1", "bool2" },
                new object[] {true, true});
            var dataVectorFF = new DataVector(
                new string[] { "bool1", "bool2" },
                new object[] { false, false });
            var dataVectorTF = new DataVector(
                new string[] { "bool1", "bool2" },
                new object[] { true, false });
            var dataVectorFT = new DataVector(
                new string[] { "bool1", "bool2" },
                new object[] { false, true });

            Assert.False(Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVectorTT)));
            Assert.False(Convert.ToBoolean(thePolicy.Classify_ByTree(dataVectorTT)));
            Assert.False(Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVectorFF)));
            Assert.False(Convert.ToBoolean(thePolicy.Classify_ByTree(dataVectorFF)));
            Assert.True(Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVectorTF)));
            Assert.True(Convert.ToBoolean(thePolicy.Classify_ByTree(dataVectorTF)));
            Assert.True(Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVectorFT)));
            Assert.True(Convert.ToBoolean(thePolicy.Classify_ByTree(dataVectorFT)));
            #endregion

        }

        //[Theory]
        //[InlineData(10)]
        //[InlineData(50)]
        //[InlineData(100)]
        //[InlineData(500)]
        //[InlineData(1000)]
        //public void RemoveFeatureValuePair(int iterations)
        //{
        //    ///Why does this sometimes fail? There are no bad examples.

        //    string[] headers = new string[]
        //    {
        //        "bool1",
        //        "bool2",
        //        "xor"
        //    };
        //    double[] importance = new double[] {
        //        0,
        //        0,
        //        0
        //    };
        //    Policy thePolicy = new Policy();

        //    //Training
        //    Random rand = new Random();
        //    object[] bool1Options = new object[] { true, false, "?", "??" };
        //    object[] bool2Options = new object[] { true, false };
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        object bool1 = bool1Options[rand.Next(0, bool1Options.Length)];
        //        object bool2 = bool2Options[rand.Next(0, bool2Options.Length)];
        //        object xor = "unclear";
        //        if (bool1.GetType() == typeof(bool))
        //            xor = (bool) bool1 ^ (bool) bool2;
        //        object[] data = new object[] { bool1.ToString(), bool2.ToString(), xor.ToString() };
        //        DataVectorTraining dvt = new DataVectorTraining(headers, data, importance, "xor");
        //        thePolicy.Learn(dvt);
        //    }

        //    thePolicy.RemoveFeatureValuePair(new FeatureValuePair("bool1", "??"));

        //    ///Not sure how to write the Assert for this. I just look at the outputed html trees.

        //    string htmlTree = thePolicy.ToDecisionTree(new DecisionTree.TreeSettings() { ShowSubScores = true, ShowBlanks = true }).ToHtmlTree();
        //    System.IO.File.WriteAllText("xor?" + iterations + ".html", htmlTree);

        //}
    }
}

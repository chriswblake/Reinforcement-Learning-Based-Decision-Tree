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

            //Training
            Random rand = new Random();
            for(int i=0; i<iterations; i++)
            {
                bool bool1 = Convert.ToBoolean(rand.Next(0,2));
                bool bool2 = Convert.ToBoolean(rand.Next(0,2));
                object[] data = new object[] {bool1, bool2, (bool1 ^ bool2) };
                DataVectorTraining dvt = new DataVectorTraining(headers, data, importance, "xor");
                thePolicy.Learn(dvt);
            }

            var dataVector1 = new DataVector(
                new string[] {"bool1", "bool2" },
                new object[] {true, true});
            var dataVector2 = new DataVector(
                new string[] { "bool1", "bool2" },
                new object[] { true, false });
            var dataVector3 = new DataVector(
                new string[] { "bool1", "bool2" },
                new object[] { false, false });
            var dataVector4 = new DataVector(
                new string[] { "bool1", "bool2" },
                new object[] { false, true });

            var result1 = Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVector1));
            var result2 = Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVector2));
            var result3 = Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVector3));
            var result4 = Convert.ToBoolean(thePolicy.Classify_ByPolicy(dataVector4));

            string htmlTree = thePolicy.ToDecisionTree(new DecisionTree.TreeSettings() { ShowSubScores = true, ShowBlanks=true }).ToHtmlTree();
            System.IO.File.WriteAllText("xor"+iterations+".html", htmlTree);

            Assert.False(result1);
            Assert.True(result2);
            Assert.False(result3);
            Assert.True(result4);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public void RemoveFeatureValuePair(int iterations)
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

            //Training
            Random rand = new Random();
            object[] bool1Options = new object[] { true, false, "?", "??" };
            object[] bool2Options = new object[] { true, false };
            for (int i = 0; i < 1000; i++)
            {
                object bool1 = bool1Options[rand.Next(0, bool1Options.Length)];
                object bool2 = bool2Options[rand.Next(0, bool2Options.Length)];
                object xor = "unclear";
                if (bool1.GetType() == typeof(bool))
                    xor = (bool) bool1 ^ (bool) bool2;
                object[] data = new object[] { bool1.ToString(), bool2.ToString(), xor.ToString() };
                DataVectorTraining dvt = new DataVectorTraining(headers, data, importance, "xor");
                thePolicy.Learn(dvt);
            }

            thePolicy.RemoveFeatureValuePair(new FeatureValuePair("bool1", "??"));

            ///Not sure how to write the Assert for this. I just look at the outputed html trees.

            string htmlTree = thePolicy.ToDecisionTree(new DecisionTree.TreeSettings() { ShowSubScores = true, ShowBlanks = true }).ToHtmlTree();
            System.IO.File.WriteAllText("xor?" + iterations + ".html", htmlTree);

        }
    }
}

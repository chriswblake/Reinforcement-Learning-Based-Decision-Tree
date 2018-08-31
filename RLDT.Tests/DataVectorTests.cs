using System;
using Xunit;
using RLDT;
using System.Linq;

namespace RLDT.Tests
{
    public class DataVectorTests
    {
        [Fact]
        public void Constructor_With2Inputs_2()
        {
            string[] featureNames = new string[] {"name1", "name2" };
            object[] featureValues = new object[] { 0.0, 5.0 };
            DataVector dv = new DataVector(featureNames, featureValues);

            Assert.Equal(2, dv.Features.Count);
            Assert.Contains("name1", dv.Features.Select(p => p.Name));
            Assert.Contains("name2", dv.Features.Select(p => p.Name));
        }

        [Fact]
        public void Indexer_validName_FeatureValuePair()
        {
            string[] featureNames = new string[] { "name1", "name2" };
            object[] featureValues = new object[] { 0.0, 5.0 };
            DataVector dv = new DataVector(featureNames, featureValues);

            Assert.NotNull(dv);
        }
        [Fact]
        public void Indexer_invalidName_null()
        {
            string[] featureNames = new string[] { "name1", "name2" };
            object[] featureValues = new object[] { 0.0, 5.0 };
            DataVector dv = new DataVector(featureNames, featureValues);

            var result = dv["name3"];

            Assert.Null(result);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RLDT.Tests
{
    public class StateTests
    {
        #region Constructors
        [Fact]
        public void Constructor_DataVector_OnlyQueries()
        {
            DataVectorTraining dvt = new DataVectorTraining(
                new string[] {"name1", "name2", "class" },
                new object[] {0.0, 0.5, "c" },
                new double[] {0.0, 0.0, 0.0},
                "class"
                );

            State s = new State(dvt);
        }
        #endregion

        #region Methods
        [Fact]
        public void AddFeature()
        {
            Assert.True(false);
        }
        [Fact]
        public void AdjustLabels()
        {
            Assert.True(false);
        }
        [Fact]
        public void AdjustQuery()
        {
            Assert.True(false);
        }
        [Fact]
        public void GetBestQuery()
        {
            Assert.True(false);
        }
        [Fact]
        public void GetRandomQuery()
        {
            Assert.True(false);
        }
        [Fact]
        public void AddMissingQueriesAndLabels()
        {
            Assert.True(false);
        }
        [Fact]
        public void GetHashCodeWith()
        {
            Assert.True(false);
        }
        [Fact]
        public void GetHashCodeWithout()
        {
            Assert.True(false);
        }
        #endregion
    }
}

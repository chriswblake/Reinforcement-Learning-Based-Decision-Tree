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

        /// <summary>
        /// Several or all of the below methods have been set to fail testing.
        /// This is on purpose as a reminder to write the tests at some point.
        /// </summary>
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

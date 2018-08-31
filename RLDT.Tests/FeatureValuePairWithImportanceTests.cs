using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RLDT.Tests
{
    public class FeatureValuePairWithImportanceTests
    {
        [Fact]
        public void Constructor()
        {
            var fvp1 = new FeatureValuePairWithImportance("name1", 0.0, -5.0);
            var fvp2 = new FeatureValuePairWithImportance("name1", 0.0, -1.0);
            var fvp3 = new FeatureValuePairWithImportance("name1", 0.0,  0.0);
            var fvp4 = new FeatureValuePairWithImportance("name1", 0.0,  1.0);
            var fvp5 = new FeatureValuePairWithImportance("name1", 0.0,  5.0);

            Assert.Equal(-1, fvp1.Importance);
            Assert.Equal(-1, fvp2.Importance);
            Assert.Equal( 0, fvp3.Importance);
            Assert.Equal( 1, fvp4.Importance);
            Assert.Equal( 1, fvp5.Importance);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RLDT.Tests
{
    public class FeatureValuePairTests
    {
        [Fact]
        public void Constructor()
        {
            var fvp = new FeatureValuePair("name1", 0.0);

            Assert.Equal("name1", fvp.Name);
            Assert.Equal(0.0, fvp.Value);
            Assert.Equal(fvp, new FeatureValuePair("name1", 0.0));
        }
    }
}

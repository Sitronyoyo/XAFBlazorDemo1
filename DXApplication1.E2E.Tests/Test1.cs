using System;
using Xunit;

namespace DXApplication1.E2E.Tests
{
    public class Test1
    {
        [Fact]
        public void DateTime_Default_Value_Test()
        {
            var dt = new DateTime();
            DateTime? ndt = null;
            int i;

            Assert.Equal(DateTime.MinValue, dt);
            Assert.Null(ndt);
            Assert.Equal(0, i = default);
        }
    }
}

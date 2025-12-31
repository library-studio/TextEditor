using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LibraryStudio.Forms
{
    public class TestUtility2
    {
        [Theory]
        [InlineData("01", "", "", 0, 0)]
        // 左侧比较用掉了，右侧就不能再用了
        [InlineData("02", "1", "1", 1, 0)]
        [InlineData("03", "1", "2", 0, 0)]
        [InlineData("04", "1_2", "12", 1, 1)]
        [InlineData("05", "12_34", "1234",2, 2)]
        [InlineData("06", "1_34", "1234", 1, 2)]
        [InlineData("07", "12_4", "124", 2, 1)]
        [InlineData("08", "12_34", "12", 2, 0)]
        // 左侧比较用掉了，右侧就不能再用了
        [InlineData("09", "1111", "11", 2, 0)]

        public void compareTwoContent(
            string index,
            string content1,
            string content2,
            int start_length,
            int end_length)
        {
            var result = Utility.CompareTwoContent(content1, content2);
            Assert.Equal(start_length, result.StartLength);
            Assert.Equal(end_length, result.EndLength);
        }
    }
}

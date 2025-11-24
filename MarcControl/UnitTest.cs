using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryStudio.Forms;
using Xunit;


namespace LibraryStudio.Forms
{


    public class UnitTest
    {
        [Theory]
        [InlineData("", "", "")]
        [InlineData("", "n", "n")]
        [InlineData("o", "", "")]
        [InlineData("o", "n", "n")]
        [InlineData("o", "n1", "n1")]

        [InlineData("oo", "", "")]
        [InlineData("oo", "n", "n")]
        [InlineData("oo", "n1", "n1")]


        [InlineData("|", "", " ")]
        [InlineData("||", "", "  ")]
        [InlineData("|o", "", " ")]
        [InlineData("|o", "n", "n")]
        [InlineData("|o", "n1", "n1")]
        [InlineData("|o", "n12", "n12")]
        [InlineData("||o", "", "  ")]
        [InlineData("||o", "1", "1 ")]
        [InlineData("||o", "12", "12")]
        [InlineData("||o", "123", "123")]

        [InlineData("||oo", "", "  ")]
        [InlineData("||oo", "1", "1 ")]
        [InlineData("||oo", "12", "12")]
        [InlineData("||oo", "123", "123")]

        [InlineData("|o|", "", "  ")]
        [InlineData("|o|", "n ", "n ")]
        [InlineData("|o|", "n1", "n1")]
        [InlineData("|o|", "n12", "n12")]

        [InlineData("||o|", "", "   ")]
        [InlineData("||o|", "n ", "n  ")]
        [InlineData("||o|", "n1 ", "n1 ")]
        [InlineData("||o|", "n12", "n12")]
        [InlineData("||o|", "n123", "n123")]
        [InlineData("||o|", "n1234", "n1234")]

        [InlineData("||o||", "", "    ")]
        [InlineData("||o||", "n ", "n   ")]
        [InlineData("||o||", "n1 ", "n1  ")]
        [InlineData("||o||", "n12", "n12 ")]
        [InlineData("||o||", "n123", "n123")]
        [InlineData("||o||", "n1234", "n1234")]

        //
        [InlineData("|oo|", "", "  ")]
        [InlineData("|oo|", "n ", "n ")]
        [InlineData("|oo|", "n1", "n1")]
        [InlineData("|oo|", "n12", "n12")]

        [InlineData("||oo|", "", "   ")]
        [InlineData("||oo|", "n ", "n  ")]
        [InlineData("||oo|", "n1 ", "n1 ")]
        [InlineData("||oo|", "n12", "n12")]
        [InlineData("||oo|", "n123", "n123")]
        [InlineData("||oo|", "n1234", "n1234")]

        [InlineData("||oo||", "", "    ")]
        [InlineData("||oo||", "n ", "n   ")]
        [InlineData("||oo||", "n1 ", "n1  ")]
        [InlineData("||oo||", "n12", "n12 ")]
        [InlineData("||oo||", "n123", "n123")]
        [InlineData("||oo||", "n1234", "n1234")]

        [InlineData("||oo||o", "", "    ")]
        [InlineData("||oo||o", "n ", "n   ")]
        [InlineData("||oo||o", "n1 ", "n1  ")]
        [InlineData("||oo||o", "n12", "n12 ")]
        [InlineData("||oo||o", "n123", "n123")]
        [InlineData("||oo||o", "n1234", "n1234")]
        [InlineData("||oo||o", "n12345", "n12345")]

        [InlineData("||oo||oo", "", "    ")]
        [InlineData("||oo||oo", "n ", "n   ")]
        [InlineData("||oo||oo", "n1 ", "n1  ")]
        [InlineData("||oo||oo", "n12", "n12 ")]
        [InlineData("||oo||oo", "n123", "n123")]
        [InlineData("||oo||oo", "n1234", "n1234")]
        [InlineData("||oo||oo", "n12345", "n12345")]
        [InlineData("||oo||oo", "n123456", "n123456")]

        [InlineData("o||oo||oo", "", "    ")]
        [InlineData("o||oo||oo", "n ", "n   ")]
        [InlineData("o||oo||oo", "n1 ", "n1  ")]
        [InlineData("o||oo||oo", "n12", "n12 ")]
        [InlineData("o||oo||oo", "n123", "n123")]
        [InlineData("o||oo||oo", "n1234", "n1234")]
        [InlineData("o||oo||oo", "n12345", "n12345")]
        [InlineData("o||oo||oo", "n123456", "n123456")]

        [InlineData("oo||oo||oo", "", "    ")]
        [InlineData("oo||oo||oo", "n ", "n   ")]
        [InlineData("oo||oo||oo", "n1 ", "n1  ")]
        [InlineData("oo||oo||oo", "n12", "n12 ")]
        [InlineData("oo||oo||oo", "n123", "n123")]
        [InlineData("oo||oo||oo", "n1234", "n1234")]
        [InlineData("oo||oo||oo", "n12345", "n12345")]
        [InlineData("oo||oo||oo", "n123456", "n123456")]

        public void test_softReplace_01(string old_mask_text,
            string new_text,
            string correct)
        {
            var result = MarcControl.SoftReplace(old_mask_text,
            new_text,
            '|');
            Assert.Equal(correct, result);
        }

        [Theory]
        [InlineData("", 0, new string[] { "" })]
        [InlineData("1", 0, new string[] { "1" })]
        [InlineData("12", 0, new string[] { "12" })]
        [InlineData("012345678901234567890123", 0, new string[] { "012345678901234567890123" })]
        [InlineData("012345678901234567890123a", 0, new string[] { "012345678901234567890123", "a" })]
        [InlineData("012345678901234567890123abcde12", 0, new string[] { "012345678901234567890123", "abcde12" })]
        [InlineData("012345678901234567890123\u001e", 0, new string[] { "012345678901234567890123", "", "" })]
        [InlineData("012345678901234567890123abcde12\u001e", 0, new string[] { "012345678901234567890123", "abcde12", "" })]

        [InlineData("12345ABCD\x001e", 24, new string[] { "12345ABCD", "" })]
        [InlineData("12345ABCD\x001e\x001e", 24, new string[] { "12345ABCD", "", "" })]
        public void test_splitFiels_01(string content, int start, string[] correct)
        {
            var results = MarcRecord.SplitFields(content, start);
            AssertAreEqual(correct, results.ToArray());
        }

        static void AssertAreEqual(string[] expected, string[] actual)
        {
            if (expected.Length != actual.Length)
                throw new Exception($"期望的字符串数量为 {expected.Length}, 但实际为 {actual.Length}");
            int i = 0;
            foreach (var item in expected)
            {
                if (item != actual[i])
                    throw new Exception($"集合中位置 {i} 期望的字符串为 {item}, 但实际为 {actual[i]}");
                i++;
            }
        }
    }
}

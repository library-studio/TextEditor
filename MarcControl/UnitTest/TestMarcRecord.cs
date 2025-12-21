using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static LibraryStudio.Forms.MarcRecord;    // BuildContent()
using static Vanara.PInvoke.Gdi32;


namespace LibraryStudio.Forms
{
    public class TestMarcControl
    {
        // 在类中增加字段与构造函数注入 ITestOutputHelper
        private readonly Xunit.Abstractions.ITestOutputHelper _output;
        public TestMarcControl(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }


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

        [Theory]
        // 在(空的)头标区开头插入
        [InlineData(
            1,
            @"",
            0,
            0,
            "",
            "",
            "",
            0,
            ""
            )]
        // 已有一个字符，在它之前插入
        [InlineData(
            2,
            @"1",
            0,
            0,
            "",
            "",
            "1",
            0,
            "1"
            )]
        // 已有一个字符，在它之后插入
        [InlineData(
            3,
            @"1",
            1,
            1,
            "1",
            "",
            "",
            0,
            "1"
            )]
        // 已有一个字符，替换它
        [InlineData(
            4,
            @"1",
            0,
            1,
            "",
            "1",
            "",
            0,
            "1"
            )]
        // 已有 23 个字符，在它后面插入
        [InlineData(
            5,
            @"01234567890123456789012",
            23,
            23,
            "01234567890123456789012",
            "",
            "",
            0,
            "01234567890123456789012"
            )]
        // 已有 23 个字符，替换最后一个
        [InlineData(
            6,
            @"01234567890123456789012",
            22,
            23,
            "0123456789012345678901",
            "2",
            "",
            0,
            "01234567890123456789012"
            )]
        // 尝试在头标区后插入字符
        [InlineData(
            7,
            @"012345678901234567890123",
            24,
            24,
            "",
            "",
            "",
            1,
            ""
            )]
        // 替换整个头标区
        [InlineData(
            8,
            @"012345678901234567890123",
            0,
            24,
            "",
            "012345678901234567890123",
            "",
            0,
            "012345678901234567890123"
            )]
        // 替换部分头标区
        [InlineData(
            9,
            @"012345678901234567890123",
            0,
            23,
            "",
            "01234567890123456789012",
            "3",
            0,
            "012345678901234567890123"
            )]
        // 替换部分头标区
        [InlineData(
            10,
            @"012345678901234567890123",
            1,
            24,
            "0",
            "12345678901234567890123",
            "",
            0,
            "012345678901234567890123"
            )]
        // 替换部分头标区
        [InlineData(
            11,
            @"012345678901234567890123",
            1,
            23,
            "0",
            "1234567890123456789012",
            "3",
            0,
            "012345678901234567890123"
            )]
        // 跨越头标区和 001 字段的替换
        [InlineData(
            12,
            @"012345678901234567890123
00112345",
            0,
            32,
            "",
            "01234567890123456789012300112345",
            "\u001e",
            0,
            @"012345678901234567890123
00112345"
            )]
        // 跨越头标区和 001 字段的替换
        [InlineData(
            13,
            @"012345678901234567890123
00112345",
            1,
            31,
            "0",
            "123456789012345678901230011234",
            "5\u001e",
            0,
            @"012345678901234567890123
00112345"
            )]
        [InlineData(
            14,
            @"012345678901234567890123
001AAA",
            24,
            27,
            "",
            "001",
            "AAA\u001e",
            1,
            "001AAA"
            )]
        [InlineData(
            15,
            @"012345678901234567890123
001AAA",
            24,
            30,
            "",
            "001AAA",
            "\u001e",
            1,
            "001AAA"
            )]
        [InlineData(
            16,
            @"012345678901234567890123
001AAA",
            24,
            31,
            "",
            "001AAA\u001e",
            "",
            1,
            "001AAA"
            )]
        [InlineData(
            17,
            @"012345678901234567890123
001AAA",
            30,
            31,
            "001AAA",
            "\u001e",
            "",
            1,
            "001AAA"
            )]
        [InlineData(
            18,
            @"012345678901234567890123
001AAA",
            30,
            30,
            "001AAA",
            "",
            "\u001e",
            1,
            "001AAA"
            )]
        // TODO: 跨越多个普通字段的替换
        public void test_findFields(
            int number,
            string fields,
            int start,
            int end,
            string left_text,
            string replaced,
            string right_text,
            int first_index,
            string found_fields)
        {
            var record = BuildRecord(fields);
            var result_fields = record.FindFields(
            // BuildFields(fields),
            start,
            out string current_left_text,
            end,
            out string current_right_text,
            out int first_paragraph_index,
            out string current_replaced);

            _output.WriteLine($"case {number}");

            Assert.Equal(left_text, current_left_text);
            Assert.Equal(right_text, current_right_text);
            Assert.Equal(first_index, first_paragraph_index);
            Assert.Equal(replaced, current_replaced);

            {
                int index = 0;
                if (found_fields.StartsWith("..."))
                {
                    found_fields = found_fields.Substring(3);
                    start = 24;
                }
                var correct = BuildFields(found_fields, index);
                result_fields.SequenceEqual(correct);
            }
        }

        [Theory]
        [InlineData(1,
            @"012345678901234567890123
001ABCD",
            0,
            0,
            new int[] { 0 })]
        [InlineData(2,
            @"012345678901234567890123
001ABCD",
            0,
            1,
            new int[] { 0, 24 })]
        [InlineData(3,
            @"012345678901234567890123
001ABCD",
            0,
            -1,
            new int[] { 0, 24, 32 })]
        [InlineData(4,
            @"012345678901234567890123
001ABCD",
            0,
            3,
            null,
            true)]
        [InlineData(5,
            @"012345678901234567890123
001ABCD",
            1,
            0,
            new int[] { 24 })]
        [InlineData(6,
            @"012345678901234567890123
001ABCD",
            1,
            1,
            new int[] { 24, 32 })]
        [InlineData(7,
            @"012345678901234567890123
001ABCD",
            1,
            -1,
            new int[] { 24, 32 })]
        [InlineData(8,
            @"012345678901234567890123
001ABCD",
            1,
            2,
            null,
            true)]
        [InlineData(9,
            @"012345678901234567890123
001ABCD",
            -1,
            2,
            null,
            true)]
        [InlineData(10,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            0,
            -1,
            new int[] { 0, 24, 32, 43})]
        [InlineData(11,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            3,
            0,
            new int[] { 43 })]
        [InlineData(12,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            2,
            0,
            new int[] { 32 })]
        [InlineData(13,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            1,
            0,
            new int[] { 24 })]
        [InlineData(14,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            0,
            0,
            new int[] { 0 })]
        [InlineData(15,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            2,
            1,
            new int[] { 32, 43 })]
        [InlineData(16,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            1,
            1,
            new int[] { 24, 32 })]
        [InlineData(17,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            0,
            1,
            new int[] { 0, 24 })]
        [InlineData(18,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            1,
            2,
            new int[] { 24, 32, 43 })]
        [InlineData(19,
            @"012345678901234567890123
001ABCD
200  $aAAA",
            0,
            2,
            new int[] { 0, 24, 32 })]
        public void getFieldOffsList(int number,
            string text,
            int index,
            int count,
            int[] correct_results,
            bool expect_exception = false)
        {
            var record = BuildRecord(text);
            try
            {
                var results = record.GetFieldOffsList(index, count);

                if (expect_exception)
                    Assert.Fail($"({number}) 未如预期抛出异常");

                Assert.Equal(correct_results, results);
            }
            catch (ArgumentException)
            {
                if (expect_exception == false)
                    throw;
            }
        }



        static List<IBox> BuildFields(string text, int start = 0)
        {
            text = BuildContent(text, false);
            // text.Replace("\r\n", new string(Metrics.FieldEndCharDefault, 1));

            var property = new Metrics();

            using (var font = new Font("宋体", 12))
            using (var fonts = new FontContext(font))
            using (var bitmap = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IContext context = new Context()
                {
                    GetFont = (p, o) =>
                    {
                        return fonts.Fonts;
                    }
                };

                var handle = g.GetHdc();
                var dc = new SafeHDC(handle);

                List<IBox> new_fields = new List<IBox>();
                var lines = MarcRecord.SplitFields(text, start);
                foreach (var line in lines)
                {
                    var new_field = new MarcField(null, property);
                    if (new_fields.Count == 0)
                        new_field.IsHeader = true;

                    // return:
                    //      0   未给出本次修改的像素宽度。需要调主另行计算
                    //      其它  本次修改后的像素宽度
                    var ret = new_field.ReplaceText(
                        context,
                        dc,
                        0,
                        -1,
                        line,
                        -1/*,
                        out string _,
                        out Rectangle update_rect1,
                        out Rectangle scroll_rect1,
                        out int scroll_distance1*/);
                    new_fields.Add(new_field);
                }
                return new_fields;
            }
        }

        static MarcRecord BuildRecord(string text)
        {
            text = BuildContent(text, false);

            var property = new Metrics();

            using (var font = new Font("宋体", 12))
            using (var fonts = new FontContext(font))
            using (var bitmap = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IContext context = new Context()
                {
                    GetFont = (p, o) =>
                    {
                        return fonts.Fonts;
                    }
                };

                var handle = g.GetHdc();
                var dc = new SafeHDC(handle);

                var record = new MarcRecord(null, property);
                record.ReplaceText(context,
                    dc,
                    0,
                    -1,
                    text,
                    2000);
                return record;
            }
        }

    }
}

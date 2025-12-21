using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static LibraryStudio.Forms.MarcRecord;

namespace LibraryStudio.Forms
{
    public class TestCompressMaskText
    {
        // InLineData 中表达的方法:
        // 字符 ABCDE 分别代表字段名和指示符，F 代表头标区
        // # 代表字段结束符，| 代表空格

        // 没有任何保护，普通字符被抹去
        [Theory]
        [InlineData("01",
            "012345678901234567890123",
            "")]
        // F 字符保护头标区
        [InlineData("02",
            "FFFFFFFFFFFFFFFFFFFFFFFF",
            "||||||||||||||||||||||||")]
        // 最后有字段结束符，说明这是全的字段内容，ABCtest# 会被全部抹去
        [InlineData("03",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
ABCtest#",
            "||||||||||||||||||||||||")]
        // 最后没有字段结束符，说明不确定这是全的字段内容，ABC 会被保护
        [InlineData("04",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
ABCtest",
            "|||||||||||||||||||||||||||")]
        // AB 被保护
        [InlineData("05",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
AB",
            "||||||||||||||||||||||||||")]
        // A 被保护
        [InlineData("06",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
A",
            "|||||||||||||||||||||||||")]
        // ABCDE 被保护
        [InlineData("07",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
ABCDEtest",
            "|||||||||||||||||||||||||||||")]
        // ABCDE 被保护
        [InlineData("08",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
ABCDEtest#",
            "||||||||||||||||||||||||")]
        // 一个完整的普通字段
        [InlineData("09",
            "ABCDEtest#",
            "")]
        // 一个完整的控制字段
        [InlineData("10",
            "ABCtest#",
            "")]
        // 缺乏结束符的普通字段
        [InlineData("11",
            "ABCDEtest",
            "|||||")]
        // 缺乏结束符的控制字段
        [InlineData("12",
            "ABCtest",
            "|||")]
        // 缺乏头部的普通字段
        [InlineData("13",
            "BCDEtest#",
            "||||")]
        [InlineData("14",
            "CDEtest#",
            "|||")]
        [InlineData("15",
            "DEtest#",
            "||")]
        [InlineData("16",
            "Etest#",
            "|")]
        [InlineData("17",
            "test#",
            "")]
        [InlineData("18",
            "#",
            "")]
        // 缺乏头部的控制字段
        [InlineData("19",
            "ABCtest#",
            "")]
        [InlineData("20",
            "BCtest#",
            "||")]
        [InlineData("21",
            "Ctest#",
            "|")]
        // 这时其实无法分辨内容到底是普通字段还是控制字段的
        [InlineData("22",
            "test#",
            "")]
        // 两个完整字段
        [InlineData("23",
            @"ABCDEtest#
ABCDE1234#",
            "")]
        [InlineData("24",
            @"ABCtest#
ABCDE1234#",
            "")]
        [InlineData("25",
            @"ABCDEtest#
ABC1234#",
            "")]
        // 头尾不完整
        [InlineData("26",
            @"ABCDEtest#
ABCDE1234",
            "|||||")]
        [InlineData("27",
            @"BCDEtest#
ABCDE1234",
            "|||||||||")]
        // 保护 BCDE
        [InlineData("28",
            @"BCDEtest#
ABCDE1234#",
            "||||")]
        // 保护 ABCDE
        [InlineData("29",
            @"test#
ABCDE1234",
            "|||||")]
        // 三个完整字段
        [InlineData("30",
            @"ABCDEtest#
ABCDE1234#
ABC5678#",
            "")]
        [InlineData("31",
            @"FFFFFFFFFFFFFFFFFFFFFFFF
ABCDEtest#
ABCDE1234#
ABC5678#",
            "||||||||||||||||||||||||")]
        public void compressMaskText(
            string index,
            string text,
            string expected_result)
        {
            Console.WriteLine(index);
            var result = MarcRecord.CompressMaskText(BuildMaskText(text));
            Assert.Equal(expected_result, DisplayText(result));
        }

        // 转换为被测试函数需要的形态
        static string BuildMaskText(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach(var ch in s.Replace("\r\n", ""))
            {
                if (ch >= 'A' && ch <= 'F')
                    b.Append((char)(((int)ch - (int)'A') + 1));
                else if (ch == '#')
                    b.Append(Metrics.FieldEndCharDefault);
                else
                    b.Append(ch);
            }
            return b.ToString();
        }

        // 转换为适合验证和显示的形态
        static string DisplayText(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (var ch in s)
            {
                if (ch >= 1 && ch <= 6)
                    b.Append((char)((int)'A' + (int)ch - 1));
                else if (ch == Metrics.FieldEndCharDefault)
                    b.Append("#");
                else if (ch == ' ')
                    b.Append('|');
                else
                    b.Append(ch);
            }
            return b.ToString();
        }
    }
}

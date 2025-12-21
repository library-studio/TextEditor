using System;
using System.Collections.Generic;
using System.Drawing;

using static LibraryStudio.Forms.MarcField;
using static Vanara.PInvoke.Gdi32;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryStudio.Forms
{
    [TestClass]
    public class TestMarcField
    {
        [TestMethod]
        public void parseFieldParts_01()
        {
            string text = "012345678901234567890123";
            bool isHeader = true;
            bool pad_right = true;
            string correct_name = "";
            string correct_indicator = "";
            string correct_content = "012345678901234567890123";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_02()
        {
            string text = "012345678901234567890123";
            bool isHeader = true;
            bool pad_right = false; // 不填充
            string correct_name = "";
            string correct_indicator = "";
            string correct_content = "012345678901234567890123";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_03()
        {
            string text = "01234567890123456789012";    // 少一位
            bool isHeader = true;
            bool pad_right = true;
            string correct_name = "";
            string correct_indicator = "";
            string correct_content = "01234567890123456789012 ";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_04()
        {
            string text = "01234567890123456789012";    // 少一位
            bool isHeader = true;
            bool pad_right = false;  // 不填充
            string correct_name = "";
            string correct_indicator = "";
            string correct_content = "01234567890123456789012";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_11()
        {
            string text = "012abABCD";
            bool isHeader = false;
            bool pad_right = false;  // 不填充
            string correct_name = "012";
            string correct_indicator = "ab";
            string correct_content = "ABCD";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_12()
        {
            string text = "001abABCD";  // controlfield
            bool isHeader = false;
            bool pad_right = false;
            string correct_name = "001";
            string correct_indicator = "";
            string correct_content = "abABCD";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_13()
        {
            string text = "200";
            bool isHeader = false;
            bool pad_right = false;
            string correct_name = "200";
            string correct_indicator = "";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_14()
        {
            string text = "20";
            bool isHeader = false;
            bool pad_right = false;
            string correct_name = "20";
            string correct_indicator = "";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_15()
        {
            string text = "2";
            bool isHeader = false;
            bool pad_right = false;
            string correct_name = "2";
            string correct_indicator = "";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_16()
        {
            string text = "";
            bool isHeader = false;
            bool pad_right = false;
            string correct_name = "";
            string correct_indicator = "";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        //
        //

        [TestMethod]
        public void parseFieldParts_17()
        {
            string text = "200";
            bool isHeader = false;
            bool pad_right = true;  // 填充
            string correct_name = "200";
            string correct_indicator = "  ";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_18()
        {
            string text = "20";
            bool isHeader = false;
            bool pad_right = true;  // 填充
            string correct_name = "20 ";
            string correct_indicator = "  ";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_19()
        {
            string text = "2";
            bool isHeader = false;
            bool pad_right = true;  // 填充
            string correct_name = "2  ";
            string correct_indicator = "  ";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void parseFieldParts_20()
        {
            string text = "";
            bool isHeader = false;
            bool pad_right = true;  // 填充
            string correct_name = "   ";
            string correct_indicator = "  ";
            string correct_content = "";

            MarcField.ParseFieldParts(text,
                isHeader,
                pad_right,
                out string name,
                out string indicator,
                out string content);

            Assert.AreEqual(correct_name, name);
            Assert.AreEqual(correct_indicator, indicator);
            Assert.AreEqual(correct_content, content);
        }

        [TestMethod]
        public void GetSubfieldBounds_01()
        {
            var text = "200abCD$eEFG$hHIJ";
            var is_header = false;
            var offs = 0;
            var corrent_bounds = new SubfieldBound {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_02()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 5,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_03()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 6;
            var corrent_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 6,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_04()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 7;
            var corrent_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 7,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_05()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 8;
            var corrent_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 8,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_06()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 9;
            var corrent_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 9,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_07()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 10;
            var corrent_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 10,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_08()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 14;
            var corrent_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 14,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。right_most == false
        [TestMethod]
        public void GetSubfieldBounds_09()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 15;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。right_most == true
        [TestMethod]
        public void GetSubfieldBounds_10()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 15;
            var corrent_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 15,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, true);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。并且没有字段内容
        [TestMethod]
        public void GetSubfieldBounds_11()
        {
            var text = "2001 ";
            var is_header = false;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。并且没有字段内容
        [TestMethod]
        public void GetSubfieldBounds_12()
        {
            var text = "2001 ";
            var is_header = false;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, true);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 字段名中出现子字段符号
        [TestMethod]
        public void GetSubfieldBounds_13()
        {
            var text = "20$1 ";
            var is_header = false;
            var offs = 2;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 指示符少一位，$ 字符进入了指示符范围
        [TestMethod]
        public void GetSubfieldBounds_14()
        {
            var text = "2001$a";
            var is_header = false;
            var offs = 4;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 字段内容前部出现一段不在任何子字段中的内容
        [TestMethod]
        public void GetSubfieldBounds_15()
        {
            var text = "2001 KK$aAAA";
            var is_header = false;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 字段内容前部出现一段不在任何子字段中的内容
        [TestMethod]
        public void GetSubfieldBounds_16()
        {
            var text = "2001 KK$aAAA";
            var is_header = false;
            var offs = 6;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 头标区
        [TestMethod]
        public void GetSubfieldBounds_20()
        {
            var text = "012345678901234567890123";
            var is_header = true;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 头标区。有一个子字段符号
        [TestMethod]
        public void GetSubfieldBounds_21()
        {
            var text = "012345678901$34567890123";
            var is_header = true;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 001 字段。有子字段符号
        [TestMethod]
        public void GetSubfieldBounds_22()
        {
            var text = "001$aAAA$bBBB";
            var is_header = true;
            var offs = 5;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        // 001 字段。有子字段符号
        [TestMethod]
        public void GetSubfieldBounds_23()
        {
            var text = "001$aAAA$bBBB";
            var is_header = true;
            var offs = 10;
            var corrent_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBounds(offs, false);
            Assert.AreEqual(corrent_bounds.ToString(), ret.ToString());
        }

        static MarcField BuildField(string text, bool isHeader = false)
        {
            text = text.Replace("$", new string(Metrics.SubfieldCharDefault, 1));
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

                {
                    var new_field = new MarcField(null, property);
                    new_field.IsHeader = isHeader;

                    // return:
                    //      0   未给出本次修改的像素宽度。需要调主另行计算
                    //      其它  本次修改后的像素宽度
                    var ret = new_field.ReplaceText(
                        context,
                        dc,
                        0,
                        -1,
                        text,
                        -1/*,
                        out string _,
                        out Rectangle update_rect1,
                        out Rectangle scroll_rect1,
                        out int scroll_distance1*/);
                    return new_field;
                }
            }
        }


    }
}

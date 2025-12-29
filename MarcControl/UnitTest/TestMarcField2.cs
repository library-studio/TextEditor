using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LibraryStudio.Forms.MarcField;
using static Vanara.PInvoke.Gdi32;


namespace LibraryStudio.Forms
{
    [TestClass]
    public class TestMarcField2
    {
        [TestMethod]
        public void GetSubfieldBounds_01()
        {
            var text = "200abCD$eEFG$hHIJ";
            var is_header = false;
            var offs = 0;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 3,
                CaretOffs = 0,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_01_2()
        {
            var text = "200abCD$eEFG$hHIJ";
            var is_header = false;
            var offs = 1;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 3,
                CaretOffs = 1,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_01_3()
        {
            var text = "200abCD$eEFG$hHIJ";
            var is_header = false;
            var offs = 3;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 5,
                CaretOffs = 3,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_01_4()
        {
            var text = "200abCD$eEFG$hHIJ";
            var is_header = false;
            var offs = 4;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 5,
                CaretOffs = 4,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_02()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 5,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_03()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 6;
            var correct_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 6,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_04()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 7;
            var correct_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 7,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_05()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 8;
            var correct_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 8,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_06()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 9;
            var correct_bounds = new SubfieldBound
            {
                Name = "a",
                StartOffs = 5,
                ContentStartOffs = 5 + 2,
                EndOffs = 5 + 5,
                CaretOffs = 9,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_07()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 10;
            var correct_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 10,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_08()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 14;
            var correct_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 14,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_08_2()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 14;
            var correct_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 14,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }


        // 插入符在末尾。right_most == false
        [TestMethod]
        public void GetSubfieldBounds_09()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 15;
            var correct_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。right_most == true
        [TestMethod]
        public void GetSubfieldBounds_10()
        {
            var text = "2001 $aAAA$hHHH";
            var is_header = false;
            var offs = 15;
            var correct_bounds = new SubfieldBound
            {
                Name = "h",
                StartOffs = 10,
                ContentStartOffs = 10 + 2,
                EndOffs = 10 + 5,
                CaretOffs = 15,
                Found = true
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。并且没有字段内容
        [TestMethod]
        public void GetSubfieldBounds_11()
        {
            var text = "2001 ";
            var is_header = false;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 插入符在末尾。并且没有字段内容
        [TestMethod]
        public void GetSubfieldBounds_12()
        {
            var text = "2001 ";
            var is_header = false;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 5,
                CaretOffs = 5,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 字段名中出现子字段符号
        [TestMethod]
        public void GetSubfieldBounds_13()
        {
            var text = "20$1 ";
            var is_header = false;
            var offs = 2;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 3,
                CaretOffs = 2,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 指示符少一位，$ 字符进入了指示符范围
        [TestMethod]
        public void GetSubfieldBounds_14()
        {
            var text = "2001$a";
            var is_header = false;
            var offs = 4;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 5,
                CaretOffs = 4,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 字段内容前部出现一段不在任何子字段中的内容
        [TestMethod]
        public void GetSubfieldBounds_15()
        {
            var text = "2001 KK$aAAA";
            var is_header = false;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 5,
                ContentStartOffs = 5,
                EndOffs = 7,
                CaretOffs = 5,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 字段内容前部出现一段不在任何子字段中的内容
        [TestMethod]
        public void GetSubfieldBounds_16()
        {
            var text = "2001 KK$aAAA";
            var is_header = false;
            var offs = 6;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 5,
                ContentStartOffs =5,
                EndOffs = 7,
                CaretOffs = 6,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 头标区
        [TestMethod]
        public void GetSubfieldBounds_20()
        {
            var text = "012345678901234567890123";
            var is_header = true;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 24,
                CaretOffs = 5,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 头标区。有一个子字段符号
        [TestMethod]
        public void GetSubfieldBounds_21()
        {
            var text = "012345678901$34567890123";
            var is_header = true;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 24,
                CaretOffs = 5,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 001 字段。有子字段符号
        [TestMethod]
        public void GetSubfieldBounds_22()
        {
            var text = "001$aAAA$bBBB";
            var is_header = false;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 13,
                CaretOffs = 5,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 001 字段。有子字段符号
        [TestMethod]
        public void GetSubfieldBounds_23()
        {
            var text = "001$aAAA$bBBB";
            var is_header = false;
            var offs = 10;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 13,
                CaretOffs = 10,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_24()
        {
            var text = "300  3333";
            var is_header = false;
            var offs = 9;
            var correct_bounds = new SubfieldBound
            {
                Name = "!content",
                StartOffs = 5,
                ContentStartOffs = 5,
                EndOffs = 9,
                CaretOffs = 9,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        // 只有 5 字符，offs 在 5
        [TestMethod]
        public void GetSubfieldBounds_25()
        {
            var text = "300  ";
            var is_header = false;
            var offs = 5;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 5,
                CaretOffs = 5,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_26()
        {
            var text = "300 ";
            var is_header = false;
            var offs = 4;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 4,
                CaretOffs = 4,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_26_2()
        {
            var text = "300 ";
            var is_header = false;
            var offs = 4;
            var correct_bounds = new SubfieldBound
            {
                Name = "!indicator",
                StartOffs = 3,
                ContentStartOffs = 3,
                EndOffs = 4,
                CaretOffs = 4,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_27()
        {
            var text = "300";
            var is_header = false;
            var offs = 3;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 3,
                CaretOffs = 3,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_27_2()
        {
            var text = "300";
            var is_header = false;
            var offs = 3;
            var correct_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_28()
        {
            var text = "30";
            var is_header = false;
            var offs = 2;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 2,
                CaretOffs = 2,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_28_2()
        {
            var text = "30";
            var is_header = false;
            var offs = 2;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 2,
                CaretOffs = 2,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_29()
        {
            var text = "3";
            var is_header = false;
            var offs = 1;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 1,
                CaretOffs = 1,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_29_2()
        {
            var text = "3";
            var is_header = false;
            var offs = 1;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 1,
                CaretOffs = 1,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_30()
        {
            var text = "";
            var is_header = false;
            var offs = 0;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 0,
                CaretOffs = 0,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_30_2()
        {
            var text = "";
            var is_header = false;
            var offs = 0;
            var correct_bounds = new SubfieldBound
            {
                Name = "!name",
                StartOffs = 0,
                ContentStartOffs = 0,
                EndOffs = 0,
                CaretOffs = 0,
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_31()
        {
            var text = "300";
            var is_header = false;
            var offs = -1;
            var correct_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, true);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        [TestMethod]
        public void GetSubfieldBounds_31_2()
        {
            var text = "300";
            var is_header = false;
            var offs = -1;
            var correct_bounds = new SubfieldBound
            {
                Found = false
            };
            var field = BuildField(text, is_header);
            var ret = field.GetSubfieldBoundsEx(offs, false);
            Assert.AreEqual(correct_bounds.ToString(), ret.ToString());
        }

        public static MarcField BuildField(string text, bool isHeader = false)
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

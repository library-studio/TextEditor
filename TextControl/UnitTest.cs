using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LibraryStudio.Forms
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void test_splitLines_01()
        {
            string text = "";
            string[] corrects = new string[] { "" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_02()
        {
            string text = "\r";
            string[] corrects = new string[] { "\r", "" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_03()
        {
            string text = "1\r";
            string[] corrects = new string[] { "1\r", "" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_04()
        {
            string text = "\r2";
            string[] corrects = new string[] { "\r", "2" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_05()
        {
            string text = "1\r2";
            string[] corrects = new string[] { "1\r", "2" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_06()
        {
            string text = "1\r2\r";
            string[] corrects = new string[] { "1\r", "2\r", "" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_07()
        {
            string text = "1\r2\r3";
            string[] corrects = new string[] { "1\r", "2\r", "3" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_08()
        {
            string text = "1\r\r3";
            string[] corrects = new string[] { "1\r", "\r", "3" };
            var lines = SimpleText.SplitLines(text);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        // 
        [TestMethod]
        public void test_splitLines_11()
        {
            string text = "";
            string[] corrects = new string[] { "" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_12()
        {
            string text = "\r";
            string[] corrects = new string[] { "", "" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_13()
        {
            string text = "1\r";
            string[] corrects = new string[] { "1", "" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_14()
        {
            string text = "\r2";
            string[] corrects = new string[] { "", "2" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_15()
        {
            string text = "1\r2";
            string[] corrects = new string[] { "1", "2" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_16()
        {
            string text = "1\r2\r";
            string[] corrects = new string[] { "1", "2", "" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_17()
        {
            string text = "1\r2\r3";
            string[] corrects = new string[] { "1", "2", "3" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        [TestMethod]
        public void test_splitLines_18()
        {
            string text = "1\r\r3";
            string[] corrects = new string[] { "1", "", "3" };
            var lines = SimpleText.SplitLines(text, '\r', false);
            Assert.AreEqual(corrects.Length, lines.Length);
            AssertLines(lines, corrects);
        }

        void AssertLines(string[] lines, string[] corrects)
        {
            for (int i = 0; i < corrects.Length; i++)
            {
                Assert.AreEqual(corrects[i], lines[i]);
            }
        }

        [TestMethod]
        public void test_findParagraph_01()
        {
            var paragraphs = new List<Paragraph>() {
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        0,
        out string left_text,
        0,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(0, results.Count);
            Assert.AreEqual("", left_text);
            Assert.AreEqual("", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("", replaced);
        }

        [TestMethod]
        public void test_findParagraph_02()
        {
            var paragraphs = new List<Paragraph>()
            {
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        0,
        out string left_text,
        -1,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(0, results.Count);
            Assert.AreEqual("", left_text);
            Assert.AreEqual("", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("", replaced);
        }

        [TestMethod]
        public void test_findParagraph_03()
        {
            var paragraphs = new List<Paragraph>()
            {
                new Paragraph("123456"),
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        0,
        out string left_text,
        -1,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("", left_text);
            Assert.AreEqual("", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("123456", replaced);
        }

        [TestMethod]
        public void test_findParagraph_04()
        {
            var paragraphs = new List<Paragraph>()
            {
                new Paragraph("123456"),
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        2,
        out string left_text,
        4,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("12", left_text);
            Assert.AreEqual("56", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("34", replaced);
        }

        [TestMethod]
        public void test_findParagraph_11()
        {
            var paragraphs = new List<Paragraph>()
            {
                new Paragraph("123456"),
                new Paragraph("abcdef"),
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        2,
        out string left_text,
        11,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("12", left_text);
            Assert.AreEqual("ef", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("3456\rabcd", replaced);
        }

        [TestMethod]
        public void test_findParagraph_12()
        {
            var paragraphs = new List<Paragraph>()
            {
                new Paragraph("123456"),
                new Paragraph("abcdef"),
                new Paragraph("ABCDEF"),
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        2,
        out string left_text,
        18,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("12", left_text);
            Assert.AreEqual("EF", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("3456\rabcdef\rABCD", replaced);
        }

        // 如果范围涉及到尾部 \r，则要把下一个 Paragraph 也拉上，以便后继实现 \r 也被删除的效果
        [TestMethod]
        public void test_findParagraph_13()
        {
            var paragraphs = new List<Paragraph>()
            {
                new Paragraph("123456"),
                new Paragraph("abcdef"),
                new Paragraph("ABCDEF"),
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        2,
        out string left_text,
        14,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("12", left_text);
            Assert.AreEqual("ABCDEF", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("3456\rabcdef\r", replaced);
        }

        // 范围不包含最后的 \r
        [TestMethod]
        public void test_findParagraph_14()
        {
            var paragraphs = new List<Paragraph>()
            {
                new Paragraph("123456"),
                new Paragraph("abcdef"),
                new Paragraph("ABCDEF"),
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        2,
        out string left_text,
        13,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("12", left_text);
            Assert.AreEqual("\r", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("3456\rabcdef", replaced);
        }


        [TestMethod]
        public void test_findParagraph_21()
        {
            var paragraphs = new List<Paragraph>() {
                new Paragraph { },
                new Paragraph { },
            };
            var results = SimpleText.FindParagraphs(
        paragraphs,
        0,
        out string left_text,
        0,
        out string right_text,
        out int first_paragraph_index,
        out string replaced);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("", left_text);
            Assert.AreEqual("\r", right_text);
            Assert.AreEqual(0, first_paragraph_index);
            Assert.AreEqual("", replaced);
        }


    }
}

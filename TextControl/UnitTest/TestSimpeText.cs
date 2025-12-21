using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryStudio.Forms
{
    [TestClass]
    public class TestSimpeText
    {
        [TestMethod]
        public void splitSubfields_01()
        {
            var text = "$aAAA$bBBB";
            var results = SimpleText.SplitSubfields(text, '$');
            var correct = new string[] { 
                "$",
                "aAAA",
                "$",
                "bBBB",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 空字符串
        [TestMethod]
        public void splitSubfields_02()
        {
            var text = "";
            var results = SimpleText.SplitSubfields(text, '$');
            var correct = new string[] {
                "",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // null
        [TestMethod]
        public void splitSubfields_03()
        {
            string text = null;
            var results = SimpleText.SplitSubfields(text, '$');
            var correct = new string[] {
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        [TestMethod]
        public void splitSubfields2_01()
        {
            var text = "$aAAA$bBBB";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "$a",
                "AAA",
                "$b",
                "BBB",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 空字符串
        [TestMethod]
        public void splitSubfields2_02()
        {
            var text = "";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // null
        [TestMethod]
        public void splitSubfields2_03()
        {
            string text = null;
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 没有分隔符
        [TestMethod]
        public void splitSubfields2_04()
        {
            var text = "a";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "a",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 没有分隔符
        [TestMethod]
        public void splitSubfields2_05()
        {
            var text = "ab";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "ab",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 没有分隔符
        [TestMethod]
        public void splitSubfields2_06()
        {
            var text = "abc";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "abc",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 只有一个分隔符
        [TestMethod]
        public void splitSubfields2_07()
        {
            var text = "$";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "$",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 只有一个分隔符
        [TestMethod]
        public void splitSubfields2_08()
        {
            var text = "$a";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "$a",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 只有一个分隔符
        [TestMethod]
        public void splitSubfields2_09()
        {
            var text = "$ab";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');

            var correct = new string[] {
                "$a",
                "b",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 只有一个分隔符
        [TestMethod]
        public void splitSubfields2_10()
        {
            var text = "$abc";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');
            var correct = new string[] {
                "$a",
                "bc",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 第一个部分没有分隔符
        [TestMethod]
        public void splitSubfields2_11()
        {
            var text = "1$abc";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');
            var correct = new string[] {
                "1",
                "$a",
                "bc",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 第二个部分除了分隔符没有内容
        [TestMethod]
        public void splitSubfields2_12()
        {
            var text = "$abc$";
            var results = SimpleText.SplitSubfields2(text, '$');
            AssertNotContainDelemeter(results, '$');
            var correct = new string[] {
                "$a",
                "bc",
                "$",
            };
            Console.WriteLine(string.Join("\r\n", results));
            Assert.IsTrue(correct.SequenceEqual(results));
        }

        // 断言第一字符以外的其它字符不能是分隔符
        void AssertNotContainDelemeter(string[] results,
            char delimeter = '\\')
        {
            foreach(var result in results)
            {
                if (result.Skip(1).Where(c => c == delimeter).Any())
                    Assert.Fail($"内容 '{result}' 中第一字符以后不应包含分隔符 '{delimeter}'");
            }
        }
    }
}

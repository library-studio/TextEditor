using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
// using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    [TestClass]
    public class TestUtility
    {
        // Empty
        [TestMethod]
        public void offset_01()
        {
            Rectangle rect = Rectangle.Empty;
            Rectangle correct = Rectangle.Empty;
            var result = Utility.Offset(ref rect, 1, 2);
            Assert.AreEqual(correct, rect);
            Assert.AreEqual(result, rect);
        }

        [TestMethod]
        public void offset_02()
        {
            Rectangle rect = new Rectangle(0, 0, 1, 2);
            Rectangle correct = new Rectangle(1, 2, 1, 2);
            var result = Utility.Offset(ref rect, 1, 2);
            Assert.AreEqual(correct, rect);
            Assert.AreEqual(result, rect);
        }

        [TestMethod]
        public void union_01()
        {
            Rectangle rect1 = Rectangle.Empty;
            Rectangle rect2 = Rectangle.Empty;
            Rectangle correct = Rectangle.Empty;
            var result = Utility.Union(rect1, rect2);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void union_02()
        {
            Rectangle rect1 = new Rectangle(0, 1, 2, 3);
            Rectangle rect2 = Rectangle.Empty;
            Rectangle correct = new Rectangle(0, 1, 2, 3);
            var result = Utility.Union(rect1, rect2);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void union_03()
        {
            Rectangle rect1 = Rectangle.Empty;
            Rectangle rect2 = new Rectangle(0, 1, 2, 3);
            Rectangle correct = new Rectangle(0, 1, 2, 3);
            var result = Utility.Union(rect1, rect2);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void union_04()
        {
            Rectangle rect1 = new Rectangle(0, 1, 2, 3);
            Rectangle rect2 = new Rectangle(0, 1, 2, 3);
            Rectangle correct = new Rectangle(0, 1, 2, 3);
            var result = Utility.Union(rect1, rect2);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void union_05()
        {
            Rectangle rect1 = new Rectangle(0, 1, 2, 3);
            Rectangle rect2 = new Rectangle(1, 1, 2, 3);
            Rectangle correct = new Rectangle(0, 1, 3, 3);
            var result = Utility.Union(rect1, rect2);
            Assert.AreEqual(correct, result);
        }

        [TestMethod]
        public void inRange()
        {
            Assert.AreEqual(false, Utility.InRange(0, 1, 2));
            Assert.AreEqual(true, Utility.InRange(1, 1, 2));
            Assert.AreEqual(false, Utility.InRange(2, 1, 2));
            Assert.AreEqual(false, Utility.InRange(3, 1, 2));

            // 1~1 是空区间
            Assert.AreEqual(false, Utility.InRange(1, 1, 1));

        }

        [TestMethod]
        public void cross()
        {
            Assert.AreEqual(false, Utility.Cross(0, 1, 2, 3));
            Assert.AreEqual(false, Utility.Cross(0, 1, 1, 2));

            Assert.AreEqual(true, Utility.Cross(0, 1, 0, 1));
            Assert.AreEqual(true, Utility.Cross(0, 1, 0, 2));
            Assert.AreEqual(true, Utility.Cross(0, 2, 0, 1));

            // 只有一个空区间，可能相交
            Assert.AreEqual(false, Utility.Cross(1, 1, 0, 1));
            Assert.AreEqual(true, Utility.Cross(1, 1, 1, 2));
            Assert.AreEqual(false, Utility.Cross(1, 1, 2, 3));

            // 两组互换位置
            Assert.AreEqual(false, Utility.Cross(0, 1, 1, 1));
            Assert.AreEqual(true, Utility.Cross(1, 2, 1, 1));
            Assert.AreEqual(false, Utility.Cross(2, 3, 1, 1));

            // 两个都是空区间，无法相交
            Assert.AreEqual(false, Utility.Cross(1, 1, 1, 1));

            // 一个包围另外一个
            Assert.AreEqual(true, Utility.Cross(1, 2, 0, 3));
            Assert.AreEqual(true, Utility.Cross(0, 3, 1, 2));

            // 一段交叉
            Assert.AreEqual(true, Utility.Cross(1, 3, 2, 4));
            Assert.AreEqual(true, Utility.Cross(2, 4, 1, 3));

            Assert.AreEqual(false, Utility.Cross(0, 24, 24, 24));
            Assert.AreEqual(true, Utility.Cross(0, 24, 23, 23));

            /*
            Assert.AreEqual(true, Utility.InRange(1, 1, 2));
            Assert.AreEqual(false, Utility.InRange(2, 1, 2));
            Assert.AreEqual(false, Utility.InRange(3, 1, 2));
            */
        }

        [TestMethod]
        public void cross_1()
        {
            Assert.AreEqual(false, Utility.Cross(0, 1, 1, 2));
        }
    }
}

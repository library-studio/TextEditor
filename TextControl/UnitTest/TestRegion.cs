using System;
using System.Collections.Generic;
using System.Drawing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryStudio.Forms
{
    [TestClass]
    public class TestRegion
    {
        [TestMethod]
        public void region_translate()
        {
            Region region = new Region(new RectangleF());
            region.Union(new RectangleF(0, 0, 1, 1));


            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                var bounds = region.GetBounds(g); // 返回 RectangleF
                Console.WriteLine(bounds.ToString());
                Assert.AreEqual(new RectangleF(0, 0, 1, 1), bounds);
            }
        }
    }
}

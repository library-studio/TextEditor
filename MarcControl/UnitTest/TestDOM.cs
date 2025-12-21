using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LibraryStudio.Forms.MarcRecord;    // BuildContent()

namespace LibraryStudio.Forms
{
    [TestClass]
    public class TestDOM
    {
        [TestMethod]
        public void replaceText_01()
        {
            UseControl((ctl) =>
            {
                // 直接调用 API
                ctl.ReplaceText(0, 0, "abc", delay_update: false);

                Application.DoEvents(); // 若需要处理 Timer 或重绘
                Assert.AreEqual("abc", ctl.Content);
            });
        }

        // 头标区中缩减字符数，会自动补上
        [TestMethod]
        public void replaceText_02()
        {
            UseControl((ctl) =>
            {
                ctl.Content = BuildContent(@"012345678901234567890123
001ABCDE");
                // 删除掉头标区一个字符
                ctl.ReplaceText(0, 1, "", delay_update: false);

                Assert.AreEqual(2, ctl.GetDomRecord().FieldCount);
                Assert.AreEqual("123456789012345678901230", ctl.GetDomRecord().GetField(0).Content);
                Assert.AreEqual("01ABCDE", ctl.GetDomRecord().GetField(1).Text);
            });
        }

        // 头标区中插入字符数，会自动挤走最后的
        [TestMethod]
        public void replaceText_03()
        {
            UseControl((ctl) =>
            {
                ctl.Content = BuildContent(@"012345678901234567890123
001ABCDE");
                // 删除掉头标区一个字符
                ctl.ReplaceText(0, 0, "-", delay_update: false);

                Assert.AreEqual(2, ctl.GetDomRecord().FieldCount);
                Assert.AreEqual("-01234567890123456789012", ctl.GetDomRecord().GetField(0).Content);
                Assert.AreEqual("3001ABCDE", ctl.GetDomRecord().GetField(1).Text);
            });
        }

        [TestMethod]
        public void replaceText_04()
        {
            UseControl((ctl) =>
            {
                ctl.Content = BuildContent(@"01234567890123456789012");
                // 头标区尾部插入一个字符
                ctl.ReplaceText(23, 23, "-", delay_update: false);

                Assert.AreEqual(1, ctl.GetDomRecord().FieldCount);
                Assert.AreEqual("01234567890123456789012-", ctl.GetDomRecord().GetField(0).Content);
            });
        }


        // 空内容中插入一个头标区
        [TestMethod]
        public void domRecord_01()
        {
            UseDomRecord((record) =>
            {
                // 插入字段前内容为空
                Assert.AreEqual("", record.GetControl().Content);

                // 插入一个头标区
                record.InsertField(0, "", "", "012345678901234567890123");

                Application.DoEvents(); // 若需要处理 Timer 或重绘
                Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
            });
        }

        // 已有头标区，再在它之前插入头标区
        [TestMethod]
        public void domRecord_02()
        {
            UseDomRecord((record) =>
            {
                // 插入字段前内容为空
                Assert.AreEqual("", record.GetControl().Content);

                // 插入一个头标区
                record.InsertField(0, "", "", "012345678901234567890123");

                try
                {
                    // 再次插入一个头标区
                    record.InsertField(0, "", "", "012345678901234567890123");
                    Assert.Fail("未如期待的抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
                }
            });
        }

        // 修改头标区的 Name
        [TestMethod]
        public void domRecord_03()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                try
                {
                    // 修改头标区 Name
                    record.GetField(0).Name = "200";
                    Assert.Fail("未如期待的抛出异常");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
                }
            });
        }

        // 修改头标区的 Indicator
        [TestMethod]
        public void domRecord_04()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                try
                {
                    // 修改头标区 Name
                    record.GetField(0).Indicator = "11";
                    Assert.Fail("未如期待的抛出异常");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
                }
            });
        }


        // 修改头标区的 Content，并且长度不足
        [TestMethod]
        public void domRecord_05()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                try
                {
                    // 修改头标区 Content
                    record.GetField(0).Content = "11";
                    Assert.Fail("未如期待的抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
                }
            });
        }

        // 修改头标区的 Content，并且长度超过 24
        [TestMethod]
        public void domRecord_06()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                try
                {
                    // 修改头标区 Content
                    record.GetField(0).Content = "012345678901234567890123A";
                    Assert.Fail("未如期待的抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
                }
            });
        }

        // 修改头标区的 Content，并且长度正好 24
        [TestMethod]
        public void domRecord_07()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                // 修改头标区 Content
                record.GetField(0).Content = "321098765432109876543210";

                Application.DoEvents(); // 若需要处理 Timer 或重绘
                Assert.AreEqual("321098765432109876543210", record.GetControl().Content);
            });
        }

        // 删除头标区
        [TestMethod]
        public void domRecord_08()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                // 删除头标区
                record.DeleteField(0);

                Application.DoEvents(); // 若需要处理 Timer 或重绘
                Assert.AreEqual("", record.GetControl().Content);
            });
        }

        [TestMethod]
        public void domRecord_08_a()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                try
                {
                    // 删除不存在的字段
                    record.DeleteField(1);
                    Assert.Fail($"未抛出期望的异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123", record.GetControl().Content);
                }
            });
        }

        // 按照 index 删除也会自动更新 DomField 对象状态
        [TestMethod]
        public void domRecord_08_b()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123";

                var header = record.GetField(0);
                // 删除头标区
                record.DeleteField(0);

                Assert.AreEqual(true, header.IsDeleted);

                Application.DoEvents(); // 若需要处理 Timer 或重绘
                Assert.AreEqual("", record.GetControl().Content);
            });
        }


        // 删除头标区。删除前存在一共两个字段
        [TestMethod]
        public void domRecord_09()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = "012345678901234567890123ABC12\u001e";

                Assert.AreEqual(2, record.FieldCount);

                try
                {
                    // 删除头标区
                    record.DeleteField(0);

                    Assert.Fail("未如期待的抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.AreEqual("012345678901234567890123ABC12\u001e", record.GetControl().Content);
                }
            });
        }

        // 追踪字段插入删除
        [TestMethod]
        public void traceChange_01()
        {
            UseDomRecord((record) =>
            {

                record.InsertField(0, "", "", "012345678901234567890123");
                record.InsertField(1, "001", "", "ABCD");
                record.InsertField(2, "200", "  ", "1234");

                var field1 = record.GetField(1);
                Assert.AreEqual("ABCD", field1.Content);
                var field2 = record.GetField(2);
                Assert.AreEqual("1234", field2.Content);

                record.DeleteField(1);
                Assert.AreEqual("1234", field2.Content);
                Assert.AreEqual(2, record.FieldCount);
            });
        }

        // using 版本
        [TestMethod]
        public void traceChange_02()
        {
            UseDomRecord((record) =>
            {

                record.InsertField(0, "", "", "012345678901234567890123");
                record.InsertField(1, "001", "", "ABCD");
                record.InsertField(2, "200", "  ", "1234");

                using (var field1 = record.GetField(1))
                using (var field2 = record.GetField(2))
                {
                    Assert.AreEqual("ABCD", field1.Content);
                    Assert.AreEqual("1234", field2.Content);

                    record.DeleteField(1);
                    Assert.AreEqual("1234", field2.Content);
                    Assert.AreEqual(2, record.FieldCount);
                }
            });
        }


        #region 删除字段

        [TestMethod]
        public void deleteField_01()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                record.DeleteField(1);
                Assert.AreEqual(2, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123
200  1234"
),
                    record.GetControl().Content);

                record.DeleteField(1);
                Assert.AreEqual(1, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123"
),
                    record.GetControl().Content);

                record.DeleteField(0);
                Assert.AreEqual(0, record.FieldCount);
                Assert.AreEqual(
                    "",
                    record.GetControl().Content);

                Application.DoEvents();
            });
        }

        [TestMethod]
        public void deleteField_02()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                var header = record.GetField(0);
                var field_001 = record.GetField(1);
                var field_200 = record.GetField(2);

                Assert.AreEqual("012345678901234567890123", header.Text);
                Assert.AreEqual("001ABCD", field_001.Text);
                Assert.AreEqual("200  1234", field_200.Text);

                record.DeleteField(1);
                Assert.AreEqual(2, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123
200  1234"
),
                    record.GetControl().Content);

                {
                    Assert.AreEqual("012345678901234567890123", header.Text);
                    try
                    {
                        Assert.AreEqual("001ABCD", field_001.Text);
                        Assert.Fail("未如期待地抛出异常");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    Assert.AreEqual("200  1234", field_200.Text);
                }

                record.DeleteField(1);
                Assert.AreEqual(1, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123"
),
                    record.GetControl().Content);

                {
                    Assert.AreEqual("012345678901234567890123", header.Text);
                    try
                    {
                        Assert.AreEqual("001ABCD", field_001.Text);
                        Assert.Fail("未如期待地抛出异常");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    try
                    {
                        Assert.AreEqual("200  1234", field_200.Text);
                        Assert.Fail("未如期待地抛出异常");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                record.DeleteField(0);
                Assert.AreEqual(0, record.FieldCount);
                Assert.AreEqual(
                    "",
                    record.GetControl().Content);

                {
                    try
                    {
                        Assert.AreEqual("012345678901234567890123", header.Text);
                        Assert.Fail("未如期待地抛出异常");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
        }

        // 删除一个范围的字段
        [TestMethod]
        public void deleteField_03()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                record.DeleteField(1, 2);
                Assert.AreEqual(1, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123"
),
                    record.GetControl().Content);
            });
        }

        // 删除时越过范围
        [TestMethod]
        public void deleteField_04()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                try
                {
                    record.DeleteField(1, 3);   // 越过最大范围
                    Assert.Fail("未如期待地抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);

                    Assert.AreEqual(3, record.FieldCount);
                    Assert.AreEqual(
                        BuildContent(
    @"012345678901234567890123
001ABCD
200  1234"
    ),
                        record.GetControl().Content);

                }
            });
        }

        // 删除时越过范围
        [TestMethod]
        public void deleteField_05()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                try
                {
                    record.DeleteField(0, 4);   // 越过最大范围
                    Assert.Fail("未如期待地抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);

                    Assert.AreEqual(3, record.FieldCount);
                    Assert.AreEqual(
                        BuildContent(
    @"012345678901234567890123
001ABCD
200  1234"
    ),
                        record.GetControl().Content);

                }
            });
        }

        // 在除了头标区以外还有其它字段的情况下，删除头标区“字段”
        [TestMethod]
        public void deleteField_06()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                try
                {
                    record.DeleteField(0, 1);
                    Assert.Fail("未如期待地抛出异常");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);

                    Assert.AreEqual(3, record.FieldCount);
                    Assert.AreEqual(
                        BuildContent(
    @"012345678901234567890123
001ABCD
200  1234"
    ),
                        record.GetControl().Content);

                }
            });
        }

        // 删除全部字段
        [TestMethod]
        public void deleteField_07()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);
                record.DeleteField(0, 3);

                Assert.AreEqual(0, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@""
),
                    record.GetControl().Content);

            });
        }

        // 清除全部字段
        [TestMethod]
        public void deleteField_08()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                var header = record.GetField(0);
                var field_001 = record.GetField(1);
                var field_200 = record.GetField(2);

                Assert.AreEqual("012345678901234567890123", header.Text);
                Assert.AreEqual("001ABCD", field_001.Text);
                Assert.AreEqual("200  1234", field_200.Text);

                record.Clear();

                Assert.AreEqual(0, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@""
),
                    record.GetControl().Content);

                Assert.AreEqual(true, header.IsDeleted);
                Assert.AreEqual(true, field_001.IsDeleted);
                Assert.AreEqual(true, field_200.IsDeleted);
            });
        }

        [TestMethod]
        public void insertField_01()
        {
            UseDomRecord((record) =>
            {
                record.GetControl().Content = BuildContent(
@"012345678901234567890123
001ABCD
200  1234"
);
                Assert.AreEqual(3, record.FieldCount);

                var header = record.GetField(0);
                var field_001 = record.GetField(1);
                var field_200 = record.GetField(2);

                Assert.AreEqual("012345678901234567890123", header.Text);
                Assert.AreEqual("001ABCD", field_001.Text);
                Assert.AreEqual("200  1234", field_200.Text);

                // 插入
                record.InsertField(1,
                    "009", "", "999");
                Assert.AreEqual(4, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123
009999
001ABCD
200  1234"
),
                    record.GetControl().Content);

                Assert.AreEqual("012345678901234567890123", header.Text);
                Assert.AreEqual("001ABCD", field_001.Text);
                Assert.AreEqual("200  1234", field_200.Text);


                record.InsertField(4,
                    "300", "  ", "aAAA");
                Assert.AreEqual(5, record.FieldCount);
                Assert.AreEqual(
                    BuildContent(
@"012345678901234567890123
009999
001ABCD
200  1234
300  aAAA"
),
                    record.GetControl().Content);

                Assert.AreEqual("012345678901234567890123", header.Text);
                Assert.AreEqual("001ABCD", field_001.Text);
                Assert.AreEqual("200  1234", field_200.Text);

            });
        }


        #endregion



        delegate void delegate_useMarcControl(MarcControl marcControl);

        void UseControl(delegate_useMarcControl action)
        {
            UiTestHelpers.RunInSta(() =>
            {
                using (var form = new Form())
                {
                    var ctl = new MarcControl();
                    ctl.Size = new Size(1024, 768);
                    form.Controls.Add(ctl);
                    form.CreateControl();

                    action(ctl);

                    /*
                    // 直接调用 API
                    ctl.ReplaceText(0, 0, "abc", delay_update: false);

                    Application.DoEvents(); // 若需要处理 Timer 或重绘
                    Assert.IsTrue(ctl.Content.Contains("abc"));
                    */
                }
            });
        }

        delegate void delegate_useDomRecord(DomRecord domRecord);

        void UseDomRecord(delegate_useDomRecord action)
        {
            UiTestHelpers.RunInSta(() =>
            {
                using (var form = new Form())
                {
                    var ctl = new MarcControl();
                    ctl.Size = new Size(1024, 768);
                    form.Controls.Add(ctl);
                    form.CreateControl();

                    Application.DoEvents();
                    action(ctl.GetDomRecord());
                    Application.DoEvents();
                    ctl.Dispose();
                }
            });
        }


    }
}
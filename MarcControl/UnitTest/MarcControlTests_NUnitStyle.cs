// csharp Tests\MarcControlTests_NUnit.cs
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace LibraryStudio.Forms
{
#if REMOVED
    [TestFixture]
    public class MarcControlTests_NUnitStyle
    {
        [Test]
        [Apartment(ApartmentState.STA)]
        public void MarcControl_SetContent_CreateHandle_Works()
        {
            using (var form = new Form())
            {
                var ctl = new MarcControl();
                ctl.Size = new Size(600, 300);
                form.Controls.Add(ctl);

                // 确保创建句柄（不必显示窗口）
                form.CreateControl();     // 创建 form 的句柄并递归创建子控件的句柄
                Assert.IsTrue(ctl.IsHandleCreated);

                // 设置内容（Relayout/绘制可能会调用 CreateGraphics）
                ctl.Content = "测试字段\u001e第二字段\r";
                // 视具体测试场景，可能需要 Application.DoEvents() 让消息循环处理（Timers 等）
                Application.DoEvents();

                Assert.IsNotNull(ctl.Content);
                Assert.IsTrue(ctl.Content.Length > 0);
            }
        }
    }


#endif
}
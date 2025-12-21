// csharp Tests\UiTestHelpers.cs
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    public static class UiTestHelpers
    {
        // 在 STA 线程上运行一个 action，并在该线程上启动一个短暂的消息循环以保证 WinForms 能正常初始化。
        // 如果 action 抛出异常，会在调用线程重新抛出以便测试框架捕获。
        public static void RunInSta(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception remoteEx = null;
            var done = new ManualResetEventSlim(false);

            var t = new Thread(() =>
            {
                // 保证在 UI 线程上处理未捕获异常，避免弹出 ThreadExceptionDialog
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                {
                    remoteEx = remoteEx ?? e.Exception;
                    try { Application.ExitThread(); } catch { }
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e is UnhandledExceptionEventArgs ue && ue.ExceptionObject is Exception ex)
                        remoteEx = remoteEx ?? ex;
                    try { Application.ExitThread(); } catch { }
                };

                try
                {
                    // 提前启用 Visual Styles / 文本渲染配置（可选但常用）
                    try { Application.EnableVisualStyles(); } catch { }
                    try { Application.SetCompatibleTextRenderingDefault(false); } catch { }

                    // 建立一个隐藏窗口用于启动消息循环，并在 Load 时执行 action
                    using (var hidden = new Form()
                    {
                        Size = new Size(0, 0),
                        ShowInTaskbar = false,
                        StartPosition = FormStartPosition.Manual,
                        Location = new Point(-10000, -10000)
                    })
                    {
                        hidden.Load += (s, e) =>
                        {
                            try
                            {
                                // 在 UI 线程上下文执行测试操作
                                action();
                            }
                            catch (Exception ex)
                            {
                                remoteEx = remoteEx ?? ex;
                            }
                            finally
                            {
                                // 结束消息循环
                                try { Application.ExitThread(); } catch { }
                            }
                        };

                        try
                        {
                            Application.Run(hidden);
                        }
                        catch (Exception ex)
                        {
                            // 捕获运行时未预期异常
                            remoteEx = remoteEx ?? ex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    remoteEx = remoteEx ?? ex;
                }
                finally
                {
                    done.Set();
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();

            // 等待 UI 线程完成
            done.Wait();

            if (remoteEx != null)
                throw new AggregateException("UiTestHelpers.RunInSta action threw an exception.", remoteEx);
        }

#if REMOVED
        // 在 STA 线程上运行一个 action，并在该线程上启动一个短暂的消息循环以保证 WinForms 能正常初始化。
        // 如果 action 抛出异常，会在调用线程重新抛出以便测试框架捕获。
        public static void RunInSta(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception remoteEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    // 避免默认的 ThreadException 对话框在测试环境中被弹出
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                    // 提前启用 Visual Styles / 文本渲染配置（可选但常用）
                    try { Application.EnableVisualStyles(); } catch { }
                    try { Application.SetCompatibleTextRenderingDefault(false); } catch { }

                    using (var hidden = new Form() { Size = new Size(0, 0), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000) })
                    {
                        hidden.Load += (s, e) =>
                        {
                            try
                            {
                                // 在 UI 线程上下文执行测试操作
                                action();
                            }
                            catch (Exception ex)
                            {
                                remoteEx = ex;
                            }
                            // 结束消息循环
                            hidden.BeginInvoke(new Action(() => hidden.Close()));
                        };

                        // 启动消息循环（保证控件创建、字体/Graphics 初始化等有一个正式的 UI 环境）
                        Application.Run(hidden);
                    }
                }
                catch (Exception ex)
                {
                    // 捕获 Run 内部不可预见异常
                    remoteEx = remoteEx ?? ex;
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            t.Join();

            if (remoteEx != null)
                throw new AggregateException("UiTestHelpers.RunInSta action threw an exception.", remoteEx);
        }
#endif


#if REMOVED
        // 在 STA 线程上执行 action，捕获并转发异常
        public static void RunInSta(Action action)
        {
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    // 为 WinForms 操作设置同步上下文（可选，但有用）
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
                throw new AggregateException("STA thread threw", ex);
        }

#endif
    }
}
using System;
using System.Threading;

namespace AppFramework.Core.Tests;

/// <summary>
/// مساعد لتشغيل اختبار على خيط STA (مطلوب لعناصر WPF).
/// </summary>
public static class StaTestHelper
{
    public static void RunSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
            throw captured;
    }

    public static T RunSta<T>(Func<T> func)
    {
        T result = default!;
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try { result = func(); }
            catch (Exception ex) { captured = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
            throw captured;
        return result;
    }
}
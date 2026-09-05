using System.Runtime.InteropServices;

namespace TiaoKe.App.Services;

/// <summary>
/// Pins a top-level window to every Windows virtual desktop.
/// The virtual desktop pinning interfaces are Shell COM interfaces rather
/// than part of WPF, so this helper deliberately treats them as optional.
/// </summary>
internal static class VirtualDesktopService
{
    private static readonly Guid ImmersiveShellClsid = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    private static readonly Guid VirtualDesktopPinnedAppsService = new("B5A399E7-1C87-46B8-88E9-FC5747B171BD");
    private static readonly Guid ApplicationViewCollectionService = new("1841C6D7-4F9D-42C0-AF41-8747538F10E5");

    public static bool TryPinToAllDesktops(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            return false;
        }

        object? shellObject = null;
        object? viewCollectionObject = null;
        object? pinnedAppsObject = null;
        IApplicationView? view = null;

        try
        {
            var shellType = Type.GetTypeFromCLSID(ImmersiveShellClsid, throwOnError: false);
            if (shellType is null) return false;
            shellObject = Activator.CreateInstance(shellType);
            if (shellObject is not IServiceProvider shell)
            {
                return false;
            }

            var viewCollectionService = ApplicationViewCollectionService;
            var viewCollectionInterface = typeof(IApplicationViewCollection).GUID;
            viewCollectionObject = shell.QueryService(
                ref viewCollectionService,
                ref viewCollectionInterface);
            var pinnedAppsService = VirtualDesktopPinnedAppsService;
            var pinnedAppsInterface = typeof(IVirtualDesktopPinnedApps).GUID;
            pinnedAppsObject = shell.QueryService(
                ref pinnedAppsService,
                ref pinnedAppsInterface);

            if (viewCollectionObject is not IApplicationViewCollection viewCollection ||
                pinnedAppsObject is not IVirtualDesktopPinnedApps pinnedApps)
            {
                return false;
            }

            var result = viewCollection.GetViewForHwnd(windowHandle, out view);
            if (result < 0 || view is null)
            {
                return false;
            }

            if (!pinnedApps.IsViewPinned(view))
            {
                pinnedApps.PinView(view);
            }

            return true;
        }
        catch (COMException)
        {
            // Windows can deny the Shell operation during session startup or
            // on editions without virtual desktop support. Keep the reminder
            // usable on its normal desktop in those cases.
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (TypeLoadException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
        finally
        {
            ReleaseComObject(view);
            ReleaseComObject(pinnedAppsObject);
            ReleaseComObject(viewCollectionObject);
            ReleaseComObject(shellObject);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    private interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryService(ref Guid service, ref Guid requestedInterface);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
    private interface IApplicationViewCollection
    {
        int GetViews(out IObjectArray array);
        int GetViewsByZOrder(out IObjectArray array);
        int GetViewsByAppUserModelId(string id, out IObjectArray array);
        int GetViewForHwnd(IntPtr hwnd, out IApplicationView view);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513")]
    private interface IApplicationView
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("4CE81583-1E4C-4632-A621-07A53543148F")]
    private interface IVirtualDesktopPinnedApps
    {
        bool IsAppIdPinned([MarshalAs(UnmanagedType.LPWStr)] string appId);
        void PinAppID([MarshalAs(UnmanagedType.LPWStr)] string appId);
        void UnpinAppID([MarshalAs(UnmanagedType.LPWStr)] string appId);
        bool IsViewPinned([MarshalAs(UnmanagedType.Interface)] IApplicationView applicationView);
        void PinView([MarshalAs(UnmanagedType.Interface)] IApplicationView applicationView);
        void UnpinView([MarshalAs(UnmanagedType.Interface)] IApplicationView applicationView);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
    private interface IObjectArray
    {
    }
}

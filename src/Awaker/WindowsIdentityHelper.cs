﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace Awaker;

class WindowsIdentityHelper
{
    [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern int OpenProcessToken(
        IntPtr ProcessHandle, // handle to process
        int DesiredAccess, // desired access to process
        ref IntPtr TokenHandle // handle to open access token
    );

    [DllImport("kernel32", SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern bool CloseHandle(IntPtr handle);
    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool DuplicateToken(IntPtr ExistingTokenHandle,
        int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

    public const int TOKEN_ASSIGN_PRIMARY = 0x0001;
    public const int TOKEN_DUPLICATE = 0x0002;
    public const int TOKEN_IMPERSONATE = 0x0004;
    public const int TOKEN_QUERY = 0x0008;
    public const int TOKEN_QUERY_SOURCE = 0x0010;
    public const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const int TOKEN_ADJUST_GROUPS = 0x0040;
    public const int TOKEN_ADJUST_DEFAULT = 0x0080;
    public const int TOKEN_ADJUST_SESSIONID = 0x0100;
    public const int TOKEN_READ = 0x00020000 | TOKEN_QUERY;
    public const int TOKEN_WRITE = 0x00020000 | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT;
    public const int TOKEN_EXECUTE = 0x00020000;

    public static List<WindowsIdentity> GetLoggedOnUsers()
    {
        var users = new List<WindowsIdentity>();
        var errs = "";
        var hToken = IntPtr.Zero;
        //Get a process that will always be available.
        foreach (var proc in Process.GetProcessesByName("explorer"))
        {
            try
            {
                if (OpenProcessToken(proc.Handle,
                        TOKEN_QUERY | TOKEN_IMPERSONATE | TOKEN_DUPLICATE,
                        ref hToken) != 0)
                {
                    var newId = new WindowsIdentity(hToken);
                    CloseHandle(hToken);
                    users.Add(newId);
                }
                else
                {
                    errs += $"OpenProcess Failed {Marshal.GetLastWin32Error()}, privilege not held\r\n";
                }

            }
            catch (Exception ex)
            {
                errs += $"OpenProcess Failed {ex.Message}\r\n";
            }
        }

        if (errs.Length > 0) { throw new Exception(errs); }
        return users.DistinctBy(identity => identity.Owner.Value).ToList();
    }
}
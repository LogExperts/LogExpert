using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Expanded with some helpers from: https://code.msdn.microsoft.com/windowsapps/How-to-know-the-process-704839f4/
// Uses Windows Restart Manager. 
// A more involved and cross platform solution to this problem is here: https://github.com/cklutz/LockCheck


namespace FileLockFinder
{
    public class LockFinder
    {

        /// <summary>
        /// Method <c>FindLockedProcessName</c> Retrieve the first process name
        /// that is locking the file at the specified path
        /// </summary>
        /// <param name="path">The path of a file with a write lock held by a 
        /// process</param>
        /// <resturns>The name of the first process found with a lock</resturns>
        /// <exception cref="Exception">
        /// Thrown when the file path is not locked
        /// </exception>
        static public string FindLockedProcessName(string path)
        {
            var list = FindLockProcesses(path);
            if (list.Count == 0)
            {
                throw new Exception(
                "No processes are locking the path specified");
            }
            return list[0].ProcessName;
        }

        /// <summary>
        /// Method <c>CheckIfFileIsLocked</c> Check if the file specified has a
        /// write lock held by a process
        /// </summary>
        /// <param name="path">The path of a file being checked if a write lock
        /// held by a process</param>
        /// <returns>true when one or more processes with lock</returns>
        static public bool CheckIfFileIsLocked(string path)
        {
            var list = FindLockProcesses(path);
            if (list.Count > 0) { return true; }
            return false;
        }

        /// <summary>
        /// Used to find processes holding a lock on the file. This would cause
        /// other usage, such as file truncation or write opretions to throw
        /// IOException if an exclusive lock is attempted. 
        /// </summary>
        /// <param name="path">Path being checked</param>
        /// <returns>List of processes holding file lock to path</returns>
        /// <exception cref="Exception"></exception>
        static public List<Process> FindLockProcesses(string path)
        {
            var key = Guid.NewGuid().ToString();
            var processes = new List<Process>();

            int res = RmStartSession(out uint handle, 0, key);
            if (res != 0)
            {
                throw new Exception("Could not begin restart session. " +
                                    "Unable to determine file locker.");
            }

            try
            {
                uint pnProcInfo = 0;
                uint lpdwRebootReasons = RmRebootReasonNone;
                string[] resources = [path];

                res = RmRegisterResources(handle, (uint)resources.Length,
                                            resources, 0, null, 0, null);
                if (res != 0)
                {
                    throw new Exception("Could not register resource.");
                }
                res = RmGetList(handle, out uint pnProcInfoNeeded, ref pnProcInfo, null,
                                ref lpdwRebootReasons);
                const int ERROR_MORE_DATA = 234;
                if (res == ERROR_MORE_DATA)
                {
                    RM_PROCESS_INFO[] processInfo =
                        new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;
                    // Get the list.
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].
                                    Process.dwProcessId));
                            }
                            catch (ArgumentException) { }
                        }
                    }
                    else
                    {
                        throw new Exception("Could not list processes locking resource");
                    }
                }
                else if (res != 0)
                {
                    throw new Exception("Could not list processes locking resource." +
                                        "Failed to get size of result.");
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception.Message);
            }
            finally
            {
                Trace.WriteLine($"RmEndSession: {RmEndSession(handle)}");
            }

            return processes;
        }
        private const int RmRebootReasonNone = 0;
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;

        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.
                ComTypes.FILETIME ProcessStartTime;
        }
        [DllImport("rstrtmgr.dll",
            CharSet = CharSet.Auto, SetLastError = true)]
        static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);
        [StructLayout(LayoutKind.Sequential,
            CharSet = CharSet.Auto)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr,
                SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int RmRegisterResources(
            uint pSessionHandle,
            UInt32 nFiles,
            string[] rgsFilenames,
            UInt32 nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            UInt32 nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int RmStartSession(
            out uint pSessionHandle,
            int dwSessionFlags,
            string strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int RmEndSession(uint pSessionHandle);
    }
}


using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

[DllImport("kernel32.dll", SetLastError = true )]
static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

[DllImport("kernel32.dll", SetLastError = true )]
static extern bool CloseHandle(IntPtr handle);

[DllImport("kernel32.dll", SetLastError = true )]
static extern bool DuplicateHandle(
        IntPtr hSourceProcessHandle,
        IntPtr hSourceHandle,
        IntPtr hTargetProcessHandle,
        out IntPtr targetHandle,
        uint dwDesiredAccess,
        bool bInheritHandle,
        uint dwOptions);

[DllImport("kernel32.dll")]
static extern IntPtr GetCurrentProcess();

[DllImport("ntdll.dll")]
static extern int NtQuerySystemInformation(
    int InfoClass,
    IntPtr Info,
    uint Size,
    out uint Length);

[DllImport("ntdll.dll")]
static extern int NtQueryObject(
    IntPtr objectHandle,
    int informationClass,
    IntPtr infomrationPtr,
    uint informationLength,
    out uint returnLength
);


static List<SYSTEM_HANDLE> GetAllHandles()
{
    
    var AllHandles = new List<SYSTEM_HANDLE>();
    
    int size = 1024 * 1024 * 8; // 4mb
    IntPtr buffer = Marshal.AllocHGlobal(size);
    int SystemHandleInformation = 16;
    int status = NtQuerySystemInformation(SystemHandleInformation, buffer, (uint)size, out uint returnLength);
    


    if (status == unchecked((int)0xC0000004)) // STATUS_INFO_LENGTH_MISMATCH
    {
        Marshal.FreeHGlobal(buffer);
        size = (int)returnLength + 0x10000; 
        buffer = Marshal.AllocHGlobal(size);
        status = NtQuerySystemInformation(SystemHandleInformation, buffer, (uint)size, out returnLength);
    }
    
    if (status != 0)
    {
        Marshal.FreeHGlobal(buffer);
        return AllHandles;
    }
    
    
    var handleCount = (uint)Marshal.ReadInt32(buffer);

    var structSize = Marshal.SizeOf<SYSTEM_HANDLE>();


    var handleArray = IntPtr.Add(buffer, 8); // skip the handlecount at the start of buffer byte array

    for (var i = 0; i < handleCount; i++)
    {
        var ptr = IntPtr.Add(handleArray, i * structSize); // get the ptr to our index (e.g 1st entry etc)
        
        var handle = Marshal.PtrToStructure<SYSTEM_HANDLE>(ptr); // convert to our struct
        
        AllHandles.Add(handle);
    }
    
    Marshal.FreeHGlobal(buffer);
    return AllHandles;
}

static void HandleNewProcess(int pId)
{
    const uint DUPLICATE_SAME_ACCESS = 0x00000002;
    const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
    const uint PROCESS_DUP_HANDLE = 0x0040;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const int ObjectNameInformation = 1;

    var handleName = @"\Sessions\1\BaseNamedObjects\ROBLOX_singletonEvent";
    
    
    var allProcesses = GetAllHandles();
    List<SYSTEM_HANDLE> wantedProcess = allProcesses.Where(x => x.ProcessId == pId).ToList();
    
    IntPtr handle = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION,  false, pId);

    if (handle == IntPtr.Zero)
    {
        int error = Marshal.GetLastWin32Error();
        Console.WriteLine("open process failed: "+ error);
        return;
    }

    foreach (var x in wantedProcess)
    {
        
        int size = 1024;
        IntPtr buffer = Marshal.AllocHGlobal(size);
        
        
        bool weGood = DuplicateHandle(
            handle,
            x.Handle,
            GetCurrentProcess(),
            out IntPtr result,
            0,
            false,
            DUPLICATE_SAME_ACCESS
        );

        if (!weGood)
        {
            Marshal.FreeHGlobal(buffer);
            continue; // handle failed, all good
        }
        int status = NtQueryObject(result, ObjectNameInformation, buffer, (uint)size, out uint returnLength);

        
        
        if (status == 0)
        {
            var nameInfo = Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(buffer);

            string name = "";
            if (nameInfo.Name.Buffer != IntPtr.Zero && nameInfo.Name.Length > 0)
            {
                name = Marshal.PtrToStringUni(nameInfo.Name.Buffer, nameInfo.Name.Length / 2);
            }

            Marshal.FreeHGlobal(buffer);

            if (!string.IsNullOrEmpty(name) && name.EndsWith(handleName))
            {
                DuplicateHandle(
                    handle,
                    x.Handle,
                    IntPtr.Zero,
                    out IntPtr unneededResult,
                    0,
                    false,
                    DUPLICATE_CLOSE_SOURCE);

                Console.WriteLine("Singleton handle removed on RobloxPlayerBeta with PID: " + pId);
                CloseHandle(result);
                CloseHandle(unneededResult);
                
                break;
            }

        }
        else
        {
            Console.WriteLine("error : {0}", Marshal.GetLastWin32Error());
            Marshal.FreeHGlobal(buffer);
        }
        
        
    }
    
    CloseHandle(handle);
    
    
}

var pName = "RobloxPlayerBeta.exe";
HashSet<int> alreadyProcessed = new();

var query = new WqlEventQuery(
    "SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '" + pName + "'");
var watcher = new ManagementEventWatcher(query);

watcher.EventArrived += (s, e) =>
{
    string reportedName = e.NewEvent.Properties["ProcessName"]?.Value?.ToString() ?? "NULL";

    int pId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
    Thread.Sleep(3000);
    var processes = Process.GetProcessesByName("RobloxPlayerBeta");
    foreach (var proc in processes)
    {
        if (alreadyProcessed.Contains(proc.Id)) continue;
        alreadyProcessed.Add(proc.Id);
        
        Console.WriteLine($"Resolved actual running PID: {proc.Id}");
        HandleNewProcess(proc.Id);
    }

};

watcher.Start();
Console.WriteLine("Watching rblx players, press ctrl+c to stop");
Thread.Sleep(Timeout.Infinite);











[StructLayout(LayoutKind.Sequential)]
struct SYSTEM_HANDLE
{
    public uint ProcessId;
    public byte ObjectTypeNumber;
    public byte Flags;
    public ushort Handle;
    public IntPtr Object;
    public uint GrantedAccess;
}

[StructLayout(LayoutKind.Sequential)]
struct UNICODE_STRING
{
    public ushort Length;
    public ushort MaximumLength;  
    public IntPtr Buffer;         
}

[StructLayout(LayoutKind.Sequential)]
struct OBJECT_NAME_INFORMATION
{
    public UNICODE_STRING Name;
}

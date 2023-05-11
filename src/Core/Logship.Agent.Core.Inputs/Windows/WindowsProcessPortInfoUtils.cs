using Logship.Agent.Core.Inputs.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Logship.Agent.Core.Inputs.Windows
{
    internal class WindowsProcessPortInfoUtils
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ulAf, int tcpTableType, uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, UDP_TABLE_CLASS tblClass, uint reserved);

        public static IReadOnlyList<ProcessInfoService.ProcessPortInfo> GetTcpPortInfo()
        {
            Dictionary<int, string> portsAndProcesses = new Dictionary<int, string>();
            int size = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref size, true, 2, 5, 0);

            IntPtr tcpTable = Marshal.AllocHGlobal(size);
            try
            {
                uint result = GetExtendedTcpTable(tcpTable, ref size, true, 2, 5, 0);
                if (result != 0)
                {
                    return Array.Empty<ProcessInfoService.ProcessPortInfo>();
                }

                MIB_TCPTABLE_OWNER_PID? tcpTableOwnerPid = (MIB_TCPTABLE_OWNER_PID?)Marshal.PtrToStructure(tcpTable, typeof(MIB_TCPTABLE_OWNER_PID));
                if (tcpTableOwnerPid == null)
                {
                    return Array.Empty<ProcessInfoService.ProcessPortInfo>();
                }

                return tcpTableOwnerPid.Value.Rows.Select(
                    t => new ProcessInfoService.ProcessPortInfo(t.dwLocalAddr, t.LocalPort, t.dwRemoteAddr, t.dwRemotePort, t.dwOwningPid)).ToArray();
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTable);
            }
        }

        public static IReadOnlyList<ProcessInfoService.ProcessPortInfo> GetUdpPortInfo()
        {
            var processPortMap = new Dictionary<int, string>();
            var udpTable = IntPtr.Zero;
            try
            {
                var bufferSize = 0;
                GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, 2 /*AF_INET*/, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                udpTable = Marshal.AllocHGlobal(bufferSize);
                var result = GetExtendedUdpTable(udpTable, ref bufferSize, true, 2 /*AF_INET*/, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                if (result != 0)
                {
                    throw new Exception($"Failed to retrieve UDP table with error code {result}");
                }

                var table = (MIB_UDPTABLE_OWNER_PID)Marshal.PtrToStructure(udpTable, typeof(MIB_UDPTABLE_OWNER_PID))!;
                return table.Rows.Select(t => new ProcessInfoService.ProcessPortInfo(t.localAddr, t.LocalPort, 0, 0, t.owningPid)).ToArray();
            }
            finally
            {
                if (udpTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(udpTable);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
            public uint dwOwningPid;

            public int ProcessId { get { return Convert.ToInt32(dwOwningPid); } }
            public ushort LocalPort { get { return Convert.ToUInt16((dwLocalPort >> 8) & 0xff | (dwLocalPort << 8) & 0xff00); } }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            private MIB_TCPROW_OWNER_PID table;

            public MIB_TCPROW_OWNER_PID[] Rows
            {
                get
                {
                    MIB_TCPROW_OWNER_PID[] result = new MIB_TCPROW_OWNER_PID[dwNumEntries];
                    var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<MIB_TCPROW_OWNER_PID>() * (int)dwNumEntries);
                    Marshal.StructureToPtr(table, ptr, false);
                    for (int i = 0; i < dwNumEntries; ++i)
                    {
                        result[i] = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(ptr + (i * Marshal.SizeOf<MIB_TCPROW_OWNER_PID>()), typeof(MIB_TCPROW_OWNER_PID))!;
                    }
                    
                    return result;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            public uint owningPid;

            public ushort LocalPort => BitConverter.ToUInt16(new byte[2] { localPort[1], localPort[0] }, 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            private readonly IntPtr table;

            public MIB_UDPROW_OWNER_PID[] Rows
            {
                get
                {
                    MIB_UDPROW_OWNER_PID[] result = new MIB_UDPROW_OWNER_PID[dwNumEntries];
                    var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<MIB_UDPROW_OWNER_PID>() * (int)dwNumEntries);
                    Marshal.StructureToPtr(table, ptr, false);
                    for (int i = 0; i < dwNumEntries; ++i)
                    {
                        result[i] = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(ptr + (i * Marshal.SizeOf<MIB_UDPROW_OWNER_PID>()), typeof(MIB_UDPROW_OWNER_PID))!;
                    }

                    return result;
                }
            }

        }

        private enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }
    }
}

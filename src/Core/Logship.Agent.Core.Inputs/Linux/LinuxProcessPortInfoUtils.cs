using Logship.Agent.Core.Events;
using Logship.Agent.Core.Inputs.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Logship.Agent.Core.Inputs.Linux
{
    internal static class LinuxProcessPortInfoUtils
    {
        public static async Task<IReadOnlyList<ProcessInfoService.ProcessPortInfo>> ListProcessesUsingTcpPortsAsync(CancellationToken token)
        {
            var tcpLines = File.ReadAllLinesAsync("/proc/net/tcp", token);
            Dictionary<long, uint> inodeToPid = GetInodePidMap();

            var processPorts = new List<ProcessInfoService.ProcessPortInfo>();
            foreach (var line in await tcpLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length <= 9)
                {
                    continue;
                }

                bool TryParseAddr(string input, out uint addr, out uint port)
                {
                    addr = port = 0;
                    var parts = input.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2
                        || false == uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out addr))
                    {
                        return false;
                    }

                    return uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out port);
                }

                if (false == TryParseAddr(fields[1], out var localAddr, out var localPort)
                    || false == TryParseAddr(fields[2], out var remoteAddr, out var remotePort)
                    || false == int.TryParse(fields[9], out var inode)
                    || false == inodeToPid.ContainsKey(inode))
                {
                    continue;
                }

                processPorts.Add(new ProcessInfoService.ProcessPortInfo(localAddr, localPort, remoteAddr, remotePort, inodeToPid[inode]));
            }

            return processPorts;
        }

        public static async Task<IReadOnlyList<ProcessInfoService.ProcessPortInfo>> ListProcessesUsingUdpPortsAsync(CancellationToken token)
        {
            var udpLines = File.ReadAllLinesAsync("/proc/net/udp", token);
            Dictionary<long, uint> inodeToPid = GetInodePidMap();

            var processPorts = new List<ProcessInfoService.ProcessPortInfo>();
            foreach (var line in await udpLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length <= 9)
                {
                    continue;
                }

                bool TryParseAddr(string input, out uint addr, out uint port)
                {
                    addr = port = 0;
                    var parts = input.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2
                        || false == uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out addr))
                    {
                        return false;
                    }

                    return uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out port);
                }

                if (false == TryParseAddr(fields[1], out var localAddr, out var localPort)
                    || false == TryParseAddr(fields[2], out var remoteAddr, out var remotePort)
                    || false == int.TryParse(fields[9], out var inode)
                    || false == inodeToPid.ContainsKey(inode))
                {
                    continue;
                }

                processPorts.Add(new ProcessInfoService.ProcessPortInfo(localAddr, localPort, remoteAddr, remotePort, inodeToPid[inode]));
            }

            return processPorts;
        }

        private static Dictionary<long, uint> GetInodePidMap()
        {
            var inodeToPid = new Dictionary<long, uint>();
            foreach (var dir in Directory.GetDirectories("/proc"))
            {
                if (uint.TryParse(Path.GetFileName(dir), out var pid))
                {
                    var inodes = GetPidInodes(pid);
                    foreach (var inode in inodes)
                    {
                        inodeToPid[inode] = pid;
                    }
                }
            }

            return inodeToPid;
        }

        private static HashSet<long> GetPidInodes(uint pid)
        {
            var inodes = new HashSet<long>();
            string fdPath = $"/proc/{pid}/fd";
            string[] files = Directory.GetFiles(fdPath);
            foreach (string file in files)
            {
                string target = GetSymbolicLinkTarget(file);
                if (target?.StartsWith("socket:[") == true)
                {
                    int index = target.IndexOf("[") + 1;
                    int length = target.IndexOf("]") - index;
                    string inodeString = target.Substring(index, length);
                    if (long.TryParse(inodeString, out var result))
                    {
                        inodes.Add(result);
                    }
                }
            }

            return inodes;
        }

        private static string GetSymbolicLinkTarget(string path)
        {
            string target = Path.GetFullPath(path);
            if (File.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                var info = new FileInfo(target);
                target = info.LinkTarget!;
            }

            return target;
        }
    }
}

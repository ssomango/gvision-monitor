using GVisionWpf.Models.UiModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace GVisionWpf.Utils
{
    public class SystemScanner
    {
        private static readonly Lazy<SystemScanner> lazy = new Lazy<SystemScanner>(() => new SystemScanner());
        public static SystemScanner Instance => lazy.Value;

        private readonly PerformanceCounter cpuCounter;
        private readonly ManagementObjectSearcher wmi = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

        private SystemScanner()
        {
            this.cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        public ObservableCollection<HardDiskInfo> GetHddInfo()
        {
            DriveInfo[] hddDrives = DriveInfo.GetDrives();
            ObservableCollection<HardDiskInfo> tmpHdd = new ObservableCollection<HardDiskInfo>();
            foreach (DriveInfo drive in hddDrives)
            {
                if (drive.DriveType != DriveType.Fixed)
                {
                    continue;
                }

                HardDiskInfo hdd = new HardDiskInfo
                {
                    Name = drive.Name,
                    TotalSize = drive.TotalSize / 1024 / 1024 / 1024,
                    FreeSize = drive.AvailableFreeSpace / 1024 / 1024 / 1024,
                    UseSize = (drive.TotalSize - drive.AvailableFreeSpace) / 1024 / 1024 / 1024
                };

                tmpHdd.Add(hdd);
            }

            return tmpHdd;
        }

        public MemoryInfo GetMemoryInfo()
        {
            MemoryInfo info = this.wmi.Get().Cast<ManagementObject>().Select(mo => new MemoryInfo()
            {
                TotalSize = Math.Round(double.Parse(mo["TotalVisibleMemorySize"].ToString()) / 1024 / 1024, 2),
                FreeSize = Math.Round(double.Parse(mo["FreePhysicalMemory"].ToString()) / 1024 / 1024, 2),
                UseSize = Math.Round((double.Parse(mo["TotalVisibleMemorySize"].ToString()) - double.Parse(mo["FreePhysicalMemory"].ToString())) / 1024 / 1024, 2)
            }).FirstOrDefault() ?? throw new ArgumentNullException("wmi.Get().Cast<ManagementObject>().Select(mo => new MemoryInfo() { TotalSize = Math.Round(double.Parse(mo[\"TotalVisibleMemorySize\"].ToString()) / 1024 / 1024, 2), FreeSize = Math.Round(double.Parse(mo[\"FreePhysicalMemory\"].ToString()) / 1024 / 1024, 2), UseSize = Math.Round((double.Parse(mo[\"TotalVisibleMemorySize\"].ToString()) - double.Parse(mo[\"FreePhysicalMemory\"].ToString())) / 1024 / 1024, 2) }).FirstOrDefault()");

            return info;
        }

        public double GetCpuUsage()
        {
            this.cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            return this.cpuCounter.NextValue();
        }
    }
}
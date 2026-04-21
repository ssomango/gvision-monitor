using GVisionWpf.Models.UiModels;
using System.Threading.Tasks;

namespace GVisionWpf.UIs.ViewModels
{
    public class SystemUsageMonitorViewModel : ViewModelBase
    {
        private int cpuUsage;
        private int ramUsage;

        #region Property

        public int CpuUsage
        {
            get => this.cpuUsage;
            set => SetField(ref this.cpuUsage, value);
        }

        public int RamUsage
        {
            get => this.ramUsage;
            set => SetField(ref this.ramUsage, value);
        }

        #endregion

        public SystemUsageMonitorViewModel()
        {
            new Task(() =>
            {
                while (true)
                {
                    CpuUsage = (int)SystemScanner.Instance.GetCpuUsage();
                    MemoryInfo memoryInfo = SystemScanner.Instance.GetMemoryInfo();
                    RamUsage = (int)(100 * memoryInfo.UseSize / memoryInfo.TotalSize);
                    Task.Delay(100);
                }
            }).Start();
        }
    }
}
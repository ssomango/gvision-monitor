using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GVisionWpf.GlobalStates;
using GVisionWpf.Types;
namespace GVisionWpf.UIs.ViewModels
{
    class SelectCameraViewModel : ViewModelBase
    {
        public ObservableCollection<ECamera> CameraList { get; set; }

        public SelectCameraViewModel() : this(Enum.GetValues(typeof(ECamera)).Cast<ECamera>().ToList()) {}

        public SelectCameraViewModel(List<ECamera> cameras)
        {
            CameraList = new ObservableCollection<ECamera>(cameras);
        }
    }
}

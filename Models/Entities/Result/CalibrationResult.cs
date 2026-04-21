namespace GVisionWpf.Models.Entities.Result
{
    public class CalibrationResult
    {
        public ECalibration CalibrationType;
        public ECamera CameraType;
        public HObject? Image;
        public HObject? Region;
        public Pose TargetPose; // 찾은 중심
        public Point? ImageCenterPoint; // 이미지 중심
        public bool IsFound;
        public Pose Offset;

        public override string ToString()
        {
            return $"[{this.CalibrationType}] {this.CameraType} {(this.IsFound ? "(" + this.Offset + ")" : "Failed")}";
        }
    }
}
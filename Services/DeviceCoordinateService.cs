using GVisionWpf.Models.Entities.Result;
using GVisionWpf.Models.UiModels;

namespace GVisionWpf.Services
{
    public class DeviceCoordinateService
    {
        private Device device;

        public TableLayout FOVSize => device.FovSize;

        public TableLayout VisionTableSize => device.TraySize;

        public DeviceCoordinateService(Device device) => this.device = device;

        public bool IsInVisionTable(int xPositionForGrid, int yPositionForGrid)
        {
            return device.TraySize.Row > yPositionForGrid
                && device.TraySize.Col > xPositionForGrid;
        }

        public bool IsInBlock(TableLayout lastCoordinate, int xPositionForGrid, int yPositionForGrid)
        {
            return lastCoordinate.Row >= yPositionForGrid 
                && lastCoordinate.Col >= xPositionForGrid;
        }

        public void CalculateFovPos(int xVTPosition, int yVTPosition, InspectionResult result, TableLayout fovSize, out int xPositionForGrid, out int yPositionForGrid)
        {
            this.CalculateNthOfTheBlock(yVTPosition, xVTPosition, out int nthOfY, out int nthOfX);

            this.CalculateNMissing(out int nXMissing, out int nYMissing);

            xPositionForGrid = (xVTPosition * fovSize.Col) + result.XPosition - ((nthOfX - 1) * nXMissing);
            yPositionForGrid = (yVTPosition * fovSize.Row) + result.YPosition - ((nthOfY - 1) * nYMissing);
        }

        public void CalculateFovPos(int xVTPosition, int yVTPosition, int XPositionInFOV, int YPositionInFOV, TableLayout fovSize, out int xPositionForGrid, out int yPositionForGrid)
        {
            CalculateNthOfTheBlock(yVTPosition, xVTPosition, out int nthOfY, out int nthOfX);

            CalculateNMissing(out int nXMissing, out int nYMissing);

            xPositionForGrid = (xVTPosition * fovSize.Col) + XPositionInFOV - ((nthOfX - 1) * nXMissing);
            yPositionForGrid = (yVTPosition * fovSize.Row) + YPositionInFOV - ((nthOfY - 1) * nYMissing);
        }

        public void CalculateNMissing(out int nXMissing, out int nYMissing)
        {
            TableLayout tableSize = device.TraySize;
            TableLayout blockSize = device.BlockSize;
            TableLayout fovSize = device.FovSize;

            int nYDevices = tableSize.Row / blockSize.Row;
            int nXDevices = tableSize.Col / blockSize.Col;

            nYMissing = (fovSize.Row - nYDevices % fovSize.Row) % fovSize.Row;
            nXMissing = (fovSize.Col - nXDevices % fovSize.Col) % fovSize.Col;
        }

        public void CalculateNTotalFov(out int nTotalYFov, out int nTotalXFov)
        {
            TableLayout tableSize = device.TraySize;
            TableLayout fovSize = device.FovSize;

            nTotalYFov = (int)Math.Ceiling((double)tableSize.Row / fovSize.Row);
            nTotalXFov = (int)Math.Ceiling((double)tableSize.Col / fovSize.Col);
        }

        public void CalculateNFovForABlock(out int nYFovForABlock, out int nXFovForABlock)
        {
            TableLayout blockSize = device.BlockSize;

            CalculateNTotalFov(out int nTotalYFov, out int nTotalXFov);

            nYFovForABlock = (int)Math.Ceiling((double)nTotalYFov / blockSize.Row);
            nXFovForABlock = (int)Math.Ceiling((double)nTotalXFov / blockSize.Col);
        }

        public void CalculateNthOfTheBlock(int yPosition, int xPosition, out int nthOfY, out int nthOfX)
        {
            CalculateNFovForABlock(out int nYFovForABlock, out int nXFovForABlock);

            nthOfY = (int)(yPosition / nYFovForABlock) + 1;
            nthOfX = (int)(xPosition / nXFovForABlock) + 1;
        }

        public TableLayout GetLastCoordinateOfBlock(int xPosition, int yPosition)
        {
            TableLayout tableSize = device.TraySize;
            TableLayout blockSize = device.BlockSize;

            CalculateNthOfTheBlock(yPosition, xPosition, out int nthOfY, out int nthOfX);

            int y = nthOfY * (tableSize.Row / blockSize.Row) - 1;
            int x = nthOfX * (tableSize.Col / blockSize.Col) - 1;

            return new TableLayout(y, x);
        }
    }
}
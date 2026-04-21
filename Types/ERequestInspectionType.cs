namespace GVisionWpf.Types
{
    public enum ERequestInspectionType
    {
        MAP_INSPECTION = 1,
        BOTTOM_INSPECTION = 10,
        PAD_PITCH = 11,
        BOTTOM_ZIG = 12,

        X1_ZIG = 20,
        X2_ZIG = 30,

        X1_GRID_TABLE_CAL = 21,
        X1_TRAY_TRANSFER_CAL = 22,

        X2_GRID_TABLE_CAL = 31,
        X2_TRAY_TRANSFER_CAL = 32,

        TOP_BARCODE = 40,
        BOTTOM_BARCODE = 41,

        STRIP_HALL = 43,
        FIDUCIAL = 44
    }
}

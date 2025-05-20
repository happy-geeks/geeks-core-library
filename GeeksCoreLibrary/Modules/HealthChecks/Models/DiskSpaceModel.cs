using GeeksCoreLibrary.Core.Helpers;

namespace GeeksCoreLibrary.Modules.HealthChecks.Models;

/// <summary>
/// A model for storing information about disk space usage of a system.
/// </summary>
public class DiskSpaceModel
{
    /// <summary>
    /// The total size of the disk, in bytes.
    /// </summary>
    public double TotalSize { get; set; }

    /// <summary>
    /// The available space on the disk, in bytes.
    /// </summary>
    public double AvailableSpace { get; set; }

    /// <summary>
    /// The used space on the disk, in bytes.
    /// </summary>
    public double UsedSpace { get; set; }

    public double TotalSizeInKiloBytes => TotalSize * 1024;
    public double AvailableSpaceInKiloBytes => AvailableSpace * 1024;
    public double UsedSpaceInKiloBytes => UsedSpace * 1024;

    public double TotalSizeInMegaBytes => TotalSizeInKiloBytes * 1024;
    public double AvailableSpaceInMegaBytes => AvailableSpaceInKiloBytes * 1024;
    public double UsedSpaceInMegaBytes => UsedSpaceInKiloBytes * 1024;

    public double TotalSizeInGigaBytes => TotalSizeInMegaBytes * 1024;
    public double AvailableSpaceInGigaBytes => AvailableSpaceInMegaBytes * 1024;
    public double UsedSpaceInGigaBytes => UsedSpaceInMegaBytes * 1024;

    public double TotalSizeInTeraBytes => TotalSizeInGigaBytes * 1024;
    public double AvailableSpaceInTeraBytes => AvailableSpaceInGigaBytes * 1024;
    public double UsedSpaceInTeraBytes => UsedSpaceInGigaBytes * 1024;

    public string FormattedTotalSize => NumberHelpers.FormatByteSize(TotalSize);
    public string FormattedAvailableSpace => NumberHelpers.FormatByteSize(AvailableSpace);
    public string FormattedUsedSpace => NumberHelpers.FormatByteSize(UsedSpace);
}

public static class Format
{
}
using AceHeadsetFix.Adb;
using AceHeadsetFix.Headset;
using Xunit;

namespace AceHeadsetFix.Tests;

public class DeviceListParserTests
{
    [Fact]
    public void ParsesReadyQuestWithModel()
    {
        var devices = DeviceListParser.Parse("List of devices attached\n2G97C5ZH3C017L         device product:eureka model:Quest_3 device:eureka transport_id:1\n\n");

        var device = Assert.Single(devices);
        Assert.Equal("2G97C5ZH3C017L", device.Serial);
        Assert.Equal(AdbDeviceState.Ready, device.State);
        Assert.Equal("Quest 3", device.Model);
    }

    [Theory]
    [InlineData("unauthorized", AdbDeviceState.Unauthorized)]
    [InlineData("offline", AdbDeviceState.Offline)]
    [InlineData("recovery", AdbDeviceState.Other)]
    public void ParsesNonReadyStates(string state, AdbDeviceState expected)
    {
        var devices = DeviceListParser.Parse($"List of devices attached\nSERIAL {state} transport_id:2\n");

        Assert.Equal(expected, Assert.Single(devices).State);
    }

    [Fact]
    public void IgnoresDaemonNoiseAndBlankLines()
    {
        var devices = DeviceListParser.Parse("* daemon not running; starting now at tcp:5037\n* daemon started successfully\nList of devices attached\n\n");

        Assert.Empty(devices);
    }
}

public class AcePackageInspectorTests
{
    const string Dumpsys = "Packages:\n  Package [com.Ace.Ace] (abc):\n    appId=10154\n    versionCode=1619 minSdk=32 targetSdk=34\n    versionName=0.20.3\n";

    [Fact]
    public void ReadsVersionAndAppUser()
    {
        var package = AcePackageInspector.Parse(Dumpsys);

        Assert.True(package.Installed);
        Assert.Equal(1619, package.VersionCode);
        Assert.Equal("0.20.3", package.VersionName);
        Assert.Equal("u0_a154", package.AppUser);
    }

    [Fact]
    public void MissingPackageIsNotInstalled()
    {
        var package = AcePackageInspector.Parse("Unable to find package: com.Ace.Ace\n");

        Assert.False(package.Installed);
    }
}

public class ObbInspectorTests
{
    const string Listing =
        "/sdcard/Android/obb/com.Ace.Ace/main.1619.com.Ace.Ace.obb|u0_a50|ext_obb_rw|660|2834919743\n" +
        "/sdcard/Android/obb/com.Ace.Ace/update|u0_a154|ext_obb_rw|2770|4096\n";

    [Fact]
    public void ParsesStatLines()
    {
        var entries = ObbInspector.Parse(Listing);

        Assert.Equal(2, entries.Count);
        Assert.Equal("main.1619.com.Ace.Ace.obb", entries[0].FileName);
        Assert.Equal("u0_a50", entries[0].Owner);
        Assert.Equal(2834919743, entries[0].Size);
        Assert.False(entries[0].WorldReadable);
    }

    [Theory]
    [InlineData("660", false)]
    [InlineData("666", true)]
    [InlineData("644", true)]
    [InlineData("2770", false)]
    public void WorldReadableFollowsOthersReadBit(string mode, bool expected)
    {
        var entry = new ObbEntry("/x/main.1.com.Ace.Ace.obb", "shell", "ext_obb_rw", mode, 1);

        Assert.Equal(expected, entry.WorldReadable);
    }

    [Fact]
    public void IgnoresErrorLines()
    {
        var entries = ObbInspector.Parse("stat: '/sdcard/Android/obb/com.Ace.Ace/*': No such file or directory\n");

        Assert.Empty(entries);
    }

    [Fact]
    public void ExpectedNameUsesVersionCode()
    {
        Assert.Equal("main.1619.com.Ace.Ace.obb", ObbInspector.ExpectedFileName(1619));
    }
}

public class ObbRepairParsingTests
{
    [Fact]
    public void ReadsAvailableKilobytesFromDf()
    {
        var bytes = ObbRepair.ParseAvailableBytes("Filesystem     1K-blocks    Used Available Use% Mounted on\n/dev/fuse      470432388 8505364 461779568   2% /storage/emulated\n");

        Assert.Equal(461779568L * 1024, bytes);
    }

    [Fact]
    public void UnparseableDfDoesNotBlockTheFix()
    {
        Assert.Equal(long.MaxValue, ObbRepair.ParseAvailableBytes("df: /sdcard: Permission denied\n"));
    }

    [Theory]
    [InlineData("2bb20aace48ad9a1ce57fcb6a49de2c0  /a\n2bb20aace48ad9a1ce57fcb6a49de2c0  /b\n", true)]
    [InlineData("2bb20aace48ad9a1ce57fcb6a49de2c0  /a\n00000000000000000000000000000000  /b\n", false)]
    [InlineData("2bb20aace48ad9a1ce57fcb6a49de2c0  /a\n", false)]
    public void ChecksumsMustBothExistAndMatch(string output, bool expected)
    {
        Assert.Equal(expected, ObbRepair.ChecksumsMatch(output));
    }
}

public class LaunchVerifierTests
{
    [Fact]
    public void ContentFailureWinsOverVrMarkers()
    {
        var log = "E Unity   : ApkAddCentralDirectory : Unable to open '/storage/emulated/0/Android/obb/com.Ace.Ace/main.1619.com.Ace.Ace.obb'\nV OpenXR-Loader: xrCreateInstance\n";

        Assert.Equal(LaunchOutcome.ContentUnreadable, LaunchVerifier.Classify(log));
    }

    [Fact]
    public void SessionStartMeansVr()
    {
        Assert.Equal(LaunchOutcome.ReachedVr, LaunchVerifier.Classify("I OpenXR-Loader: xrBeginSession\n"));
    }

    [Fact]
    public void NoMarkersIsUndetermined()
    {
        Assert.Equal(LaunchOutcome.Undetermined, LaunchVerifier.Classify("I Unity: Preload scene loaded\n"));
    }
}

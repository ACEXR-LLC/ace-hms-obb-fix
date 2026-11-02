using AceHeadsetFix.Usb;
using Xunit;

namespace AceHeadsetFix.Tests;

public class HeadsetUsbProbeTests
{
    const string Parent = @"USB\VID_2833&PID_5013\2G97C5ZH3C017L";

    static HeadsetUsbProbe.UsbEntry Interface(int index, string name, uint errorCode = 0) =>
        new($@"USB\VID_2833&PID_5013&MI_0{index}\6&21E57EC8&1&000{index}", name, errorCode);

    [Fact]
    public void NothingPluggedIn()
    {
        var report = HeadsetUsbProbe.Classify([]);

        Assert.Equal(UsbHeadsetState.NotSeen, report.State);
        Assert.Equal(0, report.HeadsetCount);
    }

    [Fact]
    public void ThreeInterfacesWithoutAdbMeansDebuggingOff()
    {
        var report = HeadsetUsbProbe.Classify([
            new HeadsetUsbProbe.UsbEntry(Parent, "USB Composite Device", 0),
            Interface(0, "Reality Labs Composite XRSP Interface"),
            Interface(1, "Reality Labs Composite Commlib Interface"),
            Interface(2, "Reality Labs Composite Highwind Interface"),
        ]);

        Assert.Equal(UsbHeadsetState.DebuggingOff, report.State);
        Assert.Equal(1, report.HeadsetCount);
    }

    [Fact]
    public void WorkingAdbInterfaceMeansDebuggingOn()
    {
        var report = HeadsetUsbProbe.Classify([
            new HeadsetUsbProbe.UsbEntry(Parent, "USB Composite Device", 0),
            Interface(0, "Reality Labs Composite XRSP Interface"),
            Interface(3, "Reality Labs Composite ADB Interface"),
        ]);

        Assert.Equal(UsbHeadsetState.DebuggingOn, report.State);
    }

    [Fact]
    public void AdbInterfaceWithoutDriverIsADriverProblem()
    {
        var report = HeadsetUsbProbe.Classify([
            new HeadsetUsbProbe.UsbEntry(Parent, "USB Composite Device", 0),
            Interface(3, "ADB Interface", 28),
        ]);

        Assert.Equal(UsbHeadsetState.DriverProblem, report.State);
        Assert.Contains("28", report.Detail);
    }

    [Fact]
    public void FourUnnamedInterfacesCountAsDebuggingOn()
    {
        var report = HeadsetUsbProbe.Classify([
            new HeadsetUsbProbe.UsbEntry(Parent, "USB Composite Device", 0),
            Interface(0, "Unknown"),
            Interface(1, "Unknown"),
            Interface(2, "Unknown"),
            Interface(3, "Unknown"),
        ]);

        Assert.Equal(UsbHeadsetState.DebuggingOn, report.State);
    }

    [Fact]
    public void BrokenNonAdbInterfaceWhileDebuggingOffIsStillDebuggingOff()
    {
        var report = HeadsetUsbProbe.Classify([
            new HeadsetUsbProbe.UsbEntry(Parent, "USB Composite Device", 0),
            Interface(0, "XRSP", 28),
            Interface(1, "Commlib", 28),
            Interface(2, "Highwind", 28),
        ]);

        Assert.Equal(UsbHeadsetState.DebuggingOff, report.State);
    }
}

using AceHeadsetFix.Adb;
using Xunit;

namespace AceHeadsetFix.Tests;

public class AdbInstallerNetworkTests
{
    [Fact]
    [Trait("Category", "Network")]
    public void DownloadsPlatformToolsIntoAFreshFolderAndFindsAdb()
    {
        var folder = Path.Combine(Path.GetTempPath(), "AceHeadsetFix.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var reported = new List<int>();
            var adbPath = new AdbInstaller(folder).InstallAsync(reported.Add, CancellationToken.None).GetAwaiter().GetResult();

            Assert.True(File.Exists(adbPath));
            Assert.Equal(adbPath, AdbLocator.Find(folder));
            Assert.Contains(reported, percent => percent >= 75);
            Assert.False(File.Exists(Path.Combine(folder, "platform-tools-download.zip")));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}

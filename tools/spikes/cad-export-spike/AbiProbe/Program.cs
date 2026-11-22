using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace CadExportSpike.AbiProbe;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<int> Main(string[] args)
    {
        string outPath = args.Length >= 2 && args[0] == "--out"
            ? args[1]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "proof", "spike-cad-01", "abi-probe.json"));

        string nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        var hostInfo = new
        {
            osDescription = Scrub(RuntimeInformation.OSDescription),
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            runtimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            frameworkDescription = RuntimeInformation.FrameworkDescription,
            capturedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var rhino3dmPackage = InspectLocalPackage(nugetRoot, "rhino3dm", "8.35.0");
        var rhino3dmLiveLoad = RunRhino3dmLiveProbe();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CFD-Workbench-SPIKE-CAD-01/1.0");

        var occtNugetPackages = new List<object>();
        foreach (var (pkgId, pkgVer) in new[]
        {
            ("Occt.NET", "7.9.0"),
            ("Xbim.Geometry.Occt", "7.8.1"),
            ("OCCT.WIN64", "7.8.1.3"),
            ("Ab4d.OpenCascade", "1.0.0")
        })
        {
            occtNugetPackages.Add(await InspectNuGetRemotePackageAsync(http, pkgId, pkgVer));
        }

        var hostTools = ProbeHostTools();

        var receipt = new
        {
            spike = "SPIKE-CAD-01",
            part = "Part 1 - Empirical win-arm64 ABI & Packaging Probe",
            host = hostInfo,
            rhino3dm = new
            {
                packageId = "Rhino3dm",
                version = "8.35.0",
                license = rhino3dmPackage.License,
                hasWinArm64Native = rhino3dmPackage.HasWinArm64,
                hasWinX64Native = rhino3dmPackage.HasWinX64,
                hasOsxArm64OrUniversalNative = rhino3dmPackage.HasOsxArm64OrUniversal,
                nativeAssets = rhino3dmPackage.NativeAssets,
                managedAssets = rhino3dmPackage.ManagedAssets,
                liveLoadWinArm64 = rhino3dmLiveLoad
            },
            occt = new
            {
                localHostTools = hostTools,
                nugetPackagesInspected = occtNugetPackages,
                brepCheckAnalyzerStatus = hostTools.FreeCadInstalled || hostTools.OcctInstalled
                    ? "Available locally"
                    : "Flagged / deferred to SPIKE-CAD-02 (no local OCCT or FreeCAD STEP reader installed on this Windows ARM64 host)"
            }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        string json = JsonSerializer.Serialize(receipt, JsonOptions) + "\n";
        await File.WriteAllTextAsync(outPath, json, new UTF8Encoding(false));
        Console.WriteLine($"ABI probe receipt written to: {Scrub(outPath)}");
        Console.WriteLine($"Rhino3dm 8.35.0: win-arm64 asset={rhino3dmPackage.HasWinArm64}, win-x64={rhino3dmPackage.HasWinX64}, osx={rhino3dmPackage.HasOsxArm64OrUniversal}, liveLoadSucceeded={rhino3dmLiveLoad.Succeeded}");
        if (!rhino3dmLiveLoad.Succeeded)
            Console.WriteLine($"Rhino3dm live load error: {rhino3dmLiveLoad.ExceptionType}: {rhino3dmLiveLoad.ErrorMessage}");
        return 0;
    }

    private sealed record PackageAsset(string RelativePath, long Bytes, string BinaryFormat, string[] Architectures);

    private sealed record LocalPackageReport(
        string License,
        bool HasWinArm64,
        bool HasWinX64,
        bool HasOsxArm64OrUniversal,
        List<PackageAsset> NativeAssets,
        List<string> ManagedAssets);

    private static LocalPackageReport InspectLocalPackage(string nugetRoot, string idLower, string version)
    {
        string pkgDir = Path.Combine(nugetRoot, idLower, version);
        string license = "unknown";
        var nativeAssets = new List<PackageAsset>();
        var managedAssets = new List<string>();

        if (!Directory.Exists(pkgDir))
            return new LocalPackageReport("not-restored", false, false, false, nativeAssets, managedAssets);

        string? nuspecPath = Directory.EnumerateFiles(pkgDir, "*.nuspec", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (nuspecPath is not null)
            license = ExtractNuspecLicense(File.ReadAllText(nuspecPath));

        foreach (string file in Directory.EnumerateFiles(pkgDir, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            string rel = Path.GetRelativePath(pkgDir, file).Replace('\\', '/');
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (rel.StartsWith("runtimes/", StringComparison.OrdinalIgnoreCase) || ext is ".dll" or ".dylib" or ".so" or ".lib")
            {
                if (rel.StartsWith("lib/", StringComparison.OrdinalIgnoreCase))
                {
                    managedAssets.Add(rel);
                    continue;
                }
                if (ext is ".dll" or ".dylib" or ".so")
                {
                    byte[] header = ReadHeader(file, 4096);
                    var (fmt, archs) = DetectBinaryArch(header);
                    nativeAssets.Add(new PackageAsset(rel, new FileInfo(file).Length, fmt, archs));
                }
            }
        }

        bool hasWinArm64 = nativeAssets.Any(a =>
            a.RelativePath.Contains("win-arm64", StringComparison.OrdinalIgnoreCase) ||
            (a.RelativePath.Contains("win", StringComparison.OrdinalIgnoreCase) && a.Architectures.Contains("ARM64")));
        bool hasWinX64 = nativeAssets.Any(a =>
            a.RelativePath.Contains("win-x64", StringComparison.OrdinalIgnoreCase) ||
            (a.RelativePath.Contains("win", StringComparison.OrdinalIgnoreCase) && a.Architectures.Contains("x64")));
        bool hasOsxArm64 = nativeAssets.Any(a =>
            a.RelativePath.Contains("osx", StringComparison.OrdinalIgnoreCase) &&
            (a.Architectures.Contains("ARM64") || a.RelativePath.Contains("osx-arm64", StringComparison.OrdinalIgnoreCase)));

        return new LocalPackageReport(license, hasWinArm64, hasWinX64, hasOsxArm64, nativeAssets, managedAssets);
    }

    private sealed record LiveProbeResult(
        bool Succeeded,
        string? ExceptionType,
        string? InnerExceptionType,
        string? ErrorMessage,
        double ElapsedMs,
        string? UnitSystemReadBack,
        int? LayerCountReadBack,
        string[]? LayerNamesReadBack,
        int? ObjectCountReadBack,
        int? SurfaceDegreeU,
        int? SurfaceDegreeV,
        long? WrittenBytes,
        bool? ZeroPiiVerified);

    private static LiveProbeResult RunRhino3dmLiveProbe()
    {
        var sw = Stopwatch.StartNew();
        string temp3dm = Path.Combine(Path.GetTempPath(), $"cfdw-spike-cad-01-{Guid.NewGuid():N}.3dm");
        try
        {
            using var file = new Rhino.FileIO.File3dm();
            file.Settings.ModelUnitSystem = Rhino.UnitSystem.Millimeters;
            file.ApplicationName = "CFD Workbench SPIKE-CAD-01";
            file.ApplicationDetails = "Zero-PII empirical ABI probe";
            file.Notes = new Rhino.FileIO.File3dmNotes { Notes = "SPIKE-CAD-01 synthetic wing patch (mm, unscaled)" };

            int skinLayer = file.AllLayers.AddLayer("Wing Skin", System.Drawing.Color.SteelBlue);
            int masterLayer = file.AllLayers.AddLayer("Master Curves", System.Drawing.Color.DarkOrange);
            int stationLayer = file.AllLayers.AddLayer("Authored Stations", System.Drawing.Color.ForestGreen);

            var srf = Rhino.Geometry.NurbsSurface.Create(3, false, 3, 3, 3, 3);
            if (srf is null)
                throw new InvalidOperationException("NurbsSurface.Create returned null");
            double[] ys = [0, 225, 450];
            for (int v = 0; v < 3; v++)
            {
                srf.Points.SetPoint(0, v, new Rhino.Geometry.Point3d(0, ys[v], 0));
                srf.Points.SetPoint(1, v, new Rhino.Geometry.Point3d(60, ys[v], 10));
                srf.Points.SetPoint(2, v, new Rhino.Geometry.Point3d(120, ys[v], 0));
            }
            srf.KnotsU.CreateUniformKnots(1.0);
            srf.KnotsV.CreateUniformKnots(1.0);

            var skinAttr = new Rhino.DocObjects.ObjectAttributes { LayerIndex = skinLayer, Name = "UpperSkin" };
            file.Objects.AddSurface(srf, skinAttr);

            var masterAttr = new Rhino.DocObjects.ObjectAttributes { LayerIndex = masterLayer, Name = "LeadingEdge" };
            var leCurve = Rhino.Geometry.NurbsCurve.Create(false, 2, new[] { new Rhino.Geometry.Point3d(0, 0, 0), new Rhino.Geometry.Point3d(0, 225, 0), new Rhino.Geometry.Point3d(0, 450, 0) });
            file.Objects.AddCurve(leCurve, masterAttr);

            var stationAttr = new Rhino.DocObjects.ObjectAttributes { LayerIndex = stationLayer, Name = "RootStation" };
            var stCurve = Rhino.Geometry.NurbsCurve.Create(false, 2, new[] { new Rhino.Geometry.Point3d(0, 0, 0), new Rhino.Geometry.Point3d(60, 0, 10), new Rhino.Geometry.Point3d(120, 0, 0) });
            file.Objects.AddCurve(stCurve, stationAttr);

            bool written = file.Write(temp3dm, 8);
            if (!written)
                throw new InvalidOperationException("File3dm.Write returned false");

            byte[] raw = File.ReadAllBytes(temp3dm);
            bool zeroPii = CheckBytesZeroPii(raw);

            using var readBack = Rhino.FileIO.File3dm.Read(temp3dm);
            string unit = readBack.Settings.ModelUnitSystem.ToString();
            string[] layers = readBack.AllLayers.Select(l => l.Name).ToArray();
            var srfObj = readBack.Objects.Select(o => o.Geometry).OfType<Rhino.Geometry.NurbsSurface>().FirstOrDefault();

            sw.Stop();
            return new LiveProbeResult(
                true,
                null,
                null,
                null,
                Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                unit,
                layers.Length,
                layers,
                readBack.Objects.Count,
                srfObj?.Degree(0),
                srfObj?.Degree(1),
                raw.Length,
                zeroPii);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new LiveProbeResult(
                false,
                ex.GetType().FullName,
                ex.InnerException?.GetType().FullName,
                Scrub(ex.InnerException?.Message ?? ex.Message),
                Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                null, null, null, null, null, null, null, null);
        }
        finally
        {
            if (File.Exists(temp3dm))
                File.Delete(temp3dm);
        }
    }

    private static bool CheckBytesZeroPii(byte[] raw)
    {
        string ascii = Encoding.Latin1.GetString(raw);
        string user = Environment.UserName;
        string host = Environment.MachineName;
        if (!string.IsNullOrEmpty(user) && user.Length >= 3 && ascii.Contains(user, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(host) && host.Length >= 3 && ascii.Contains(host, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static async Task<object> InspectNuGetRemotePackageAsync(HttpClient http, string packageId, string version)
    {
        string lowerId = packageId.ToLowerInvariant();
        string lowerVer = version.ToLowerInvariant();
        string nupkgUrl = $"https://api.nuget.org/v3-flatcontainer/{lowerId}/{lowerVer}/{lowerId}.{lowerVer}.nupkg";
        string nuspecUrl = $"https://api.nuget.org/v3-flatcontainer/{lowerId}/{lowerVer}/{lowerId}.nuspec";

        try
        {
            string nuspec = await http.GetStringAsync(nuspecUrl);
            string license = ExtractNuspecLicense(nuspec);
            string description = ExtractNuspecTag(nuspec, "description");

            using var resp = await http.GetAsync(nupkgUrl, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            long? contentLen = resp.Content.Headers.ContentLength;

            // Only download packages <= 80 MB to inspect zip entries in temp memory/file; otherwise record size & nuspec.
            if (contentLen is > 80 * 1024 * 1024)
            {
                return new
                {
                    packageId,
                    version,
                    license,
                    nupkgBytes = contentLen,
                    inspectedEntries = false,
                    note = "Skipped full download (> 80 MB) during spike; license & metadata inspected from .nuspec",
                    description = Truncate(description, 180)
                };
            }

            string tmpZip = Path.Combine(Path.GetTempPath(), $"cfdw-nupkg-{lowerId}-{Guid.NewGuid():N}.zip");
            try
            {
                await using (var fs = File.Create(tmpZip))
                await using (var stream = await resp.Content.ReadAsStreamAsync())
                    await stream.CopyToAsync(fs);

                using var archive = ZipFile.OpenRead(tmpZip);
                var allEntries = archive.Entries.Select(e => e.FullName).ToList();
                var nativeSummary = new List<object>();
                int inspectedBinaries = 0;
                foreach (var entry in archive.Entries.OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
                {
                    string ext = Path.GetExtension(entry.FullName).ToLowerInvariant();
                    if (ext is ".dll" or ".dylib" or ".so")
                    {
                        string fmt = "unknown";
                        string[] archs = [];
                        if (inspectedBinaries < 12)
                        {
                            using var es = entry.Open();
                            byte[] buf = ReadStreamHeader(es, 4096);
                            (fmt, archs) = DetectBinaryArch(buf);
                            inspectedBinaries++;
                        }
                        if (nativeSummary.Count < 25)
                            nativeSummary.Add(new { path = entry.FullName, bytes = entry.Length, format = fmt, architectures = archs });
                    }
                }

                bool hasWinArm64 = allEntries.Any(p => p.Contains("win-arm64", StringComparison.OrdinalIgnoreCase) || p.Contains("arm64", StringComparison.OrdinalIgnoreCase));
                bool hasWinX64 = allEntries.Any(p => p.Contains("win-x64", StringComparison.OrdinalIgnoreCase) || p.Contains("x64", StringComparison.OrdinalIgnoreCase) || p.Contains("win64", StringComparison.OrdinalIgnoreCase));
                bool hasOsxArm64 = allEntries.Any(p => p.Contains("osx-arm64", StringComparison.OrdinalIgnoreCase) || p.Contains("osx", StringComparison.OrdinalIgnoreCase));

                return new
                {
                    packageId,
                    version,
                    license,
                    nupkgBytes = new FileInfo(tmpZip).Length,
                    totalEntries = allEntries.Count,
                    nativeBinaryCount = archive.Entries.Count(e => Path.GetExtension(e.FullName).ToLowerInvariant() is ".dll" or ".dylib" or ".so"),
                    hasWinArm64Path = hasWinArm64,
                    hasWinX64Path = hasWinX64,
                    hasOsxPath = hasOsxArm64,
                    sampleBinaries = nativeSummary,
                    description = Truncate(description, 180)
                };
            }
            finally
            {
                if (File.Exists(tmpZip)) File.Delete(tmpZip);
            }
        }
        catch (Exception ex)
        {
            return new
            {
                packageId,
                version,
                error = Scrub(ex.Message)
            };
        }
    }

    private sealed record HostToolsReport(
        bool FreeCadInstalled,
        bool OcctInstalled,
        bool VcpkgInstalled,
        bool CmakeInstalled,
        bool MsvcArm64ToolchainInstalled,
        string? MsvcVersion,
        string[] MsvcTargetBins,
        string[] PythonCadModulesPresent);

    private static HostToolsReport ProbeHostTools()
    {
        bool hasFreeCad = CommandExists("freecad") || CommandExists("freecadcmd") ||
            Directory.Exists(@"C:\Program Files\FreeCAD") ||
            Directory.EnumerateDirectories(@"C:\Program Files", "FreeCAD*", SearchOption.TopDirectoryOnly).Any();
        bool hasOcct = CommandExists("DRAWEXE") ||
            Directory.EnumerateDirectories(@"C:\Program Files", "*OpenCASCADE*", SearchOption.TopDirectoryOnly).Any();
        bool hasVcpkg = CommandExists("vcpkg");
        bool hasCmake = CommandExists("cmake");

        string msvcRoot = @"C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC";
        string? msvcVer = null;
        var msvcBins = new List<string>();
        if (Directory.Exists(msvcRoot))
        {
            string? verDir = Directory.EnumerateDirectories(msvcRoot).OrderByDescending(d => d).FirstOrDefault();
            if (verDir is not null)
            {
                msvcVer = Path.GetFileName(verDir);
                string binDir = Path.Combine(verDir, "bin");
                if (Directory.Exists(binDir))
                {
                    foreach (string hostDir in Directory.EnumerateDirectories(binDir, "Host*"))
                    {
                        foreach (string targetDir in Directory.EnumerateDirectories(hostDir))
                        {
                            if (File.Exists(Path.Combine(targetDir, "cl.exe")))
                                msvcBins.Add($"{Path.GetFileName(hostDir)}/{Path.GetFileName(targetDir)}");
                        }
                    }
                }
            }
        }

        return new HostToolsReport(
            hasFreeCad,
            hasOcct,
            hasVcpkg,
            hasCmake,
            msvcBins.Contains("Hostarm64/arm64"),
            msvcVer,
            msvcBins.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            []);
    }

    private static bool CommandExists(string name)
    {
        string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        string[] exts = [".exe", ".cmd", ".bat", ""];
        foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (string ext in exts)
            {
                try
                {
                    if (File.Exists(Path.Combine(dir, name + ext))) return true;
                }
                catch { }
            }
        }
        return false;
    }

    private static byte[] ReadHeader(string path, int maxBytes)
    {
        using var fs = File.OpenRead(path);
        return ReadStreamHeader(fs, maxBytes);
    }

    private static byte[] ReadStreamHeader(Stream stream, int maxBytes)
    {
        var buf = new byte[maxBytes];
        int total = 0;
        while (total < maxBytes)
        {
            int n = stream.Read(buf, total, maxBytes - total);
            if (n <= 0) break;
            total += n;
        }
        if (total == maxBytes) return buf;
        return buf[..total];
    }

    private static (string Format, string[] Architectures) DetectBinaryArch(byte[] data)
    {
        if (data.Length < 4) return ("too-short", []);

        // PE (Windows DLL/EXE): "MZ"
        if (data[0] == (byte)'M' && data[1] == (byte)'Z' && data.Length >= 0x40)
        {
            int peOffset = BitConverter.ToInt32(data, 0x3C);
            if (peOffset >= 0 && peOffset + 6 <= data.Length &&
                data[peOffset] == (byte)'P' && data[peOffset + 1] == (byte)'E' && data[peOffset + 2] == 0 && data[peOffset + 3] == 0)
            {
                ushort machine = BitConverter.ToUInt16(data, peOffset + 4);
                string arch = machine switch
                {
                    0x8664 => "x64",
                    0x014c => "x86",
                    0xAA64 => "ARM64",
                    0xA641 => "ARM64EC",
                    _ => $"0x{machine:X4}"
                };
                return ("PE", [arch]);
            }
            return ("PE-unparsed", []);
        }

        // Mach-O Universal (Fat) binary: 0xCA 0xFE 0xBA 0xBE (big-endian)
        uint magicBE = (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
        if (magicBE == 0xCAFEBABE && data.Length >= 8)
        {
            int nfat = (data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7];
            var archs = new List<string>();
            for (int i = 0; i < nfat && 8 + (i + 1) * 20 <= data.Length; i++)
            {
                int off = 8 + i * 20;
                uint cputype = (uint)((data[off] << 24) | (data[off + 1] << 16) | (data[off + 2] << 8) | data[off + 3]);
                archs.Add(MachOCpuName(cputype));
            }
            return ("Mach-O-Universal", archs.Distinct().ToArray());
        }

        // Mach-O 64-bit little-endian: 0xCF 0xFA 0xED 0xFE
        uint magicLE = BitConverter.ToUInt32(data, 0);
        if (magicLE == 0xFEEDFACF && data.Length >= 8)
        {
            uint cputype = BitConverter.ToUInt32(data, 4);
            return ("Mach-O-64", [MachOCpuName(cputype)]);
        }

        // ELF: 0x7F 'E' 'L' 'F'
        if (data[0] == 0x7F && data[1] == (byte)'E' && data[2] == (byte)'L' && data[3] == (byte)'F' && data.Length >= 20)
        {
            ushort eMachine = BitConverter.ToUInt16(data, 18);
            string arch = eMachine switch
            {
                62 => "x64",
                183 => "ARM64",
                _ => $"elf-0x{eMachine:X4}"
            };
            return ("ELF", [arch]);
        }

        return ("unknown", []);
    }

    private static string MachOCpuName(uint cputype) => cputype switch
    {
        0x01000007 => "x64",
        0x0100000C => "ARM64",
        _ => $"macho-0x{cputype:X8}"
    };

    private static string ExtractNuspecLicense(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var lic = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "license");
            if (lic is not null) return $"{lic.Attribute("type")?.Value ?? "expr"}:{lic.Value.Trim()}";
            var licUrl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "licenseUrl");
            if (licUrl is not null) return $"url:{licUrl.Value.Trim()}";
        }
        catch { }
        return "unspecified";
    }

    private static string ExtractNuspecTag(string xml, string localName)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim() ?? "";
        }
        catch { return ""; }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

    private static string Scrub(string text)
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            text = text.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
            text = text.Replace(userProfile.Replace('\\', '/'), "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }
        string user = Environment.UserName;
        if (!string.IsNullOrEmpty(user) && user.Length >= 3)
            text = text.Replace(user, "<user>", StringComparison.OrdinalIgnoreCase);
        string host = Environment.MachineName;
        if (!string.IsNullOrEmpty(host) && host.Length >= 3)
            text = text.Replace(host, "<host>", StringComparison.OrdinalIgnoreCase);
        return text;
    }
}

SyntacticFisheye

Introduction:
    This extension adds a transform that compresses lines that do not contain numbers or text.

History:
		v1.1   Muneeb R. Baig 10/16/2024
        Update projects to .NET 4.8 and VS 17 with ARM64 support

        Updated multiple project files to target .NET Framework 4.8, including:
        - `CopyAsHtml.csproj`, `CopyAsHtmlTest.csproj`, `FixMixedTabs.csproj`, `FixMixedTabsUnitTests.csproj`, `GoToDef.csproj`, `MatchMargin.csproj`, `MiddleClickScroll.csproj`, `OptionsPage.csproj`, `PeekF1.csproj`, `PeekF1Tests.csproj`, `BlockTagger.csproj`, `TelemetryForPPT.csproj`, `TimeStampMargin.csproj`.

        Added `<TargetFrameworkProfile />` to `CopyAsHtmlTest.csproj` and `FixMixedTabsUnitTests.csproj`.

        Updated `ProPowerTools.Open.sln` to Visual Studio 17, removed `Debug|Any CPU.Build.0` configuration for several projects, and added `Debug|arm64` configuration.

        Updated `SyntacticFisheye.csproj`:
        - Changed `ToolsVersion` to "12.0".
        - Added `TargetFrameworkProfile`, `NuGetPackageImportStamp`, and `PropertyGroup` for `Debug|arm64`.
        - Added new references to various Microsoft and System libraries.
        - Removed references to `System.Xaml`, `WindowsBase`, `PresentationCore`, and some Microsoft Visual Studio libraries.
        - Added analyzers and import statements for `Microsoft.VisualStudio.SDK.Analyzers`.

        Updated `source.extension.vsixmanifest` to support `arm64` architecture for Visual Studio versions 17.0 to 18.0.

        Added a new `packages.config` file listing various NuGet packages.

        These changes collectively update the projects to use a more recent version of the .NET Framework and Visual Studio, providing improved performance, security, and access to newer features.


    v1.0    David Pugh 08/21/2012
        Initial release

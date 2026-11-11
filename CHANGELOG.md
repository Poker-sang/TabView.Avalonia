# Changelog

## 2.0.0-preview.1

- Target .NET 10 and .NET 11; enable Runtime Async V2 for .NET 11.
- Rename `TabsViewTheme` to `TabsViewFluentTheme` and add an independent `TabsViewFluent2Theme`.
- Add the separate `TabView.Avalonia.Material3` package, using Material3Expressive tokens and Chrome document-tab geometry.
- Switch all three demo themes while retaining pages, content, selection, and windows.
- Share compact header layout: reserve the complete close button above 75 DIP; show it only on the selected tab at or below that width.
- Correct connected circular corners, content seams, icon clipping, hover feedback, and adjacent separators.
- Fix Material 3 tear-off by keeping the drag thumb's pointer capture when the pointer leaves the tab.
- Update Avalonia to `12.2.999-cibuild0101188-alpha`, FluentIcons.Avalonia to 2.1.345, Microsoft.NET.Test.Sdk to 18.11.1, and xunit.runner.visualstudio to 4.0.1.
- Keep xUnit 3.2.2 for compatibility with Avalonia.Headless.XUnit; other direct dependencies already use their latest stable versions.

This prerelease requires the Avalonia nightly feed configured in `NuGet.Config`.
All Avalonia packages in a consuming application must use the same nightly version.

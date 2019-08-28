# Deployment

## Bin Updates

Deploy the following Dlls that were built in `release` mode.

- rocks.kfs.Reporting.SQLReportingServices.dll
- Microsoft.ReportViewer.Common.dll
- Microsoft.ReportViewer.DataVisualization.dll
- Microsoft.ReportViewer.Design.dll
- Microsoft.ReportViewer.ProcessingObjectModel.dll
- Microsoft.ReportViewer.WebDesign.dll
- Microsoft.ReportViewer.WebForms.dll
- Microsoft.ReportViewer.WinForms.dll

## Plugin

Deploy the contents of the `/Plugins/rocks_kfs/Reporting` folder

## Web.config

```
  <system.web>
    <compilation debug="true" targetFramework="4.5.2">
      <assemblies>
        <!-- KFS SSRS BEGIN -->
        <!-- All assemblies updated to version 15.0.0.0. -->
        <add assembly="Microsoft.ReportViewer.Common, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91"/>
        <add assembly="Microsoft.ReportViewer.WebForms, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91"/>
        <!-- KFS SSRS END -->
      </assemblies>
      <!-- KFS SSRS BEGIN -->
      <buildProviders>
        <!-- Version updated to 15.0.0.0. -->
        <add extension=".rdlc"
          type="Microsoft.Reporting.RdlBuildProvider, Microsoft.ReportViewer.WebForms, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91"/>
      </buildProviders>
      <!-- KFS SSRS END -->
    </compilation>
    <!-- KFS SSRS BEGIN -->
    <httpHandlers>
      <!-- Version updated to 15.0.0.0 -->
      <add path="Reserved.ReportViewerWebControl.axd" verb="*"
        type="Microsoft.Reporting.WebForms.HttpHandler, Microsoft.ReportViewer.WebForms, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91"
        validate="false"/>
    </httpHandlers>
    <!-- KFS SSRS END -->
  </system.web>
  <system.webServer>
    <!-- KFS SSRS BEGIN -->
    <validation validateIntegratedModeConfiguration="false"/>
    <!-- KFS SSRS END -->
    <handlers>
      <!-- KFS SSRS BEGIN -->
      <!-- Version updated to 15.0.0.0 -->
      <add name="ReportViewerWebControlHandler" verb="*" path="Reserved.ReportViewerWebControl.axd" preCondition="integratedMode" type="Microsoft.Reporting.WebForms.HttpHandler, Microsoft.ReportViewer.WebForms, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91"/>
      <!-- KFS SSRS END -->
    </handlers>
  </system.webServer>
```

# Setup

1. Make sure that all blocks are registered
2. Add the Reporting Services Configuration block to a settings page (I recommend System Settings) and provide the reporting services settings
3. Create a Report viewer page that has a Left Sidebar layout
   1. Add the Tree View to the left pane 
      1. configured in report mode

# Sample Report Layout

- RockRMS

  - Datasource

    - Rock Shared Datasource

  - Finance

  - Groups

  - Person

    ​
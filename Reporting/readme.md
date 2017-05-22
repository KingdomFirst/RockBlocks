# Deployment

## Bin Updates

Deploy the following Dlls that were built in `release` mode.

- com.kfs.Reporting.SQLReportingServices.dll
- Microsoft.ReportViewer.Common.dll
- Microsoft.ReportViewer.DataVisualization.dll
- Microsoft.ReportViewer.Design.dll
- Microsoft.ReportViewer.ProcessingObjectModel.dll
- Microsoft.ReportViewer.WebDesign.dll
- Microsoft.ReportViewer.WebForms.dll
- Microsoft.ReportViewer.WinForms.dll

## Plugin

Deploy the contents of the `/Plugins/com_kfs/Reporting` folder

## Web.config

1.  Add to `<System.Web>`\ `<compilation>`\ `<assembiles>`

   ```
   <add assembly="Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91" />
   ```

2.  Add to `<system.webServer>`\\`<handlers>`

   ```
   <add name="ReportViewerWebControlHandler" preCondition="integratedMode" verb="*" path="Reserved.ReportViewerWebControl.axd" type="Microsoft.Reporting.WebForms.HttpHandler, Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" />
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
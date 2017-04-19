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

Add the nodes that are between the `##Begin Update##` and `##End Update##` below

```xml
<configuration>
<system.web>
	<complication debug="false" targetFramework="4.5.2">
		<assemblies>
		<!-- ##Begin Add## -->
			<add assembly="Microsoft.ReportViewer.Common, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91" />
			<add assembly="Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91" />
		<!-- ##End Add## -->
		</assemblies>
	</complication>
	<!-- ##Begin Add## -->
	<httpHandlers>
	
	  <add path="Reserved.ReportViewerWebControl.axd" verb="*" type="Microsoft.Reporting.WebForms.HttpHandler, Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91" validate="false" />
	</httpHandlers>
	<!-- ##End Add## -->
</system.web>
<system.webServer>
	<handlers>
	<!-- ##Begin Add## -->
		<add name="ReportViewerWebControlHandler" verb="*" path="Reserved.ReportViewerWebControl.axd" preCondition="integratedMode" type="Microsoft.Reporting.WebForms.HttpHandler, Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845DCD8080CC91" />	
		<!-- ##End Add## -->
	</handlers>
</system.webServer>
</configuration>
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
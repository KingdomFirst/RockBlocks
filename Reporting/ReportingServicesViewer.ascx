<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesViewer.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.ReportingServicesViewer" %>
<%@ Register Assembly="Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" Namespace="Microsoft.Reporting.WebForms" TagPrefix="rsweb" %>
<style>
    .fill {
        height: 100%;
    }
</style>
<asp:UpdatePanel ID="upReportViewer" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:Panel ID="pnlReportViewer" runat="server" CssClass="panel panel-block fill">
            <div class="panel-head">
                <h1 class="panel-title"><i class="fa fa-file-text-o"></i>
                    <asp:Literal ID="lReportTitle" runat="server" /></h1>
            </div>
            <div class="panel-body fill">
                <rsweb:ReportViewer ID="rsViewer" runat="server" ProcessingMode="Remote" CssClass="fill"></rsweb:ReportViewer>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

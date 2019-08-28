<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesViewer.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Reporting.ReportingServicesViewer" %>
<%@ Register Assembly="Microsoft.ReportViewer.WebForms, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" Namespace="Microsoft.Reporting.WebForms" TagPrefix="rsweb" %>
<style>
    .WaitControlBackground {
        display: none !important;
    }
</style>
<asp:UpdatePanel ID="upReportViewer" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-file-text-o"></i>&nbsp;<asp:Literal ID="lReportTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbError" runat="server" Visible="false" NotificationBoxType="Danger" />
                <asp:Panel ID="pnlReportViewer" runat="server" Visible="false" Style="height: 100vh; border: 1px solid #dbdbdb;">
                    <rsweb:ReportViewer ID="rsViewer" runat="server" ProcessingMode="Remote" Height="100%" Width="100%"></rsweb:ReportViewer>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

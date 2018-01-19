<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesViewer.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.ReportingServicesViewer" %>
<%@ Register TagPrefix="rsweb" TagName="ReportViewer" Namespace="Microsoft.Reporting.WebForms" Assembly="Microsoft.ReportViewer.WebForms"  %>
<%--<%@ Register TagPrefix="rsweb" Namespace="Microsoft.Reporting.WebForms" Assembly="Microsoft.ReportViewer.WebForms, Version=14.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" %>--%>
<style>
    .fill {
        height: 800px;
        min-height: 600px;
        background-color: #ffffff;
        display: inline-block;
        border: 1px solid #000;
        padding: 2px;
    }
    .WaitControlBackground {
        display: none !important;
    }
</style>
<asp:UpdatePanel ID="upReportViewer" runat="server" UpdateMode="Conditional" >
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-file-text-o"></i>
                    <asp:Literal ID="lReportTitle" runat="server" /></h1>
            </div>
            <div class="panel-body ">
                <Rock:NotificationBox ID="nbError" runat="server" Visible="false" NotificationBoxType="Danger" />
                <asp:Panel ID="pnlReportViewer" runat="server" Visible="false">
                    <rsweb:ReportViewer ID="rsViewer" runat="server" ProcessingMode="Remote" CssClass="col-sm-12 fill kfs-ReportViewer"></rsweb:ReportViewer>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesPDFViewer.ascx.cs" Inherits="com.kfs.Reporting.SQLReportingServices.ReportingServicesPDFViewer" %>
<style>
    .fill {
        display: inline-block;
        border: 1px solid #000;
        padding: 2px;
    }

    iframe.fill {
        height: 800px;
        max-height: 800px;
        width: 100%;
    }
</style>
<asp:UpdatePanel ID="upPDFViewer" runat="server" UpdateMode="Conditional">

    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-file-pdf-o" aria-hidden="true"></i>
                    <asp:Literal ID="lReportTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbError" runat="server" Visible="false" />
                <asp:Panel ID="pnlPdfViewer" runat="server" Visible="false">
                    <asp:PlaceHolder ID="phViewer" runat="server"></asp:PlaceHolder>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

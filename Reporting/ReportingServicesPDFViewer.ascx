<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesPDFViewer.ascx.cs" Inherits="com.kfs.Reporting.SQLReportingServices.ReportingServicesPDFViewer" %>
<style>
    .fill {
        display: inline-block;
        border: 1px solid #000000;
        padding: 2px;
    }

    .fill iframe {
        height: 800px;
        max-height: 800px;
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
                <Rock:NotificationBox ID="nbError" runat="server" Visible="false" NotificationBoxType="Danger"  />
                <asp:Panel ID="pnlPdfViewer" runat="server" CssClass="col-sm-12 fill kfs-ReportViewer" Visible="false">
                    <asp:PlaceHolder ID="phViewer" runat="server"></asp:PlaceHolder>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

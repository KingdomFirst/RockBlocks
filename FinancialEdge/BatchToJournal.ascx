<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BatchToJournal.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.FinancialEdge.BatchToJournal" %>

<asp:UpdatePanel ID="upnlSync" runat="server">
    <ContentTemplate>
        <asp:Literal runat="server" ID="litDateExported" Visible="false"></asp:Literal>
        <Rock:BootstrapButton runat="server" Visible="false" ID="btnRemoveDate" Text="Remove Date Exported" OnClick="btnRemoveDateExported_Click" CssClass="btn btn-link" />
        <Rock:BootstrapButton runat="server" Visible="false" ID="btnExportToFinancialEdge" OnClick="btnExportToFinancialEdge_Click" CssClass="btn btn-primary" />
    </ContentTemplate>
</asp:UpdatePanel>
<iframe id="feDownload" src="/Plugins/com_kfs/FinancialEdge/FinancialEdgeCsvExport.aspx" frameborder="0" width="0" height="0"></iframe>
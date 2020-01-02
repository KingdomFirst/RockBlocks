<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BatchToJournal.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Intacct.BatchToJournal" %>

<asp:UpdatePanel ID="upnlSync" runat="server">
    <ContentTemplate>
        <asp:Literal runat="server" ID="litDateExported" Visible="false"></asp:Literal>
        <Rock:BootstrapButton runat="server" Visible="false" ID="btnRemoveDate" Text="Remove Date Exported" OnClick="btnRemoveDateExported_Click" CssClass="btn btn-link" />
        <Rock:BootstrapButton runat="server" Visible="false" ID="btnExportToIntacct" OnClick="btnExportToIntacct_Click" CssClass="btn btn-primary" />
        <asp:Literal ID="lDebug" runat="server" Visible="false" />
    </ContentTemplate>
</asp:UpdatePanel>

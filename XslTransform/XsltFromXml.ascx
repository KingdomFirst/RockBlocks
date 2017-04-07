<%@ Control Language="C#" AutoEventWireup="true" CodeFile="XsltFromXml.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Utility.XsltFromXml" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Literal ID="litXml" runat="server" Visible="true"></asp:Literal>
        <asp:Panel ID="pnlError" runat="server" CssClass="alert alert-warning" Visible="false"></asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

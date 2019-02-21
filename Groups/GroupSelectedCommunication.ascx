<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupSelectedCommunication.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Groups.GroupSelectedCommunication" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Button ID="btnEmailSelected" runat="server" CssClass="btn btn-primary" Text="Email Selected Members" OnClick="btnEmailGroup_Click" />
        <asp:Button ID="btnCommunicateSelected" runat="server" CssClass="btn btn-primary" Text="Communicate to Selected Members" OnClick="btnAlternateGroup_Click" Visible="false" />

    </ContentTemplate>
</asp:UpdatePanel>

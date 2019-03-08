<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupSelectedCommunication.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Groups.GroupSelectedCommunication" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:BootstrapButton ID="btnEmailSelected" runat="server" CssClass="btn btn-primary" Text="Email Selected Members" OnClick="btnEmailGroup_Click" />
        <Rock:BootstrapButton ID="btnCommunicateSelected" runat="server" CssClass="btn btn-primary" Text="Communicate to Selected Members" OnClick="btnAlternateGroup_Click" Visible="false" />
        <Rock:ModalAlert ID="mdAlert" runat="server" />

    </ContentTemplate>
</asp:UpdatePanel>

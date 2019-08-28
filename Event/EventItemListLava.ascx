<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventItemListLava.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Event.EventItemListLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Literal ID="lMessages" runat="server" />

        <asp:Literal ID="lContent" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>

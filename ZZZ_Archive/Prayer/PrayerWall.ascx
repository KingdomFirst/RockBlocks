<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PrayerWall.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Prayer.PrayerWall" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbError" runat="server" Visible="false" />
        <asp:Literal ID="lContent" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>

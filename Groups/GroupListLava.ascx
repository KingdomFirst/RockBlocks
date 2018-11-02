<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupListLava.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.RsvpGroups.RsvpGroupList" %>

<asp:UpdatePanel ID="upnlGroupList" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlLavaOutput" runat="server">
            <asp:Literal ID="lContent" runat="server"></asp:Literal>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

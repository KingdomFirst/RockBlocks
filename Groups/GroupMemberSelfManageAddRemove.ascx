<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberSelfManageAddRemove.ascx.cs" Inherits="RockWeb.Plugins.com_kingdomfirstsolutions.Groups.GroupMemberSelfManageAddRemove" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="alert alert-warning" id="divAlert" runat="server">
            <asp:Literal ID="lAlerts" runat="server" />
        </div>

        <asp:Literal ID="lContent" runat="server" />
        <asp:Literal ID="lDebug" runat="server" />

    </ContentTemplate>
</asp:UpdatePanel>
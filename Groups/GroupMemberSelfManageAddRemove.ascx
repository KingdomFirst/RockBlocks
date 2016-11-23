<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberSelfManageAddRemove.ascx.cs" Inherits="RockWeb.Plugins.com_kingdomfirstsolutions.Groups.GroupMemberSelfManageAddRemove" %>
<link rel="stylesheet" href ="/Plugins/com_kingdomfirstsolutions/Groups/css/SelfJoinGroups.css" />
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="alert alert-warning" id="divAlert" runat="server">
            <asp:Literal ID="lAlerts" runat="server" />
        </div>

        <asp:Literal ID="lContent" runat="server" />
        <asp:Literal ID="lDebug" runat="server" />

        <asp:Panel ID="pnlInputInfo" runat="server">

            <fieldset>
                <div>
                    <asp:PlaceHolder ID="phGroups" runat="server"></asp:PlaceHolder>
                </div>
            </fieldset>
            <div class="clearfix"></div>

            <Rock:NotificationBox ID="nbError" runat="server" Visible="false" NotificationBoxType="Danger"></Rock:NotificationBox>

            <div id="divActions" runat="server" class="actions">
                <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
            </div>

        </asp:Panel>

        <asp:Panel ID="pnlSuccess" runat="server" Visible="false">

            <Rock:NotificationBox ID="nbSuccess" runat="server" Title="Thank-you" NotificationBoxType="Success"></Rock:NotificationBox>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

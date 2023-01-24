<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ZoomSettings.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Zoom.ZoomSettings" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maUpdated" runat="server" />
        <asp:Panel ID="pnlWrapper" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-video mr-2"></i>Zoom Settings</h1>
                <asp:Label ID="lblLoginStatus" runat="server" CssClass="pull-right label label-danger" Text="Not Authenticated"></asp:Label>
            </div>
            <div class="panel-body">
                <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-danger" />
                <Rock:NotificationBox ID="nbNotification" runat="server" Title="Please correct the following:" NotificationBoxType="Danger" Visible="false" />
                <asp:Panel ID="pnlApiSettings" runat="server" Visible="false">
                    <p>The Zoom integration runs off Account ID, Client ID, and Client Secret values from a Server-to-Server OAuth application registered in the Zoom Marketplace. For instructions on how to set up a Server-to-Server OAuth application and access the needed values, visit <a href="https://marketplace.zoom.us/docs/guides/build/server-to-server-oauth-app/" target="_blank">this site</a>.</p>
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:RockTextBox ID="tbAppAccountId" runat="server" Label="Account ID" Required="true" RequiredErrorMessage="Account ID is Required" Help="The Zoom integration requires an Account ID from a Server-to-Server OAuth developer application registered in the Zoom Marketplace." />
                            <Rock:RockTextBox ID="tbAppClientId" runat="server" Label="Client ID" Required="true" RequiredErrorMessage="Client ID is Required" Help="The Zoom integration requires a Client ID from a Server-to-Server OAuth developer application registered in the Zoom Marketplace." />
                            <Rock:RockTextBox ID="tbAppClientSecret" runat="server" Label="Client Secret" Required="true" RequiredErrorMessage="Client Secret is Required" Help="The Zoom integration requires a Client Secret from a Server-to-Server OAuth developer application registered in the Zoom Marketplace." />
                            <div class="actions">
                                <asp:LinkButton ID="btnSave" runat="server" CssClass="btn btn-primary" OnClick="btnSave_Click">Save</asp:LinkButton>
                                <asp:LinkButton ID="btnCancel" runat="server" CssClass="btn btn-default" OnClick="btnCancel_Click">Cancel</asp:LinkButton>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
                <asp:Literal ID="lView" runat="server"></asp:Literal>
                <div class="actions">
                    <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn btn-primary" OnClick="btnEdit_Click">Edit Settings</asp:LinkButton>
                    <asp:LinkButton runat="server" ID="btnSyncNow" CssClass="btn btn-default" OnClick="btnSyncNow_Click" Enabled="false"><i class="fa fa-refresh"></i> Sync Zoom Rooms</asp:LinkButton>
                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

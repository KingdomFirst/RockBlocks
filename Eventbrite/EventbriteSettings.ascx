<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventbriteSettings.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Eventbrite.EventbriteSettings" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maUpdated" runat="server" />
        <asp:Panel ID="pnlWrapper" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-calendar-alt"></i>Eventbrite Settings</h1>
                <asp:Label ID="lblLoginStatus" runat="server" CssClass="pull-right label label-danger" Text="Not Authenticiated"></asp:Label>
            </div>
            <div class="panel-body">
                <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-danger" />
                <Rock:NotificationBox ID="nbNotification" runat="server" Title="Please correct the following:" NotificationBoxType="Danger" Visible="false" />
                <asp:Panel ID="pnlToken" runat="server" Visible="false">
                    <p>Eventbrite runs off of your personal oAuth Key from <a href="https://www.eventbrite.com/platform/api-keys" target="_blank">here</a>. If you do not have one please create one now.</p>
                    <div class="row">
                        <div class="col-md-12">
                            <div class="row">
                                <div class="col-md-12">
                                    <Rock:RockTextBox ID="tbOAuthToken" runat="server" Label="Private Token" Required="true" RequiredErrorMessage="An Eventbrite OAuth Key is Required" Help="The Eventbrite OAuth token is generated from this page https://www.eventbrite.com/platform/api-keys." />
                                    <Rock:RockDropDownList ID="ddlOrganization" runat="server" Label="Organization" Required="true" RequiredErrorMessage="You must select an organization to complete the Eventbrite setup." />
                                    <div class="actions">
                                        <asp:LinkButton ID="btnSave" runat="server" CssClass="btn btn-primary" OnClick="btnSave_Click">Save</asp:LinkButton>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

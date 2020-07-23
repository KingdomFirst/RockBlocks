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
                <asp:Literal ID="lView" runat="server"></asp:Literal>
                <div class="actions">
                    <asp:LinkButton ID="btnEdit" runat="server" CssClass="btn btn-primary" OnClick="btnEdit_Click">Edit</asp:LinkButton>
                </div>
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlGridWrapper" runat="server" CssClass="panel panel-block">

            <div class="panel-heading clearfix">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-users"></i>&nbsp;<asp:Literal ID="lHeading" runat="server" Text="Linked Groups" />
                </h1>
            </div>

            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:NotificationBox ID="nbLinkedGroups" runat="server" NotificationBoxType="Danger" Dismissable="true" Visible="false" />
                    <Rock:Grid ID="gEBLinkedGroups" runat="server" AllowSorting="true" DisplayType="Full" RowItemText="Group" DataKeyNames="RockGroupId" OnRowDataBound="gEBLinkedGroups_RowDataBound">
                        <Columns>
                            <asp:HyperLinkField DataNavigateUrlFields="PersonId" DataTextField="Person" Visible="false" HeaderText="Rock Group Name" SortExpression="RockGroupName" />
                            <Rock:RockBoundField DataField="RockGroupName" HeaderText="Rock Group Name" SortExpression="RockGroupName" />
                            <Rock:RockBoundField DataField="EventbriteEventName" HeaderText="Eventbrite Event Name" SortExpression="EventbriteEventName" />
                            <Rock:DateField DataField="LastSynced" DataFormatString="{0:d} {0:t}" HeaderText="Last Synced" SortExpression="LastSynced" />
                            <Rock:LinkButtonField ID="btnSyncNow" CssClass="btn btn-default btn-sm fa fa-refresh" HeaderText="Sync" OnClick="lbSyncNow_Click"></Rock:LinkButtonField>
                            <Rock:LinkButtonField ID="btnEditRow" CssClass="btn btn-default btn-sm fa fa-edit" HeaderText="Edit" OnClick="lbEditRow_Click"></Rock:LinkButtonField>
                            <Rock:DeleteField OnClick="lbDelete_Click"></Rock:DeleteField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlCreateGroupFromEventbrite" runat="server" CssClass="panel panel-block">
            <div class="panel-heading clearfix">
                <h1 class="panel-title pull-left">Create new Rock Group from existing Eventbrite Event</h1>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-12">
                        <Rock:RockDropDownList runat="server" ID="ddlEventbriteEvents"></Rock:RockDropDownList>
                        <Rock:NotificationBox ID="nbLinkNew" runat="server" NotificationBoxType="Danger" Dismissable="true" Visible="false" />
                    </div>
                </div>
            </div>
            <div class="panel-footer">
                <asp:LinkButton CssClass="btn btn-primary btn-sm" runat="server" ID="lbCreateNewRockGroup" OnClick="lbCreateNewRockGroup_Click" Text="Create New Rock Group"></asp:LinkButton>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

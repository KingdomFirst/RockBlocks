<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupPanel.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Event.KFSGroupPanel" %>

<asp:UpdatePanel ID="upnlSubGroup" runat="server">
    <ContentTemplate>
        <Rock:HiddenFieldWithClass ID="hfGroupId" runat="server" CssClass="panel-widget-groupid" />
        <Rock:PanelWidget ID="pnlSubGroup" runat="server">
            <asp:Panel ID="pnlGroupDescription" runat="server" CssClass="alert alert-info" >
                <asp:Label ID="lblGroupDescription" runat="server"></asp:Label>
            </asp:Panel>
            <Rock:Grid ID="gGroupMembers" runat="server" DisplayType="Full" AllowSorting="true" CssClass="js-grid-group-members" >
                <Columns>
                    <Rock:SelectField></Rock:SelectField>
                    <Rock:RockBoundField DataField="Person.FullName" HeaderText="Name" SortExpression="Person.LastName,Person.NickName" HtmlEncode="false" />
                    <Rock:RockBoundField DataField="GroupRole" HeaderText="Role" SortExpression="GroupRole.Name" />
                    <Rock:RockBoundField DataField="GroupMemberStatus" HeaderText="Status" SortExpression="GroupMemberStatus" />
                </Columns>
            </Rock:Grid><br />
            <div class="actions">
                <asp:LinkButton ID="lbGroupEdit" runat="server" AccessKey="m" Text="Edit" CommandName="EditSubGroup" CssClass="btn btn-primary" />
                <asp:LinkButton ID="lbGroupDelete" runat="server" Text="Delete" CommandName="DeleteSubGroup" CssClass="btn btn-link js-delete-subGroup" CausesValidation="false" />
            </div>
        </Rock:PanelWidget>
    </ContentTemplate>
</asp:UpdatePanel>

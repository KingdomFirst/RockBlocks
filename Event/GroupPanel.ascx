<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupPanel.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Event.GroupPanel" %>

<asp:UpdatePanel ID="upnlSubGroup" runat="server">
    <ContentTemplate>

        <Rock:PanelWidget ID="pnlSubGroup" runat="server">
            
            <asp:Panel ID="pnlGroupDescription" runat="server" CssClass="alert alert-info" >
                <asp:Label ID="lblGroupDescription" runat="server"></asp:Label>
            </asp:Panel>
            <%--<Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                <Rock:RockCheckBoxList ID="cblRole" runat="server" Label="Role" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                <Rock:RockCheckBoxList ID="cblGroupMemberStatus" runat="server" Label="Group Member Status" RepeatDirection="Horizontal" />
                <Rock:CampusPicker ID="cpCampusFilter" runat="server" Label="Family Campus" />
                <Rock:RockCheckBoxList ID="cblGenderFilter" runat="server" RepeatDirection="Horizontal" Label="Gender">
                    <asp:ListItem Text="Male" Value="Male" />
                    <asp:ListItem Text="Female" Value="Female" />
                    <asp:ListItem Text="Unknown" Value="Unknown" />
                </Rock:RockCheckBoxList>
                <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
            </Rock:GridFilter>--%>
            <Rock:Grid ID="gGroupMembers" runat="server" DisplayType="Full" AllowSorting="true" PersonIdField="PersonId" OnRowSelected="gGroupMembers_RowSelected" CssClass="js-grid-group-members" PagerSettings-Visible="false" FooterStyle-HorizontalAlign="Center" >
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

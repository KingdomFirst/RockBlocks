<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupPanel.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Event.GroupPanel" %>

<asp:UpdatePanel ID="upnlSubGroup" runat="server">
    <ContentTemplate>

        <Rock:PanelWidget ID="pnlSubGroup" runat="server">
            
            <div class="panel-labels">
                <asp:HyperLink ID="hlSyncSource" runat="server"><Rock:HighlightLabel ID="hlSyncStatus" runat="server" LabelType="Info" Visible="false" Text="<i class='fa fa-exchange'></i>" /></asp:HyperLink> &nbsp;
            </div>
            
            <Rock:Grid ID="pnlGroupMembers" runat="server" DisplayType="Full" AllowSorting="true" CssClass="js-grid-group-members" PagerSettings-Visible="false" FooterStyle-HorizontalAlign="Center" >
                <Columns>
                    <Rock:SelectField></Rock:SelectField>
                    <Rock:RockBoundField DataField="Person.FullName" HeaderText="Name" SortExpression="Person.LastName,Person.NickName" HtmlEncode="false" />
                    <Rock:RockBoundField DataField="Person.NickName" HeaderText="First Name" ExcelExportBehavior="AlwaysInclude" Visible="false" />
                    <Rock:RockBoundField DataField="Person.LastName" HeaderText="Last Name" ExcelExportBehavior="AlwaysInclude" Visible="false" />
                    <Rock:RockBoundField DataField="Person.Gender" HeaderText="Gender" ExcelExportBehavior="AlwaysInclude" />
                    <Rock:RockBoundField DataField="GroupRole" HeaderText="Role" SortExpression="GroupRole.Name" />
                    <Rock:RockBoundField DataField="Note" HeaderText="Notes" SortExpression="Note" />
                </Columns>
            </Rock:Grid><br />
            <div class="actions">
                <asp:LinkButton ID="lbGroupEdit" runat="server" AccessKey="m" Text="Edit" CommandName="EditSubGroup" CssClass="btn btn-primary" />
                <asp:LinkButton ID="lbGroupDelete" runat="server" Text="Delete" CommandName="DeleteSubGroup" CssClass="btn btn-link js-delete-subGroup" CausesValidation="false" />
            </div>
            
        </Rock:PanelWidget>
    </ContentTemplate>
</asp:UpdatePanel>

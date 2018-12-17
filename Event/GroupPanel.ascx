<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupPanel.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Event.GroupPanel" %>

<script>
    Sys.Application.add_load( function () {
        // person-link-popover
        $('.js-person-popover').popover({
            placement: 'right', 
            trigger: 'manual',
            delay: 500,
            html: true,
            content: function() {
                var dataUrl = Rock.settings.get('baseUrl') + 'api/People/PopupHtml/' +  $(this).attr('personid') + '/false';

                var result = $.ajax({ 
                                    type: 'GET', 
                                    url: dataUrl, 
                                    dataType: 'json', 
                                    contentType: 'application/json; charset=utf-8',
                                    async: false }).responseText;
            
                var resultObject = jQuery.parseJSON(result);

                return resultObject.PickerItemDetailsHtml;

            }
        }).on('mouseenter', function () {
            var _this = this;
            $(this).popover('show');
            $(this).siblings('.popover').on('mouseleave', function () {
                $(_this).popover('hide');
            });
        }).on('mouseleave', function () {
            var _this = this;
            setTimeout(function () {
                if (!$('.popover:hover').length) {
                    $(_this).popover('hide')
                }
            }, 100);
        });
    });
</script>

<asp:UpdatePanel ID="upnlSubGroup" runat="server">
    <ContentTemplate>

        <Rock:PanelWidget ID="pnlSubGroup" runat="server">
            
            <div class="panel-labels">
                <asp:HyperLink ID="hlSyncSource" runat="server"><Rock:HighlightLabel ID="hlSyncStatus" runat="server" LabelType="Info" Visible="false" Text="<i class='fa fa-exchange'></i>" /></asp:HyperLink> &nbsp;
            </div>

            <asp:Panel ID="pnlGroupDescription" runat="server" CssClass="alert alert-info" >
                <asp:Label ID="lblGroupDescription" runat="server"></asp:Label>
            </asp:Panel>
            
            <Rock:Grid ID="pnlGroupMembers" runat="server" DisplayType="Full" AllowSorting="true" OnRowSelected="pnlGroupMembers_RowSelected" CssClass="js-grid-group-members" PagerSettings-Visible="false" FooterStyle-HorizontalAlign="Center" >
                <Columns>
                    <Rock:SelectField></Rock:SelectField>
                    <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Person.LastName,Person.NickName" HtmlEncode="false" />
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

<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesFolderTree.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.ReportingServicesFolderTree" %>
<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="hfSelectedItem" runat="server" />
        <asp:HiddenField ID="hfSelectionType" runat="server" />
        <asp:HiddenField ID="hfInitialLoad" runat="server" />
        <Rock:NotificationBox ID="nbRockError" runat="server" NotificationBoxType="Danger" Visible="false" />
        <asp:Panel ID="pnlFolders" runat="server">
            <asp:Panel ID="pnlHeader" runat="server" CssClass="panel-heading">
                <h1 class="panel-title"><i class="fa fa-folder-open-o"></i>
                    <asp:Literal ID="lTitle" runat="server" /></h1>
            </asp:Panel>
            <asp:Panel ID="pnlTree" runat="server">
                <div id="folders" style="display: none;">
                    <asp:Literal ID="lFolders" runat="server" ViewStateMode="Disabled" />
                </div>
            </asp:Panel>
        </asp:Panel>

        <script type="text/javascript">
            Sys.Application.add_load(function () {
                var selectedFolder = $('#<%= hfSelectedItem.ClientID%>').val();

                $("#folders")
                    .on("rockTree:selected", function (e, id) {
                        var $li = $(this).find('[data-id="' + id + '"]'),
                            rockTree = $(this).data('rockTree'),
                            modelType,
                            action,
                            i;
                        var expandedDataIds = $(e.currentTarget).find('.rocktree-children').filter(":visible").closest('.rocktree-item').map(function () {
                            return $(this).attr('data-id')
                        }).get().join(',');
                        var selectionMode = $('#<%=hfSelectionType.ClientID %>').val().toLowerCase();

                        var validSelection = false;
                        var selectedItemType = id.substring(0, 1);
                        if (selectionMode == "folder") {
                            if (selectedItemType == "f") {
                                validSelection = true;
                            }
                        }
                        else if (selectionMode == "report") {
                            if (selectedItemType == "r") {
                                validSelection = true;
                            }
                        }

                        if (validSelection === true) {
                            $('#<%= hfSelectedItem.ClientID%>').val(id);

                            var action = $li.find('a').first().attr('href');

                            if (action != null) {
                                window.location = action;
                            }

                        }
                        else {
                            var index = -1;
                            for (var i = 0; i < selected.length; i++) {
                                if (selected[i].id == id) {
                                    index = i;
                                    break;
                                }
                            }
                            if (index >= 0) {
                                rockTree.clear();
                            }
                        }




                    })
            .rockTree({
                mapping: {
                    include: ["model"]
                },

<%--                selectedIds: [$('#<%= hfSelectedItem.ClientID%>').val()],--%>
                multSelect: false
            });

                $("#folders").show();
            });


        </script>
    </ContentTemplate>
</asp:UpdatePanel>


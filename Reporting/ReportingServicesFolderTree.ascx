<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesFolderTree.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.ReportingServicesFolderTree" %>
<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="hfExpandedFolders" runat="server" />
        <asp:Panel ID="pnFolders" CssClass="panel panel-block" runat="server">
            <div id="folders" style="display: none;">
                <asp:Literal ID="lFolders" runat="server" ViewStateMode="Disabled" />
            </div>
        </asp:Panel>

        <script type="text/javascript">
            Sys.Application.add_load(function () {
                $("#folders")
                    .on("rockTree:selected", function (e, id) {
                        debugger;
                        var $li = $(this).find('[data-id="' + id + '"]'),
                            rockTree = $(this).data('rockTree'),
                            modelType,
                            action,
                            i;
                        var expandedDataIds = $(e.currentTarget).find('.rocktree-children').filter(":visible").closest('.rocktree-item').map(function () {
                            return $(this).attr('data-id')
                        }).get().join(',');

                        $('<%= hfExpandedFolders.ClientID %>').val(expandedDataIds);

                if ($li.length > 1) {
                    for (i = 0; i < $li.length; i++) {
                        if (!rockTree.selectedNodes[0].name === $li.find('span').text()) {
                            $li = $li[i];
                            break;
                        }
                    }
                }

            })
            .rockTree({
                mapping: {
                    include: ["model"]
                }
            });

        $("#folders").show();
    });


        </script>
    </ContentTemplate>
</asp:UpdatePanel>


<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ReportingServicesFolderTree.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Reporting.ReportingServicesFolderTree" %>
<asp:UpdatePanel ID="upFolderTree" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="hfRootPath" runat="server" />
        <asp:HiddenField ID="hfShowHidden" runat="server" />
        <asp:HiddenField ID="hfRecursive" runat="server" />
        <asp:Label ID="lPath" runat="server" />
        <div class="treeview-scroll scroll-container scroll-container-horizontal">
            <div class="viewport">
                <div class="overview">
                    <div class="panel-body treeview-frame">
                        <asp:Panel ID="pnlTreeviewContent" runat="server" />
                    </div>
                </div>
            </div>
            <div class="scrollbar">
                <div class="track">
                    <div class="thumb">
                        <div class="end"></div>
                    </div>
                </div>
            </div>
        </div>

        <script type="text/javascript">
            $(function () {
                $('#<%=pnlTreeviewContent.ClientID%>')
                    .rockTree({
                        id: '',
                        restUrl: '/api/com.kfs/ReportingServices/GetFolderList/',
                        restParams: '?rootPath=' + $('#<%=hfRootPath.ClientID%>').val()
                            + '&getChildren=' + $('#<%=hfRecursive.ClientID%>').val()
                            + '&includeHidden=' + $('#<%=hfShowHidden.ClientID%>').val(),
                    multiSelect: false
                    });
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>

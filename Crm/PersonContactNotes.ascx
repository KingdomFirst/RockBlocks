<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonContactNotes.ascx.cs" Inherits="RockWeb.Plugins.kfs_rocks.Crm.Notes" %>

<asp:UpdatePanel ID="upPersonContactNotes" runat="server">
    <ContentTemplate>
        <div class="row row-eq-height">
            <div class="col-md-4">
                <asp:Panel ID="pnlSelectPerson" CssClass="panel panel-block" runat="server">
                    <div class="panel-heading">
                        <h1 class="panel-title"><i class="fa fa-user"></i>Select a Person</h1>
                    </div>
                    <div class="panel-body">
                        <Rock:PersonPicker ID="ppPerson" runat="server" OnSelectPerson="SelectPerson" />
                        <Rock:GroupMemberPicker ID="gmpGroupMember" runat="server" OnSelectedIndexChanged="SelectPerson"></Rock:GroupMemberPicker>
                        <asp:Literal ID="lLavaPersonInfo" runat="server" />
                    </div>
                </asp:Panel>
            </div>
            <div class="col-md-8">
                <Rock:NoteContainer ID="notesTimeline" runat="server" />
            </div>
        </div>
        <script>
            $(document).ready(function () {
                $('.note-new-kfs textarea').on("input", function () {
                    $(this).css("height", ""); //reset the height
                    $(this).css("height", Math.min($(this).prop('scrollHeight'), 400) + "px");
                });
                $('.note-new-kfs textarea').css("height", Math.min($('.note-new-kfs textarea').prop('scrollHeight'), 400) + "px");
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>

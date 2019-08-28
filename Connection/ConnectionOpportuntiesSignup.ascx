<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ConnectionOpportuntiesSignup.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Connection.ConnectionOpportuntiesSignup" %>

<style>
    input.opportunities_disabled + label {
        color: Gray;
    }
</style>

<script>
    var requestManager = Sys.WebForms.PageRequestManager.getInstance();
    requestManager.add_endRequest(function (sender, args) {
        BindEventsConnectionOpportunities();
    });
    $(function () {
        BindEventsConnectionOpportunities();
    });
    function BindEventsConnectionOpportunities() {
        $('input.opportunity').each(function (index) {
            $(this).click({ cb: this }, opportunity_updateStates);
        });
    }
    function opportunity_updateStates(event) {
        var box = document.getElementById(event.data.cb.id);
        var activeCount = 0;
        if (!box.checked) {
            $(box).addClass('removed');
        }
        else {
            $(box).removeClass('removed');
        }

        $("input:checkbox[class=opportunity]:checked").each(function () {
            activeCount += 1;
        });

        if (activeCount >= MaxConnections && MaxConnections != 0)
            $('input[type=checkbox].opportunity:not(:checked)').attr('disabled', 'disabled').addClass('opportunities_disabled');
        else
            $('input[type=checkbox].opportunity:not(:checked)').removeAttr('disabled').removeClass('opportunities_disabled');
        var parent = box.parentNode;
        var grandParent = box.parentNode.parentNode;

        if (box.checked) {
            $(grandParent).children('div').eq(0).slideDown('slow');
            $(grandParent).children('div').eq(0).children('table').children('tbody').children('tr').children('td').children('[id*=validator_cf_]').each(function (index) {
                ValidatorEnable(this, true);
            });
        }
        else {
            $(grandParent).children('div').eq(0).slideUp('slow');
            $(grandParent).children('div').eq(0).children('table').children('tbody').children('tr').children('td').children('[id*=validator_cf_]').each(function (index) {
                ValidatorEnable(this, false);
            });
        }

    }
</script>

<asp:UpdatePanel ID="upnlOpportunityDetail" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbErrorMessage" runat="server" NotificationBoxType="Danger" Visible="false" />

        <asp:Literal ID="lResponseMessage" runat="server" Visible="false" />
        <asp:Literal ID="lDebug" Visible="false" runat="server"></asp:Literal>

        <asp:Panel ID="pnlSignup" runat="server" CssClass="panel panel-block">

            <div class="panel-body">

                <asp:Panel ID="pnlPersonInfo" Visible="false" runat="server" CssClass="person-info">

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="true" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" Required="true" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:PhoneNumberBox ID="pnHome" runat="server" Label="Home Phone" />
                        </div>
                        <div class="col-md-6">
                            <Rock:PhoneNumberBox ID="pnMobile" runat="server" Label="Mobile Phone" />
                        </div>
                    </div>
                </asp:Panel>

                <div>
                    <asp:PlaceHolder ID="phConnections" runat="server"></asp:PlaceHolder>
                </div>

                <Rock:RockTextBox Visible="false" ID="tbComments" runat="server" Label="Comments" TextMode="MultiLine" Rows="4" />

                <div class="actions">
                    <asp:LinkButton ID="btnConnect" runat="server" AccessKey="m" Text="Connect" CssClass="btn btn-primary" OnClick="btnConnect_Click" />
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

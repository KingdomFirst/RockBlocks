<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PagerEntry.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.CheckIn.PagerEntry" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <script>
        Sys.Application.add_load(function () {

            $(document).ready(function() {
                // set focus to the input unless on a touch device
                var isTouchDevice = 'ontouchstart' in document.documentElement;
                if (!isTouchDevice) {

                    if ($('.modal-open').length == 0) {
                        $('.pager-input').focus();
                    }

                }

                $('.tenkey a.digit').on('click', function () {
                    $phoneNumber = $("input[id$='tbPagerNumber']");
                    $phoneNumber.val($phoneNumber.val() + $(this).html());
                });
                $('.tenkey a.back').on('click', function () {
                    $phoneNumber = $("input[id$='tbPagerNumber']");
                    $phoneNumber.val($phoneNumber.val().slice(0,-1));
                });
                $('.tenkey a.clear').on('click', function () {
                    $phoneNumber = $("input[id$='tbPagerNumber']");
                    $phoneNumber.val('');
                });
            });

        });
        </script>

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <div class="checkin-header">
            <h1>
                <asp:Literal ID="lTitle" runat="server" /></h1>
        </div>

        <div class="checkin-body">
            <div class="checkin-scroll-panel">
            <div class="scroller">

                <div class="checkin-search-body pager-entry">

                    <asp:Panel ID="pnlPagerNumber" runat="server" CssClass="clearfix">
                        <Rock:RockTextBox ID="tbPagerNumber" CssClass="pager-input input-lg" runat="server" Label="Pager Number" autocomplete="off" />

                        <asp:Panel ID="pnlKeypad" runat="server" CssClass="tenkey checkin-phone-keypad">
                            <div>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">1</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">2</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">3</a>
                            </div>
                            <div>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">4</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">5</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">6</a>
                            </div>
                            <div>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">7</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">8</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">9</a>
                            </div>
                            <div>
                                <a href="#" class="btn btn-default btn-lg btn-keypad command clear">Clear</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad digit">0</a>
                                <a href="#" class="btn btn-default btn-lg btn-keypad command back"><i class="fas fa-backspace"></i></a>
                            </div>
                        </asp:Panel>
                    </asp:Panel>
                </div>

            </div>
        </div>
        </div>



    <div class="checkin-footer">
        <div class="checkin-actions">
            <asp:LinkButton CssClass="btn btn-primary" ID="lbSubmit" runat="server" OnClick="lbSubmit_Click" />
            <asp:LinkButton CssClass="btn btn-default btn-back" ID="lbBack" runat="server" OnClick="lbBack_Click" Text="Back" />
            <asp:LinkButton CssClass="btn btn-default btn-cancel" ID="lbCancel" runat="server" OnClick="lbCancel_Click" Text="Cancel" />
        </div>
    </div>

    </ContentTemplate>
</asp:UpdatePanel>

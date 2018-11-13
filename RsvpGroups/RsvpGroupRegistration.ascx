<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RsvpGroupRegistration.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.RsvpGroups.RsvpGroupRegistration" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbNotice" runat="server" Visible="false" NotificationBoxType="Danger" />
        <Rock:NotificationBox ID="nbWarning" runat="server" Visible="false" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlView" runat="server">

            <asp:Literal ID="lLavaOverview" runat="server" />
            <asp:Literal ID="lLavaOutputDebug" runat="server" />

            <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />

            <div class="row">
                <asp:Panel ID="pnlCol1" runat="server" CssClass="col-md-12">
                    <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true"></Rock:RockTextBox>
                    <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="true"></Rock:RockTextBox>
                    <asp:Panel ID="pnlHomePhone" runat="server" CssClass="row">
                        <div class="col-sm-7">
                            <Rock:PhoneNumberBox ID="pnHome" runat="server" Label="Home Phone" />
                        </div>
                        <div class="col-sm-5">
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlCellPhone" runat="server" CssClass="row">
                        <div class="col-sm-7">
                            <Rock:PhoneNumberBox ID="pnCell" runat="server" Label="Cell Phone" />
                        </div>
                        <div class="col-sm-5">
                            <Rock:RockCheckBox ID="cbSms" runat="server" Label="&nbsp;" Text="Enable SMS" />
                        </div>
                    </asp:Panel>
                    <Rock:EmailBox ID="tbEmail" runat="server" Label="Email"></Rock:EmailBox>
                    <Rock:AddressControl ID="acAddress" runat="server" />
                </asp:Panel>
            </div>
            <div class="text-center">
                <Rock:NotificationBox ID="nbCapacity" runat="server" Visible="false" NotificationBoxType="Warning" />
                <Rock:NumberUpDown ID="numHowMany" runat="server" CssClass="input-lg" OnNumberUpdated="numHowMany_NumberUpdated" />
            </div>


            <div class="actions">
                <asp:LinkButton ID="btnRegister" runat="server" CssClass="btn btn-primary" OnClick="btnRegister_Click" />
            </div>

        </asp:Panel>

        <asp:Panel ID="pnlResult" runat="server" Visible="false">
            <asp:Literal ID="lResult" runat="server" />
            <asp:Literal ID="lResultDebug" runat="server" />
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

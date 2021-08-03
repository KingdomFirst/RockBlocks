<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareEntry.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareEntry" %>

<asp:UpdatePanel runat="server" ID="upnlCareEntry">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:HiddenField ID="hfCareNeedId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fa fa-hand-holding"></i>Care Need</h1>

                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlStatus" runat="server" LabelType="Default" Text="Pending" />
                </div>
            </div>
            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <asp:ValidationSummary ID="valValidation" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />

                <div class="">
                    <div class="row">
                        <div class="col-md-3">
                            <Rock:DatePicker ID="dpDate" runat="server" Label="Date" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="DateEntered" />
                        </div>
                        <div class="col-md-3">
                            <Rock:PersonPicker ID="ppSubmitter" runat="server" Label="Submitter" Visible="false" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="SubmitterPersonAlias" />
                        </div>
                        <div class="col-md-3">
                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />
                        </div>
                        <div class="col-md-3">
                            <Rock:DefinedValuePicker ID="dvpStatus" runat="server" Label="Status" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="StatusValueId" Required="true" />
                        </div>
                    </div>
                </div>

                <Rock:PanelWidget ID="wpRequestor" runat="server" Title="Requestor" Expanded="true" CssClass="margin-t-md">
                    <div class="row">
                        <div class="col-md-8">
                            <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" OnSelectPerson="ppPerson_SelectPerson" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="PersonAlias" />
                        </div>
                    </div>

                    <asp:Panel runat="server" ID="pnlNewPersonFields">
                        <div class="row">
                            <div class="col-md-4">
                                <Rock:RockTextBox ID="dtbFirstName" runat="server" Label="First Name" />
                            </div>
                            <div class="col-md-4">
                                <Rock:RockTextBox ID="dtbLastName" runat="server" Label="Last Name" />
                            </div>
                            <div class="col-md-4">
                                <Rock:EmailBox ID="ebEmail" runat="server" Label="Email" />
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4">
                                <Rock:PhoneNumberBox ID="pnbHomePhone" runat="server" Label="Home Phone" />
                            </div>
                            <div class="col-md-4">
                                <Rock:PhoneNumberBox ID="pnbCellPhone" runat="server" Label="Cell Phone" />
                            </div>
                            <div class="col-md-4">
                                <Rock:LocationAddressPicker ID="lapAddress" runat="server" Label="Address" />
                            </div>
                        </div>
                    </asp:Panel>
                </Rock:PanelWidget>

                <Rock:PanelWidget ID="pwDetails" runat="server" Title="Need Details" Expanded="true">
                    <Rock:DefinedValuePicker ID="dvpCategory" runat="server" Label="Category" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="CategoryValueId" Required="true" />

                    <Rock:DataTextBox ID="dtbDetailsText" runat="server" Label="Description of Need" TextMode="MultiLine" Rows="4" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="Details" />

                    <Rock:DynamicPlaceholder ID="phAttributes" runat="server" />
                </Rock:PanelWidget>

                <asp:LinkButton ID="lbSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                <asp:LinkButton ID="lbCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
            </div>

        </asp:Panel>

        <Rock:ConfirmPageUnload ID="confirmExit" runat="server" ConfirmationMessage="Changes have been made to this care need that have not yet been saved." Enabled="false" />

    </ContentTemplate>
</asp:UpdatePanel>

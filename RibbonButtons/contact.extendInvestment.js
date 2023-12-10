var Contact = window.Contact || {};
(function () {
  this.ExtendInvestmentButton = (formContext) => {
    const investmentPeriodAttribute = formContext.getAttribute("ss_investmentperiodmonths");

    const investmentPeriodvalue = investmentPeriodAttribute.getValue();
    const newInvestmentPeriod = parseInt(investmentPeriodvalue, 10);
    investmentPeriodAttribute.setValue(newInvestmentPeriod + 6);
    Xrm.Navigation.openAlertDialog({
      text: "The Investment Period has been extended",
    });
    formContext.data.refresh(true);
  };
}).call(Contact);

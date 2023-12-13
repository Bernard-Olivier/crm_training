var Contact = window.Contact || {};
(function () {
  const logicalNames = {
    STATUS: "statecode",
    STATUS_REASON: "statuscode",
    JOIN_DATE: "ss_joiningdate",
    MATURED_DATE: "ss_maturitydate",
  };

  const statusCodes = {
    ACTIVE: 0,
    INACTIVE: 1,
  };

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

  this.enabledRule = (formContext) => {
    const maturityDateAttribute = formContext.getAttribute(logicalNames.MATURED_DATE);
    const statusCodeAttribute = formContext.getAttribute(logicalNames.STATUS);
    const statusCodeValue = statusCodeAttribute.getValue();
    const setVisible = maturityDateAttribute.getValue() && statusCodeValue === statusCodes.ACTIVE;
    return setVisible;
  };
}).call(Contact);
